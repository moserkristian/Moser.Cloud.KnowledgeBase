using Moser.Enterprise.Blueprint.Assistant.Application;

using System.Collections.Generic;

namespace Moser.Enterprise.Blueprint.Assistant.Infrastructure;

internal sealed class SeedSynthesizer : ISeedFaqSynthesizer
{
    public IReadOnlyList<(string Source, string Markdown)> Synthesize()
    {
        var items = new List<(string Source, string Markdown)>(50);
        var topics = new[]
        {
            ("refund", "Standard refunds for catalog SKUs follow the posted window in the matching policy document."),
            ("pto", "US PTO is tracked in Workday. EU annual leave is tracked by the local HRIS."),
            ("expense", "Submit expenses in the finance portal within 30 days of the transaction date."),
            ("travel", "Book travel through the designated corporate agency unless an exception is approved."),
            ("security", "Report suspected phishing to infosec@internal within one hour of discovery."),
            ("vendor", "Vendor access must be ticketed. Do not share standing credentials."),
            ("sla", "Missed SLA events are logged; do not invent compensation without Support leadership."),
            ("gift", "Ask Compliance before accepting any gift that is not a low-value branded item."),
            ("remote", "Core hours are published by each region; managers cannot waive security controls."),
            ("conduct", "Raise conduct concerns to HR. Do not investigate coworkers privately.")
        };

        var roles = new[] { "employee", "manager", "contractor", "finance-partner", "support-agent" };

        var i = 0;
        foreach (var (topic, rule) in topics)
        {
            foreach (var role in roles)
            {
                if (items.Count >= 50)
                {
                    return items;
                }

                i++;
                items.Add((
                    $"faq-{topic}-{role}.md",
                    $"# FAQ {i}: {topic} for {role}\n\nQ: What should a {role} remember about {topic}?\nA: {rule}\n"));
            }
        }

        return items;
    }
}
