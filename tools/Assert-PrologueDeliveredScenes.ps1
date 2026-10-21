function Assert-PrologueDeliveredScenes {
    param([Parameter(Mandatory = $true)][string]$ProjectRoot)

    # Check the on-disk scenes that ordinary play will actually load, not a transient
    # builder/capture result. This guard is deliberately read-only.
    $expected = [ordered]@{
        CanyonEntrance = 3
        CanyonDescent = 4
        CanyonBasin = 3
        CanyonAscent = 3
        LostTemple = 2
    }
    foreach ($entry in $expected.GetEnumerator()) {
        $scenePath = Join-Path $ProjectRoot ("scenes\prologue\sectors\{0}.tscn" -f $entry.Key)
        if (-not (Test-Path -LiteralPath $scenePath -PathType Leaf)) {
            throw "Delivered sector missing: $scenePath"
        }
        $scene = [IO.File]::ReadAllText($scenePath)
        $count = [regex]::Matches($scene, '(?m)^\[node [^\r\n]*parent="Gameplay/MicroScenes"(?:\s|\])').Count
        if ($count -ne $entry.Value -or $scene -notmatch 'name="GrappleSurfaceProfile"') {
            throw "Delivered sector is stale: $($entry.Key) has $count micro-scenes; expected $($entry.Value) and terrain grapple profiles. Rebuild and retain the five canyon sectors before playing."
        }
    }
    'PROLOGUE_DELIVERED_SCENES_OK sectors=5 micro_scenes=15 terrain_profiles=True'
}
