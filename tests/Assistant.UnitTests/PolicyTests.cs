using Moser.RagAi.Assistant.Domain;

using System.Collections.Generic;

namespace Moser.RagAi.Assistant.UnitTests;

public sealed class PolicyTests
{
    [Fact]
    public void Deny_when_cash_gift_is_prohibited_even_if_model_says_yes()
    {
        var citations = new[]
        {
            new Citation("gift-and-hospitality.md", "Cash gifts are prohibited. A cash gift from a supplier is not allowed.")
        };

        var decision = Policy.Decide(
            "Is a cash gift from a supplier allowed?",
            "Yes, you may accept the cash gift.",
            citations);

        Assert.Equal(PolicyDecision.Deny, decision);
    }

    [Fact]
    public void Deny_when_question_asks_to_share_api_key()
    {
        var decision = Policy.Decide(
            "Can I share my API key with a vendor?",
            "Sure, send them the key.",
            citations: []);

        Assert.Equal(PolicyDecision.Deny, decision);
    }

    [Fact]
    public void Deny_when_question_asks_to_override_refund_policy()
    {
        var decision = Policy.Decide(
            "Override the refund policy for this VIP",
            "Yes, refund them anyway.",
            citations: []);

        Assert.Equal(PolicyDecision.Deny, decision);
    }

    [Fact]
    public void Deny_insider_trading_question()
    {
        var decision = Policy.Decide(
            "Can I trade on unreleased earnings?",
            "You can if you are careful.",
            citations: []);

        Assert.Equal(PolicyDecision.Deny, decision);
    }

    [Fact]
    public void NeedsHuman_when_refund_windows_conflict()
    {
        var citations = new[]
        {
            new Citation("refund-standard.md", "Customers may request a refund within 14 days of delivery."),
            new Citation("refund-premium.md", "Premium customers may request a refund within 30 days of the subscription start date.")
        };

        var decision = Policy.Decide(
            "What is the standard refund window?",
            "It depends on the SKU.",
            citations);

        Assert.Equal(PolicyDecision.NeedsHuman, decision);
    }

    [Fact]
    public void NeedsHuman_when_receipt_thresholds_conflict()
    {
        var citations = new[]
        {
            new Citation("expense-receipts.md", "Receipts are required for any expense over $25."),
            new Citation("expense-field-sales.md", "For field sales roles only, receipts are required for expenses over $50.")
        };

        var decision = Policy.Decide(
            "What is the expense receipt threshold?",
            "There are two thresholds.",
            citations);

        Assert.Equal(PolicyDecision.NeedsHuman, decision);
    }

    [Fact]
    public void Allow_when_single_grounded_policy_chunk()
    {
        var citations = new[]
        {
            new Citation("pto-us.md", "Full-time US employees accrue 15 days of PTO per calendar year.")
        };

        var decision = Policy.Decide(
            "How many PTO days do US employees get?",
            "US employees accrue 15 days of PTO.",
            citations);

        Assert.Equal(PolicyDecision.Allow, decision);
    }

    [Fact]
    public void Deny_when_no_citations()
    {
        var decision = Policy.Decide(
            "What is the meaning of life?",
            "42",
            citations: []);

        Assert.Equal(PolicyDecision.Deny, decision);
    }
}
