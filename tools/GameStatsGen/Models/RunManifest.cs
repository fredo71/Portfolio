namespace GameStatsGen.Models;

public sealed record RunManifest(
    MapInfo? Map,
    HardwareInfo? Hardware,
    BudgetsInfo? Budgets,
    WorldInfo? World,
    GeometryInfo? Geometry);

public sealed record MapInfo(
    int? ChunksTotal,
    int? HexesPerChunk,
    int? TotalHexTiles);

public sealed record HardwareInfo(
    string? Gpu,
    string? Cpu,
    string? Resolution,
    bool? Vsync,
    int? FrameCap,
    string? UnityVersion,
    string? BuildType);

public sealed record BudgetsInfo(
    double? RamBudgetGb,
    double? VramBudgetGb);

public sealed record WorldInfo(
    double? TreeFoliageTotal,
    double? HexTileWorldAreaM2);

public sealed record GeometryInfo(
    LodTriangleTable? LodTriangleTable,
    int? LargestTrianglePixels);

public sealed record LodTriangleTable(
    int? Lod1,
    int? Lod2,
    int? Lod3,
    int? Lod4,
    int? Lod5);
