// Regenerates Pages/Game.razor.cs from a captured perf CSV + run manifest.
//
// Usage:
//   dotnet run --project tools/GameStatsGen -- <capture-id>
//
// Reads:  game-data/<capture-id>/perf_A_<capture-id>.csv
//         game-data/<capture-id>/run_manifest.json
// Writes: Pages/Game.razor.cs

using GameStatsGen;

if (args.Length != 1)
{
    Console.Error.WriteLine("Usage: dotnet run --project tools/GameStatsGen -- <capture-id>");
    return 1;
}

var captureId = args[0];

try
{
    var root = FindRepoRoot();
    var captureDir = Path.Combine(root, "game-data", captureId);
    var csvPath = Path.Combine(captureDir, $"perf_A_{captureId}.csv");
    var manifestPath = Path.Combine(captureDir, "run_manifest.json");

    if (!File.Exists(csvPath))
        throw new FileNotFoundException($"Capture CSV not found: {csvPath}");
    if (!File.Exists(manifestPath))
        throw new FileNotFoundException($"Run manifest not found: {manifestPath}");

    var rows = CsvParser.Parse(csvPath);
    var manifest = ManifestValidator.LoadAndValidate(manifestPath);
    var formulas = new Formulas(rows, manifest);
    var source = CodeGenerator.Render(formulas, captureId);

    var outputPath = Path.Combine(root, "Pages", "Game.razor.cs");
    File.WriteAllText(outputPath, source, new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

    Console.WriteLine($"Wrote {outputPath} from {rows.Length} frames (T_build = {formulas.TBuild:0.0}s).");
    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine($"GameStatsGen: {ex.Message}");
    return 1;
}

static string FindRepoRoot()
{
    var dir = new DirectoryInfo(AppContext.BaseDirectory);
    while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Portfolio.sln")))
        dir = dir.Parent;

    return dir?.FullName
        ?? throw new InvalidOperationException(
            $"Could not locate repo root (Portfolio.sln not found above {AppContext.BaseDirectory}).");
}
