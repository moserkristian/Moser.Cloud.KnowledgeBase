using Moser.RagAi.Assistant.Domain;

namespace Moser.RagAi.Assistant.UnitTests;

public sealed class AnswerTests
{
    [Fact]
    public void Stopped_defaults_to_false()
    {
        var answer = new Answer("partial", [], PolicyDecision.Allow, refused: false, reason: "");

        Assert.False(answer.Stopped);
    }

    [Fact]
    public void Stopped_keeps_draft_text_without_rewriting_it()
    {
        var citations = new[] { new Citation("gift-and-hospitality.pdf", "Cash gifts are prohibited.") };
        var answer = new Answer(
            "Cash gifts are",
            citations,
            PolicyDecision.Allow,
            refused: false,
            reason: "Generation was stopped.",
            stopped: true);

        Assert.True(answer.Stopped);
        Assert.Equal("Cash gifts are", answer.Text);
        Assert.Same(citations, answer.Citations);
    }
}
