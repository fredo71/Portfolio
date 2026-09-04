using System.Globalization;
using GameStatsGen.Models;

namespace GameStatsGen;

/// <summary>
/// One function per row of game-data-capture-spec.md §6. Identifier names match the
/// @code members they used to replace in Pages/Game.razor.
/// </summary>
public sealed class Formulas
{
    const int ChartPoints = 20; // matches the point count of today's placeholder arrays
    const double FrameBudgetMs = 33.3; // 1000/30

    readonly FrameRow[] _whole;
    readonly FrameRow[] _building;
    readonly FrameRow[] _built;
    readonly RunManifest _manifest;

    public double TBuild { get; }

    public Formulas(FrameRow[] rows, RunManifest manifest)
    {
        _manifest = manifest;

        var firstComplete = rows.FirstOrDefault(r => r.ChunksCompleted == manifest.Map!.ChunksTotal);
        if (firstComplete is null)
        {
            throw new InvalidOperationException(
                $"No row reaches chunks_completed == {manifest.Map!.ChunksTotal} " +
                "(map.chunks_total) — the capture never finishes building.");
        }
        TBuild = firstComplete.ElapsedS;

        _whole = rows.Where(r => r.ElapsedS <= 60).ToArray();
        _building = rows.Where(r => r.ElapsedS <= TBuild).ToArray();
        _built = rows.Where(r => r.ElapsedS > TBuild).ToArray();

        if (_built.Length == 0)
        {
            throw new InvalidOperationException(
                $"The 'once built' window (elapsed_s > {N1(TBuild)}) is empty — T_build >= 60.");
        }
    }

    static double Fps(FrameRow r) => 1000.0 / r.FrameTimeMs;

    static double PctBelow30(IReadOnlyCollection<FrameRow> slice) =>
        100.0 * slice.Count(r => r.FrameTimeMs > FrameBudgetMs) / slice.Count;

    static double Mean(IEnumerable<double> values)
    {
        var list = values as IReadOnlyCollection<double> ?? values.ToArray();
        return list.Count == 0 ? 0.0 : list.Average();
    }

    // ---- §1 — What is this? ----

    public string TotalHexTiles() => N0(_manifest.Map!.TotalHexTiles!.Value);

    public string PctFramesBelow30Overview() => N1(PctBelow30(_whole));

    public string AvgFpsOverview() => N0(1000.0 / Mean(_whole.Select(r => r.FrameTimeMs)));

    public double TimeToFirstHexValue() =>
        _whole.First(r => r.ChunksCompleted >= 1).ElapsedS;

    public string TimeToFirstHex() => N1(TimeToFirstHexValue());

    public string TreeCountWorld()
    {
        var thousands = Math.Round(_manifest.World!.TreeFoliageTotal!.Value / 1000.0);
        return $"{N0(thousands)}k";
    }

    public double[] RunTimelineElapsedS() => _whole.Select(r => r.ElapsedS).ToArray();

    public double[] RunTimelineFps() => _whole.Select(Fps).ToArray();

    // ---- §2 — Building the world ----

    public string BuildTime() => N1(TBuild);

    public string PctFramesBelow30Build() => N1(PctBelow30(_building));

    public double[] HexesFinishedOverTime() =>
        Downsampling.BucketMean(_building, 0, TBuild, ChartPoints, r => r.ChunksCompleted * 42.0);

    public double[] FpsOverTimeBuild() =>
        Downsampling.BucketMean(_building, 0, TBuild, ChartPoints, Fps);

    double RamPct(FrameRow r) => r.TotalMemoryBytes / (_manifest.Budgets!.RamBudgetGb!.Value * 1e9) * 100.0;

    public string AvgRamPctBudget() => N1(Mean(_building.Select(RamPct)));

    public string MaxRamPctBudget() => N1(_building.Max(RamPct));

    public double[] RamPctOverTime() =>
        Downsampling.BucketMean(_building, 0, TBuild, ChartPoints, RamPct);

    // ---- §3 — Once it is built ----

    public string ShadowsOnScreen() => N0(RoundToNearest(Mean(_built.Select(r => (double)r.ShadowGroups)), 10));

    public string DrawDistanceHexes() => N0(Math.Round(Mean(_built.Select(r => (double)r.VisibleChunks)) * 42.0));

    public string BiggestTrianglePctOfScreen() =>
        N2(_manifest.Geometry!.LargestTrianglePixels!.Value / (double)ScreenPixels(_manifest.Hardware!.Resolution!) * 100.0);

    static long ScreenPixels(string resolution)
    {
        var parts = resolution.Split(['x', 'X']);
        if (parts.Length != 2 || !int.TryParse(parts[0], out var width) || !int.TryParse(parts[1], out var height))
            throw new InvalidOperationException($"hardware.resolution '{resolution}' is not in 'WIDTHxHEIGHT' form.");
        return (long)width * height;
    }

    double GpuPct(FrameRow r) => r.GfxDriverBytes / (_manifest.Budgets!.VramBudgetGb!.Value * 1e9) * 100.0;

    public string MaxGpuPctBudget() => N0(_built.Max(GpuPct));

    public string AvgGpuPctBudget() => N0(Mean(_built.Select(GpuPct)));

    public double[] GpuOverTime() =>
        Downsampling.BucketMean(_built, TBuild, 60, ChartPoints, GpuPct);

    public string AvgFpsBuilt() => N0(1000.0 / Mean(_built.Select(r => r.FrameTimeMs)));

    public double[] FpsOverTimeBuilt() =>
        Downsampling.BucketMean(_built, TBuild, 60, ChartPoints, Fps);

    static double RoundToNearest(double value, double step) => Math.Round(value / step) * step;

    static string N0(double v) => v.ToString("N0", CultureInfo.InvariantCulture);
    static string N1(double v) => v.ToString("0.0", CultureInfo.InvariantCulture);
    static string N2(double v) => v.ToString("0.00", CultureInfo.InvariantCulture);
}
