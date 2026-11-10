namespace Knight.Save;

// Pure optional memory: no paths, codec, IO, nodes or core-save coupling.
public sealed class ExplorationMapSaveData
{
    // Optional-map addressing:1 is the unchanged8-world grid;2 is16-world.
    // Core save schema is independent. Older readers discard unknown optional2.
    public int FormatVersion { get; set; } = 1;
    public string TerrainRevision { get; set; } = string.Empty;
    public string FreeCellsBase64 { get; set; } = string.Empty;
    public string BoundarySegmentsBase64 { get; set; } = string.Empty;
}
