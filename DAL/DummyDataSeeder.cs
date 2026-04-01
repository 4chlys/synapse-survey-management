using SynapseEnqueteManagement.BL.Domain.Project.Entities;
using SynapseEnqueteManagement.BL.Domain.Project.Enums;

namespace SynapseEnqueteManagement.DAL;

public class DummyDataSeeder
{
    private static Emoji _heart = null!;
    private static Emoji _thumbsup = null!;
    private static Emoji _fire = null!;
    private static Emoji _check = null!;

    private static void AddIconComments(Idea idea, int count, Emoji[] emojis)
    {
        for (int i = 0; i < count; i++)
            idea.Comments.Add(new IconComment { Emoji = emojis[i % emojis.Length], Idea = idea });
    }

    private static void AddReactions(TextComment comment, Idea idea, int count, Emoji[] emojis)
    {
        for (int i = 0; i < count; i++)
            comment.EmojiReactions.Add(new IconComment { Emoji = emojis[i % emojis.Length], Idea = idea, TextComment = comment });
    }

    private static Idea CreateIdea(string description, string[] commentTexts, int ideaIconCount, int[] reactionCounts, Emoji[] emojis = null)
    {
        emojis ??= new[] { _heart, _thumbsup, _fire, _check };
        var idea = new Idea { Description = description, Status = ModerationStatus.Approved };
        AddIconComments(idea, ideaIconCount, emojis);
        for (int i = 0; i < commentTexts.Length; i++)
        {
            var comment = new TextComment
            {
                Idea = idea,
                Text = commentTexts[i],
                ModerationStatus = ModerationStatus.Approved
            };
            AddReactions(comment, idea, reactionCounts[i % reactionCounts.Length], emojis);
            idea.Comments.Add(comment);
        }
        return idea;
    }

    public static void Seed(SynapseEnqueteContext context)
    {
        if (context.Projects.Any()) return;

        _heart = new Emoji { Symbol = "❤️", Name = "heart" };
        _thumbsup = new Emoji { Symbol = "👍", Name = "thumbsup" };
        _fire = new Emoji { Symbol = "🔥", Name = "fire" };
        _check = new Emoji { Symbol = "✅", Name = "check" };

        // ── Project 1: Synapse ──
        var questionnaire1 = new Questionnaire();
        var q1Stress = new Question { QuestionFormat = QuestionFormat.MultipleChoice, Questionnaire = questionnaire1, Text = "Welke factoren zorgen bij jou de meeste stress?", AnswerChoices = new List<string> { "Examens", "Weinig vrije tijd", "Deadlines", "Sociale druk", "Thuissituatie" }, IsMandatory = false, Position = 1 };
        var q1Welzijn = new Question { QuestionFormat = QuestionFormat.SingleChoice, Questionnaire = questionnaire1, Text = "Hoe ervaar jij je mentaal welzijn op school de afgelopen maand?", AnswerChoices = new List<string> { "Goed", "Neutraal", "Slecht" }, IsMandatory = true, Position = 2 };
        var q1Likert = new Question { QuestionFormat = QuestionFormat.Likert, Questionnaire = questionnaire1, Text = "Op een schaal van 1 tot 10, hoeveel schoolstress ervaar jij?", IsMandatory = true, Position = 3 };
        var q1Open = new Question { QuestionFormat = QuestionFormat.Open, Questionnaire = questionnaire1, Text = "Wat zou de school volgens jou concreet kunnen doen om het mentaal welzijn van leerlingen te verbeteren?", IsMandatory = true, Position = 4 };
        questionnaire1.Questions.Add(q1Stress);
        questionnaire1.Questions.Add(q1Welzijn);
        questionnaire1.Questions.Add(q1Likert);
        questionnaire1.Questions.Add(q1Open);

        // Branch: if welzijn = "Goed", skip the stress likert and go straight to the open question
        q1Welzijn.BranchRules.Add(new BranchRule { SourceQuestion = q1Welzijn, TriggerValue = "Goed", TargetQuestion = q1Open });

        var projectActive = new Project
        {
            ProjectState = ProjectState.Active,
            FormType = FormType.VerticalScroll,
            RequestContactInfo = true,
            Questionnaire = questionnaire1,
            OrganisationName = "Synapse",
            OrganisationLogoUrl = "https://placehold.co/120x40?text=Synapse",
            OrganisationWebsiteUrl = "https://www.synapse.com",
            Introduction = new Introduction
            {
                Text = "Welkom bij de Synapse enquête over mentaal welzijn op school. " +
                       "Deze enquête peilt naar jouw ervaringen en ideeën rond stress, welzijn en wat de school beter kan doen. " +
                       "Jouw antwoorden zijn anoniem en helpen ons om een betere schoolomgeving te creëren. " +
                       "Het invullen duurt ongeveer 5 minuten."
            }
        };

        var categoryCampus = new Category { Name = "Campus welzijn", Description = "Welzijn op school" };
        categoryCampus.Ideas.Add(CreateIdea("Meer rustplekken op school", new[] { "Geweldig idee, daar hebben we echt nood aan!", "Misschien ook wat groen toevoegen voor een kalme sfeer?", "Een stille ruimte zonder schermen zou perfect zijn." }, 35, new[] { 12, 8, 20 }));
        categoryCampus.Ideas.Add(CreateIdea("Meditatie sessies tijdens de middag", new[] { "Dit zou mij enorm helpen om de namiddag beter door te komen.", "Kan dit ook online gevolgd worden?", "Misschien ook 's ochtends voor de lessen beginnen?" }, 28, new[] { 15, 7, 18 }));
        categoryCampus.Ideas.Add(CreateIdea("Anonieme klachtenbox voor leerlingen", new[] { "Eindelijk een veilige manier om problemen te melden!", "Belangrijk dat dit echt anoniem blijft.", "Wordt dit ook opgevolgd door de directie?" }, 42, new[] { 10, 22, 6 }));
        categoryCampus.Ideas.Add(CreateIdea("Vertrouwensleerkracht meer zichtbaar maken", new[] { "Veel leerlingen weten niet eens wie ze moeten aanspreken.", "Een vast spreekuur zou handig zijn.", "Posters in de gangen zouden al helpen." }, 31, new[] { 9, 14, 25 }));
        categoryCampus.Ideas.Add(CreateIdea("Korte pauzes tussen de lessen", new[] { "Vijf minuten extra tussen de lessen zou al een groot verschil maken.", "Dit helpt ook om op tijd naar je volgend lokaal te geraken.", "Mijn concentratie zakt altijd na twee uur aan een stuk." }, 47, new[] { 20, 11, 17 }));
        categoryCampus.Ideas.Add(CreateIdea("Workshop stressmanagement voor leerlingen", new[] { "Zou moeten verplicht zijn in het eerste jaar.", "Ik heb dit nodig gehad tijdens mijn examenperiode.", "Kunnen ouders hier ook aan deelnemen?" }, 38, new[] { 13, 19, 8 }));
        categoryCampus.Ideas.Add(CreateIdea("Groenere schoolomgeving aanleggen", new[] { "Planten in de klaslokalen zouden de sfeer verbeteren.", "Een schooltuin als ontspanningsproject zou geweldig zijn.", "Groen kalmeert bewezen de geest." }, 26, new[] { 16, 7, 23 }));
        categoryCampus.Ideas.Add(CreateIdea("Buddy systeem voor nieuwe leerlingen", new[] { "Ik had dit hard nodig in mijn eerste week.", "Zorgt ook voor meer verbondenheid tussen jaargroepen.", "Kan dit ook voor leerlingen met angstproblemen?" }, 33, new[] { 11, 18, 5 }));
        categoryCampus.Ideas.Add(CreateIdea("Gezonder aanbod in de schoolkantine", new[] { "Voeding heeft een grote invloed op hoe je je voelt.", "Minder suiker en meer fruit zou al helpen.", "De prijzen van gezonde opties moeten wel betaalbaar blijven." }, 44, new[] { 21, 9, 15 }));
        categoryCampus.Ideas.Add(CreateIdea("Sportactiviteiten in de middagpauze", new[] { "Bewegen helpt enorm om stress te verminderen.", "Een pingpongtafel of basketbalring zou al genoeg zijn.", "Dit trekt ook leerlingen weg van hun telefoon." }, 50, new[] { 24, 12, 19 }));

        var categoryHome = new Category { Name = "Thuis welzijn", Description = "Welzijn thuis en in je vrije tijd" };
        categoryHome.Ideas.Add(CreateIdea("Tips voor een goede nachtrust", new[] { "Ik slaap veel beter sinds ik mijn telefoon wegleg voor het slapengaan.", "Een vast slaapschema helpt enorm!", "Blauwe lichtfilters op je scherm helpen ook." }, 29, new[] { 14, 22, 8 }));
        categoryHome.Ideas.Add(CreateIdea("Online hulplijn voor thuisproblemen", new[] { "Dit is zo belangrijk, niet iedereen heeft steun thuis.", "Moet ook beschikbaar zijn in het weekend.", "Anoniem kunnen chatten zou de drempel verlagen." }, 46, new[] { 25, 10, 17 }));
        categoryHome.Ideas.Add(CreateIdea("Bewegingstips voor na school", new[] { "Een wandeling van 20 minuten doet al wonderen.", "Yoga is ook super relaxerend na een drukke dag!", "Fietsen naar school telt ook als beweging." }, 37, new[] { 9, 20, 13 }));
        categoryHome.Ideas.Add(CreateIdea("Kookworkshop voor gezond eten thuis", new[] { "Leren koken is een levensnoodzakelijke vaardigheid.", "Gezond eten hoeft niet duur te zijn.", "Zou ook goed zijn als project voor huishoudkunde." }, 32, new[] { 16, 7, 24 }));
        categoryHome.Ideas.Add(CreateIdea("Dagboek bijhouden voor mentale rust", new[] { "Schrijven helpt om gedachten te ordenen.", "Ik doe dit al en het werkt echt!", "Kan ook digitaal via een app." }, 25, new[] { 11, 18, 6 }));
        categoryHome.Ideas.Add(CreateIdea("Offline hobby's stimuleren", new[] { "Tekenen, muziek of knutselen geeft zoveel voldoening.", "Schermen weglegen is makkelijker als je iets anders te doen hebt.", "School kan een hobbyclub oprichten." }, 40, new[] { 22, 13, 19 }));
        categoryHome.Ideas.Add(CreateIdea("Ouders betrekken bij welzijnsgesprekken", new[] { "Thuis support is even belangrijk als op school.", "Ouderavonden over mentaal welzijn zouden nuttig zijn.", "Niet alle ouders begrijpen de druk die leerlingen voelen." }, 27, new[] { 8, 21, 15 }));
        categoryHome.Ideas.Add(CreateIdea("Digitale detox tips voor thuis", new[] { "Een uur per dag zonder scherm doet wonderen.", "Ik leg mijn telefoon nu in een andere kamer 's nachts.", "Apps die je schermtijd bijhouden helpen ook." }, 43, new[] { 17, 10, 25 }));
        categoryHome.Ideas.Add(CreateIdea("Vrienden betrekken bij je welzijn", new[] { "Gewoon even bellen met een vriend helpt al enorm.", "Een wandeling maken samen is beter dan chatten.", "Sociale verbinding is cruciaal voor mentale gezondheid." }, 36, new[] { 12, 23, 9 }));
        categoryHome.Ideas.Add(CreateIdea("Natuur opzoeken in je vrije tijd", new[] { "Een park of bos bezoeken werkt rustgevend.", "Zelfs een tuin of balkon met planten helpt al.", "Onderzoek toont aan dat natuur stress vermindert." }, 48, new[] { 20, 14, 7 }));

        var categoryOnline = new Category { Name = "Online welzijn", Description = "Gezond omgaan met social media en online" };
        categoryOnline.Ideas.Add(CreateIdea("Workshops over social media gebruik", new[] { "Ik zou graag leren hoe ik minder tijd op mijn telefoon spendeer.", "Dit zou verplicht moeten zijn voor alle leerlingen.", "Ook interessant voor leerkrachten en ouders." }, 39, new[] { 18, 11, 24 }));
        categoryOnline.Ideas.Add(CreateIdea("Schermtijd begrenzen via schoolapp", new[] { "Goed idee maar ouders moeten het ook steunen.", "Zou handig zijn als je zelf limieten kan instellen.", "Kan dit ook voor sociale media apart ingesteld worden?" }, 45, new[] { 7, 22, 16 }));
        categoryOnline.Ideas.Add(CreateIdea("Digitale detox dag op school organiseren", new[] { "Een dag zonder telefoon klinkt eerst eng maar achteraf super rustgevend!", "We zouden dan andere activiteiten kunnen doen zoals tekenen of sporten.", "Dit zou ook de klassfeer verbeteren." }, 30, new[] { 13, 19, 10 }));
        categoryOnline.Ideas.Add(CreateIdea("Bewustwording over vergelijkingscultuur online", new[] { "Instagram en TikTok zorgen voor veel onzekerheid.", "Les over filters en nep-perfectie zou helpen.", "Ik vergelijk mezelf veel minder sinds ik minder scroll." }, 49, new[] { 25, 8, 20 }));
        categoryOnline.Ideas.Add(CreateIdea("Cyberpesten bespreekbaar maken op school", new[] { "Veel leerlingen durven dit niet melden.", "Een anoniem meldpunt specifiek voor online pesten.", "Leerkrachten moeten ook getraind worden hierin." }, 41, new[] { 15, 21, 9 }));
        categoryOnline.Ideas.Add(CreateIdea("Positief gebruik van sociale media aanleren", new[] { "Sociale media kan ook inspirerend en verbindend zijn.", "Leren hoe je een gezonde online gemeenschap opbouwt.", "Verschil tussen passief scrollen en actief gebruik uitleggen." }, 34, new[] { 11, 17, 23 }));
        categoryOnline.Ideas.Add(CreateIdea("Privacyinstellingen uitleggen aan leerlingen", new[] { "De meeste leerlingen weten niet wat ze delen.", "Een workshop over veilig internetten is broodnodig.", "Zou al in het eerste middelbaar moeten gegeven worden." }, 27, new[] { 6, 20, 14 }));
        categoryOnline.Ideas.Add(CreateIdea("Leren omgaan met negatieve reacties online", new[] { "Hatelijke comments kunnen hard aankomen.", "Blokkeren en melden moet normaler worden.", "Emotionele weerbaarheid online aanleren is belangrijk." }, 38, new[] { 19, 12, 25 }));
        categoryOnline.Ideas.Add(CreateIdea("Appvrije zone tijdens de pauze instellen", new[] { "Even echt present zijn met elkaar doet deugd.", "Zou ook de sociale cohesie in de klas verbeteren.", "Misschien vrijwillig eerst en dan verplicht?" }, 52, new[] { 10, 18, 7 }));
        categoryOnline.Ideas.Add(CreateIdea("Online welzijnsplatform voor jongeren lanceren", new[] { "Een veilige online ruimte waar jongeren terecht kunnen.", "Met anonieme chat en professionele begeleiding.", "Zou ook tips en oefeningen kunnen bevatten." }, 44, new[] { 23, 9, 16 }));

        projectActive.Categories.Add(categoryCampus);
        projectActive.Categories.Add(categoryHome);
        projectActive.Categories.Add(categoryOnline);

        // ── Project 2: FitLife Gym ──
        var questionnaire2 = new Questionnaire();
        questionnaire2.Questions.Add(new Question { QuestionFormat = QuestionFormat.MultipleChoice, Questionnaire = questionnaire2, Text = "Welke aspecten van onze dienstverlening waardeert u het meest?", AnswerChoices = new List<string> { "Klantenservice", "Prijs-kwaliteit", "Snelheid", "Gebruiksgemak", "Productaanbod" }, IsMandatory = false, Position = 1 });
        questionnaire2.Questions.Add(new Question { QuestionFormat = QuestionFormat.SingleChoice, Questionnaire = questionnaire2, Text = "Hoe tevreden bent u overall met onze service?", AnswerChoices = new List<string> { "Zeer tevreden", "Tevreden", "Neutraal", "Ontevreden" }, IsMandatory = true, Position = 2 });
        questionnaire2.Questions.Add(new Question { QuestionFormat = QuestionFormat.Likert, Questionnaire = questionnaire2, Text = "Op een schaal van 1 tot 10, hoe waarschijnlijk is het dat u ons aanbeveelt?", IsMandatory = true, Position = 3 });
        questionnaire2.Questions.Add(new Question { QuestionFormat = QuestionFormat.Open, Questionnaire = questionnaire2, Text = "Wat kunnen wij doen om uw ervaring te verbeteren?", IsMandatory = false, Position = 4 });

        var projectActive2 = new Project
        {
            ProjectState = ProjectState.Active,
            FormType = FormType.VerticalScroll,
            Questionnaire = questionnaire2,
            OrganisationName = "FitLife Gym",
            OrganisationLogoUrl = "https://placehold.co/120x40?text=FitLife",
            OrganisationWebsiteUrl = "https://www.fitlife.com",
            Introduction = new Introduction
            {
                Text = "Welkom bij de FitLife ledenbevraging! " +
                       "We stellen deze enquête op om onze faciliteiten, lessen en diensten verder te verbeteren. " +
                       "Jouw mening als lid is enorm waardevol voor ons. " +
                       "Het invullen duurt slechts 3 minuten en alle antwoorden zijn volledig anoniem."
            }
        };

        var fitLifeEmojis = new[] { _heart, _fire };

        var catFacilities = new Category { Name = "Faciliteiten", Description = "Kleedkamers, toestellen en ruimtes" };
        catFacilities.Ideas.Add(CreateIdea("Meer cardio toestellen in de ochtendzone", new[] { "De rijen aan de loopbanden zijn 's ochtends niet te doen.", "Misschien een reservatiesysteem voor piekuren?", "Roeiers en steppers zijn ook altijd bezet." }, 38, new[] { 14, 9, 22 }, fitLifeEmojis));
        catFacilities.Ideas.Add(CreateIdea("Sauna toevoegen aan de wellnesszone", new[] { "Dit zou het lidmaatschap zoveel meer waard maken!", "Een infraroodsauna zou ideaal zijn voor spierherstel.", "Andere gyms hebben dit al standaard." }, 45, new[] { 20, 11, 17 }, fitLifeEmojis));
        catFacilities.Ideas.Add(CreateIdea("Betere ventilatie in de gewichtszaal", new[] { "Het is veel te warm en benauwd tijdens piekuren.", "Airconditioning zou een groot verschil maken.", "Extra ventilatoren als tussenoplossing?" }, 32, new[] { 8, 19, 13 }, fitLifeEmojis));
        catFacilities.Ideas.Add(CreateIdea("Ruimere kleedkamers voor dames", new[] { "Nu is het bijna onmogelijk om je te kleden tijdens spitsuren.", "Meer kluisjes zouden ook welkom zijn.", "Een aparte ruimte voor föhnen zou handig zijn." }, 29, new[] { 15, 7, 24 }, fitLifeEmojis));
        catFacilities.Ideas.Add(CreateIdea("Kinderhoek toevoegen aan de gym", new[] { "Dan kunnen ouders sporten terwijl de kinderen spelen.", "Zou veel meer jonge gezinnen aantrekken.", "Een begeleider voor de kinderen zou ideaal zijn." }, 36, new[] { 11, 20, 6 }, fitLifeEmojis));
        catFacilities.Ideas.Add(CreateIdea("Gratis parkeerplaats voor leden", new[] { "Nu is parkeren een extra kost bovenop het lidmaatschap.", "Zeker 's avonds is dit een probleem.", "Een samenwerking met de parking naast de gym?" }, 41, new[] { 18, 12, 25 }, fitLifeEmojis));
        catFacilities.Ideas.Add(CreateIdea("Smoothiebar toevoegen aan de receptie", new[] { "Gezonde snacks na het sporten zouden perfect zijn.", "Proteïneshakes en vers fruit als basis.", "Zou ook een leuke sociale plek worden." }, 27, new[] { 9, 16, 21 }, fitLifeEmojis));
        catFacilities.Ideas.Add(CreateIdea("Meer spiegels in de vrije gewichtszaal", new[] { "Spiegels zijn essentieel voor techniekcontrole.", "Nu moet je soms in rare hoeken staan.", "Zeker aan de dumbbell rek zijn er te weinig." }, 33, new[] { 13, 22, 8 }, fitLifeEmojis));
        catFacilities.Ideas.Add(CreateIdea("Wifi verbeteren in de sporthal", new[] { "Het signaal valt constant weg terwijl ik muziek stream.", "Wifi is tegenwoordig een basisverwachting.", "Zou ook nuttig zijn voor de personal trainers." }, 44, new[] { 17, 10, 19 }, fitLifeEmojis));
        catFacilities.Ideas.Add(CreateIdea("Massagestoel toevoegen aan de relaxzone", new[] { "Na een zware training is dit de perfecte beloning.", "Zelfs een betaalde massagestoel zou ik gebruiken.", "Zou de gym onderscheiden van de concurrentie." }, 50, new[] { 24, 14, 7 }, fitLifeEmojis));

        var catLessons = new Category { Name = "Lessen & Begeleiding", Description = "Groepslessen en personal training" };
        catLessons.Ideas.Add(CreateIdea("Yogales toevoegen aan het weekendrooster", new[] { "In het weekend heb ik eindelijk tijd voor yoga!", "Liefst vroeg in de ochtend zodat de rest van de dag vrij is.", "Ook een meditatieklas zou geweldig zijn." }, 39, new[] { 16, 23, 9 }, fitLifeEmojis));
        catLessons.Ideas.Add(CreateIdea("Personal training sessies goedkoper maken", new[] { "Nu is het onbetaalbaar voor studenten.", "Misschien een studentenkorting invoeren?", "Duo sessies zouden ook goedkoper zijn per persoon." }, 43, new[] { 12, 19, 25 }, fitLifeEmojis));
        catLessons.Ideas.Add(CreateIdea("Online lessen streamen voor leden", new[] { "Als ik ziek ben kan ik toch nog de les volgen.", "Zou ook handig zijn voor mensen met drukke schema's.", "Opnames achteraf bekijken zou ook fijn zijn." }, 31, new[] { 8, 14, 20 }, fitLifeEmojis));
        catLessons.Ideas.Add(CreateIdea("Beginnerscursus voor nieuwe leden", new[] { "Nu word je gewoon losgelaten in de gym zonder uitleg.", "Een rondleiding en basisuitleg zou het verschil maken.", "Veel mensen stoppen omdat ze niet weten hoe te starten." }, 46, new[] { 22, 11, 17 }, fitLifeEmojis));
        catLessons.Ideas.Add(CreateIdea("Voedingsadvies opnemen in het lidmaatschap", new[] { "Sport en voeding gaan hand in hand.", "Zelfs een basisconsultatie zou al enorm helpen.", "Nu moet je dat apart en duur betalen." }, 35, new[] { 10, 21, 6 }, fitLifeEmojis));
        catLessons.Ideas.Add(CreateIdea("Dans- en zumbaklassen toevoegen", new[] { "Zumba is superleuk en verbrand veel calorieën.", "Zou ook meer vrouwelijke leden aantrekken.", "Een wekelijkse dansles op vrijdagavond zou gezellig zijn." }, 28, new[] { 15, 9, 23 }, fitLifeEmojis));
        catLessons.Ideas.Add(CreateIdea("Boksklassen voor beginners", new[] { "Boksen is een geweldige stressafvoer.", "Een veilige omgeving voor beginners is belangrijk.", "Zou ook zelfverdediging kunnen combineren." }, 42, new[] { 19, 13, 7 }, fitLifeEmojis));
        catLessons.Ideas.Add(CreateIdea("Maandelijkse fitnessdoelen bijhouden met trainer", new[] { "Progressie bijhouden motiveert enorm.", "Een korte check-in van 15 minuten per maand zou al genoeg zijn.", "Nu weet ik niet of ik vooruitga." }, 37, new[] { 11, 18, 24 }, fitLifeEmojis));
        catLessons.Ideas.Add(CreateIdea("Senior-vriendelijke lessen toevoegen", new[] { "Ouderen hebben andere noden dan jonge sporters.", "Lage impact cardio en stretching zouden perfect zijn.", "Zou ook nieuwe doelgroep aantrekken." }, 26, new[] { 20, 8, 16 }, fitLifeEmojis));
        catLessons.Ideas.Add(CreateIdea("Aqua aerobics in het zwembad", new[] { "Gewrichtsvriendelijk sporten voor iedereen.", "Ideaal voor mensen met rugklachten.", "Het zwembad wordt nu ondergecapaciteit gebruikt." }, 48, new[] { 14, 22, 10 }, fitLifeEmojis));

        var catSchedule = new Category { Name = "Planning & Openingsuren", Description = "Bereikbaarheid en rooster" };
        catSchedule.Ideas.Add(CreateIdea("Gym ook open op zondagavond", new[] { "Zondagavond is perfect om de week voor te bereiden.", "Nu moet ik altijd wachten tot maandag.", "Zelfs maar tot 20u zou al een groot verschil maken." }, 40, new[] { 17, 11, 25 }, fitLifeEmojis));
        catSchedule.Ideas.Add(CreateIdea("App voor reservaties van toestellen", new[] { "Dan weet je op voorhand of het druk is.", "Wachtrijen zijn frustrerend na een lange werkdag.", "Zou ook de verdeling van leden over de dag verbeteren." }, 34, new[] { 9, 20, 14 }, fitLifeEmojis));
        catSchedule.Ideas.Add(CreateIdea("Vroeger openen op weekdagen", new[] { "Om 5u30 open zou mij toelaten voor het werk te sporten.", "Veel mensen willen 's ochtends vroeg trainen.", "Nu mis ik de ochtendtrein als ik eerst naar de gym ga." }, 47, new[] { 23, 12, 8 }, fitLifeEmojis));
        catSchedule.Ideas.Add(CreateIdea("Wachtlijst systeem voor populaire lessen", new[] { "Lessen zijn vaak volgeboekt tegen dat ik kan inschrijven.", "Een automatische notificatie als er een plekje vrijkomt.", "Nu kom ik soms voor niets." }, 29, new[] { 16, 7, 19 }, fitLifeEmojis));
        catSchedule.Ideas.Add(CreateIdea("Rustigere tijdslots aanduiden op de app", new[] { "Ik wil weten wanneer de gym relatief leeg is.", "Nu is het altijd een verrassing of het druk is.", "Zou mijn trainingservaring veel aangenamer maken." }, 38, new[] { 13, 21, 6 }, fitLifeEmojis));
        catSchedule.Ideas.Add(CreateIdea("Feestdagen openingsuren beter communiceren", new[] { "Ik ben al een paar keer voor een gesloten deur gestaan.", "Een automatische e-mail of pushmelding zou helpen.", "Op de website staat het soms verkeerd." }, 25, new[] { 10, 18, 22 }, fitLifeEmojis));
        catSchedule.Ideas.Add(CreateIdea("Langere openingsuren in het weekend", new[] { "Nu sluit de gym al om 18u op zondag.", "Het weekend is voor velen de enige tijd om lang te sporten.", "Zelfs tot 21u zou al een groot verschil maken." }, 43, new[] { 15, 9, 24 }, fitLifeEmojis));
        catSchedule.Ideas.Add(CreateIdea("Persoonlijk trainingsrooster in de app", new[] { "Nu moet ik alles zelf bijhouden in een notitieblok.", "Een digitaal schema met herinneringen zou perfect zijn.", "Integratie met mijn agenda zou ideaal zijn." }, 36, new[] { 20, 11, 17 }, fitLifeEmojis));
        catSchedule.Ideas.Add(CreateIdea("Druktemeter aan de ingang installeren", new[] { "Dan kan ik beslissen of ik nu ga of later.", "Sommige gyms hebben dit al en het werkt geweldig.", "Zou ook via de app beschikbaar kunnen zijn." }, 31, new[] { 8, 19, 13 }, fitLifeEmojis));
        catSchedule.Ideas.Add(CreateIdea("Speciale ochtendpas voor vroege sporters", new[] { "Een goedkopere pas voor enkel ochtenduren zou perfect zijn.", "Dan zijn de toestellen minder bezet en ik betaal minder.", "Win-win voor de gym en de leden." }, 52, new[] { 25, 14, 20 }, fitLifeEmojis));

        projectActive2.Categories.Add(catFacilities);
        projectActive2.Categories.Add(catLessons);
        projectActive2.Categories.Add(catSchedule);

        // ── Project 3: FC Eendracht ──
        var questionnaire3 = new Questionnaire();
        questionnaire3.Questions.Add(new Question { QuestionFormat = QuestionFormat.MultipleChoice, Questionnaire = questionnaire3, Text = "Welke aspecten van de club wil je verbeteren?", AnswerChoices = new List<string> { "Trainingen", "Infrastructuur", "Communicatie", "Sociale activiteiten", "Wedstrijdplanning" }, IsMandatory = false, Position = 1 });
        questionnaire3.Questions.Add(new Question { QuestionFormat = QuestionFormat.SingleChoice, Questionnaire = questionnaire3, Text = "Hoe beoordeel je de algemene sfeer in de club?", AnswerChoices = new List<string> { "Uitstekend", "Goed", "Matig", "Slecht" }, IsMandatory = true, Position = 2 });
        questionnaire3.Questions.Add(new Question { QuestionFormat = QuestionFormat.Likert, Questionnaire = questionnaire3, Text = "Op een schaal van 1 tot 10, hoe tevreden ben je over de trainingen?", IsMandatory = true, Position = 3 });
        questionnaire3.Questions.Add(new Question { QuestionFormat = QuestionFormat.Open, Questionnaire = questionnaire3, Text = "Wat zou jij veranderen aan de club als je de kans had?", IsMandatory = false, Position = 4 });

        var projectActive3 = new Project
        {
            ProjectState = ProjectState.Active,
            FormType = FormType.VerticalScroll,
            Questionnaire = questionnaire3,
            OrganisationName = "FC Eendracht",
            OrganisationLogoUrl = "https://placehold.co/120x40?text=FC+Eendracht",
            OrganisationWebsiteUrl = "https://www.fceendracht.be",
            
        };

        var catTraining = new Category { Name = "Trainingen", Description = "Kwaliteit en opbouw van de trainingen" };
        catTraining.Ideas.Add(CreateIdea("Videoanalyse invoeren bij de trainingen", new[] { "Profclubs doen dit al jaren, waarom wij niet?", "Zou mijn positiespel enorm verbeteren.", "Een simpele tablet met een app zou al voldoende zijn." }, 44, new[] { 18, 11, 25 }));
        catTraining.Ideas.Add(CreateIdea("Aparte trainingsgroep voor gevorderde spelers", new[] { "Nu wachten gevorderde spelers te veel op beginners.", "Iedereen traint op zijn eigen niveau, eerlijker voor iedereen.", "Zolang het de teamgeest niet schaadt, goed idee." }, 37, new[] { 13, 20, 8 }));
        catTraining.Ideas.Add(CreateIdea("Conditietraining twee keer per week", new[] { "Nu focussen we te veel op tactiek en te weinig op conditie.", "Een aparte conditietrainer zou ideaal zijn.", "Veel spelers zijn in de tweede helft uitgeput." }, 41, new[] { 9, 17, 22 }));
        catTraining.Ideas.Add(CreateIdea("Individuele techniektraining voor jongeren", new[] { "Jonge spelers hebben meer individuele aandacht nodig.", "Een techniektraining naast de groepstraining zou perfect zijn.", "Zou de jeugdopleiding enorm versterken." }, 29, new[] { 15, 23, 7 }));
        catTraining.Ideas.Add(CreateIdea("Gasttrainer uitnodigen eens per maand", new[] { "Een frisse blik van buitenaf kan veel losmaken.", "Zou ook motiverend zijn voor de spelers.", "Misschien een oud-profvoetballer uitnodigen?" }, 48, new[] { 21, 12, 19 }));
        catTraining.Ideas.Add(CreateIdea("Warming-up protocol standaardiseren", new[] { "Nu doet iedereen maar wat voor de training.", "Een goede warming-up vermindert blessures.", "De FIFA 11+ methode is bewezen effectief." }, 33, new[] { 10, 18, 24 }));
        catTraining.Ideas.Add(CreateIdea("Mentale weerbaarheid trainen", new[] { "Voetbal is ook een mentale sport.", "Een sportpsycholoog uitnodigen zou waardevol zijn.", "Veel wedstrijden worden gewonnen of verloren in het hoofd." }, 26, new[] { 16, 9, 21 }));
        catTraining.Ideas.Add(CreateIdea("Trainingsschema digitaal beschikbaar stellen", new[] { "Nu weet ik pas op de dag zelf wat we gaan doen.", "Een weekoverzicht via een app zou perfect zijn.", "Zou ook helpen om je voor te bereiden." }, 35, new[] { 14, 22, 6 }));
        catTraining.Ideas.Add(CreateIdea("Kleine groepsoefeningen meer integreren", new[] { "Rondjes lopen is saai, kleine spelvormen zijn leuker.", "Leert ook sneller samenspelen.", "Profteams trainen bijna uitsluitend zo." }, 42, new[] { 19, 8, 25 }));
        catTraining.Ideas.Add(CreateIdea("Keeperstraining apart organiseren", new[] { "De keeper wordt nu vergeten tijdens veldtrainingen.", "Een keeperscoach zou een enorm verschil maken.", "Zelfs één extra training per week zou helpen." }, 50, new[] { 23, 15, 11 }));

        var catInfra = new Category { Name = "Infrastructuur", Description = "Velden, kleedkamers en materiaal" };
        catInfra.Ideas.Add(CreateIdea("Nieuw kunstgrasveld aanleggen", new[] { "Het huidige veld is in slechte staat na de winter.", "Kunstgras betekent ook spelen bij slecht weer.", "Subsidies aanvragen bij de gemeente?" }, 46, new[] { 20, 13, 9 }));
        catInfra.Ideas.Add(CreateIdea("Betere verlichting op het trainingsveld", new[] { "In de winter trainen we bijna in het donker.", "LED verlichting is energiezuinig en helder.", "Dit verhoogt ook de veiligheid tijdens avondtrainingen." }, 38, new[] { 12, 24, 17 }));
        catInfra.Ideas.Add(CreateIdea("Nieuwe ballen aanschaffen", new[] { "De huidige ballen zijn versleten en onregelmatig.", "Goede materialen motiveren spelers.", "Misschien sponsors zoeken voor nieuwe ballen?" }, 31, new[] { 8, 19, 14 }));
        catInfra.Ideas.Add(CreateIdea("Kleedkamers renoveren", new[] { "De douches zijn verouderd en soms koud.", "Een propere kleedkamer creëert een professionele sfeer.", "Zou ook nieuwe leden aantrekken." }, 43, new[] { 22, 10, 18 }));
        catInfra.Ideas.Add(CreateIdea("Tribunes overdekken", new[] { "Supporters staan in de regen bij thuiswedstrijden.", "Een eenvoudig dak zou al het verschil maken.", "Zou ook meer supporters aantrekken." }, 27, new[] { 11, 20, 7 }));
        catInfra.Ideas.Add(CreateIdea("Materiaalberging uitbreiden", new[] { "Nu liggen ballen en hesjes overal.", "Een georganiseerde berging bespaart veel tijd.", "Zou ook beschadiging van materiaal voorkomen." }, 36, new[] { 16, 9, 23 }));
        catInfra.Ideas.Add(CreateIdea("Scorebord installeren", new[] { "Nu weten supporters de score soms niet.", "Een digitaal scorebord zou professioneler ogen.", "Zou ook de wedstrijdervaring verbeteren." }, 25, new[] { 13, 21, 6 }));
        catInfra.Ideas.Add(CreateIdea("Fietsenstalling bouwen bij het stadion", new[] { "Nu worden fietsen overal gestald.", "Een overdekte stalling zou gewaardeerd worden.", "Duurzaam verplaatsen stimuleren." }, 40, new[] { 17, 11, 25 }));
        catInfra.Ideas.Add(CreateIdea("Ehbo-post moderniseren", new[] { "De huidige ehbo-kit is onvolledig.", "Een defibrillator is tegenwoordig noodzakelijk.", "Zou ook het vertrouwen van ouders vergroten." }, 34, new[] { 9, 18, 22 }));
        catInfra.Ideas.Add(CreateIdea("Oefenruimte voor keeperstraining", new[] { "Nu trainen keepers tussen de veldspelers.", "Een aparte ruimte met doelen en matten zou ideaal zijn.", "Kleine investering met grote impact." }, 49, new[] { 24, 14, 8 }));

        var catSocial = new Category { Name = "Sociale activiteiten", Description = "Teambuilding en clubactiviteiten" };
        catSocial.Ideas.Add(CreateIdea("Jaarlijks clubkampioenschap organiseren", new[] { "Een intern tornooi zou de teamgeest enorm boosten!", "Met een trofee en feestje achteraf!", "Families uitnodigen als supporters zou gezellig zijn." }, 45, new[] { 19, 12, 25 }));
        catSocial.Ideas.Add(CreateIdea("Teambuilding weekend plannen", new[] { "Een weekend weg versterkt de band enorm.", "Escape room of outdoor activiteit zou perfect zijn.", "Hoeft niet duur te zijn als iedereen bijdraagt." }, 38, new[] { 14, 22, 9 }));
        catSocial.Ideas.Add(CreateIdea("Clubavond voor spelers en supporters", new[] { "Een quiz of feestje zou iedereen samenbrengen.", "Nu kennen spelers en supporters elkaar nauwelijks.", "Zou ook nieuwe vrijwilligers aantrekken." }, 32, new[] { 10, 17, 21 }));
        catSocial.Ideas.Add(CreateIdea("Nieuwjaarsreceptie voor alle leden", new[] { "Een traditionele receptie versterkt het clubgevoel.", "Met terugblik op het seizoen en vooruitblik.", "Zou ook sponsors kunnen uitnodigen." }, 27, new[] { 8, 20, 13 }));
        catSocial.Ideas.Add(CreateIdea("Jeugd en senioren samen laten trainen", new[] { "Jongeren leren van ervaren spelers.", "Zou ook de doorstroom naar het eerste elftal verbeteren.", "Een gezamenlijke training per maand zou al genoeg zijn." }, 41, new[] { 16, 11, 24 }));
        catSocial.Ideas.Add(CreateIdea("Clubmagazine of nieuwsbrief opstarten", new[] { "Wedstrijdverslagen en clubnieuws voor alle leden.", "Zou ook de betrokkenheid van supporters vergroten.", "Een digitale versie is gratis en makkelijk te verspreiden." }, 29, new[] { 12, 19, 7 }));
        catSocial.Ideas.Add(CreateIdea("Vrijwilligersdag organiseren", new[] { "De club draait op vrijwilligers en die verdienen waardering.", "Een bedankfeestje of etentje zou veel betekenen.", "Zou ook nieuwe vrijwilligers motiveren." }, 36, new[] { 23, 9, 18 }));
        catSocial.Ideas.Add(CreateIdea("Supporters club oprichten", new[] { "Nu is er geen officiële supportersgroep.", "Een supportersclub zou de sfeer bij wedstrijden verbeteren.", "Zou ook extra inkomsten genereren voor de club." }, 44, new[] { 15, 21, 6 }));
        catSocial.Ideas.Add(CreateIdea("Kerstdiner voor het volledige team", new[] { "Een jaarlijks diner brengt iedereen samen.", "Zou ook een moment zijn om te reflecteren op het jaar.", "Met awards voor beste speler, meeste inzet etc." }, 33, new[] { 11, 25, 14 }));
        catSocial.Ideas.Add(CreateIdea("Open trainingsdag voor families", new[] { "Families zien graag waarmee hun kind bezig is.", "Zou ook nieuwe leden aantrekken.", "Een kleine demonstratie aan het einde zou leuk zijn." }, 50, new[] { 20, 13, 22 }));

        projectActive3.Categories.Add(catTraining);
        projectActive3.Categories.Add(catInfra);
        projectActive3.Categories.Add(catSocial);

        // ── Inactive & Draft ──
        var projectInactive = new Project
        {
            ProjectState = ProjectState.Archived,
            FormType = FormType.VerticalScroll,
            Questionnaire = new Questionnaire(),
            OrganisationName = "Inactive Org",
            OrganisationLogoUrl = "https://placehold.co/120x40?text=Inactive",
            OrganisationWebsiteUrl = "https://www.inactive.com",
            Introduction = new Introduction { Text = "Dit project is momenteel inactief." }
        };

        var projectDraft = new Project
        {
            ProjectState = ProjectState.Draft,
            FormType = FormType.VerticalScroll,
            Questionnaire = new Questionnaire(),
            OrganisationName = "Draft Org",
            OrganisationLogoUrl = "https://placehold.co/120x40?text=Draft",
            OrganisationWebsiteUrl = "https://www.draft.com",
            Introduction = new Introduction { Text = "Dit project is nog in opbouw." }
        };

        context.Projects.Add(projectActive);
        context.Projects.Add(projectActive2);
        context.Projects.Add(projectActive3);
        context.Projects.Add(projectInactive);
        context.Projects.Add(projectDraft);

        context.Emojis.AddRange(_heart, _thumbsup, _fire, _check);

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