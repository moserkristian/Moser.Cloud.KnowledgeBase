using System;
using System.Collections.Generic;

namespace Moser.RagAi.Assistant.Application;

/// <summary>
/// Enterprise RAG demo corpora. Each folder holds the kind of internal material a
/// Slovak organisation in that sector would actually publish, so answers cite
/// Slovak and EU rules rather than generic boilerplate.
/// </summary>
public enum RagScenario
{
    Legal = 0,
    RealEstate = 1,
    Healthcare = 2,
    Finance = 3,
    Insurance = 4,
    Consulting = 5,
    Corporate = 6
}

public sealed record RagScenarioInfo(
    RagScenario Id,
    string Folder,
    string Title,
    string Blurb,
    IReadOnlyList<string> SampleQuestions);

public static class RagScenarios
{
    public static RagScenario Default { get; } = RagScenario.Legal;

    public static IReadOnlyList<RagScenarioInfo> All { get; } =
    [
        new(
            RagScenario.Legal,
            "legal",
            "Legal / law firm",
            "Slovak advocacy practice: engagements, conflicts, confidentiality, retainers, litigation hold, AML intake.",
            [
                "Čo treba spraviť pred začatím meritornej právnej práce?",
                "How do we handle a conflict with a former client?",
                "Where are retainers held before they are earned?"
            ]),
        new(
            RagScenario.RealEstate,
            "real-estate",
            "Real estate",
            "Slovak brokerage: listings, both-sides consent, deposits, leases, disclosure, land registry closing, owners' associations.",
            [
                "When is listing commission earned on an exclusive listing?",
                "How should a buyer deposit be handled?",
                "How long does the land registry take to register a transfer?"
            ]),
        new(
            RagScenario.Healthcare,
            "healthcare",
            "Healthcare",
            "Slovak outpatient clinic: patient data privacy, informed consent, records access, remote care, insurer billing, emergencies.",
            [
                "When can we disclose patient data without consent?",
                "How fast must we fulfil a request for the medical record?",
                "Which video tools are allowed for a remote consultation?"
            ]),
        new(
            RagScenario.Finance,
            "finance",
            "Finance / wealth",
            "Slovak investment firm under NBS supervision: KYC, suitability, market abuse, complaints, fees, custody.",
            [
                "What is required before opening an advisory account?",
                "Can we recommend leveraged ETFs without extra documentation?",
                "How do we verify a change of payment instructions?"
            ]),
        new(
            RagScenario.Insurance,
            "insurance",
            "Insurance",
            "Slovak insurer: claims intake, coverage interpretation, underwriting authority, fraud investigation, renewals.",
            [
                "How quickly must we acknowledge a new claim?",
                "How long may a claim investigation take?",
                "What routes a claim to the special investigation unit?"
            ]),
        new(
            RagScenario.Consulting,
            "consulting",
            "Consulting",
            "Slovak advisory firm: scope and change orders, NDA, acceptance, T&M rates, conflicts, IP reuse.",
            [
                "How do we handle work outside the statement of work?",
                "How long does the client have to accept a deliverable?",
                "Is travel time fully billable?"
            ]),
        new(
            RagScenario.Corporate,
            "corporate",
            "Corporate HR / policy",
            "Intranet of Moser Slovakia s. r. o.: leave, expenses, security, gifts, procurement — with deliberate collisions.",
            [
                "What is the standard refund window?",
                "Koľko dní dovolenky majú zamestnanci bratislavskej kancelárie?",
                "Is a cash gift from a supplier allowed?"
            ])
    ];

    public static RagScenarioInfo Get(RagScenario id)
    {
        foreach (var item in All)
        {
            if (item.Id == id)
            {
                return item;
            }
        }

        return Get(Default);
    }

    public static bool TryParse(string? value, out RagScenario scenario)
    {
        if (Enum.TryParse(value, ignoreCase: true, out scenario)
            && Enum.IsDefined(scenario))
        {
            return true;
        }

        foreach (var item in All)
        {
            if (string.Equals(item.Folder, value, StringComparison.OrdinalIgnoreCase))
            {
                scenario = item.Id;
                return true;
            }
        }

        scenario = Default;
        return false;
    }

    public static string RelativeSeedPath(RagScenario id, string seedRoot = "data/seed")
        => $"{seedRoot.TrimEnd('/', '\\')}/{Get(id).Folder}";
}
