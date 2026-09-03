using Moser.RagAi.Ingestion.Infrastructure;

using System;
using System.IO;
using System.Threading.Tasks;

var root = args.Length > 0
    ? args[0]
    : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "data", "seed"));

if (!Directory.Exists(root))
{
    Console.Error.WriteLine("Seed root not found: " + root);
    return 1;
}

var pack = new OfficeSeedPack();
foreach (var directory in Directory.GetDirectories(root))
{
    await pack.MaterializeAsync(directory);
    Console.WriteLine(directory);
}

return 0;
