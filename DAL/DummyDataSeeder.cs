using SynapseEnqueteManagement.BL.Domain.I18n.Entities;
using SynapseEnqueteManagement.BL.Domain.Platform.Entities;
using SynapseEnqueteManagement.BL.Domain.Project.Entities;
using SynapseEnqueteManagement.BL.Domain.Project.Enums;

namespace SynapseEnqueteManagement.DAL;

public class DummyDataSeeder
{
    private static Emoji _heart = null!;
    private static Emoji _thumbsup = null!;
    private static Emoji _fire = null!;
    private static Emoji _check = null!;

    // Content language for ideas/comments whose text is in the project's default
    // language. The author is picked at random from a mixed-language pool, so the
    // idea's language must NOT be derived from the author — it follows the text.
    private static Language _defaultLanguage = null!;

    // Deterministic so a reseed produces the same demo state (reaction counts,
    // authorship spread, timestamps) every run.
    private static readonly Random Rng = new(20260522);
    private static readonly DateTime Now = DateTime.UtcNow;

    private static DateTime Clamp(DateTime t) => t > Now ? Now : t;

    private static Participant Pick(IReadOnlyList<Participant> pool) => pool[Rng.Next(pool.Count)];

    // Reactions and comments are attributed to random members of the project's
    // participant pool so the demo looks like real, varied engagement.
    private static void AddIconComments(Idea idea, int count, Emoji[] emojis, IReadOnlyList<Participant> pool, DateTime baseDate)
    {
        for (int i = 0; i < count; i++)
            idea.Comments.Add(new IconComment
            {
                Emoji = emojis[i % emojis.Length], Idea = idea,
                Participant = Pick(pool), CreatedAt = Clamp(baseDate.AddHours(Rng.Next(0, 120)))
            });
    }

    private static void AddReactions(TextComment comment, Idea idea, int count, Emoji[] emojis, IReadOnlyList<Participant> pool, DateTime baseDate)
    {
        for (int i = 0; i < count; i++)
            comment.EmojiReactions.Add(new IconComment
            {
                Emoji = emojis[i % emojis.Length], Idea = idea, TextComment = comment,
                Participant = Pick(pool), CreatedAt = Clamp(baseDate.AddHours(Rng.Next(0, 72)))
            });
    }

    private static Idea CreateIdea(string description, string[] commentTexts, int ideaIconCount, int[] reactionCounts,
        IReadOnlyList<Participant> pool, Emoji[] emojis = null, Language language = null, string imageUrl = null,
        int minDaysAgo = 0, int maxDaysAgo = 31)
    {
        emojis ??= new[] { _heart, _thumbsup, _fire, _check };
        var author = Pick(pool);
        // Language follows the idea's TEXT, never the author: in a multi-language
        // project a participant may post in a non-default language, so a random
        // author (whose profile language may differ) is intentional and realistic.
        var lang = language ?? _defaultLanguage;
        var created = Now.AddDays(-Rng.Next(minDaysAgo, maxDaysAgo + 1)).AddHours(-Rng.Next(0, 24)).AddMinutes(-Rng.Next(0, 60));
        var idea = new Idea
        {
            Description = description, Status = ModerationStatus.Approved,
            Participant = author, Language = lang, ImageUrl = imageUrl, CreatedAt = created
        };
        AddIconComments(idea, ideaIconCount, emojis, pool, created);
        for (int i = 0; i < commentTexts.Length; i++)
        {
            var commentAt = Clamp(created.AddHours(Rng.Next(1, 96)));
            var comment = new TextComment
            {
                Idea = idea,
                Text = commentTexts[i],
                ModerationStatus = ModerationStatus.Approved,
                Participant = Pick(pool),
                Language = lang,
                CreatedAt = commentAt
            };
            AddReactions(comment, idea, reactionCounts[i % reactionCounts.Length], emojis, pool, commentAt);
            idea.Comments.Add(comment);
        }
        return idea;
    }

    // Creates a participant who completed the survey, attaches them to the project,
    // and returns them so the caller can record their answers. Email is only stored
    // when the project actually requests contact info (opt-in), regardless of the
    // address passed in — this keeps the opt-in invariant in one place.
    private static Participant AddRespondent(Project project, Questionnaire questionnaire, Language language,
        string email, int daysAgo, int durationSeconds)
    {
        var created = Now.AddDays(-daysAgo).AddHours(-Rng.Next(0, 24)).AddMinutes(-Rng.Next(0, 60));
        var participant = new Participant
        {
            Project = project,
            Questionnaire = questionnaire,
            Language = language,
            HasSubmitted = true,
            CreatedAt = created,
            CompletedAt = created.AddSeconds(durationSeconds),
            Email = project.RequestContactInfo ? email : null
        };
        project.Participants.Add(participant);
        return participant;
    }

    // A participant who started but never submitted — feeds the started-vs-completed funnel.
    private static void AddAbandoner(Project project, Questionnaire questionnaire, Language language,
        Question firstQuestion, string firstValue, int daysAgo)
    {
        var created = Now.AddDays(-daysAgo).AddHours(-Rng.Next(0, 24)).AddMinutes(-Rng.Next(0, 60));
        var participant = new Participant
        {
            Project = project,
            Questionnaire = questionnaire,
            Language = language,
            HasSubmitted = false,
            CreatedAt = created
        };
        participant.Responses.Add(new Response
        {
            Question = firstQuestion, Participant = participant, Value = firstValue,
            Language = language, IsCommitted = false, CreatedAt = created
        });
        project.Participants.Add(participant);
    }

    private static void Answer(Participant participant, Question question, string value) =>
        participant.Responses.Add(new Response
        {
            Question = question, Participant = participant, Value = value,
            Language = participant.Language, IsCommitted = true, CreatedAt = participant.CreatedAt
        });

    // Resolve an answer-choice string in the participant's own language so saved
    // values match what the survey-flow logic compares against.
    private static string Choice(Question question, Language language, int index) =>
        question.Translations.First(t => t.Language == language).AnswerChoices.ElementAt(index);

    private static string Choices(Question question, Language language, params int[] indices) =>
        string.Join(", ", indices.Select(i => Choice(question, language, i)));

    public static void Seed(SynapseEnqueteContext context, string apiBaseUrl)
    {
        Language languageNl, languageEn;
        if (!context.Languages.Any())
        {
            languageNl = new Language { Code = "nl", DisplayName = "Nederlands", IsActive = true };
            languageEn = new Language { Code = "en", DisplayName = "English", IsActive = true };
            context.Languages.AddRange(languageNl, languageEn);
            context.SaveChanges();
        }
        else
        {
            languageNl = context.Languages.First(l => l.Code == "nl");
            languageEn = context.Languages.First(l => l.Code == "en");
        }

        if (context.Projects.Any()) return;

        // The content language for ideas/comments whose text is in the project's
        // default language (nl). English ideas pass languageEn explicitly; this is
        // only the fallback for the untagged Dutch ones — it is NOT the author's language.
        _defaultLanguage = languageNl;

        // Seed media is app-bundled static content under wwwroot/seed, served by
        // app.UseStaticFiles() in every environment. Building the URL from the
        // per-environment ApiBaseUrl keeps it correct in dev, test and prod alike
        // (unlike runtime user uploads, which go through IStorageService / GCS).
        string SeedUrl(string relativePath) =>
            apiBaseUrl is not null ? $"{apiBaseUrl}/seed/{relativePath}" : null;

        _heart = new Emoji { Symbol = "❤️", Name = "heart" };
        _thumbsup = new Emoji { Symbol = "👍", Name = "thumbsup" };
        _fire = new Emoji { Symbol = "🔥", Name = "fire" };
        _check = new Emoji { Symbol = "✅", Name = "check" };
        context.Emojis.AddRange(_heart, _thumbsup, _fire, _check);
        context.SaveChanges();

        var platformSynapse = new ProjectPlatform
        {
            Name = "Synapse",
            LogoUrl = SeedUrl("logos/logo_synapse.jpg"),
            WebsiteUrl = "https://synapse-ip.xyz",
            CreatedAt = Now
        };
        var platformFitLife = new ProjectPlatform
        {
            Name = "FitLife Gym",
            LogoUrl = SeedUrl("logos/logo_fitlife.png"),
            WebsiteUrl = "https://www.fitlife.com",
            CreatedAt = Now
        };
        var platformEendracht = new ProjectPlatform
        {
            Name = "FC Eendracht",
            LogoUrl = SeedUrl("logos/logo_eendracht.png"),
            WebsiteUrl = "https://www.fceendracht.be",
            CreatedAt = Now
        };

        // ──────────────────────────────────────────────────────────────────────
        // Project 1: Synapse — mental wellbeing at school (Chat form, opt-in on)
        // ──────────────────────────────────────────────────────────────────────
        var questionnaire1 = new Questionnaire();
        var q1Stress = new Question
        {
            QuestionFormat = QuestionFormat.MultipleChoice, Questionnaire = questionnaire1,
            AnswerChoiceCount = 5, IsMandatory = false, Position = 1,
            // Supporting media: locally-hosted image (showcases the photo-on-question feature).
            MediaUrl = SeedUrl("questions/images/study-stress.jpg"),
            MediaType = QuestionMediaType.Image, MediaIsEmbed = false,
            Translations = new List<QuestionTranslation>
            {
                new() { Language = languageNl, Text = "Welke factoren zorgen bij jou de meeste stress?", AnswerChoices = new List<string> { "Examens", "Weinig vrije tijd", "Deadlines", "Sociale druk", "Thuissituatie" }, MediaText = "Veel jongeren ervaren stress door meerdere factoren tegelijk. Bekijk dit beeld en denk na over wat voor **jou** het zwaarst weegt." },
                new() { Language = languageEn, Text = "Which factors cause you the most stress?", AnswerChoices = new List<string> { "Exams", "Little free time", "Deadlines", "Social pressure", "Home situation" }, MediaText = "Many young people experience stress from several factors at once. Look at this image and think about what weighs heaviest for **you**." }
            }
        };
        var q1Welzijn = new Question
        {
            QuestionFormat = QuestionFormat.SingleChoice, Questionnaire = questionnaire1,
            AnswerChoiceCount = 3, IsMandatory = true, Position = 2,
            Translations = new List<QuestionTranslation>
            {
                new() { Language = languageNl, Text = "Hoe ervaar jij je mentaal welzijn op school de afgelopen maand?", AnswerChoices = new List<string> { "Goed", "Neutraal", "Slecht" } },
                new() { Language = languageEn, Text = "How do you experience your mental wellbeing at school over the past month?", AnswerChoices = new List<string> { "Good", "Neutral", "Bad" } }
            }
        };
        var q1Likert = new Question
        {
            QuestionFormat = QuestionFormat.Likert, Questionnaire = questionnaire1,
            AnswerChoiceCount = 0, IsMandatory = true, Position = 3,
            // Supporting media: YouTube embed (showcases the embed video path).
            MediaUrl = "https://www.youtube.com/embed/aqz-KE-bpKQ",
            MediaType = QuestionMediaType.Video, MediaIsEmbed = true,
            Translations = new List<QuestionTranslation>
            {
                new() { Language = languageNl, Text = "Op een schaal van 1 tot 10, hoeveel schoolstress ervaar jij?", MediaText = "Korte video met enkele tips om met schoolstress om te gaan." },
                new() { Language = languageEn, Text = "On a scale of 1 to 10, how much school stress do you experience?", MediaText = "Short video with a few tips on dealing with school stress." }
            }
        };
        var q1Open = new Question
        {
            QuestionFormat = QuestionFormat.Open, Questionnaire = questionnaire1,
            AnswerChoiceCount = 0, IsMandatory = true, Position = 4,
            // Supporting media: locally-hosted audio (showcases the audio-on-question feature).
            MediaUrl = SeedUrl("questions/audios/counselor-message.mp3"),
            MediaType = QuestionMediaType.Audio, MediaIsEmbed = false,
            Translations = new List<QuestionTranslation>
            {
                new() { Language = languageNl, Text = "Wat zou de school volgens jou concreet kunnen doen om het mentaal welzijn van leerlingen te verbeteren?", MediaText = "Beluister kort de boodschap van onze zorgcoördinator voor je antwoordt." },
                new() { Language = languageEn, Text = "What do you think the school could concretely do to improve the mental wellbeing of students?", MediaText = "Listen briefly to the message from our student counselor before you answer." }
            }
        };
        questionnaire1.Questions.Add(q1Stress);
        questionnaire1.Questions.Add(q1Welzijn);
        questionnaire1.Questions.Add(q1Likert);
        questionnaire1.Questions.Add(q1Open);

        // "Goed" → survey ends immediately for students who feel fine.
        q1Welzijn.EndingAnswers.Add(new EndingAnswer
        {
            Question = q1Welzijn,
            Translations = new List<EndingAnswerTranslation>
            {
                new() { Language = languageNl, Value = "Goed" },
                new() { Language = languageEn, Value = "Good" }
            }
        });

        // "Slecht" → drill down with two sub-questions; the first has its own
        // recursive child to exercise nested branching.
        var q1WelzijnSlechtFollowup = new Question
        {
            QuestionFormat = QuestionFormat.SingleChoice,
            Questionnaire = questionnaire1,
            AnswerChoiceCount = 2,
            IsMandatory = true,
            Position = 1,
            ParentQuestion = q1Welzijn,
            ChoiceIndex = 2, // "Slecht" is at index 2 in q1Welzijn choices
            Translations = new List<QuestionTranslation>
            {
                new() { Language = languageNl, Text = "Wil je graag praten met een vertrouwenspersoon op school?", AnswerChoices = new List<string> { "Ja", "Nee" } },
                new() { Language = languageEn, Text = "Would you like to talk to a trusted person at school?", AnswerChoices = new List<string> { "Yes", "No" } }
            }
        };
        var q1WelzijnSlechtWho = new Question
        {
            QuestionFormat = QuestionFormat.Open,
            Questionnaire = questionnaire1,
            AnswerChoiceCount = 0,
            IsMandatory = false,
            Position = 1,
            ParentQuestion = q1WelzijnSlechtFollowup,
            ChoiceIndex = 0, // "Ja" is at index 0 in q1WelzijnSlechtFollowup choices
            Translations = new List<QuestionTranslation>
            {
                new() { Language = languageNl, Text = "Wie zou je graag willen spreken en waarover?" },
                new() { Language = languageEn, Text = "Who would you like to speak to and about what?" }
            }
        };
        var q1WelzijnSlechtSupport = new Question
        {
            QuestionFormat = QuestionFormat.Open,
            Questionnaire = questionnaire1,
            AnswerChoiceCount = 0,
            IsMandatory = false,
            Position = 2,
            ParentQuestion = q1Welzijn,
            ChoiceIndex = 2, // "Slecht" is at index 2 in q1Welzijn choices
            Translations = new List<QuestionTranslation>
            {
                new() { Language = languageNl, Text = "Wat kunnen wij concreet doen om je beter te laten voelen?" },
                new() { Language = languageEn, Text = "What can we concretely do to make you feel better?" }
            }
        };
        q1Welzijn.Children.Add(q1WelzijnSlechtFollowup);
        q1Welzijn.Children.Add(q1WelzijnSlechtSupport);
        q1WelzijnSlechtFollowup.Children.Add(q1WelzijnSlechtWho);
        questionnaire1.Questions.Add(q1WelzijnSlechtFollowup);
        questionnaire1.Questions.Add(q1WelzijnSlechtWho);
        questionnaire1.Questions.Add(q1WelzijnSlechtSupport);

        var projectActive = new Project
        {
            ProjectState = ProjectState.Active,
            FormType = FormType.Chat,
            RequestContactInfo = true,
            AllowCrossLanguageIdeas = true,
            Questionnaire = questionnaire1,
            ShortForm = "mentaal-welzijn",
            ImageUrl = SeedUrl("projects/wellbeing-campus.jpg"),
            ViewCount = 54,
            ProjectPlatform = platformSynapse,
            Theme = ProjectTheme.Minimal,
            DefaultLanguage = languageNl,
            SupportedLanguages = new List<Language> { languageNl, languageEn },
            Translations = new List<ProjectTranslation>
            {
                new() { Language = languageNl, Name = "Mentaal Welzijn", Topic = "Welzijn op school" },
                new() { Language = languageEn, Name = "Mental Wellbeing", Topic = "Wellbeing at school" },
            },
            Introduction = new Introduction
            {
                Translations = new List<IntroductionTranslation>
                {
                    new()
                    {
                        Language = languageNl,
                        Text = "Welkom bij de Synapse enquête over mentaal welzijn op school. " +
                               "Deze enquête peilt naar jouw ervaringen en ideeën rond stress, welzijn en wat de school beter kan doen. " +
                               "Jouw antwoorden zijn anoniem en helpen ons om een betere schoolomgeving te creëren. " +
                               "Het invullen duurt ongeveer 5 minuten."
                    },
                    new()
                    {
                        Language = languageEn,
                        Text = "Welcome to the Synapse survey on mental wellbeing at school. " +
                               "This survey explores your experiences and ideas around stress, wellbeing, and what the school can do better. " +
                               "Your answers are anonymous and help us create a better school environment. " +
                               "It takes about 5 minutes to complete."
                    },
                }
            }
        };

        // Filled-in surveys: a varied pool of fake youth, each with committed answers
        // that respect the branching (Goed → ends; Slecht → drills down). Because this
        // project opts in to contact info, these participants get email addresses.
        var wellbeingPool = new List<Participant>();
        void Wellbeing(string email, bool en, int daysAgo, int durationSeconds, int[] stress, int welzijn,
            int likert = 0, string open = null, int? talk = null, string who = null, string support = null)
        {
            var lang = en ? languageEn : languageNl;
            var p = AddRespondent(projectActive, questionnaire1, lang, email, daysAgo, durationSeconds);
            wellbeingPool.Add(p);

            Answer(p, q1Stress, Choices(q1Stress, lang, stress));
            Answer(p, q1Welzijn, Choice(q1Welzijn, lang, welzijn));
            if (welzijn == 0) return; // "Goed"/"Good" terminates the survey

            if (welzijn == 2) // "Slecht"/"Bad" → drill-down branch
            {
                Answer(p, q1WelzijnSlechtFollowup, Choice(q1WelzijnSlechtFollowup, lang, talk!.Value)); // 0 = Ja/Yes
                if (talk == 0 && who is not null) Answer(p, q1WelzijnSlechtWho, who);
                if (support is not null) Answer(p, q1WelzijnSlechtSupport, support);
            }

            Answer(p, q1Likert, likert.ToString());
            if (open is not null) Answer(p, q1Open, open);
        }

        Wellbeing("lotte.vermeulen@leerling.synapse-college.be", false, 26, 240, new[] { 0, 2, 3 }, 2, 8,
            "Meer begrip tonen tijdens de examenperiode en deadlines beter spreiden.", talk: 0,
            who: "Graag met de zorgcoördinator over de werkdruk thuis en op school.",
            support: "Een vast aanspreekpunt en kortere wachttijden bij het CLB.");
        Wellbeing("milan.peeters@leerling.synapse-college.be", false, 22, 180, new[] { 1, 2 }, 1, 6,
            "Soms voel ik me oké, maar drukke weken zijn echt zwaar.");
        Wellbeing(null, false, 20, 95, new[] { 1 }, 0);
        Wellbeing("yasmine.elamrani@leerling.synapse-college.be", false, 18, 300, new[] { 0, 3, 4 }, 2, 9,
            "De sociale druk is enorm, vooral online.", talk: 1,
            support: "Anonieme gesprekken zouden de drempel verlagen.");
        Wellbeing("senne.maes@leerling.synapse-college.be", false, 15, 210, new[] { 2, 3 }, 1, 5,
            "Deadlines stapelen zich te veel op rond dezelfde periode.");
        Wellbeing("fatima.haddou@leerling.synapse-college.be", false, 12, 280, new[] { 0, 4 }, 2, 7,
            "Thuis is het soms moeilijk om rustig te studeren.", talk: 0,
            who: "Met een leerkracht die ik vertrouw, over mijn thuissituatie.",
            support: "Een rustige studeerplek op school na de lesuren.");
        Wellbeing(null, false, 10, 80, new[] { 3 }, 0);
        Wellbeing("arthur.claes@leerling.synapse-college.be", false, 8, 160, new[] { 1, 2 }, 1, 6,
            "Korte pauzes tussen de lessen zouden al helpen.");
        Wellbeing("emma.johnson@student.synapse-college.be", true, 7, 200, new[] { 0, 2 }, 1, 5,
            "The workload around exams is intense, but manageable with support.");
        Wellbeing("liam.smith@student.synapse-college.be", true, 5, 320, new[] { 0, 3, 4 }, 2, 9,
            "Social media makes everything feel like a competition.", talk: 0,
            who: "I'd like to talk to the school counselor about anxiety.",
            support: "More awareness around online pressure would help a lot.");
        Wellbeing(null, true, 4, 90, new[] { 1 }, 0);
        Wellbeing("olivia.brown@student.synapse-college.be", true, 3, 190, new[] { 2, 3 }, 1, 6,
            "Spreading deadlines more evenly would reduce a lot of stress.");
        Wellbeing("noah.janssens@leerling.synapse-college.be", false, 2, 260, new[] { 0, 2, 3 }, 2, 8,
            "De examendruk is voor mij echt het grootste probleem.", talk: 1,
            support: "Meer studiebegeleiding in de aanloop naar de examens.");
        Wellbeing("elise.wouters@leerling.synapse-college.be", false, 1, 175, new[] { 1, 4 }, 1, 7,
            "Een stille ruimte om even te ontspannen zou fijn zijn.");

        AddAbandoner(projectActive, questionnaire1, languageNl, q1Stress, Choices(q1Stress, languageNl, 2), 6);
        AddAbandoner(projectActive, questionnaire1, languageEn, q1Stress, Choices(q1Stress, languageEn, 0, 3), 9);

        var categoryCampus = new Category
        {
            Translations = new List<CategoryTranslation>
            {
                new() { Language = languageNl, Name = "Campus welzijn", Description = "Welzijn op school" },
                new() { Language = languageEn, Name = "Campus wellbeing", Description = "Wellbeing at school" },
            }
        };
        categoryCampus.Ideas.Add(CreateIdea("Meer rustplekken op school", new[] { "Geweldig idee, daar hebben we echt nood aan!", "Misschien ook wat groen toevoegen voor een kalme sfeer?", "Een stille ruimte zonder schermen zou perfect zijn." }, 35, new[] { 12, 8, 20 }, wellbeingPool));
        categoryCampus.Ideas.Add(CreateIdea("Meditatie sessies tijdens de middag", new[] { "Dit zou mij enorm helpen om de namiddag beter door te komen.", "Kan dit ook online gevolgd worden?", "Misschien ook 's ochtends voor de lessen beginnen?" }, 28, new[] { 15, 7, 18 }, wellbeingPool, imageUrl: SeedUrl("ideas/idea-meditation.jpg")));
        categoryCampus.Ideas.Add(CreateIdea("Anonieme klachtenbox voor leerlingen", new[] { "Eindelijk een veilige manier om problemen te melden!", "Belangrijk dat dit echt anoniem blijft.", "Wordt dit ook opgevolgd door de directie?" }, 42, new[] { 10, 22, 6 }, wellbeingPool));
        categoryCampus.Ideas.Add(CreateIdea("Vertrouwensleerkracht meer zichtbaar maken", new[] { "Veel leerlingen weten niet eens wie ze moeten aanspreken.", "Een vast spreekuur zou handig zijn.", "Posters in de gangen zouden al helpen." }, 31, new[] { 9, 14, 25 }, wellbeingPool));
        categoryCampus.Ideas.Add(CreateIdea("Korte pauzes tussen de lessen", new[] { "Vijf minuten extra tussen de lessen zou al een groot verschil maken.", "Dit helpt ook om op tijd naar je volgend lokaal te geraken.", "Mijn concentratie zakt altijd na twee uur aan een stuk." }, 47, new[] { 20, 11, 17 }, wellbeingPool));
        categoryCampus.Ideas.Add(CreateIdea("Stress management workshops for students", new[] { "Should be mandatory in the first year.", "I really needed this during exam period.", "Can parents also attend?" }, 38, new[] { 13, 19, 8 }, wellbeingPool, language: languageEn));
        categoryCampus.Ideas.Add(CreateIdea("Create a greener school environment", new[] { "Plants in classrooms would really improve the atmosphere.", "A school garden as a relaxation project would be great.", "Nature is proven to calm the mind." }, 26, new[] { 16, 7, 23 }, wellbeingPool, language: languageEn, imageUrl: SeedUrl("ideas/idea-greenspace.jpg")));
        categoryCampus.Ideas.Add(CreateIdea("Buddy system for new students", new[] { "I really needed this in my first week.", "Also helps build connections across year groups.", "Could this work for students with anxiety too?" }, 33, new[] { 11, 18, 5 }, wellbeingPool, language: languageEn));
        categoryCampus.Ideas.Add(CreateIdea("Healthier options in the school cafeteria", new[] { "Food has a big impact on how you feel.", "Less sugar and more fruit would already help.", "Healthy options need to stay affordable." }, 44, new[] { 21, 9, 15 }, wellbeingPool, language: languageEn, imageUrl: SeedUrl("ideas/idea-cafeteria.jpg")));
        categoryCampus.Ideas.Add(CreateIdea("Sports activities during lunch break", new[] { "Exercise is incredibly effective at reducing stress.", "A ping-pong table or basketball hoop would be enough.", "This also gets students away from their phones." }, 50, new[] { 24, 12, 19 }, wellbeingPool, language: languageEn));

        var categoryHome = new Category
        {
            Translations = new List<CategoryTranslation>
            {
                new() { Language = languageNl, Name = "Thuis welzijn", Description = "Welzijn thuis en in je vrije tijd" },
                new() { Language = languageEn, Name = "Home wellbeing", Description = "Wellbeing at home and in your free time" },
            }
        };
        categoryHome.Ideas.Add(CreateIdea("Tips voor een goede nachtrust", new[] { "Ik slaap veel beter sinds ik mijn telefoon wegleg voor het slapengaan.", "Een vast slaapschema helpt enorm!", "Blauwe lichtfilters op je scherm helpen ook." }, 29, new[] { 14, 22, 8 }, wellbeingPool));
        categoryHome.Ideas.Add(CreateIdea("Online hulplijn voor thuisproblemen", new[] { "Dit is zo belangrijk, niet iedereen heeft steun thuis.", "Moet ook beschikbaar zijn in het weekend.", "Anoniem kunnen chatten zou de drempel verlagen." }, 46, new[] { 25, 10, 17 }, wellbeingPool));
        categoryHome.Ideas.Add(CreateIdea("Bewegingstips voor na school", new[] { "Een wandeling van 20 minuten doet al wonderen.", "Yoga is ook super relaxerend na een drukke dag!", "Fietsen naar school telt ook als beweging." }, 37, new[] { 9, 20, 13 }, wellbeingPool));
        categoryHome.Ideas.Add(CreateIdea("Kookworkshop voor gezond eten thuis", new[] { "Leren koken is een levensnoodzakelijke vaardigheid.", "Gezond eten hoeft niet duur te zijn.", "Zou ook goed zijn als project voor huishoudkunde." }, 32, new[] { 16, 7, 24 }, wellbeingPool));
        categoryHome.Ideas.Add(CreateIdea("Dagboek bijhouden voor mentale rust", new[] { "Schrijven helpt om gedachten te ordenen.", "Ik doe dit al en het werkt echt!", "Kan ook digitaal via een app." }, 25, new[] { 11, 18, 6 }, wellbeingPool));
        categoryHome.Ideas.Add(CreateIdea("Encourage offline hobbies", new[] { "Drawing, music or crafting gives so much satisfaction.", "Putting screens away is easier when you have something else to do.", "School could start a hobby club." }, 40, new[] { 22, 13, 19 }, wellbeingPool, language: languageEn));
        categoryHome.Ideas.Add(CreateIdea("Involve parents in wellbeing conversations", new[] { "Home support matters just as much as school support.", "Parent evenings on mental wellbeing would be useful.", "Not all parents understand the pressure students face." }, 27, new[] { 8, 21, 15 }, wellbeingPool, language: languageEn));
        categoryHome.Ideas.Add(CreateIdea("Digital detox tips for home", new[] { "One hour a day without a screen does wonders.", "I now put my phone in another room at night.", "Apps that track your screen time also help." }, 43, new[] { 17, 10, 25 }, wellbeingPool, language: languageEn));
        categoryHome.Ideas.Add(CreateIdea("Involve friends in your wellbeing", new[] { "Just calling a friend already helps enormously.", "Going for a walk together is better than chatting online.", "Social connection is crucial for mental health." }, 36, new[] { 12, 23, 9 }, wellbeingPool, language: languageEn));
        categoryHome.Ideas.Add(CreateIdea("Spend time in nature during free time", new[] { "Visiting a park or forest is very calming.", "Even a garden or balcony with plants helps.", "Research shows that nature reduces stress." }, 48, new[] { 20, 14, 7 }, wellbeingPool, language: languageEn));

        var categoryOnline = new Category
        {
            Translations = new List<CategoryTranslation>
            {
                new() { Language = languageNl, Name = "Online welzijn", Description = "Gezond omgaan met social media en online" },
                new() { Language = languageEn, Name = "Online wellbeing", Description = "Healthy use of social media and online platforms" },
            }
        };
        categoryOnline.Ideas.Add(CreateIdea("Workshops over social media gebruik", new[] { "Ik zou graag leren hoe ik minder tijd op mijn telefoon spendeer.", "Dit zou verplicht moeten zijn voor alle leerlingen.", "Ook interessant voor leerkrachten en ouders." }, 39, new[] { 18, 11, 24 }, wellbeingPool));
        categoryOnline.Ideas.Add(CreateIdea("Schermtijd begrenzen via schoolapp", new[] { "Goed idee maar ouders moeten het ook steunen.", "Zou handig zijn als je zelf limieten kan instellen.", "Kan dit ook voor sociale media apart ingesteld worden?" }, 45, new[] { 7, 22, 16 }, wellbeingPool));
        categoryOnline.Ideas.Add(CreateIdea("Digitale detox dag op school organiseren", new[] { "Een dag zonder telefoon klinkt eerst eng maar achteraf super rustgevend!", "We zouden dan andere activiteiten kunnen doen zoals tekenen of sporten.", "Dit zou ook de klassfeer verbeteren." }, 30, new[] { 13, 19, 10 }, wellbeingPool));
        categoryOnline.Ideas.Add(CreateIdea("Bewustwording over vergelijkingscultuur online", new[] { "Instagram en TikTok zorgen voor veel onzekerheid.", "Les over filters en nep-perfectie zou helpen.", "Ik vergelijk mezelf veel minder sinds ik minder scroll." }, 49, new[] { 25, 8, 20 }, wellbeingPool));
        categoryOnline.Ideas.Add(CreateIdea("Cyberpesten bespreekbaar maken op school", new[] { "Veel leerlingen durven dit niet melden.", "Een anoniem meldpunt specifiek voor online pesten.", "Leerkrachten moeten ook getraind worden hierin." }, 41, new[] { 15, 21, 9 }, wellbeingPool));
        categoryOnline.Ideas.Add(CreateIdea("Teaching positive use of social media", new[] { "Social media can also be inspiring and connecting.", "Learning how to build a healthy online community.", "Explaining the difference between passive scrolling and active use." }, 34, new[] { 11, 17, 23 }, wellbeingPool, language: languageEn));
        categoryOnline.Ideas.Add(CreateIdea("Explaining privacy settings to students", new[] { "Most students don't know what they're sharing.", "A workshop on safe internet use is urgently needed.", "Should already be taught in the first year of secondary school." }, 27, new[] { 6, 20, 14 }, wellbeingPool, language: languageEn));
        categoryOnline.Ideas.Add(CreateIdea("Learning to handle negative reactions online", new[] { "Hateful comments can really hurt.", "Blocking and reporting should become more normal.", "Teaching emotional resilience online is important." }, 38, new[] { 19, 12, 25 }, wellbeingPool, language: languageEn));
        categoryOnline.Ideas.Add(CreateIdea("Phone-free zone during break time", new[] { "Being truly present with each other feels good.", "Would also improve the social atmosphere in class.", "Maybe voluntary first, then mandatory?" }, 52, new[] { 10, 18, 7 }, wellbeingPool, language: languageEn));
        categoryOnline.Ideas.Add(CreateIdea("Launch an online wellbeing platform for youth", new[] { "A safe online space where young people can go for help.", "With anonymous chat and professional guidance.", "Could also include tips and exercises." }, 44, new[] { 23, 9, 16 }, wellbeingPool, language: languageEn));

        projectActive.Categories.Add(categoryCampus);
        projectActive.Categories.Add(categoryHome);
        projectActive.Categories.Add(categoryOnline);

        // ──────────────────────────────────────────────────────────────────────
        // Project 2: FitLife Gym — member survey (VerticalScroll, no opt-in)
        // ──────────────────────────────────────────────────────────────────────
        var questionnaire2 = new Questionnaire();
        var q2Aspects = new Question
        {
            QuestionFormat = QuestionFormat.MultipleChoice, Questionnaire = questionnaire2,
            AnswerChoiceCount = 5, IsMandatory = false, Position = 1,
            // Supporting media: locally-hosted image.
            MediaUrl = SeedUrl("questions/images/gym-floor.jpg"),
            MediaType = QuestionMediaType.Image, MediaIsEmbed = false,
            Translations = new List<QuestionTranslation>
            {
                new() { Language = languageNl, Text = "Welke aspecten van onze dienstverlening waardeert u het meest?", AnswerChoices = new List<string> { "Klantenservice", "Prijs-kwaliteit", "Snelheid", "Gebruiksgemak", "Productaanbod" }, MediaText = "Een blik op onze vernieuwde cardiozone." },
                new() { Language = languageEn, Text = "Which aspects of our service do you value most?", AnswerChoices = new List<string> { "Customer service", "Value for money", "Speed", "Ease of use", "Product range" }, MediaText = "A look at our renovated cardio area." }
            }
        };
        var q2Satisfaction = new Question
        {
            QuestionFormat = QuestionFormat.SingleChoice, Questionnaire = questionnaire2,
            AnswerChoiceCount = 4, IsMandatory = true, Position = 2,
            Translations = new List<QuestionTranslation>
            {
                new() { Language = languageNl, Text = "Hoe tevreden bent u overall met onze service?", AnswerChoices = new List<string> { "Zeer tevreden", "Tevreden", "Neutraal", "Ontevreden" } },
                new() { Language = languageEn, Text = "How satisfied are you overall with our service?", AnswerChoices = new List<string> { "Very satisfied", "Satisfied", "Neutral", "Dissatisfied" } }
            }
        };
        var q2Recommend = new Question
        {
            QuestionFormat = QuestionFormat.Likert, Questionnaire = questionnaire2,
            AnswerChoiceCount = 0, IsMandatory = true, Position = 3,
            // Supporting media: directly-uploaded video (not an embed).
            MediaUrl = SeedUrl("questions/videos/club-intro.mp4"),
            MediaType = QuestionMediaType.Video, MediaIsEmbed = false,
            Translations = new List<QuestionTranslation>
            {
                new() { Language = languageNl, Text = "Op een schaal van 1 tot 10, hoe waarschijnlijk is het dat u ons aanbeveelt?", MediaText = "Korte sfeervideo van een dag bij FitLife." },
                new() { Language = languageEn, Text = "On a scale of 1 to 10, how likely are you to recommend us?", MediaText = "Short atmosphere video of a day at FitLife." }
            }
        };
        var q2Improve = new Question
        {
            QuestionFormat = QuestionFormat.Open, Questionnaire = questionnaire2,
            AnswerChoiceCount = 0, IsMandatory = false, Position = 4,
            // Supporting media: external audio URL.
            MediaUrl = "https://www.soundhelix.com/examples/mp3/SoundHelix-Song-1.mp3",
            MediaType = QuestionMediaType.Audio, MediaIsEmbed = false,
            Translations = new List<QuestionTranslation>
            {
                new() { Language = languageNl, Text = "Wat kunnen wij doen om uw ervaring te verbeteren?", MediaText = "Een korte boodschap van onze clubmanager." },
                new() { Language = languageEn, Text = "What can we do to improve your experience?", MediaText = "A short message from our club manager." }
            }
        };
        questionnaire2.Questions.Add(q2Aspects);
        questionnaire2.Questions.Add(q2Satisfaction);
        questionnaire2.Questions.Add(q2Recommend);
        questionnaire2.Questions.Add(q2Improve);

        var projectActive2 = new Project
        {
            ProjectState = ProjectState.Active,
            FormType = FormType.VerticalScroll,
            AllowCrossLanguageIdeas = false,
            Questionnaire = questionnaire2,
            ShortForm = "ledenbevraging",
            ImageUrl = SeedUrl("projects/gym-facility.jpg"),
            ViewCount = 41,
            ProjectPlatform = platformFitLife,
            Theme = ProjectTheme.Formal,
            DefaultLanguage = languageNl,
            SupportedLanguages = new List<Language> { languageNl, languageEn },
            Translations = new List<ProjectTranslation>
            {
                new() { Language = languageNl, Name = "Ledenbevraging", Topic = "Ervaring van leden" },
                new() { Language = languageEn, Name = "Member Survey", Topic = "Member experience" },
            },
            Introduction = new Introduction
            {
                Translations = new List<IntroductionTranslation>
                {
                    new()
                    {
                        Language = languageNl,
                        Text = "Welkom bij de FitLife ledenbevraging! " +
                               "We stellen deze enquête op om onze faciliteiten, lessen en diensten verder te verbeteren. " +
                               "Jouw mening als lid is enorm waardevol voor ons. " +
                               "Het invullen duurt slechts 3 minuten en alle antwoorden zijn volledig anoniem."
                    },
                    new()
                    {
                        Language = languageEn,
                        Text = "Welcome to the FitLife member survey! " +
                               "We have set up this survey to further improve our facilities, classes and services. " +
                               "Your opinion as a member is extremely valuable to us. " +
                               "It only takes 3 minutes to fill in and all answers are completely anonymous."
                    },
                }
            }
        };

        // No opt-in here, so AddRespondent stores no email even though we pass one.
        var fitLifePool = new List<Participant>();
        void FitLife(bool en, int daysAgo, int durationSeconds, int[] aspects, int satisfaction, int likert, string open = null)
        {
            var lang = en ? languageEn : languageNl;
            var p = AddRespondent(projectActive2, questionnaire2, lang, null, daysAgo, durationSeconds);
            fitLifePool.Add(p);
            Answer(p, q2Aspects, Choices(q2Aspects, lang, aspects));
            Answer(p, q2Satisfaction, Choice(q2Satisfaction, lang, satisfaction));
            Answer(p, q2Recommend, likert.ToString());
            if (open is not null) Answer(p, q2Improve, open);
        }

        FitLife(false, 25, 150, new[] { 0, 1 }, 1, 8, "Meer cardiotoestellen tijdens de ochtendspits.");
        FitLife(false, 23, 200, new[] { 1, 3 }, 0, 9);
        FitLife(false, 21, 170, new[] { 0, 2 }, 1, 7, "Langere openingsuren in het weekend zou top zijn.");
        FitLife(false, 19, 140, new[] { 1 }, 2, 6);
        FitLife(false, 17, 220, new[] { 0, 1, 3 }, 0, 10, "De nieuwe sauna is fantastisch, blijven doen!");
        FitLife(false, 14, 160, new[] { 2, 3 }, 1, 8);
        FitLife(false, 11, 190, new[] { 1, 4 }, 1, 7, "Iets meer variatie in de groepslessen zou welkom zijn.");
        FitLife(false, 9, 130, new[] { 0 }, 3, 4, "De wachttijden aan de toestellen zijn frustrerend.");
        FitLife(true, 7, 180, new[] { 0, 1 }, 1, 8, "Friendlier staff at the reception desk would be great.");
        FitLife(true, 5, 210, new[] { 1, 2, 3 }, 0, 9);
        FitLife(false, 3, 150, new[] { 3, 4 }, 1, 7, "Een app voor reservaties zou enorm handig zijn.");
        FitLife(false, 1, 165, new[] { 0, 1 }, 2, 6);

        AddAbandoner(projectActive2, questionnaire2, languageNl, q2Aspects, Choices(q2Aspects, languageNl, 1, 2), 13);
        AddAbandoner(projectActive2, questionnaire2, languageNl, q2Aspects, Choices(q2Aspects, languageNl, 0), 4);

        var fitLifeEmojis = new[] { _heart, _fire };

        var catFacilities = new Category
        {
            Translations = new List<CategoryTranslation>
            {
                new() { Language = languageNl, Name = "Faciliteiten", Description = "Kleedkamers, toestellen en ruimtes" },
                new() { Language = languageEn, Name = "Facilities", Description = "Changing rooms, equipment and spaces" },
            }
        };
        catFacilities.Ideas.Add(CreateIdea("Meer cardio toestellen in de ochtendzone", new[] { "De rijen aan de loopbanden zijn 's ochtends niet te doen.", "Misschien een reservatiesysteem voor piekuren?", "Roeiers en steppers zijn ook altijd bezet." }, 38, new[] { 14, 9, 22 }, fitLifePool, fitLifeEmojis));
        catFacilities.Ideas.Add(CreateIdea("Sauna toevoegen aan de wellnesszone", new[] { "Dit zou het lidmaatschap zoveel meer waard maken!", "Een infraroodsauna zou ideaal zijn voor spierherstel.", "Andere gyms hebben dit al standaard." }, 45, new[] { 20, 11, 17 }, fitLifePool, fitLifeEmojis, imageUrl: SeedUrl("ideas/idea-sauna.jpg")));
        catFacilities.Ideas.Add(CreateIdea("Betere ventilatie in de gewichtszaal", new[] { "Het is veel te warm en benauwd tijdens piekuren.", "Airconditioning zou een groot verschil maken.", "Extra ventilatoren als tussenoplossing?" }, 32, new[] { 8, 19, 13 }, fitLifePool, fitLifeEmojis));
        catFacilities.Ideas.Add(CreateIdea("Ruimere kleedkamers voor dames", new[] { "Nu is het bijna onmogelijk om je te kleden tijdens spitsuren.", "Meer kluisjes zouden ook welkom zijn.", "Een aparte ruimte voor föhnen zou handig zijn." }, 29, new[] { 15, 7, 24 }, fitLifePool, fitLifeEmojis));
        catFacilities.Ideas.Add(CreateIdea("Kinderhoek toevoegen aan de gym", new[] { "Dan kunnen ouders sporten terwijl de kinderen spelen.", "Zou veel meer jonge gezinnen aantrekken.", "Een begeleider voor de kinderen zou ideaal zijn." }, 36, new[] { 11, 20, 6 }, fitLifePool, fitLifeEmojis));
        catFacilities.Ideas.Add(CreateIdea("Gratis parkeerplaats voor leden", new[] { "Nu is parkeren een extra kost bovenop het lidmaatschap.", "Zeker 's avonds is dit een probleem.", "Een samenwerking met de parking naast de gym?" }, 41, new[] { 18, 12, 25 }, fitLifePool, fitLifeEmojis));
        catFacilities.Ideas.Add(CreateIdea("Smoothiebar toevoegen aan de receptie", new[] { "Gezonde snacks na het sporten zouden perfect zijn.", "Proteïneshakes en vers fruit als basis.", "Zou ook een leuke sociale plek worden." }, 27, new[] { 9, 16, 21 }, fitLifePool, fitLifeEmojis));
        catFacilities.Ideas.Add(CreateIdea("Meer spiegels in de vrije gewichtszaal", new[] { "Spiegels zijn essentieel voor techniekcontrole.", "Nu moet je soms in rare hoeken staan.", "Zeker aan de dumbbell rek zijn er te weinig." }, 33, new[] { 13, 22, 8 }, fitLifePool, fitLifeEmojis));
        catFacilities.Ideas.Add(CreateIdea("Wifi verbeteren in de sporthal", new[] { "Het signaal valt constant weg terwijl ik muziek stream.", "Wifi is tegenwoordig een basisverwachting.", "Zou ook nuttig zijn voor de personal trainers." }, 44, new[] { 17, 10, 19 }, fitLifePool, fitLifeEmojis));
        catFacilities.Ideas.Add(CreateIdea("Massagestoel toevoegen aan de relaxzone", new[] { "Na een zware training is dit de perfecte beloning.", "Zelfs een betaalde massagestoel zou ik gebruiken.", "Zou de gym onderscheiden van de concurrentie." }, 50, new[] { 24, 14, 7 }, fitLifePool, fitLifeEmojis));

        var catLessons = new Category
        {
            Translations = new List<CategoryTranslation>
            {
                new() { Language = languageNl, Name = "Lessen & Begeleiding", Description = "Groepslessen en personal training" },
                new() { Language = languageEn, Name = "Classes & Coaching", Description = "Group classes and personal training" },
            }
        };
        catLessons.Ideas.Add(CreateIdea("Yogales toevoegen aan het weekendrooster", new[] { "In het weekend heb ik eindelijk tijd voor yoga!", "Liefst vroeg in de ochtend zodat de rest van de dag vrij is.", "Ook een meditatieklas zou geweldig zijn." }, 39, new[] { 16, 23, 9 }, fitLifePool, fitLifeEmojis));
        catLessons.Ideas.Add(CreateIdea("Personal training sessies goedkoper maken", new[] { "Nu is het onbetaalbaar voor studenten.", "Misschien een studentenkorting invoeren?", "Duo sessies zouden ook goedkoper zijn per persoon." }, 43, new[] { 12, 19, 25 }, fitLifePool, fitLifeEmojis));
        catLessons.Ideas.Add(CreateIdea("Online lessen streamen voor leden", new[] { "Als ik ziek ben kan ik toch nog de les volgen.", "Zou ook handig zijn voor mensen met drukke schema's.", "Opnames achteraf bekijken zou ook fijn zijn." }, 31, new[] { 8, 14, 20 }, fitLifePool, fitLifeEmojis));
        catLessons.Ideas.Add(CreateIdea("Beginnerscursus voor nieuwe leden", new[] { "Nu word je gewoon losgelaten in de gym zonder uitleg.", "Een rondleiding en basisuitleg zou het verschil maken.", "Veel mensen stoppen omdat ze niet weten hoe te starten." }, 46, new[] { 22, 11, 17 }, fitLifePool, fitLifeEmojis));
        catLessons.Ideas.Add(CreateIdea("Voedingsadvies opnemen in het lidmaatschap", new[] { "Sport en voeding gaan hand in hand.", "Zelfs een basisconsultatie zou al enorm helpen.", "Nu moet je dat apart en duur betalen." }, 35, new[] { 10, 21, 6 }, fitLifePool, fitLifeEmojis));
        catLessons.Ideas.Add(CreateIdea("Dans- en zumbaklassen toevoegen", new[] { "Zumba is superleuk en verbrand veel calorieën.", "Zou ook meer vrouwelijke leden aantrekken.", "Een wekelijkse dansles op vrijdagavond zou gezellig zijn." }, 28, new[] { 15, 9, 23 }, fitLifePool, fitLifeEmojis));
        catLessons.Ideas.Add(CreateIdea("Boksklassen voor beginners", new[] { "Boksen is een geweldige stressafvoer.", "Een veilige omgeving voor beginners is belangrijk.", "Zou ook zelfverdediging kunnen combineren." }, 42, new[] { 19, 13, 7 }, fitLifePool, fitLifeEmojis));
        catLessons.Ideas.Add(CreateIdea("Maandelijkse fitnessdoelen bijhouden met trainer", new[] { "Progressie bijhouden motiveert enorm.", "Een korte check-in van 15 minuten per maand zou al genoeg zijn.", "Nu weet ik niet of ik vooruitga." }, 37, new[] { 11, 18, 24 }, fitLifePool, fitLifeEmojis));
        catLessons.Ideas.Add(CreateIdea("Senior-vriendelijke lessen toevoegen", new[] { "Ouderen hebben andere noden dan jonge sporters.", "Lage impact cardio en stretching zouden perfect zijn.", "Zou ook nieuwe doelgroep aantrekken." }, 26, new[] { 20, 8, 16 }, fitLifePool, fitLifeEmojis));
        catLessons.Ideas.Add(CreateIdea("Aqua aerobics in het zwembad", new[] { "Gewrichtsvriendelijk sporten voor iedereen.", "Ideaal voor mensen met rugklachten.", "Het zwembad wordt nu ondergecapaciteit gebruikt." }, 48, new[] { 14, 22, 10 }, fitLifePool, fitLifeEmojis));

        var catSchedule = new Category
        {
            Translations = new List<CategoryTranslation>
            {
                new() { Language = languageNl, Name = "Planning & Openingsuren", Description = "Bereikbaarheid en rooster" },
                new() { Language = languageEn, Name = "Schedule & Opening Hours", Description = "Accessibility and timetable" },
            }
        };
        catSchedule.Ideas.Add(CreateIdea("Gym ook open op zondagavond", new[] { "Zondagavond is perfect om de week voor te bereiden.", "Nu moet ik altijd wachten tot maandag.", "Zelfs maar tot 20u zou al een groot verschil maken." }, 40, new[] { 17, 11, 25 }, fitLifePool, fitLifeEmojis));
        catSchedule.Ideas.Add(CreateIdea("App voor reservaties van toestellen", new[] { "Dan weet je op voorhand of het druk is.", "Wachtrijen zijn frustrerend na een lange werkdag.", "Zou ook de verdeling van leden over de dag verbeteren." }, 34, new[] { 9, 20, 14 }, fitLifePool, fitLifeEmojis));
        catSchedule.Ideas.Add(CreateIdea("Vroeger openen op weekdagen", new[] { "Om 5u30 open zou mij toelaten voor het werk te sporten.", "Veel mensen willen 's ochtends vroeg trainen.", "Nu mis ik de ochtendtrein als ik eerst naar de gym ga." }, 47, new[] { 23, 12, 8 }, fitLifePool, fitLifeEmojis));
        catSchedule.Ideas.Add(CreateIdea("Wachtlijst systeem voor populaire lessen", new[] { "Lessen zijn vaak volgeboekt tegen dat ik kan inschrijven.", "Een automatische notificatie als er een plekje vrijkomt.", "Nu kom ik soms voor niets." }, 29, new[] { 16, 7, 19 }, fitLifePool, fitLifeEmojis));
        catSchedule.Ideas.Add(CreateIdea("Rustigere tijdslots aanduiden op de app", new[] { "Ik wil weten wanneer de gym relatief leeg is.", "Nu is het altijd een verrassing of het druk is.", "Zou mijn trainingservaring veel aangenamer maken." }, 38, new[] { 13, 21, 6 }, fitLifePool, fitLifeEmojis));
        catSchedule.Ideas.Add(CreateIdea("Feestdagen openingsuren beter communiceren", new[] { "Ik ben al een paar keer voor een gesloten deur gestaan.", "Een automatische e-mail of pushmelding zou helpen.", "Op de website staat het soms verkeerd." }, 25, new[] { 10, 18, 22 }, fitLifePool, fitLifeEmojis));
        catSchedule.Ideas.Add(CreateIdea("Langere openingsuren in het weekend", new[] { "Nu sluit de gym al om 18u op zondag.", "Het weekend is voor velen de enige tijd om lang te sporten.", "Zelfs tot 21u zou al een groot verschil maken." }, 43, new[] { 15, 9, 24 }, fitLifePool, fitLifeEmojis));
        catSchedule.Ideas.Add(CreateIdea("Persoonlijk trainingsrooster in de app", new[] { "Nu moet ik alles zelf bijhouden in een notitieblok.", "Een digitaal schema met herinneringen zou perfect zijn.", "Integratie met mijn agenda zou ideaal zijn." }, 36, new[] { 20, 11, 17 }, fitLifePool, fitLifeEmojis));
        catSchedule.Ideas.Add(CreateIdea("Druktemeter aan de ingang installeren", new[] { "Dan kan ik beslissen of ik nu ga of later.", "Sommige gyms hebben dit al en het werkt geweldig.", "Zou ook via de app beschikbaar kunnen zijn." }, 31, new[] { 8, 19, 13 }, fitLifePool, fitLifeEmojis));
        catSchedule.Ideas.Add(CreateIdea("Speciale ochtendpas voor vroege sporters", new[] { "Een goedkopere pas voor enkel ochtenduren zou perfect zijn.", "Dan zijn de toestellen minder bezet en ik betaal minder.", "Win-win voor de gym en de leden." }, 52, new[] { 25, 14, 20 }, fitLifePool, fitLifeEmojis));

        projectActive2.Categories.Add(catFacilities);
        projectActive2.Categories.Add(catLessons);
        projectActive2.Categories.Add(catSchedule);

        // ──────────────────────────────────────────────────────────────────────
        // Project 3: FC Eendracht — club survey (VerticalScroll, no opt-in)
        // ──────────────────────────────────────────────────────────────────────
        var questionnaire3 = new Questionnaire();
        var q3Aspects = new Question
        {
            QuestionFormat = QuestionFormat.MultipleChoice, Questionnaire = questionnaire3,
            AnswerChoiceCount = 5, IsMandatory = false, Position = 1,
            // Supporting media: external image URL (showcases linking media not hosted by us).
            MediaUrl = "https://images.unsplash.com/photo-1459865264687-595d652de67e?w=1200&q=70",
            MediaType = QuestionMediaType.Image, MediaIsEmbed = false,
            Translations = new List<QuestionTranslation>
            {
                new() { Language = languageNl, Text = "Welke aspecten van de club wil je verbeteren?", AnswerChoices = new List<string> { "Trainingen", "Infrastructuur", "Communicatie", "Sociale activiteiten", "Wedstrijdplanning" }, MediaText = "Ons hoofdveld op een wedstrijddag." },
                new() { Language = languageEn, Text = "Which aspects of the club do you want to improve?", AnswerChoices = new List<string> { "Training", "Infrastructure", "Communication", "Social activities", "Match schedule" }, MediaText = "Our main pitch on a match day." }
            }
        };
        var q3Atmosphere = new Question
        {
            QuestionFormat = QuestionFormat.SingleChoice, Questionnaire = questionnaire3,
            AnswerChoiceCount = 4, IsMandatory = true, Position = 2,
            Translations = new List<QuestionTranslation>
            {
                new() { Language = languageNl, Text = "Hoe beoordeel je de algemene sfeer in de club?", AnswerChoices = new List<string> { "Uitstekend", "Goed", "Matig", "Slecht" } },
                new() { Language = languageEn, Text = "How do you rate the general atmosphere in the club?", AnswerChoices = new List<string> { "Excellent", "Good", "Average", "Poor" } }
            }
        };
        var q3Training = new Question
        {
            QuestionFormat = QuestionFormat.Likert, Questionnaire = questionnaire3,
            AnswerChoiceCount = 0, IsMandatory = true, Position = 3,
            Translations = new List<QuestionTranslation>
            {
                new() { Language = languageNl, Text = "Op een schaal van 1 tot 10, hoe tevreden ben je over de trainingen?" },
                new() { Language = languageEn, Text = "On a scale of 1 to 10, how satisfied are you with the training sessions?" }
            }
        };
        var q3Change = new Question
        {
            QuestionFormat = QuestionFormat.Open, Questionnaire = questionnaire3,
            AnswerChoiceCount = 0, IsMandatory = false, Position = 4,
            // Supporting media: external audio URL.
            MediaUrl = "https://www.soundhelix.com/examples/mp3/SoundHelix-Song-8.mp3",
            MediaType = QuestionMediaType.Audio, MediaIsEmbed = false,
            Translations = new List<QuestionTranslation>
            {
                new() { Language = languageNl, Text = "Wat zou jij veranderen aan de club als je de kans had?", MediaText = "Beluister de oproep van onze voorzitter." },
                new() { Language = languageEn, Text = "What would you change about the club if you had the chance?", MediaText = "Listen to the message from our chairman." }
            }
        };
        questionnaire3.Questions.Add(q3Aspects);
        questionnaire3.Questions.Add(q3Atmosphere);
        questionnaire3.Questions.Add(q3Training);
        questionnaire3.Questions.Add(q3Change);

        var projectActive3 = new Project
        {
            ProjectState = ProjectState.Active,
            FormType = FormType.VerticalScroll,
            Questionnaire = questionnaire3,
            ShortForm = "clubbevraging",
            ImageUrl = SeedUrl("projects/football-club.jpg"),
            ViewCount = 33,
            ProjectPlatform = platformEendracht,
            Theme = ProjectTheme.Bold,
            PrimaryColor = "#16A34A",
            DefaultLanguage = languageNl,
            SupportedLanguages = new List<Language> { languageNl, languageEn },
            Translations = new List<ProjectTranslation>
            {
                new() { Language = languageNl, Name = "Clubbevraging", Topic = "De club verbeteren" },
                new() { Language = languageEn, Name = "Club Survey", Topic = "Improving the club" },
            },
        };

        var eendrachtPool = new List<Participant>();
        void Eendracht(bool en, int daysAgo, int durationSeconds, int[] aspects, int atmosphere, int likert, string open = null)
        {
            var lang = en ? languageEn : languageNl;
            var p = AddRespondent(projectActive3, questionnaire3, lang, null, daysAgo, durationSeconds);
            eendrachtPool.Add(p);
            Answer(p, q3Aspects, Choices(q3Aspects, lang, aspects));
            Answer(p, q3Atmosphere, Choice(q3Atmosphere, lang, atmosphere));
            Answer(p, q3Training, likert.ToString());
            if (open is not null) Answer(p, q3Change, open);
        }

        Eendracht(false, 27, 200, new[] { 0, 1 }, 1, 8, "Een nieuw kunstgrasveld zou de club enorm vooruithelpen.");
        Eendracht(false, 24, 160, new[] { 2, 3 }, 0, 9);
        Eendracht(false, 22, 180, new[] { 0, 4 }, 1, 7, "Betere communicatie over wedstrijdwijzigingen.");
        Eendracht(false, 20, 140, new[] { 1 }, 2, 5);
        Eendracht(false, 18, 210, new[] { 0, 1, 3 }, 0, 10, "De teamgeest is geweldig, ga zo door!");
        Eendracht(false, 15, 170, new[] { 2 }, 1, 7);
        Eendracht(false, 13, 190, new[] { 3, 4 }, 1, 6, "Meer sociale activiteiten naast het voetbal zelf.");
        Eendracht(false, 10, 150, new[] { 0 }, 1, 8);
        Eendracht(true, 8, 200, new[] { 1, 2 }, 2, 5, "The changing rooms are in urgent need of renovation.");
        Eendracht(false, 6, 175, new[] { 0, 3 }, 0, 9);
        Eendracht(false, 4, 185, new[] { 1, 4 }, 1, 7, "Verlichting op het trainingsveld in de winter.");
        Eendracht(false, 2, 160, new[] { 0, 1 }, 1, 8);

        AddAbandoner(projectActive3, questionnaire3, languageNl, q3Aspects, Choices(q3Aspects, languageNl, 0, 1), 11);

        var catTraining = new Category
        {
            Translations = new List<CategoryTranslation>
            {
                new() { Language = languageNl, Name = "Trainingen", Description = "Kwaliteit en opbouw van de trainingen" },
                new() { Language = languageEn, Name = "Training", Description = "Quality and structure of training sessions" },
            }
        };
        catTraining.Ideas.Add(CreateIdea("Videoanalyse invoeren bij de trainingen", new[] { "Profclubs doen dit al jaren, waarom wij niet?", "Zou mijn positiespel enorm verbeteren.", "Een simpele tablet met een app zou al voldoende zijn." }, 44, new[] { 18, 11, 25 }, eendrachtPool));
        catTraining.Ideas.Add(CreateIdea("Aparte trainingsgroep voor gevorderde spelers", new[] { "Nu wachten gevorderde spelers te veel op beginners.", "Iedereen traint op zijn eigen niveau, eerlijker voor iedereen.", "Zolang het de teamgeest niet schaadt, goed idee." }, 37, new[] { 13, 20, 8 }, eendrachtPool));
        catTraining.Ideas.Add(CreateIdea("Conditietraining twee keer per week", new[] { "Nu focussen we te veel op tactiek en te weinig op conditie.", "Een aparte conditietrainer zou ideaal zijn.", "Veel spelers zijn in de tweede helft uitgeput." }, 41, new[] { 9, 17, 22 }, eendrachtPool));
        catTraining.Ideas.Add(CreateIdea("Individuele techniektraining voor jongeren", new[] { "Jonge spelers hebben meer individuele aandacht nodig.", "Een techniektraining naast de groepstraining zou perfect zijn.", "Zou de jeugdopleiding enorm versterken." }, 29, new[] { 15, 23, 7 }, eendrachtPool));
        catTraining.Ideas.Add(CreateIdea("Gasttrainer uitnodigen eens per maand", new[] { "Een frisse blik van buitenaf kan veel losmaken.", "Zou ook motiverend zijn voor de spelers.", "Misschien een oud-profvoetballer uitnodigen?" }, 48, new[] { 21, 12, 19 }, eendrachtPool));
        catTraining.Ideas.Add(CreateIdea("Warming-up protocol standaardiseren", new[] { "Nu doet iedereen maar wat voor de training.", "Een goede warming-up vermindert blessures.", "De FIFA 11+ methode is bewezen effectief." }, 33, new[] { 10, 18, 24 }, eendrachtPool));
        catTraining.Ideas.Add(CreateIdea("Mentale weerbaarheid trainen", new[] { "Voetbal is ook een mentale sport.", "Een sportpsycholoog uitnodigen zou waardevol zijn.", "Veel wedstrijden worden gewonnen of verloren in het hoofd." }, 26, new[] { 16, 9, 21 }, eendrachtPool));
        catTraining.Ideas.Add(CreateIdea("Trainingsschema digitaal beschikbaar stellen", new[] { "Nu weet ik pas op de dag zelf wat we gaan doen.", "Een weekoverzicht via een app zou perfect zijn.", "Zou ook helpen om je voor te bereiden." }, 35, new[] { 14, 22, 6 }, eendrachtPool));
        catTraining.Ideas.Add(CreateIdea("Kleine groepsoefeningen meer integreren", new[] { "Rondjes lopen is saai, kleine spelvormen zijn leuker.", "Leert ook sneller samenspelen.", "Profteams trainen bijna uitsluitend zo." }, 42, new[] { 19, 8, 25 }, eendrachtPool));
        catTraining.Ideas.Add(CreateIdea("Keeperstraining apart organiseren", new[] { "De keeper wordt nu vergeten tijdens veldtrainingen.", "Een keeperscoach zou een enorm verschil maken.", "Zelfs één extra training per week zou helpen." }, 50, new[] { 23, 15, 11 }, eendrachtPool));

        var catInfra = new Category
        {
            Translations = new List<CategoryTranslation>
            {
                new() { Language = languageNl, Name = "Infrastructuur", Description = "Velden, kleedkamers en materiaal" },
                new() { Language = languageEn, Name = "Infrastructure", Description = "Pitches, changing rooms and equipment" },
            }
        };
        catInfra.Ideas.Add(CreateIdea("Nieuw kunstgrasveld aanleggen", new[] { "Het huidige veld is in slechte staat na de winter.", "Kunstgras betekent ook spelen bij slecht weer.", "Subsidies aanvragen bij de gemeente?" }, 46, new[] { 20, 13, 9 }, eendrachtPool, imageUrl: SeedUrl("ideas/idea-pitch.png")));
        catInfra.Ideas.Add(CreateIdea("Betere verlichting op het trainingsveld", new[] { "In de winter trainen we bijna in het donker.", "LED verlichting is energiezuinig en helder.", "Dit verhoogt ook de veiligheid tijdens avondtrainingen." }, 38, new[] { 12, 24, 17 }, eendrachtPool));
        catInfra.Ideas.Add(CreateIdea("Nieuwe ballen aanschaffen", new[] { "De huidige ballen zijn versleten en onregelmatig.", "Goede materialen motiveren spelers.", "Misschien sponsors zoeken voor nieuwe ballen?" }, 31, new[] { 8, 19, 14 }, eendrachtPool));
        catInfra.Ideas.Add(CreateIdea("Kleedkamers renoveren", new[] { "De douches zijn verouderd en soms koud.", "Een propere kleedkamer creëert een professionele sfeer.", "Zou ook nieuwe leden aantrekken." }, 43, new[] { 22, 10, 18 }, eendrachtPool));
        catInfra.Ideas.Add(CreateIdea("Tribunes overdekken", new[] { "Supporters staan in de regen bij thuiswedstrijden.", "Een eenvoudig dak zou al het verschil maken.", "Zou ook meer supporters aantrekken." }, 27, new[] { 11, 20, 7 }, eendrachtPool));
        catInfra.Ideas.Add(CreateIdea("Materiaalberging uitbreiden", new[] { "Nu liggen ballen en hesjes overal.", "Een georganiseerde berging bespaart veel tijd.", "Zou ook beschadiging van materiaal voorkomen." }, 36, new[] { 16, 9, 23 }, eendrachtPool));
        catInfra.Ideas.Add(CreateIdea("Scorebord installeren", new[] { "Nu weten supporters de score soms niet.", "Een digitaal scorebord zou professioneler ogen.", "Zou ook de wedstrijdervaring verbeteren." }, 25, new[] { 13, 21, 6 }, eendrachtPool));
        catInfra.Ideas.Add(CreateIdea("Fietsenstalling bouwen bij het stadion", new[] { "Nu worden fietsen overal gestald.", "Een overdekte stalling zou gewaardeerd worden.", "Duurzaam verplaatsen stimuleren." }, 40, new[] { 17, 11, 25 }, eendrachtPool));
        catInfra.Ideas.Add(CreateIdea("Ehbo-post moderniseren", new[] { "De huidige ehbo-kit is onvolledig.", "Een defibrillator is tegenwoordig noodzakelijk.", "Zou ook het vertrouwen van ouders vergroten." }, 34, new[] { 9, 18, 22 }, eendrachtPool));
        catInfra.Ideas.Add(CreateIdea("Oefenruimte voor keeperstraining", new[] { "Nu trainen keepers tussen de veldspelers.", "Een aparte ruimte met doelen en matten zou ideaal zijn.", "Kleine investering met grote impact." }, 49, new[] { 24, 14, 8 }, eendrachtPool));

        var catSocial = new Category
        {
            Translations = new List<CategoryTranslation>
            {
                new() { Language = languageNl, Name = "Sociale activiteiten", Description = "Teambuilding en clubactiviteiten" },
                new() { Language = languageEn, Name = "Social activities", Description = "Team building and club events" },
            }
        };
        catSocial.Ideas.Add(CreateIdea("Jaarlijks clubkampioenschap organiseren", new[] { "Een intern tornooi zou de teamgeest enorm boosten!", "Met een trofee en feestje achteraf!", "Families uitnodigen als supporters zou gezellig zijn." }, 45, new[] { 19, 12, 25 }, eendrachtPool));
        catSocial.Ideas.Add(CreateIdea("Teambuilding weekend plannen", new[] { "Een weekend weg versterkt de band enorm.", "Escape room of outdoor activiteit zou perfect zijn.", "Hoeft niet duur te zijn als iedereen bijdraagt." }, 38, new[] { 14, 22, 9 }, eendrachtPool));
        catSocial.Ideas.Add(CreateIdea("Clubavond voor spelers en supporters", new[] { "Een quiz of feestje zou iedereen samenbrengen.", "Nu kennen spelers en supporters elkaar nauwelijks.", "Zou ook nieuwe vrijwilligers aantrekken." }, 32, new[] { 10, 17, 21 }, eendrachtPool));
        catSocial.Ideas.Add(CreateIdea("Nieuwjaarsreceptie voor alle leden", new[] { "Een traditionele receptie versterkt het clubgevoel.", "Met terugblik op het seizoen en vooruitblik.", "Zou ook sponsors kunnen uitnodigen." }, 27, new[] { 8, 20, 13 }, eendrachtPool));
        catSocial.Ideas.Add(CreateIdea("Jeugd en senioren samen laten trainen", new[] { "Jongeren leren van ervaren spelers.", "Zou ook de doorstroom naar het eerste elftal verbeteren.", "Een gezamenlijke training per maand zou al genoeg zijn." }, 41, new[] { 16, 11, 24 }, eendrachtPool));
        catSocial.Ideas.Add(CreateIdea("Clubmagazine of nieuwsbrief opstarten", new[] { "Wedstrijdverslagen en clubnieuws voor alle leden.", "Zou ook de betrokkenheid van supporters vergroten.", "Een digitale versie is gratis en makkelijk te verspreiden." }, 29, new[] { 12, 19, 7 }, eendrachtPool));
        catSocial.Ideas.Add(CreateIdea("Vrijwilligersdag organiseren", new[] { "De club draait op vrijwilligers en die verdienen waardering.", "Een bedankfeestje of etentje zou veel betekenen.", "Zou ook nieuwe vrijwilligers motiveren." }, 36, new[] { 23, 9, 18 }, eendrachtPool));
        catSocial.Ideas.Add(CreateIdea("Supporters club oprichten", new[] { "Nu is er geen officiële supportersgroep.", "Een supportersclub zou de sfeer bij wedstrijden verbeteren.", "Zou ook extra inkomsten genereren voor de club." }, 44, new[] { 15, 21, 6 }, eendrachtPool));
        catSocial.Ideas.Add(CreateIdea("Kerstdiner voor het volledige team", new[] { "Een jaarlijks diner brengt iedereen samen.", "Zou ook een moment zijn om te reflecteren op het jaar.", "Met awards voor beste speler, meeste inzet etc." }, 33, new[] { 11, 25, 14 }, eendrachtPool));
        catSocial.Ideas.Add(CreateIdea("Open trainingsdag voor families", new[] { "Families zien graag waarmee hun kind bezig is.", "Zou ook nieuwe leden aantrekken.", "Een kleine demonstratie aan het einde zou leuk zijn." }, 50, new[] { 20, 13, 22 }, eendrachtPool));

        projectActive3.Categories.Add(catTraining);
        projectActive3.Categories.Add(catInfra);
        projectActive3.Categories.Add(catSocial);

        // ──────────────────────────────────────────────────────────────────────
        // Inactive & Draft (no participants, demonstrate non-active states)
        // ──────────────────────────────────────────────────────────────────────
        // A past Synapse school campaign that has since been archived. It carries a
        // full history (questionnaire, completed surveys with answers, ideas and
        // comments) so the archived-project views have realistic data to show. All
        // timestamps sit ~5-8 months in the past.
        var questionnaireArchived = new Questionnaire();
        var qA1 = new Question
        {
            QuestionFormat = QuestionFormat.MultipleChoice, Questionnaire = questionnaireArchived,
            AnswerChoiceCount = 5, IsMandatory = false, Position = 1,
            MediaUrl = SeedUrl("ideas/idea-greenspace.jpg"),
            MediaType = QuestionMediaType.Image, MediaIsEmbed = false,
            Translations = new List<QuestionTranslation>
            {
                new() { Language = languageNl, Text = "Wat doe je het liefst tijdens de middagpauze?", AnswerChoices = new List<string> { "Buiten zijn", "Sporten met vrienden", "Samen eten", "Op je telefoon", "Studeren" }, MediaText = "De speelplaats tijdens een pauze vorig schooljaar." },
                new() { Language = languageEn, Text = "What do you most enjoy doing during the lunch break?", AnswerChoices = new List<string> { "Being outside", "Sports with friends", "Eating together", "On your phone", "Studying" }, MediaText = "The playground during a break last school year." }
            }
        };
        var qA2 = new Question
        {
            QuestionFormat = QuestionFormat.SingleChoice, Questionnaire = questionnaireArchived,
            AnswerChoiceCount = 3, IsMandatory = true, Position = 2,
            Translations = new List<QuestionTranslation>
            {
                new() { Language = languageNl, Text = "Hoe ervaar je de pauzes op school momenteel?", AnswerChoices = new List<string> { "Goed", "Neutraal", "Slecht" } },
                new() { Language = languageEn, Text = "How do you currently experience the breaks at school?", AnswerChoices = new List<string> { "Good", "Neutral", "Bad" } }
            }
        };
        var qA3 = new Question
        {
            QuestionFormat = QuestionFormat.Likert, Questionnaire = questionnaireArchived,
            AnswerChoiceCount = 0, IsMandatory = true, Position = 3,
            Translations = new List<QuestionTranslation>
            {
                new() { Language = languageNl, Text = "Op een schaal van 1 tot 10, hoe tevreden ben je over de ontspanningsruimtes?" },
                new() { Language = languageEn, Text = "On a scale of 1 to 10, how satisfied are you with the relaxation areas?" }
            }
        };
        var qA4 = new Question
        {
            QuestionFormat = QuestionFormat.Open, Questionnaire = questionnaireArchived,
            AnswerChoiceCount = 0, IsMandatory = false, Position = 4,
            Translations = new List<QuestionTranslation>
            {
                new() { Language = languageNl, Text = "Wat zou de pauzes voor jou aangenamer maken?" },
                new() { Language = languageEn, Text = "What would make the breaks more enjoyable for you?" }
            }
        };
        questionnaireArchived.Questions.Add(qA1);
        questionnaireArchived.Questions.Add(qA2);
        questionnaireArchived.Questions.Add(qA3);
        questionnaireArchived.Questions.Add(qA4);

        var projectInactive = new Project
        {
            ProjectState = ProjectState.Archived,
            FormType = FormType.VerticalScroll,
            RequestContactInfo = true,
            Questionnaire = questionnaireArchived,
            ShortForm = "pauze-ontspanning",
            ImageUrl = SeedUrl("projects/schoolyard.jpg"),
            ViewCount = 41,
            ProjectPlatform = platformSynapse,
            Theme = ProjectTheme.Minimal,
            DefaultLanguage = languageNl,
            SupportedLanguages = new List<Language> { languageNl, languageEn },
            Translations = new List<ProjectTranslation>
            {
                new() { Language = languageNl, Name = "Pauze & Ontspanning", Topic = "Pauzes op school (afgesloten)" },
                new() { Language = languageEn, Name = "Breaks & Relaxation", Topic = "Breaks at school (closed)" },
            },
            Introduction = new Introduction
            {
                Translations = new List<IntroductionTranslation>
                {
                    new() { Language = languageNl, Text = "Deze bevraging over de pauzes en ontspanningsruimtes op school is afgesloten. Bedankt aan iedereen die heeft deelgenomen!" },
                    new() { Language = languageEn, Text = "This survey about breaks and relaxation areas at school is closed. Thanks to everyone who took part!" },
                }
            }
        };

        var archivedPool = new List<Participant>();
        void Archived(string email, bool en, int daysAgo, int durationSeconds, int[] activity, int experience, int likert, string open = null)
        {
            var lang = en ? languageEn : languageNl;
            var p = AddRespondent(projectInactive, questionnaireArchived, lang, email, daysAgo, durationSeconds);
            archivedPool.Add(p);
            Answer(p, qA1, Choices(qA1, lang, activity));
            Answer(p, qA2, Choice(qA2, lang, experience));
            Answer(p, qA3, likert.ToString());
            if (open is not null) Answer(p, qA4, open);
        }

        Archived("tom.declercq@leerling.synapse-college.be", false, 232, 180, new[] { 0, 1 }, 1, 6, "Meer zitbanken op de speelplaats zou fijn zijn.");
        Archived("amber.goossens@leerling.synapse-college.be", false, 228, 150, new[] { 2 }, 0, 8);
        Archived(null, false, 221, 90, new[] { 3 }, 1, 5);
        Archived("ravi.nair@student.synapse-college.be", true, 215, 200, new[] { 0, 1, 4 }, 1, 7, "A covered area for rainy days would help a lot.");
        Archived("jana.willems@leerling.synapse-college.be", false, 205, 175, new[] { 2, 3 }, 2, 4, "De ontspanningsruimte is veel te klein voor iedereen.");
        Archived("kobe.maris@leerling.synapse-college.be", false, 198, 160, new[] { 1 }, 1, 6);
        Archived("sara.osei@student.synapse-college.be", true, 190, 210, new[] { 0, 2 }, 0, 9, "More board games and a quiet corner would be great.");
        Archived(null, false, 182, 85, new[] { 3 }, 1, 5);
        Archived("daan.verhoeven@leerling.synapse-college.be", false, 171, 195, new[] { 0, 1 }, 1, 7, "Een buitentafeltennistafel zou top zijn.");
        Archived("lina.haddad@leerling.synapse-college.be", false, 158, 170, new[] { 2, 4 }, 2, 5, "Meer rustige plekken om te studeren tijdens de pauze.");

        AddAbandoner(projectInactive, questionnaireArchived, languageNl, qA1, Choices(qA1, languageNl, 1), 210);
        AddAbandoner(projectInactive, questionnaireArchived, languageNl, qA1, Choices(qA1, languageNl, 0, 2), 188);

        var catPlayground = new Category
        {
            Translations = new List<CategoryTranslation>
            {
                new() { Language = languageNl, Name = "Speelplaats", Description = "De buitenruimte tijdens de pauzes" },
                new() { Language = languageEn, Name = "Playground", Description = "The outdoor space during breaks" },
            }
        };
        catPlayground.Ideas.Add(CreateIdea("Meer zitbanken op de speelplaats", new[] { "Nu moet iedereen op de grond of de stoep zitten.", "Banken in de schaduw zouden ideaal zijn.", "Picknicktafels zouden ook leuk zijn om samen te eten." }, 34, new[] { 12, 18, 7 }, archivedPool, minDaysAgo: 150, maxDaysAgo: 230, imageUrl: SeedUrl("ideas/idea-greenspace.jpg")));
        catPlayground.Ideas.Add(CreateIdea("Overdekte zone voor regenachtige dagen", new[] { "Bij regen moeten we nu allemaal in de gangen staan.", "Een afdak zou een groot verschil maken.", "Zo kunnen we ook bij slecht weer buiten zijn." }, 41, new[] { 20, 9, 16 }, archivedPool, minDaysAgo: 150, maxDaysAgo: 230));
        catPlayground.Ideas.Add(CreateIdea("Buitentafeltennistafel plaatsen", new[] { "Een betonnen pingpongtafel gaat jaren mee.", "Zou veel leerlingen van hun telefoon weghouden.", "Misschien zelfs een klein tornooi organiseren?" }, 29, new[] { 8, 22, 14 }, archivedPool, minDaysAgo: 150, maxDaysAgo: 230));
        catPlayground.Ideas.Add(CreateIdea("Meer groen en planten op de speelplaats", new[] { "Een groene speelplaats voelt veel rustiger aan.", "Bomen geven schaduw in de zomer.", "Een moestuintje zou een leuk project zijn." }, 37, new[] { 15, 11, 24 }, archivedPool, minDaysAgo: 150, maxDaysAgo: 230));
        catPlayground.Ideas.Add(CreateIdea("Lijnen voor sportveldjes schilderen", new[] { "Een basketbalveldje zou super zijn.", "Ook voetbal of badminton met vaste lijnen.", "Kost weinig maar maakt veel mogelijk." }, 26, new[] { 10, 19, 6 }, archivedPool, minDaysAgo: 150, maxDaysAgo: 230));
        catPlayground.Ideas.Add(CreateIdea("Aparte rustige zone op de speelplaats", new[] { "Niet iedereen wil lawaai tijdens de pauze.", "Een hoekje om rustig te praten of te lezen.", "Vooral fijn voor wie zich snel overprikkeld voelt." }, 32, new[] { 21, 13, 9 }, archivedPool, minDaysAgo: 150, maxDaysAgo: 230));

        var catRelax = new Category
        {
            Translations = new List<CategoryTranslation>
            {
                new() { Language = languageNl, Name = "Ontspanningsruimtes", Description = "Binnenruimtes om te ontspannen tijdens de pauze" },
                new() { Language = languageEn, Name = "Relaxation areas", Description = "Indoor spaces to relax during breaks" },
            }
        };
        catRelax.Ideas.Add(CreateIdea("Grotere ontspanningsruimte voorzien", new[] { "De huidige ruimte zit altijd vol.", "Veel leerlingen geraken er niet eens binnen.", "Een tweede lokaal zou de druk verlichten." }, 39, new[] { 18, 12, 23 }, archivedPool, minDaysAgo: 150, maxDaysAgo: 230));
        catRelax.Ideas.Add(CreateIdea("Gezelschapsspelletjes en boeken aanbieden", new[] { "Een kast met spelletjes zou de sfeer verbeteren.", "Stripverhalen en boeken om even weg te dromen.", "Schaken tijdens de middag zou leuk zijn." }, 28, new[] { 9, 20, 15 }, archivedPool, minDaysAgo: 150, maxDaysAgo: 230));
        catRelax.Ideas.Add(CreateIdea("Comfortabele zitzakken plaatsen", new[] { "Zitzakken zijn veel gezelliger dan harde stoelen.", "Perfect om even te ontspannen na de lessen.", "Makkelijk te verplaatsen ook." }, 44, new[] { 22, 10, 17 }, archivedPool, minDaysAgo: 150, maxDaysAgo: 230));
        catRelax.Ideas.Add(CreateIdea("Stille studieruimte tijdens de pauze openen", new[] { "Soms wil ik in alle rust mijn huiswerk maken.", "Een aparte stille ruimte zou ideaal zijn.", "Helpt ook wie thuis geen rustige plek heeft." }, 31, new[] { 14, 8, 21 }, archivedPool, minDaysAgo: 150, maxDaysAgo: 230));
        catRelax.Ideas.Add(CreateIdea("Muziek toelaten in de ontspanningsruimte", new[] { "Rustige achtergrondmuziek maakt de sfeer aangenaam.", "Misschien een speellijst die leerlingen mee samenstellen?", "Niet te luid zodat het ontspannend blijft." }, 25, new[] { 11, 19, 7 }, archivedPool, minDaysAgo: 150, maxDaysAgo: 230));
        catRelax.Ideas.Add(CreateIdea("Warme drankjesautomaat toevoegen", new[] { "Een warme thee of chocomelk in de winter zou top zijn.", "Gezonde opties en niet te duur.", "Zou de ontspanningsruimte nog gezelliger maken." }, 36, new[] { 20, 13, 24 }, archivedPool, minDaysAgo: 150, maxDaysAgo: 230));

        projectInactive.Categories.Add(catPlayground);
        projectInactive.Categories.Add(catRelax);

        var projectDraft = new Project
        {
            ProjectState = ProjectState.Draft,
            FormType = FormType.VerticalScroll,
            Questionnaire = new Questionnaire(),
            ShortForm = "draft-project",
            ImageUrl = SeedUrl("projects/youth-workshop.jpg"),
            ProjectPlatform = platformSynapse,
            Theme = ProjectTheme.HighContrast,
            DefaultLanguage = languageNl,
            SupportedLanguages = new List<Language> { languageNl, languageEn },
            Translations = new List<ProjectTranslation>
            {
                new() { Language = languageNl, Name = "Draft Project", Topic = "" },
                new() { Language = languageEn, Name = "Draft Project", Topic = "" },
            },
            Introduction = new Introduction
            {
                Translations = new List<IntroductionTranslation>
                {
                    new() { Language = languageNl, Text = "Dit project is nog in opbouw." },
                    new() { Language = languageEn, Text = "This project is still under construction." },
                }
            }
        };

        context.ProjectPlatforms.AddRange(platformSynapse, platformFitLife, platformEendracht);

        context.Projects.Add(projectActive);
        context.Projects.Add(projectActive2);
        context.Projects.Add(projectActive3);
        context.Projects.Add(projectInactive);
        context.Projects.Add(projectDraft);

        // Enable emojis per project
        var allEmojis = new[] { _heart, _thumbsup, _fire, _check };
        foreach (var emoji in allEmojis)
        {
            projectActive.Emojis.Add(emoji);
            projectActive3.Emojis.Add(emoji);
            projectInactive.Emojis.Add(emoji);
        }
        projectActive2.Emojis.Add(_heart);
        projectActive2.Emojis.Add(_fire);

        context.SaveChanges();
    }
}
