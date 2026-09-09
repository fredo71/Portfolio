namespace GameStatsGen.Models;

public sealed record FrameRow(
    int FrameIndex,
    double ElapsedS,
    double FrameTimeMs,
    long TotalMemoryBytes,
    long GfxDriverBytes,
    int VisibleChunks,
    int ShadowGroups,
    int ChunksCompleted);
