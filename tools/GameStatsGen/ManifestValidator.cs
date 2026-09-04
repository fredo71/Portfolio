using System.Text.Json;
using GameStatsGen.Models;

namespace GameStatsGen;

public sealed class ManifestValidationException(string message) : Exception(message);

public static class ManifestValidator
{
    static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    };

    public static RunManifest LoadAndValidate(string path)
    {
        var json = File.ReadAllText(path);
        var manifest = JsonSerializer.Deserialize<RunManifest>(json, JsonOptions)
            ?? throw new ManifestValidationException($"{path}: could not be parsed as JSON.");

        Require(manifest.Map?.TotalHexTiles, path, "map.total_hex_tiles", "TotalHexTiles");
        Require(manifest.World?.TreeFoliageTotal, path, "world.tree_foliage_total", "TreeCountWorld");
        Require(manifest.Budgets?.RamBudgetGb, path, "budgets.ram_budget_gb", "AvgRamPctBudget/MaxRamPctBudget/RamPctOverTime");
        Require(manifest.Budgets?.VramBudgetGb, path, "budgets.vram_budget_gb", "MaxGpuPctBudget/AvgGpuPctBudget/GpuOverTime");
        Require(manifest.Geometry?.LargestTrianglePixels, path, "geometry.largest_triangle_pixels", "BiggestTrianglePctOfScreen");
        Require(manifest.Hardware?.Resolution, path, "hardware.resolution", "BiggestTrianglePctOfScreen");

        return manifest;
    }

    static void Require(object? value, string path, string fieldPath, string usedBy)
    {
        if (value is null)
        {
            throw new ManifestValidationException(
                $"{path} is missing '{fieldPath}' (needed for {usedBy}).");
        }
    }
}
