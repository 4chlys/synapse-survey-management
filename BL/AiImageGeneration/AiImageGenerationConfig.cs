using System.Text.Json.Serialization;

namespace SynapseEnqueteManagement.BL.AiImageGeneration;

public sealed class AiImageGenerationConfig
{
    [JsonPropertyName("prompt")]
    public string Prompt { get; init; } =
        // Keep in sync with ai-config.example.json — that file takes priority at runtime.
        "Maak een realistische reportagefoto (16:9 bannerformaat) van een echte, alledaagse situatie " +
        "die direct verband houdt met het onderwerp \"{project_name}\".{context} " +
        "Toon concrete mensen in een herkenbare echte omgeving — zoals een persfoto of documentairefoto. " +
        "KLEUREN: gedempte, gewone kleuren zoals in het dagelijks leven. Geen felle, verzadigde of neonkleuren. " +
        "BELICHTING: natuurlijk daglicht of gewone binnenverlichting. Geen dramatische of artistieke lichtval. " +
        "VERBODEN: illustratie, digitale kunst, concept art, tekening, schilderij, cartoon, anime, " +
        "abstracte kunst, symbolische voorstellingen, gloei-effecten, artistieke filters, HDR-kleuren. " +
        "Geen tekst, letters, logo's of watermerken in de afbeelding.";

    [JsonPropertyName("social_prompt")]
    public string SocialPrompt { get; init; } =
        "Maak een realistische reportagefoto (16:9 bannerformaat) die de inhoud van deze social media post " +
        "visueel weergeeft:\n\n\"{post_copy}\"\n\n" +
        "Het project heet \"{project_name}\".{context} " +
        "Toon concrete mensen in een herkenbare echte omgeving — zoals een persfoto of documentairefoto. " +
        "KLEUREN: gedempte, gewone kleuren zoals in het dagelijks leven. Geen felle, verzadigde of neonkleuren. " +
        "BELICHTING: natuurlijk daglicht of gewone binnenverlichting. Geen dramatische of artistieke lichtval. " +
        "VERBODEN: illustratie, digitale kunst, concept art, tekening, schilderij, cartoon, anime, " +
        "abstracte kunst, symbolische voorstellingen, gloei-effecten, artistieke filters, HDR-kleuren. " +
        "Geen tekst, letters, logo's of watermerken in de afbeelding.";

    [JsonPropertyName("aspect_ratio")]
    public string AspectRatio { get; init; } = "16:9";

    // Output resolution sent to Vertex. Google bills image output at $30/1M tokens;
    // 1024×1024 = 1290 tokens (documented reference). Token count scales with pixel area.
    [JsonPropertyName("width")]
    public int Width { get; init; } = 1280;

    [JsonPropertyName("height")]
    public int Height { get; init; } = 720;

    [JsonPropertyName("model")]
    public string Model { get; init; } = "gemini-2.5-flash-image";
}
