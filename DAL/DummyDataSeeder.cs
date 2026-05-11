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

    private static void AddIconComments(Idea idea, int count, Emoji[] emojis, Participant participant)
    {
        for (int i = 0; i < count; i++)
            idea.Comments.Add(new IconComment { Emoji = emojis[i % emojis.Length], Idea = idea, Participant = participant });
    }

    private static void AddReactions(TextComment comment, Idea idea, int count, Emoji[] emojis, Participant participant)
    {
        for (int i = 0; i < count; i++)
            comment.EmojiReactions.Add(new IconComment { Emoji = emojis[i % emojis.Length], Idea = idea, TextComment = comment, Participant = participant });
    }

    private static Idea CreateIdea(string description, string[] commentTexts, int ideaIconCount, int[] reactionCounts, Participant participant, Emoji[] emojis = null, Language language = null)
    {
        emojis ??= new[] { _heart, _thumbsup, _fire, _check };
        var idea = new Idea { Description = description, Status = ModerationStatus.Approved, Participant = participant, Language = language ?? participant.Language };
        AddIconComments(idea, ideaIconCount, emojis, participant);
        for (int i = 0; i < commentTexts.Length; i++)
        {
            var comment = new TextComment
            {
                Idea = idea,
                Text = commentTexts[i],
                ModerationStatus = ModerationStatus.Approved,
                Participant = participant
            };
            AddReactions(comment, idea, reactionCounts[i % reactionCounts.Length], emojis, participant);
            idea.Comments.Add(comment);
        }
        return idea;
    }

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

        string LogoUrl(string fileName) =>
            apiBaseUrl is not null ? $"{apiBaseUrl}/uploads/logos/{fileName}" : null;

        _heart = new Emoji { Symbol = "❤️", Name = "heart" };
        _thumbsup = new Emoji { Symbol = "👍", Name = "thumbsup" };
        _fire = new Emoji { Symbol = "🔥", Name = "fire" };
        _check = new Emoji { Symbol = "✅", Name = "check" };
        context.Emojis.AddRange(_heart, _thumbsup, _fire, _check);
        context.SaveChanges();

        var platformSynapse = new ProjectPlatform
        {
            Name = "Synapse",
            LogoUrl = LogoUrl("logo_synapse.jpg"),
            WebsiteUrl = "https://synapse-ip.xyz",
            CreatedAt = DateTime.UtcNow
        };
        var platformFitLife = new ProjectPlatform
        {
            Name = "FitLife Gym",
            LogoUrl = LogoUrl("logo_fitlife.png"),
            WebsiteUrl = "https://www.fitlife.com",
            CreatedAt = DateTime.UtcNow
        };
        var platformEendracht = new ProjectPlatform
        {
            Name = "FC Eendracht",
            LogoUrl = LogoUrl("logo_eendracht.png"),
            WebsiteUrl = "https://www.fceendracht.be",
            CreatedAt = DateTime.UtcNow
        };

        // Project 1: Synapse
        var questionnaire1 = new Questionnaire();
        var q1Stress = new Question
        {
            QuestionFormat = QuestionFormat.MultipleChoice, Questionnaire = questionnaire1,
            AnswerChoiceCount = 5, IsMandatory = false, Position = 1,
            Translations = new List<QuestionTranslation>
            {
                new() { Language = languageNl, Text = "Welke factoren zorgen bij jou de meeste stress?", AnswerChoices = new List<string> { "Examens", "Weinig vrije tijd", "Deadlines", "Sociale druk", "Thuissituatie" } },
                new() { Language = languageEn, Text = "Which factors cause you the most stress?", AnswerChoices = new List<string> { "Exams", "Little free time", "Deadlines", "Social pressure", "Home situation" } }
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
            Translations = new List<QuestionTranslation>
            {
                new() { Language = languageNl, Text = "Op een schaal van 1 tot 10, hoeveel schoolstress ervaar jij?" },
                new() { Language = languageEn, Text = "On a scale of 1 to 10, how much school stress do you experience?" }
            }
        };
        var q1Open = new Question
        {
            QuestionFormat = QuestionFormat.Open, Questionnaire = questionnaire1,
            AnswerChoiceCount = 0, IsMandatory = true, Position = 4,
            Translations = new List<QuestionTranslation>
            {
                new() { Language = languageNl, Text = "Wat zou de school volgens jou concreet kunnen doen om het mentaal welzijn van leerlingen te verbeteren?" },
                new() { Language = languageEn, Text = "What do you think the school could concretely do to improve the mental wellbeing of students?" }
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
            ImageUrl = "https://placehold.co/400x225?text=Synapse",
            ProjectPlatform = platformSynapse,
            Theme = ProjectTheme.Default,
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

        var participant1 = new Participant { Project = projectActive, Questionnaire = questionnaire1, HasSubmitted = true, Language = languageNl };
        projectActive.Participants.Add(participant1);

        var categoryCampus = new Category
        {
            Translations = new List<CategoryTranslation>
            {
                new() { Language = languageNl, Name = "Campus welzijn", Description = "Welzijn op school" },
                new() { Language = languageEn, Name = "Campus wellbeing", Description = "Wellbeing at school" },
            }
        };
        categoryCampus.Ideas.Add(CreateIdea("Meer rustplekken op school", new[] { "Geweldig idee, daar hebben we echt nood aan!", "Misschien ook wat groen toevoegen voor een kalme sfeer?", "Een stille ruimte zonder schermen zou perfect zijn." }, 35, new[] { 12, 8, 20 }, participant1));
        categoryCampus.Ideas.Add(CreateIdea("Meditatie sessies tijdens de middag", new[] { "Dit zou mij enorm helpen om de namiddag beter door te komen.", "Kan dit ook online gevolgd worden?", "Misschien ook 's ochtends voor de lessen beginnen?" }, 28, new[] { 15, 7, 18 }, participant1));
        categoryCampus.Ideas.Add(CreateIdea("Anonieme klachtenbox voor leerlingen", new[] { "Eindelijk een veilige manier om problemen te melden!", "Belangrijk dat dit echt anoniem blijft.", "Wordt dit ook opgevolgd door de directie?" }, 42, new[] { 10, 22, 6 }, participant1));
        categoryCampus.Ideas.Add(CreateIdea("Vertrouwensleerkracht meer zichtbaar maken", new[] { "Veel leerlingen weten niet eens wie ze moeten aanspreken.", "Een vast spreekuur zou handig zijn.", "Posters in de gangen zouden al helpen." }, 31, new[] { 9, 14, 25 }, participant1));
        categoryCampus.Ideas.Add(CreateIdea("Korte pauzes tussen de lessen", new[] { "Vijf minuten extra tussen de lessen zou al een groot verschil maken.", "Dit helpt ook om op tijd naar je volgend lokaal te geraken.", "Mijn concentratie zakt altijd na twee uur aan een stuk." }, 47, new[] { 20, 11, 17 }, participant1));
        categoryCampus.Ideas.Add(CreateIdea("Stress management workshops for students", new[] { "Should be mandatory in the first year.", "I really needed this during exam period.", "Can parents also attend?" }, 38, new[] { 13, 19, 8 }, participant1, language: languageEn));
        categoryCampus.Ideas.Add(CreateIdea("Create a greener school environment", new[] { "Plants in classrooms would really improve the atmosphere.", "A school garden as a relaxation project would be great.", "Nature is proven to calm the mind." }, 26, new[] { 16, 7, 23 }, participant1, language: languageEn));
        categoryCampus.Ideas.Add(CreateIdea("Buddy system for new students", new[] { "I really needed this in my first week.", "Also helps build connections across year groups.", "Could this work for students with anxiety too?" }, 33, new[] { 11, 18, 5 }, participant1, language: languageEn));
        categoryCampus.Ideas.Add(CreateIdea("Healthier options in the school cafeteria", new[] { "Food has a big impact on how you feel.", "Less sugar and more fruit would already help.", "Healthy options need to stay affordable." }, 44, new[] { 21, 9, 15 }, participant1, language: languageEn));
        categoryCampus.Ideas.Add(CreateIdea("Sports activities during lunch break", new[] { "Exercise is incredibly effective at reducing stress.", "A ping-pong table or basketball hoop would be enough.", "This also gets students away from their phones." }, 50, new[] { 24, 12, 19 }, participant1, language: languageEn));

        var categoryHome = new Category
        {
            Translations = new List<CategoryTranslation>
            {
                new() { Language = languageNl, Name = "Thuis welzijn", Description = "Welzijn thuis en in je vrije tijd" },
                new() { Language = languageEn, Name = "Home wellbeing", Description = "Wellbeing at home and in your free time" },
            }
        };
        categoryHome.Ideas.Add(CreateIdea("Tips voor een goede nachtrust", new[] { "Ik slaap veel beter sinds ik mijn telefoon wegleg voor het slapengaan.", "Een vast slaapschema helpt enorm!", "Blauwe lichtfilters op je scherm helpen ook." }, 29, new[] { 14, 22, 8 }, participant1));
        categoryHome.Ideas.Add(CreateIdea("Online hulplijn voor thuisproblemen", new[] { "Dit is zo belangrijk, niet iedereen heeft steun thuis.", "Moet ook beschikbaar zijn in het weekend.", "Anoniem kunnen chatten zou de drempel verlagen." }, 46, new[] { 25, 10, 17 }, participant1));
        categoryHome.Ideas.Add(CreateIdea("Bewegingstips voor na school", new[] { "Een wandeling van 20 minuten doet al wonderen.", "Yoga is ook super relaxerend na een drukke dag!", "Fietsen naar school telt ook als beweging." }, 37, new[] { 9, 20, 13 }, participant1));
        categoryHome.Ideas.Add(CreateIdea("Kookworkshop voor gezond eten thuis", new[] { "Leren koken is een levensnoodzakelijke vaardigheid.", "Gezond eten hoeft niet duur te zijn.", "Zou ook goed zijn als project voor huishoudkunde." }, 32, new[] { 16, 7, 24 }, participant1));
        categoryHome.Ideas.Add(CreateIdea("Dagboek bijhouden voor mentale rust", new[] { "Schrijven helpt om gedachten te ordenen.", "Ik doe dit al en het werkt echt!", "Kan ook digitaal via een app." }, 25, new[] { 11, 18, 6 }, participant1));
        categoryHome.Ideas.Add(CreateIdea("Encourage offline hobbies", new[] { "Drawing, music or crafting gives so much satisfaction.", "Putting screens away is easier when you have something else to do.", "School could start a hobby club." }, 40, new[] { 22, 13, 19 }, participant1, language: languageEn));
        categoryHome.Ideas.Add(CreateIdea("Involve parents in wellbeing conversations", new[] { "Home support matters just as much as school support.", "Parent evenings on mental wellbeing would be useful.", "Not all parents understand the pressure students face." }, 27, new[] { 8, 21, 15 }, participant1, language: languageEn));
        categoryHome.Ideas.Add(CreateIdea("Digital detox tips for home", new[] { "One hour a day without a screen does wonders.", "I now put my phone in another room at night.", "Apps that track your screen time also help." }, 43, new[] { 17, 10, 25 }, participant1, language: languageEn));
        categoryHome.Ideas.Add(CreateIdea("Involve friends in your wellbeing", new[] { "Just calling a friend already helps enormously.", "Going for a walk together is better than chatting online.", "Social connection is crucial for mental health." }, 36, new[] { 12, 23, 9 }, participant1, language: languageEn));
        categoryHome.Ideas.Add(CreateIdea("Spend time in nature during free time", new[] { "Visiting a park or forest is very calming.", "Even a garden or balcony with plants helps.", "Research shows that nature reduces stress." }, 48, new[] { 20, 14, 7 }, participant1, language: languageEn));

        var categoryOnline = new Category
        {
            Translations = new List<CategoryTranslation>
            {
                new() { Language = languageNl, Name = "Online welzijn", Description = "Gezond omgaan met social media en online" },
                new() { Language = languageEn, Name = "Online wellbeing", Description = "Healthy use of social media and online platforms" },
            }
        };
        categoryOnline.Ideas.Add(CreateIdea("Workshops over social media gebruik", new[] { "Ik zou graag leren hoe ik minder tijd op mijn telefoon spendeer.", "Dit zou verplicht moeten zijn voor alle leerlingen.", "Ook interessant voor leerkrachten en ouders." }, 39, new[] { 18, 11, 24 }, participant1));
        categoryOnline.Ideas.Add(CreateIdea("Schermtijd begrenzen via schoolapp", new[] { "Goed idee maar ouders moeten het ook steunen.", "Zou handig zijn als je zelf limieten kan instellen.", "Kan dit ook voor sociale media apart ingesteld worden?" }, 45, new[] { 7, 22, 16 }, participant1));
        categoryOnline.Ideas.Add(CreateIdea("Digitale detox dag op school organiseren", new[] { "Een dag zonder telefoon klinkt eerst eng maar achteraf super rustgevend!", "We zouden dan andere activiteiten kunnen doen zoals tekenen of sporten.", "Dit zou ook de klassfeer verbeteren." }, 30, new[] { 13, 19, 10 }, participant1));
        categoryOnline.Ideas.Add(CreateIdea("Bewustwording over vergelijkingscultuur online", new[] { "Instagram en TikTok zorgen voor veel onzekerheid.", "Les over filters en nep-perfectie zou helpen.", "Ik vergelijk mezelf veel minder sinds ik minder scroll." }, 49, new[] { 25, 8, 20 }, participant1));
        categoryOnline.Ideas.Add(CreateIdea("Cyberpesten bespreekbaar maken op school", new[] { "Veel leerlingen durven dit niet melden.", "Een anoniem meldpunt specifiek voor online pesten.", "Leerkrachten moeten ook getraind worden hierin." }, 41, new[] { 15, 21, 9 }, participant1));
        categoryOnline.Ideas.Add(CreateIdea("Teaching positive use of social media", new[] { "Social media can also be inspiring and connecting.", "Learning how to build a healthy online community.", "Explaining the difference between passive scrolling and active use." }, 34, new[] { 11, 17, 23 }, participant1, language: languageEn));
        categoryOnline.Ideas.Add(CreateIdea("Explaining privacy settings to students", new[] { "Most students don't know what they're sharing.", "A workshop on safe internet use is urgently needed.", "Should already be taught in the first year of secondary school." }, 27, new[] { 6, 20, 14 }, participant1, language: languageEn));
        categoryOnline.Ideas.Add(CreateIdea("Learning to handle negative reactions online", new[] { "Hateful comments can really hurt.", "Blocking and reporting should become more normal.", "Teaching emotional resilience online is important." }, 38, new[] { 19, 12, 25 }, participant1, language: languageEn));
        categoryOnline.Ideas.Add(CreateIdea("Phone-free zone during break time", new[] { "Being truly present with each other feels good.", "Would also improve the social atmosphere in class.", "Maybe voluntary first, then mandatory?" }, 52, new[] { 10, 18, 7 }, participant1, language: languageEn));
        categoryOnline.Ideas.Add(CreateIdea("Launch an online wellbeing platform for youth", new[] { "A safe online space where young people can go for help.", "With anonymous chat and professional guidance.", "Could also include tips and exercises." }, 44, new[] { 23, 9, 16 }, participant1, language: languageEn));

        projectActive.Categories.Add(categoryCampus);
        projectActive.Categories.Add(categoryHome);
        projectActive.Categories.Add(categoryOnline);

        // Project 2: FitLife Gym
        var questionnaire2 = new Questionnaire();
        questionnaire2.Questions.Add(new Question
        {
            QuestionFormat = QuestionFormat.MultipleChoice, Questionnaire = questionnaire2,
            AnswerChoiceCount = 5, IsMandatory = false, Position = 1,
            Translations = new List<QuestionTranslation>
            {
                new() { Language = languageNl, Text = "Welke aspecten van onze dienstverlening waardeert u het meest?", AnswerChoices = new List<string> { "Klantenservice", "Prijs-kwaliteit", "Snelheid", "Gebruiksgemak", "Productaanbod" } },
                new() { Language = languageEn, Text = "Which aspects of our service do you value most?", AnswerChoices = new List<string> { "Customer service", "Value for money", "Speed", "Ease of use", "Product range" } }
            }
        });
        questionnaire2.Questions.Add(new Question
        {
            QuestionFormat = QuestionFormat.SingleChoice, Questionnaire = questionnaire2,
            AnswerChoiceCount = 4, IsMandatory = true, Position = 2,
            Translations = new List<QuestionTranslation>
            {
                new() { Language = languageNl, Text = "Hoe tevreden bent u overall met onze service?", AnswerChoices = new List<string> { "Zeer tevreden", "Tevreden", "Neutraal", "Ontevreden" } },
                new() { Language = languageEn, Text = "How satisfied are you overall with our service?", AnswerChoices = new List<string> { "Very satisfied", "Satisfied", "Neutral", "Dissatisfied" } }
            }
        });
        questionnaire2.Questions.Add(new Question
        {
            QuestionFormat = QuestionFormat.Likert, Questionnaire = questionnaire2,
            AnswerChoiceCount = 0, IsMandatory = true, Position = 3,
            Translations = new List<QuestionTranslation>
            {
                new() { Language = languageNl, Text = "Op een schaal van 1 tot 10, hoe waarschijnlijk is het dat u ons aanbeveelt?" },
                new() { Language = languageEn, Text = "On a scale of 1 to 10, how likely are you to recommend us?" }
            }
        });
        questionnaire2.Questions.Add(new Question
        {
            QuestionFormat = QuestionFormat.Open, Questionnaire = questionnaire2,
            AnswerChoiceCount = 0, IsMandatory = false, Position = 4,
            Translations = new List<QuestionTranslation>
            {
                new() { Language = languageNl, Text = "Wat kunnen wij doen om uw ervaring te verbeteren?" },
                new() { Language = languageEn, Text = "What can we do to improve your experience?" }
            }
        });

        var projectActive2 = new Project
        {
            ProjectState = ProjectState.Active,
            FormType = FormType.VerticalScroll,
            AllowCrossLanguageIdeas = false,
            Questionnaire = questionnaire2,
            ShortForm = "ledenbevraging",
            ImageUrl = "https://placehold.co/400x225?text=FitLife",
            ProjectPlatform = platformFitLife,
            Theme = ProjectTheme.Playful,
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

        var participant2 = new Participant { Project = projectActive2, Questionnaire = questionnaire2, HasSubmitted = true, Language = languageNl };
        projectActive2.Participants.Add(participant2);

        var fitLifeEmojis = new[] { _heart, _fire };

        var catFacilities = new Category
        {
            Translations = new List<CategoryTranslation>
            {
                new() { Language = languageNl, Name = "Faciliteiten", Description = "Kleedkamers, toestellen en ruimtes" },
                new() { Language = languageEn, Name = "Facilities", Description = "Changing rooms, equipment and spaces" },
            }
        };
        catFacilities.Ideas.Add(CreateIdea("Meer cardio toestellen in de ochtendzone", new[] { "De rijen aan de loopbanden zijn 's ochtends niet te doen.", "Misschien een reservatiesysteem voor piekuren?", "Roeiers en steppers zijn ook altijd bezet." }, 38, new[] { 14, 9, 22 }, participant2, fitLifeEmojis));
        catFacilities.Ideas.Add(CreateIdea("Sauna toevoegen aan de wellnesszone", new[] { "Dit zou het lidmaatschap zoveel meer waard maken!", "Een infraroodsauna zou ideaal zijn voor spierherstel.", "Andere gyms hebben dit al standaard." }, 45, new[] { 20, 11, 17 }, participant2, fitLifeEmojis));
        catFacilities.Ideas.Add(CreateIdea("Betere ventilatie in de gewichtszaal", new[] { "Het is veel te warm en benauwd tijdens piekuren.", "Airconditioning zou een groot verschil maken.", "Extra ventilatoren als tussenoplossing?" }, 32, new[] { 8, 19, 13 }, participant2, fitLifeEmojis));
        catFacilities.Ideas.Add(CreateIdea("Ruimere kleedkamers voor dames", new[] { "Nu is het bijna onmogelijk om je te kleden tijdens spitsuren.", "Meer kluisjes zouden ook welkom zijn.", "Een aparte ruimte voor föhnen zou handig zijn." }, 29, new[] { 15, 7, 24 }, participant2, fitLifeEmojis));
        catFacilities.Ideas.Add(CreateIdea("Kinderhoek toevoegen aan de gym", new[] { "Dan kunnen ouders sporten terwijl de kinderen spelen.", "Zou veel meer jonge gezinnen aantrekken.", "Een begeleider voor de kinderen zou ideaal zijn." }, 36, new[] { 11, 20, 6 }, participant2, fitLifeEmojis));
        catFacilities.Ideas.Add(CreateIdea("Gratis parkeerplaats voor leden", new[] { "Nu is parkeren een extra kost bovenop het lidmaatschap.", "Zeker 's avonds is dit een probleem.", "Een samenwerking met de parking naast de gym?" }, 41, new[] { 18, 12, 25 }, participant2, fitLifeEmojis));
        catFacilities.Ideas.Add(CreateIdea("Smoothiebar toevoegen aan de receptie", new[] { "Gezonde snacks na het sporten zouden perfect zijn.", "Proteïneshakes en vers fruit als basis.", "Zou ook een leuke sociale plek worden." }, 27, new[] { 9, 16, 21 }, participant2, fitLifeEmojis));
        catFacilities.Ideas.Add(CreateIdea("Meer spiegels in de vrije gewichtszaal", new[] { "Spiegels zijn essentieel voor techniekcontrole.", "Nu moet je soms in rare hoeken staan.", "Zeker aan de dumbbell rek zijn er te weinig." }, 33, new[] { 13, 22, 8 }, participant2, fitLifeEmojis));
        catFacilities.Ideas.Add(CreateIdea("Wifi verbeteren in de sporthal", new[] { "Het signaal valt constant weg terwijl ik muziek stream.", "Wifi is tegenwoordig een basisverwachting.", "Zou ook nuttig zijn voor de personal trainers." }, 44, new[] { 17, 10, 19 }, participant2, fitLifeEmojis));
        catFacilities.Ideas.Add(CreateIdea("Massagestoel toevoegen aan de relaxzone", new[] { "Na een zware training is dit de perfecte beloning.", "Zelfs een betaalde massagestoel zou ik gebruiken.", "Zou de gym onderscheiden van de concurrentie." }, 50, new[] { 24, 14, 7 }, participant2, fitLifeEmojis));

        var catLessons = new Category
        {
            Translations = new List<CategoryTranslation>
            {
                new() { Language = languageNl, Name = "Lessen & Begeleiding", Description = "Groepslessen en personal training" },
                new() { Language = languageEn, Name = "Classes & Coaching", Description = "Group classes and personal training" },
            }
        };
        catLessons.Ideas.Add(CreateIdea("Yogales toevoegen aan het weekendrooster", new[] { "In het weekend heb ik eindelijk tijd voor yoga!", "Liefst vroeg in de ochtend zodat de rest van de dag vrij is.", "Ook een meditatieklas zou geweldig zijn." }, 39, new[] { 16, 23, 9 }, participant2, fitLifeEmojis));
        catLessons.Ideas.Add(CreateIdea("Personal training sessies goedkoper maken", new[] { "Nu is het onbetaalbaar voor studenten.", "Misschien een studentenkorting invoeren?", "Duo sessies zouden ook goedkoper zijn per persoon." }, 43, new[] { 12, 19, 25 }, participant2, fitLifeEmojis));
        catLessons.Ideas.Add(CreateIdea("Online lessen streamen voor leden", new[] { "Als ik ziek ben kan ik toch nog de les volgen.", "Zou ook handig zijn voor mensen met drukke schema's.", "Opnames achteraf bekijken zou ook fijn zijn." }, 31, new[] { 8, 14, 20 }, participant2, fitLifeEmojis));
        catLessons.Ideas.Add(CreateIdea("Beginnerscursus voor nieuwe leden", new[] { "Nu word je gewoon losgelaten in de gym zonder uitleg.", "Een rondleiding en basisuitleg zou het verschil maken.", "Veel mensen stoppen omdat ze niet weten hoe te starten." }, 46, new[] { 22, 11, 17 }, participant2, fitLifeEmojis));
        catLessons.Ideas.Add(CreateIdea("Voedingsadvies opnemen in het lidmaatschap", new[] { "Sport en voeding gaan hand in hand.", "Zelfs een basisconsultatie zou al enorm helpen.", "Nu moet je dat apart en duur betalen." }, 35, new[] { 10, 21, 6 }, participant2, fitLifeEmojis));
        catLessons.Ideas.Add(CreateIdea("Dans- en zumbaklassen toevoegen", new[] { "Zumba is superleuk en verbrand veel calorieën.", "Zou ook meer vrouwelijke leden aantrekken.", "Een wekelijkse dansles op vrijdagavond zou gezellig zijn." }, 28, new[] { 15, 9, 23 }, participant2, fitLifeEmojis));
        catLessons.Ideas.Add(CreateIdea("Boksklassen voor beginners", new[] { "Boksen is een geweldige stressafvoer.", "Een veilige omgeving voor beginners is belangrijk.", "Zou ook zelfverdediging kunnen combineren." }, 42, new[] { 19, 13, 7 }, participant2, fitLifeEmojis));
        catLessons.Ideas.Add(CreateIdea("Maandelijkse fitnessdoelen bijhouden met trainer", new[] { "Progressie bijhouden motiveert enorm.", "Een korte check-in van 15 minuten per maand zou al genoeg zijn.", "Nu weet ik niet of ik vooruitga." }, 37, new[] { 11, 18, 24 }, participant2, fitLifeEmojis));
        catLessons.Ideas.Add(CreateIdea("Senior-vriendelijke lessen toevoegen", new[] { "Ouderen hebben andere noden dan jonge sporters.", "Lage impact cardio en stretching zouden perfect zijn.", "Zou ook nieuwe doelgroep aantrekken." }, 26, new[] { 20, 8, 16 }, participant2, fitLifeEmojis));
        catLessons.Ideas.Add(CreateIdea("Aqua aerobics in het zwembad", new[] { "Gewrichtsvriendelijk sporten voor iedereen.", "Ideaal voor mensen met rugklachten.", "Het zwembad wordt nu ondergecapaciteit gebruikt." }, 48, new[] { 14, 22, 10 }, participant2, fitLifeEmojis));

        var catSchedule = new Category
        {
            Translations = new List<CategoryTranslation>
            {
                new() { Language = languageNl, Name = "Planning & Openingsuren", Description = "Bereikbaarheid en rooster" },
                new() { Language = languageEn, Name = "Schedule & Opening Hours", Description = "Accessibility and timetable" },
            }
        };
        catSchedule.Ideas.Add(CreateIdea("Gym ook open op zondagavond", new[] { "Zondagavond is perfect om de week voor te bereiden.", "Nu moet ik altijd wachten tot maandag.", "Zelfs maar tot 20u zou al een groot verschil maken." }, 40, new[] { 17, 11, 25 }, participant2, fitLifeEmojis));
        catSchedule.Ideas.Add(CreateIdea("App voor reservaties van toestellen", new[] { "Dan weet je op voorhand of het druk is.", "Wachtrijen zijn frustrerend na een lange werkdag.", "Zou ook de verdeling van leden over de dag verbeteren." }, 34, new[] { 9, 20, 14 }, participant2, fitLifeEmojis));
        catSchedule.Ideas.Add(CreateIdea("Vroeger openen op weekdagen", new[] { "Om 5u30 open zou mij toelaten voor het werk te sporten.", "Veel mensen willen 's ochtends vroeg trainen.", "Nu mis ik de ochtendtrein als ik eerst naar de gym ga." }, 47, new[] { 23, 12, 8 }, participant2, fitLifeEmojis));
        catSchedule.Ideas.Add(CreateIdea("Wachtlijst systeem voor populaire lessen", new[] { "Lessen zijn vaak volgeboekt tegen dat ik kan inschrijven.", "Een automatische notificatie als er een plekje vrijkomt.", "Nu kom ik soms voor niets." }, 29, new[] { 16, 7, 19 }, participant2, fitLifeEmojis));
        catSchedule.Ideas.Add(CreateIdea("Rustigere tijdslots aanduiden op de app", new[] { "Ik wil weten wanneer de gym relatief leeg is.", "Nu is het altijd een verrassing of het druk is.", "Zou mijn trainingservaring veel aangenamer maken." }, 38, new[] { 13, 21, 6 }, participant2, fitLifeEmojis));
        catSchedule.Ideas.Add(CreateIdea("Feestdagen openingsuren beter communiceren", new[] { "Ik ben al een paar keer voor een gesloten deur gestaan.", "Een automatische e-mail of pushmelding zou helpen.", "Op de website staat het soms verkeerd." }, 25, new[] { 10, 18, 22 }, participant2, fitLifeEmojis));
        catSchedule.Ideas.Add(CreateIdea("Langere openingsuren in het weekend", new[] { "Nu sluit de gym al om 18u op zondag.", "Het weekend is voor velen de enige tijd om lang te sporten.", "Zelfs tot 21u zou al een groot verschil maken." }, 43, new[] { 15, 9, 24 }, participant2, fitLifeEmojis));
        catSchedule.Ideas.Add(CreateIdea("Persoonlijk trainingsrooster in de app", new[] { "Nu moet ik alles zelf bijhouden in een notitieblok.", "Een digitaal schema met herinneringen zou perfect zijn.", "Integratie met mijn agenda zou ideaal zijn." }, 36, new[] { 20, 11, 17 }, participant2, fitLifeEmojis));
        catSchedule.Ideas.Add(CreateIdea("Druktemeter aan de ingang installeren", new[] { "Dan kan ik beslissen of ik nu ga of later.", "Sommige gyms hebben dit al en het werkt geweldig.", "Zou ook via de app beschikbaar kunnen zijn." }, 31, new[] { 8, 19, 13 }, participant2, fitLifeEmojis));
        catSchedule.Ideas.Add(CreateIdea("Speciale ochtendpas voor vroege sporters", new[] { "Een goedkopere pas voor enkel ochtenduren zou perfect zijn.", "Dan zijn de toestellen minder bezet en ik betaal minder.", "Win-win voor de gym en de leden." }, 52, new[] { 25, 14, 20 }, participant2, fitLifeEmojis));

        projectActive2.Categories.Add(catFacilities);
        projectActive2.Categories.Add(catLessons);
        projectActive2.Categories.Add(catSchedule);

        // Project 3: FC Eendracht
        var questionnaire3 = new Questionnaire();
        questionnaire3.Questions.Add(new Question
        {
            QuestionFormat = QuestionFormat.MultipleChoice, Questionnaire = questionnaire3,
            AnswerChoiceCount = 5, IsMandatory = false, Position = 1,
            Translations = new List<QuestionTranslation>
            {
                new() { Language = languageNl, Text = "Welke aspecten van de club wil je verbeteren?", AnswerChoices = new List<string> { "Trainingen", "Infrastructuur", "Communicatie", "Sociale activiteiten", "Wedstrijdplanning" } },
                new() { Language = languageEn, Text = "Which aspects of the club do you want to improve?", AnswerChoices = new List<string> { "Training", "Infrastructure", "Communication", "Social activities", "Match schedule" } }
            }
        });
        questionnaire3.Questions.Add(new Question
        {
            QuestionFormat = QuestionFormat.SingleChoice, Questionnaire = questionnaire3,
            AnswerChoiceCount = 4, IsMandatory = true, Position = 2,
            Translations = new List<QuestionTranslation>
            {
                new() { Language = languageNl, Text = "Hoe beoordeel je de algemene sfeer in de club?", AnswerChoices = new List<string> { "Uitstekend", "Goed", "Matig", "Slecht" } },
                new() { Language = languageEn, Text = "How do you rate the general atmosphere in the club?", AnswerChoices = new List<string> { "Excellent", "Good", "Average", "Poor" } }
            }
        });
        questionnaire3.Questions.Add(new Question
        {
            QuestionFormat = QuestionFormat.Likert, Questionnaire = questionnaire3,
            AnswerChoiceCount = 0, IsMandatory = true, Position = 3,
            Translations = new List<QuestionTranslation>
            {
                new() { Language = languageNl, Text = "Op een schaal van 1 tot 10, hoe tevreden ben je over de trainingen?" },
                new() { Language = languageEn, Text = "On a scale of 1 to 10, how satisfied are you with the training sessions?" }
            }
        });
        questionnaire3.Questions.Add(new Question
        {
            QuestionFormat = QuestionFormat.Open, Questionnaire = questionnaire3,
            AnswerChoiceCount = 0, IsMandatory = false, Position = 4,
            Translations = new List<QuestionTranslation>
            {
                new() { Language = languageNl, Text = "Wat zou jij veranderen aan de club als je de kans had?" },
                new() { Language = languageEn, Text = "What would you change about the club if you had the chance?" }
            }
        });

        var projectActive3 = new Project
        {
            ProjectState = ProjectState.Active,
            FormType = FormType.VerticalScroll,
            Questionnaire = questionnaire3,
            ShortForm = "clubbevraging",
            ImageUrl = "https://placehold.co/400x225?text=FC+Eendracht",
            ProjectPlatform = platformEendracht,
            Theme = ProjectTheme.Professional,
            PrimaryColor = "#16A34A",
            DefaultLanguage = languageNl,
            SupportedLanguages = new List<Language> { languageNl, languageEn },
            Translations = new List<ProjectTranslation>
            {
                new() { Language = languageNl, Name = "Clubbevraging", Topic = "De club verbeteren" },
                new() { Language = languageEn, Name = "Club Survey", Topic = "Improving the club" },
            },
        };

        var participant3 = new Participant { Project = projectActive3, Questionnaire = questionnaire3, HasSubmitted = true, Language = languageNl };
        projectActive3.Participants.Add(participant3);

        var catTraining = new Category
        {
            Translations = new List<CategoryTranslation>
            {
                new() { Language = languageNl, Name = "Trainingen", Description = "Kwaliteit en opbouw van de trainingen" },
                new() { Language = languageEn, Name = "Training", Description = "Quality and structure of training sessions" },
            }
        };
        catTraining.Ideas.Add(CreateIdea("Videoanalyse invoeren bij de trainingen", new[] { "Profclubs doen dit al jaren, waarom wij niet?", "Zou mijn positiespel enorm verbeteren.", "Een simpele tablet met een app zou al voldoende zijn." }, 44, new[] { 18, 11, 25 }, participant3));
        catTraining.Ideas.Add(CreateIdea("Aparte trainingsgroep voor gevorderde spelers", new[] { "Nu wachten gevorderde spelers te veel op beginners.", "Iedereen traint op zijn eigen niveau, eerlijker voor iedereen.", "Zolang het de teamgeest niet schaadt, goed idee." }, 37, new[] { 13, 20, 8 }, participant3));
        catTraining.Ideas.Add(CreateIdea("Conditietraining twee keer per week", new[] { "Nu focussen we te veel op tactiek en te weinig op conditie.", "Een aparte conditietrainer zou ideaal zijn.", "Veel spelers zijn in de tweede helft uitgeput." }, 41, new[] { 9, 17, 22 }, participant3));
        catTraining.Ideas.Add(CreateIdea("Individuele techniektraining voor jongeren", new[] { "Jonge spelers hebben meer individuele aandacht nodig.", "Een techniektraining naast de groepstraining zou perfect zijn.", "Zou de jeugdopleiding enorm versterken." }, 29, new[] { 15, 23, 7 }, participant3));
        catTraining.Ideas.Add(CreateIdea("Gasttrainer uitnodigen eens per maand", new[] { "Een frisse blik van buitenaf kan veel losmaken.", "Zou ook motiverend zijn voor de spelers.", "Misschien een oud-profvoetballer uitnodigen?" }, 48, new[] { 21, 12, 19 }, participant3));
        catTraining.Ideas.Add(CreateIdea("Warming-up protocol standaardiseren", new[] { "Nu doet iedereen maar wat voor de training.", "Een goede warming-up vermindert blessures.", "De FIFA 11+ methode is bewezen effectief." }, 33, new[] { 10, 18, 24 }, participant3));
        catTraining.Ideas.Add(CreateIdea("Mentale weerbaarheid trainen", new[] { "Voetbal is ook een mentale sport.", "Een sportpsycholoog uitnodigen zou waardevol zijn.", "Veel wedstrijden worden gewonnen of verloren in het hoofd." }, 26, new[] { 16, 9, 21 }, participant3));
        catTraining.Ideas.Add(CreateIdea("Trainingsschema digitaal beschikbaar stellen", new[] { "Nu weet ik pas op de dag zelf wat we gaan doen.", "Een weekoverzicht via een app zou perfect zijn.", "Zou ook helpen om je voor te bereiden." }, 35, new[] { 14, 22, 6 }, participant3));
        catTraining.Ideas.Add(CreateIdea("Kleine groepsoefeningen meer integreren", new[] { "Rondjes lopen is saai, kleine spelvormen zijn leuker.", "Leert ook sneller samenspelen.", "Profteams trainen bijna uitsluitend zo." }, 42, new[] { 19, 8, 25 }, participant3));
        catTraining.Ideas.Add(CreateIdea("Keeperstraining apart organiseren", new[] { "De keeper wordt nu vergeten tijdens veldtrainingen.", "Een keeperscoach zou een enorm verschil maken.", "Zelfs één extra training per week zou helpen." }, 50, new[] { 23, 15, 11 }, participant3));

        var catInfra = new Category
        {
            Translations = new List<CategoryTranslation>
            {
                new() { Language = languageNl, Name = "Infrastructuur", Description = "Velden, kleedkamers en materiaal" },
                new() { Language = languageEn, Name = "Infrastructure", Description = "Pitches, changing rooms and equipment" },
            }
        };
        catInfra.Ideas.Add(CreateIdea("Nieuw kunstgrasveld aanleggen", new[] { "Het huidige veld is in slechte staat na de winter.", "Kunstgras betekent ook spelen bij slecht weer.", "Subsidies aanvragen bij de gemeente?" }, 46, new[] { 20, 13, 9 }, participant3));
        catInfra.Ideas.Add(CreateIdea("Betere verlichting op het trainingsveld", new[] { "In de winter trainen we bijna in het donker.", "LED verlichting is energiezuinig en helder.", "Dit verhoogt ook de veiligheid tijdens avondtrainingen." }, 38, new[] { 12, 24, 17 }, participant3));
        catInfra.Ideas.Add(CreateIdea("Nieuwe ballen aanschaffen", new[] { "De huidige ballen zijn versleten en onregelmatig.", "Goede materialen motiveren spelers.", "Misschien sponsors zoeken voor nieuwe ballen?" }, 31, new[] { 8, 19, 14 }, participant3));
        catInfra.Ideas.Add(CreateIdea("Kleedkamers renoveren", new[] { "De douches zijn verouderd en soms koud.", "Een propere kleedkamer creëert een professionele sfeer.", "Zou ook nieuwe leden aantrekken." }, 43, new[] { 22, 10, 18 }, participant3));
        catInfra.Ideas.Add(CreateIdea("Tribunes overdekken", new[] { "Supporters staan in de regen bij thuiswedstrijden.", "Een eenvoudig dak zou al het verschil maken.", "Zou ook meer supporters aantrekken." }, 27, new[] { 11, 20, 7 }, participant3));
        catInfra.Ideas.Add(CreateIdea("Materiaalberging uitbreiden", new[] { "Nu liggen ballen en hesjes overal.", "Een georganiseerde berging bespaart veel tijd.", "Zou ook beschadiging van materiaal voorkomen." }, 36, new[] { 16, 9, 23 }, participant3));
        catInfra.Ideas.Add(CreateIdea("Scorebord installeren", new[] { "Nu weten supporters de score soms niet.", "Een digitaal scorebord zou professioneler ogen.", "Zou ook de wedstrijdervaring verbeteren." }, 25, new[] { 13, 21, 6 }, participant3));
        catInfra.Ideas.Add(CreateIdea("Fietsenstalling bouwen bij het stadion", new[] { "Nu worden fietsen overal gestald.", "Een overdekte stalling zou gewaardeerd worden.", "Duurzaam verplaatsen stimuleren." }, 40, new[] { 17, 11, 25 }, participant3));
        catInfra.Ideas.Add(CreateIdea("Ehbo-post moderniseren", new[] { "De huidige ehbo-kit is onvolledig.", "Een defibrillator is tegenwoordig noodzakelijk.", "Zou ook het vertrouwen van ouders vergroten." }, 34, new[] { 9, 18, 22 }, participant3));
        catInfra.Ideas.Add(CreateIdea("Oefenruimte voor keeperstraining", new[] { "Nu trainen keepers tussen de veldspelers.", "Een aparte ruimte met doelen en matten zou ideaal zijn.", "Kleine investering met grote impact." }, 49, new[] { 24, 14, 8 }, participant3));

        var catSocial = new Category
        {
            Translations = new List<CategoryTranslation>
            {
                new() { Language = languageNl, Name = "Sociale activiteiten", Description = "Teambuilding en clubactiviteiten" },
                new() { Language = languageEn, Name = "Social activities", Description = "Team building and club events" },
            }
        };
        catSocial.Ideas.Add(CreateIdea("Jaarlijks clubkampioenschap organiseren", new[] { "Een intern tornooi zou de teamgeest enorm boosten!", "Met een trofee en feestje achteraf!", "Families uitnodigen als supporters zou gezellig zijn." }, 45, new[] { 19, 12, 25 }, participant3));
        catSocial.Ideas.Add(CreateIdea("Teambuilding weekend plannen", new[] { "Een weekend weg versterkt de band enorm.", "Escape room of outdoor activiteit zou perfect zijn.", "Hoeft niet duur te zijn als iedereen bijdraagt." }, 38, new[] { 14, 22, 9 }, participant3));
        catSocial.Ideas.Add(CreateIdea("Clubavond voor spelers en supporters", new[] { "Een quiz of feestje zou iedereen samenbrengen.", "Nu kennen spelers en supporters elkaar nauwelijks.", "Zou ook nieuwe vrijwilligers aantrekken." }, 32, new[] { 10, 17, 21 }, participant3));
        catSocial.Ideas.Add(CreateIdea("Nieuwjaarsreceptie voor alle leden", new[] { "Een traditionele receptie versterkt het clubgevoel.", "Met terugblik op het seizoen en vooruitblik.", "Zou ook sponsors kunnen uitnodigen." }, 27, new[] { 8, 20, 13 }, participant3));
        catSocial.Ideas.Add(CreateIdea("Jeugd en senioren samen laten trainen", new[] { "Jongeren leren van ervaren spelers.", "Zou ook de doorstroom naar het eerste elftal verbeteren.", "Een gezamenlijke training per maand zou al genoeg zijn." }, 41, new[] { 16, 11, 24 }, participant3));
        catSocial.Ideas.Add(CreateIdea("Clubmagazine of nieuwsbrief opstarten", new[] { "Wedstrijdverslagen en clubnieuws voor alle leden.", "Zou ook de betrokkenheid van supporters vergroten.", "Een digitale versie is gratis en makkelijk te verspreiden." }, 29, new[] { 12, 19, 7 }, participant3));
        catSocial.Ideas.Add(CreateIdea("Vrijwilligersdag organiseren", new[] { "De club draait op vrijwilligers en die verdienen waardering.", "Een bedankfeestje of etentje zou veel betekenen.", "Zou ook nieuwe vrijwilligers motiveren." }, 36, new[] { 23, 9, 18 }, participant3));
        catSocial.Ideas.Add(CreateIdea("Supporters club oprichten", new[] { "Nu is er geen officiële supportersgroep.", "Een supportersclub zou de sfeer bij wedstrijden verbeteren.", "Zou ook extra inkomsten genereren voor de club." }, 44, new[] { 15, 21, 6 }, participant3));
        catSocial.Ideas.Add(CreateIdea("Kerstdiner voor het volledige team", new[] { "Een jaarlijks diner brengt iedereen samen.", "Zou ook een moment zijn om te reflecteren op het jaar.", "Met awards voor beste speler, meeste inzet etc." }, 33, new[] { 11, 25, 14 }, participant3));
        catSocial.Ideas.Add(CreateIdea("Open trainingsdag voor families", new[] { "Families zien graag waarmee hun kind bezig is.", "Zou ook nieuwe leden aantrekken.", "Een kleine demonstratie aan het einde zou leuk zijn." }, 50, new[] { 20, 13, 22 }, participant3));

        projectActive3.Categories.Add(catTraining);
        projectActive3.Categories.Add(catInfra);
        projectActive3.Categories.Add(catSocial);

        // Inactive & Draft
        var projectInactive = new Project
        {
            ProjectState = ProjectState.Archived,
            FormType = FormType.VerticalScroll,
            Questionnaire = new Questionnaire(),
            ShortForm = "gearchiveerd-project",
            ProjectPlatform = platformSynapse,
            Theme = ProjectTheme.Dark,
            DefaultLanguage = languageNl,
            SupportedLanguages = new List<Language> { languageNl, languageEn },
            Translations = new List<ProjectTranslation>
            {
                new() { Language = languageNl, Name = "Gearchiveerd Project", Topic = "" },
                new() { Language = languageEn, Name = "Archived Project", Topic = "" },
            },
            Introduction = new Introduction
            {
                Translations = new List<IntroductionTranslation>
                {
                    new() { Language = languageNl, Text = "Dit project is momenteel inactief." },
                    new() { Language = languageEn, Text = "This project is currently inactive." },
                }
            }
        };

        var projectDraft = new Project
        {
            ProjectState = ProjectState.Draft,
            FormType = FormType.VerticalScroll,
            Questionnaire = new Questionnaire(),
            ShortForm = "draft-project",
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
        }
        projectActive2.Emojis.Add(_heart);
        projectActive2.Emojis.Add(_fire);

        context.SaveChanges();
    }
}
