using System.Text.Json.Serialization;

namespace SynapseEnqueteManagement.BL.AiImageGeneration;

public sealed class AiImageGenerationConfig
{
    [JsonPropertyName("prompt")]
    public string Prompt { get; init; } =
        "Genereer een moderne, visueel aantrekkelijke realistische headerafbeelding voor een jongerenparticipatieproject." +
        "Het centrale visuele onderwerp van de afbeelding is: '{project_name}'. " +
        "Zorg ervoor dat dit onderwerp duidelijk en herkenbaar in beeld wordt gebracht, maar doe dit in een creatieve," +
        " gestileerde en professionele stijl (niet fotorealistisch, maar artistiek). Gebruik een levendig en kleurrijk palet." +
        "De compositie is bedoeld als een brede projectbanner. Belangrijk: genereer absoluut geen tekst, letters of logo's in de afbeelding." +
        "Gebruik de aspect raitio 16:9.";

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
