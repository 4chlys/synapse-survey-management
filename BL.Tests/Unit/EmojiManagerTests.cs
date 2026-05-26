namespace SynapseEnqueteManagement.BL.Tests.Unit;

public class EmojiManagerTests
{
    private readonly Mock<IEmojiRepository> _emojiRepo = new(MockBehavior.Strict);
    private readonly EmojiManager _manager;

    public EmojiManagerTests()
    {
        _manager = new EmojiManager(_emojiRepo.Object);
    }

    [Fact]
    public async Task GetAllReadOnlyAsync_DelegatesToRepository()
    {
        var emojis = new[]
        {
            new Emoji { EmojiId = 1, Symbol = "👍", Name = "thumbsup" },
            new Emoji { EmojiId = 2, Symbol = "❤️", Name = "heart" },
        };
        _emojiRepo.Setup(r => r.ReadAllReadOnlyAsync()).ReturnsAsync(emojis);

        var result = await _manager.GetAllReadOnlyAsync();

        Assert.Same(emojis, result);
        _emojiRepo.VerifyAll();
    }

    [Fact]
    public async Task GetByIdForUpdateAsync_DelegatesToRepository()
    {
        var emoji = new Emoji { EmojiId = 7, Symbol = "🎯", Name = "target" };
        _emojiRepo.Setup(r => r.ReadByIdForUpdateAsync(7)).ReturnsAsync(emoji);

        var result = await _manager.GetByIdForUpdateAsync(7);

        Assert.Same(emoji, result);
        _emojiRepo.VerifyAll();
    }

    [Fact]
    public async Task GetByIdForUpdateAsync_NotFound_ReturnsNull()
    {
        _emojiRepo.Setup(r => r.ReadByIdForUpdateAsync(999)).ReturnsAsync((Emoji)null);

        var result = await _manager.GetByIdForUpdateAsync(999);

        Assert.Null(result);
    }
}
