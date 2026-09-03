using Moser.RagAi.Ingestion.Application;

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Moser.RagAi.Ingestion.Infrastructure;

public sealed class OfficeSeedPack : IOfficeSeedPack
{
    public Task MaterializeAsync(string directory, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
        {
            return Task.CompletedTask;
        }

        var folder = Path.GetFileName(directory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        var mailbox = PersonaFor(folder);
        foreach (var markdown in Directory.EnumerateFiles(directory, "*.md", SearchOption.TopDirectoryOnly))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var stem = Path.GetFileNameWithoutExtension(markdown);
            var ext = ExtensionFor(stem);
            var dest = Path.Combine(directory, stem + "." + ext);
            var title = HumanTitle(stem);
            var body = File.ReadAllText(markdown);
            OfficeFileWriter.Write(dest, ext, title, body, mailbox);
            File.Delete(markdown);
        }

        return Task.CompletedTask;
    }

    internal static string ExtensionFor(string stem)
        => stem switch
        {
            "aml-kyc" or "property-disclosure" or "emergency-protocol"
                or "fraud-indicators" or "password-policy" or "data-classification" => "png",
            "document-retention" or "travel-policy" => "doc",
            "client-confidentiality" or "litigation-hold" or "buyer-deposit"
                or "owners-association" or "medical-records-access" or "market-abuse"
                or "custody" or "underwriting" or "deliverable-acceptance"
                or "knowledge-reuse" or "annual-leave-nitra" or "information-security"
                or "vendor-offboarding" or "insider-trading" or "complaint-handling"
                or "renewal" or "conflict-check" => "eml",
            "conflict-of-interest" or "exclusive-listing" or "patient-consent"
                or "suitability" or "coverage-interpretation" or "nda"
                or "refund-premium" or "expense-field-sales" or "annual-leave"
                or "vendor-onboarding" or "remote-work-policy" or "returns-policy"
                or "billing-coding" or "fee-disclosure" or "rates-tm" => "docx",
            _ => "pdf"
        };

    private static string HumanTitle(string stem)
        => stem.Replace('-', ' ');

    internal static MailboxPersona PersonaFor(string folder)
    {
        var sent = new DateTimeOffset(2026, 3, 12, 9, 14, 0, TimeSpan.FromHours(1));
        return folder switch
        {
            "legal" => new(
                "Advokátska kancelária Hoferová & Bartoš s. r. o.",
                "Laurinská 12, 811 01 Bratislava",
                "IČO 51 234 567 · DIČ 2120123456 · IČ DPH SK2120123456 · SAK 1847",
                "Bratislava",
                "12. 3. 2026",
                sent,
                "Mgr. Jana Hoferová <j.hoferova@ak-hb.sk>",
                "spisovna@ak-hb.sk",
                "koncipienti@ak-hb.sk",
                "ak-hb.sk",
                "Mgr. Jana Hoferová",
                "advokátka, konateľka",
                "Hoferová & Bartoš s. r. o. · zapísaná v zozname SAK · www.ak-hb.sk",
                "H&B"),
            "real-estate" => new(
                "Dunaj Reality s. r. o.",
                "Štúrova 8, 811 02 Bratislava",
                "IČO 46 891 203 · DIČ 2023481201 · NARKS 2204",
                "Bratislava",
                "4. 2. 2026",
                sent.AddDays(-18),
                "Ing. Lucia Tóthová <l.tothova@dunajreality.sk>",
                "obchody@dunajreality.sk",
                "backoffice@dunajreality.sk",
                "dunajreality.sk",
                "Ing. Lucia Tóthová",
                "konateľka, realitná maklérka",
                "Dunaj Reality s. r. o. · člen NARKS · kataster BA I–V",
                "DR"),
            "healthcare" => new(
                "Poliklinika Karlovka s. r. o.",
                "Račianska 71, 831 02 Bratislava",
                "IČO 35 887 441 · IČZ 63-000441-A · ÚDZS",
                "Bratislava",
                "21. 1. 2026",
                sent.AddDays(-36),
                "MUDr. Peter Kováč <p.kovac@karlovka.sk>",
                "registratura@karlovka.sk",
                "gdpr@karlovka.sk",
                "karlovka.sk",
                "MUDr. Peter Kováč",
                "hlavný lekár",
                "Poliklinika Karlovka · ambulancie všeobecného lekárstva a špecializácií",
                "PK"),
            "finance" => new(
                "Karpaty Wealth, o. c. p., a. s.",
                "Pribinova 4, 811 09 Bratislava",
                "IČO 35 760 112 · NBS o.c.p. · LEI 097900BIGH0000123456",
                "Bratislava",
                "9. 3. 2026",
                sent.AddDays(-3),
                "Ing. Martin Vargovčík <m.vargovcik@karpatywealth.sk>",
                "compliance@karpatywealth.sk",
                "backoffice@karpatywealth.sk",
                "karpatywealth.sk",
                "Ing. Martin Vargovčík",
                "vedúci compliance",
                "Karpaty Wealth, o. c. p., a. s. · pod dohľadom Národnej banky Slovenska",
                "KW"),
            "insurance" => new(
                "Tatry Poisťovňa, a. s.",
                "Plynárenská 7/A, 821 09 Bratislava",
                "IČO 00 151 441 · NBS poisťovňa · IČ DPH SK2020123344",
                "Bratislava",
                "16. 2. 2026",
                sent.AddDays(-24),
                "Mgr. Eva Mináriková <e.minarikova@tatrypoistovna.sk>",
                "skody@tatrypoistovna.sk",
                "underwriting@tatrypoistovna.sk",
                "tatrypoistovna.sk",
                "Mgr. Eva Mináriková",
                "vedúca likvidácie škôd",
                "Tatry Poisťovňa, a. s. · pobočková sieť SR · nonstop linka 0850 111 000",
                "TP"),
            "consulting" => new(
                "Karpaty Advisory s. r. o.",
                "Námestie SNP 15, 811 01 Bratislava",
                "IČO 44 220 118 · IČ DPH SK2022684410",
                "Bratislava",
                "27. 2. 2026",
                sent.AddDays(-13),
                "Ing. Tomáš Belko <t.belko@karpatyadvisory.sk>",
                "engagements@karpatyadvisory.sk",
                "legal@karpatyadvisory.sk",
                "karpatyadvisory.sk",
                "Ing. Tomáš Belko",
                "partner",
                "Karpaty Advisory s. r. o. · manažérske poradenstvo · Bratislava / Košice",
                "KA"),
            _ => new(
                "Moser Slovakia s. r. o.",
                "Twin City C, Mlynské nivy 12, 821 09 Bratislava",
                "IČO 47 556 210 · DIČ 2022468911 · IČ DPH SK2022468911",
                "Bratislava",
                "1. 3. 2026",
                sent.AddDays(-11),
                "Mgr. Zuzana Králiková <z.kralikova@moser.sk>",
                "all-ba@moser.sk",
                "compliance@moser.sk",
                "moser.sk",
                "Mgr. Zuzana Králiková",
                "Ľudské zdroje / compliance",
                "Moser Slovakia s. r. o. · Bratislava · Nitra · interná dokumentácia",
                "MS")
        };
    }
}
