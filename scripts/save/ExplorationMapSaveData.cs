namespace Knight.Save;

// Pure optional memory: no paths, codec, IO, nodes or core-save coupling.
public sealed class ExplorationMapSaveData
{
    public int FormatVersion { get; set; } = 1;
    public string TerrainRevision { get; set; } = string.Empty;
    public string FreeCellsBase64 { get; set; } = string.Empty;
    public string BoundarySegmentsBase64 { get; set; } = string.Empty;
}
