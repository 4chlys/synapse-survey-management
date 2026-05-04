using System.Text.Json.Serialization;

namespace SynapseEnqueteManagement.BL.AiImageGeneration;

public sealed class AiImageGenerationConfig
{
    [JsonPropertyName("prompt")]
    public string Prompt { get; init; } =
        "Genereer een moderne, visueel aantrekkelijke headerafbeelding voor een jongerenparticipatieproject." +
        "Het centrale visuele onderwerp van de afbeelding is: '{project_name}'. " +
        "Zorg ervoor dat dit onderwerp duidelijk en herkenbaar in beeld wordt gebracht, maar doe dit in een creatieve," +
        " gestileerde en professionele stijl (niet fotorealistisch, maar artistiek). Gebruik een levendig en kleurrijk palet." +
        "De compositie is bedoeld als een brede projectbanner. Belangrijk: genereer absoluut geen tekst, letters of logo's in de afbeelding.";

    [JsonPropertyName("aspect_ratio")]
    public string AspectRatio { get; init; } = "16:9";

    [JsonPropertyName("model")]
    public string Model { get; init; } = "imagen-3.0-generate-002";
}
