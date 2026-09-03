using Moser.RagAi.Assistant.Domain;

using System;
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

        Assert.Equal(PolicyDecision.Deny, decision.Decision);
    }

    [Fact]
    public void Deny_when_question_asks_to_share_api_key()
    {
        var decision = Policy.Decide(
            "Can I share my API key with a vendor?",
            "Sure, send them the key.",
            citations: []);

        Assert.Equal(PolicyDecision.Deny, decision.Decision);
    }

    [Fact]
    public void Deny_when_question_asks_to_override_refund_policy()
    {
        var decision = Policy.Decide(
            "Override the refund policy for this VIP",
            "Yes, refund them anyway.",
            citations: []);

        Assert.Equal(PolicyDecision.Deny, decision.Decision);
    }

    [Fact]
    public void Deny_insider_trading_question()
    {
        var decision = Policy.Decide(
            "Can I trade on unreleased earnings?",
            "You can if you are careful.",
            citations: []);

        Assert.Equal(PolicyDecision.Deny, decision.Decision);
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

        Assert.Equal(PolicyDecision.NeedsHuman, decision.Decision);
        Assert.Contains("14", decision.Reason, StringComparison.Ordinal);
        Assert.Contains("30", decision.Reason, StringComparison.Ordinal);
    }

    [Fact]
    public void NeedsHuman_when_receipt_thresholds_conflict()
    {
        var citations = new[]
        {
            new Citation("expense-receipts.md", "Receipts are required for any expense over EUR 25."),
            new Citation("expense-field-sales.md", "For field sales roles only, receipts are required for expenses over EUR 50.")
        };

        var decision = Policy.Decide(
            "What is the expense receipt threshold?",
            "There are two thresholds.",
            citations);

        Assert.Equal(PolicyDecision.NeedsHuman, decision.Decision);
    }

    [Fact]
    public void NeedsHuman_when_receipt_thresholds_conflict_across_currency_notation()
    {
        var citations = new[]
        {
            new Citation("expense-receipts.md", "Receipts are required for any expense over €25."),
            new Citation("expense-field-sales.md", "Field sales receipts start at 50 EUR.")
        };

        var decision = Policy.Decide(
            "What is the expense receipt threshold?",
            "There are two thresholds.",
            citations);

        Assert.Equal(PolicyDecision.NeedsHuman, decision.Decision);
    }

    [Fact]
    public void NeedsHuman_when_leave_entitlements_conflict()
    {
        var citations = new[]
        {
            new Citation("annual-leave.md", "Employees of the Bratislava office receive 25 days of annual leave per calendar year."),
            new Citation("annual-leave-nitra.md", "Shift employees at Nitra accrue 20 days of annual leave per calendar year.")
        };

        var decision = Policy.Decide(
            "How many days of annual leave do employees get?",
            "It depends on the site.",
            citations);

        Assert.Equal(PolicyDecision.NeedsHuman, decision.Decision);
    }

    [Fact]
    public void Allow_when_single_grounded_policy_chunk()
    {
        var citations = new[]
        {
            new Citation("annual-leave.md", "Employees of the Bratislava office receive 25 days of annual leave per calendar year.")
        };

        var decision = Policy.Decide(
            "How many days of annual leave do Bratislava office employees get?",
            "Bratislava office employees receive 25 days of annual leave.",
            citations);

        Assert.Equal(PolicyDecision.Allow, decision.Decision);
    }

    [Fact]
    public void Deny_when_no_citations()
    {
        var decision = Policy.Decide(
            "What is the meaning of life?",
            "42",
            citations: []);

        Assert.Equal(PolicyDecision.Deny, decision.Decision);
    }
}
