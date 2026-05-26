using SynapseEnqueteManagement.BL.Helpers;

namespace SynapseEnqueteManagement.BL.Tests.Unit;

public class MultipleChoiceValueSerializerTests
{
    [Fact]
    public void Split_SingleValue_ReturnsOneElement()
    {
        var result = MultipleChoiceValueSerializer.Split("Red");
        Assert.Equal(new[] { "Red" }, result);
    }

    [Fact]
    public void Split_MultipleValues_SplitsOnCommaSpace()
    {
        var result = MultipleChoiceValueSerializer.Split("Red, Green, Blue");
        Assert.Equal(new[] { "Red", "Green", "Blue" }, result);
    }

    [Fact]
    public void Split_DropsEmptySegmentsFromLeadingTrailingOrDoubleDelimiters()
    {
        // RemoveEmptyEntries: stray delimiters must not produce phantom empty answers.
        var result = MultipleChoiceValueSerializer.Split(", Red, , Blue, ");
        Assert.Equal(new[] { "Red", "Blue" }, result);
    }

    [Fact]
    public void Split_EmptyString_ReturnsEmpty()
    {
        Assert.Empty(MultipleChoiceValueSerializer.Split(string.Empty));
    }

    [Fact]
    public void Join_JoinsWithCommaSpace()
    {
        var result = MultipleChoiceValueSerializer.Join(new[] { "Red", "Green", "Blue" });
        Assert.Equal("Red, Green, Blue", result);
    }

    [Fact]
    public void JoinThenSplit_RoundTripsWellFormedValues()
    {
        var original = new[] { "Red", "Green", "Blue" };
        var roundTripped = MultipleChoiceValueSerializer.Split(MultipleChoiceValueSerializer.Join(original));
        Assert.Equal(original, roundTripped);
    }
}
