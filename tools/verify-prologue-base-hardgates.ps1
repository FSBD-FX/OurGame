[CmdletBinding()]
param(
    [switch]$SuppressTerminal,
    [string]$PresentationEvidenceLog,
    [switch]$PresentationEvidenceOnly,
    [string]$AttackPresentationEvidenceLog,
    [switch]$AttackPresentationEvidenceOnly,
    [string]$CriticalSmokeEvidenceLog,
    [switch]$CriticalSmokeEvidenceOnly,
    [string]$ExternalEvidenceRoot,
    [string]$ExternalEvidenceManifest,
    [switch]$ExternalEvidenceManifestOnly,
    [datetime]$ExternalEvidenceNotBeforeUtc = [DateTime]::MinValue
)

$ErrorActionPreference = 'Stop'
$project = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
. (Join-Path $PSScriptRoot 'EvidenceChecks.ps1')
. (Join-Path $PSScriptRoot 'Task6ImportRetry.ps1')
$dotnetRoot = 'G:\godot\dotnet-sdk'
$godot = 'G:\godot\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64_console.exe'
$env:DOTNET_ROOT = $dotnetRoot
$env:DOTNET_ROOT_X64 = $dotnetRoot
$env:DOTNET_ROLL_FORWARD = 'LatestMajor'
$env:Path = "$dotnetRoot;$env:Path"

function Get-Task6CompatiblePowerShellHost {
    $current = Get-Process -Id $PID -ErrorAction SilentlyContinue
    if ($null -ne $current -and -not [string]::IsNullOrWhiteSpace($current.Path) -and
        (Test-Path -LiteralPath $current.Path -PathType Leaf)) {
        return $current.Path
    }
    foreach ($name in @('pwsh', 'powershell.exe', 'powershell')) {
        $command = Get-Command $name -ErrorAction SilentlyContinue
        if ($null -ne $command -and -not [string]::IsNullOrWhiteSpace($command.Source)) {
            return $command.Source
        }
    }
    throw 'No compatible PowerShell child host is available for BASE hard gates.'
}

function Get-Task6AbsolutePath {
    param([Parameter(Mandatory)][string]$Path)

    if ([string]::IsNullOrWhiteSpace($Path)) {
        throw 'BASE_EXTERNAL_EVIDENCE_PATH_REQUIRED'
    }
    return [IO.Path]::GetFullPath($Path).TrimEnd('\')
}

function Test-Task6PathWithinRoot {
    param(
        [Parameter(Mandatory)][string]$Path,
        [Parameter(Mandatory)][string]$Root
    )

    $fullPath = Get-Task6AbsolutePath -Path $Path
    $fullRoot = Get-Task6AbsolutePath -Path $Root
    return $fullPath.Equals($fullRoot, [StringComparison]::OrdinalIgnoreCase) -or
        $fullPath.StartsWith(($fullRoot + '\'), [StringComparison]::OrdinalIgnoreCase)
}

function ConvertTo-Task6UtcTimestamp {
    param([Parameter(Mandatory)][string]$Value, [Parameter(Mandatory)][string]$Label)

    $parsed = [DateTime]::MinValue
    if (-not [DateTime]::TryParse($Value, [Globalization.CultureInfo]::InvariantCulture,
            [Globalization.DateTimeStyles]::RoundtripKind, [ref]$parsed)) {
        throw "BASE_EXTERNAL_EVIDENCE_MALFORMED label=$Label value=$Value"
    }
    return $parsed.ToUniversalTime()
}

function Assert-Task6GeneratedRuntimeSemanticEvidence {
    param([Parameter(Mandatory)][string]$Path)

    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        throw "BASE_GENERATED_RUNTIME_SEMANTICS_MISSING path=$Path"
    }
    $expectedPaths = @(
        'scenes/prologue/sectors/CanyonEntrance.tscn',
        'scenes/prologue/sectors/CanyonDescent.tscn',
        'scenes/prologue/sectors/CanyonBasin.tscn',
        'scenes/prologue/sectors/CanyonAscent.tscn',
        'scenes/prologue/sectors/LostTemple.tscn',
        'scenes/prologue/PrologueLevel.tscn'
    )
    $lines = @(Get-Content -LiteralPath $Path)
    if ($lines.Count -ne ($expectedPaths.Count + 1)) {
        throw "BASE_GENERATED_RUNTIME_SEMANTICS_MALFORMED label=line_count expected=$($expectedPaths.Count + 1) actual=$($lines.Count) path=$Path"
    }
    $marker = [regex]::Match($lines[0], '^PROLOGUE_GENERATED_RUNTIME_SEMANTICS_OK sha=(?<sha>[a-f0-9]{64}) count=(?<count>\d+)$')
    if (-not $marker.Success -or [int]$marker.Groups['count'].Value -ne $expectedPaths.Count) {
        throw "BASE_GENERATED_RUNTIME_SEMANTICS_MALFORMED label=marker path=$Path"
    }
    for ($index = 0; $index -lt $expectedPaths.Count; $index++) {
        $expected = 'path=' + $expectedPaths[$index] + ' sha256='
        if ($lines[$index + 1] -cnotmatch ('^' + [regex]::Escape($expected) + '[a-f0-9]{64}$')) {
            throw "BASE_GENERATED_RUNTIME_SEMANTICS_PATH_MISMATCH index=$index expected=$($expectedPaths[$index]) path=$Path"
        }
    }
    return [pscustomobject]@{
        Sha256 = $marker.Groups['sha'].Value
        Count = $expectedPaths.Count
        Paths = $expectedPaths
    }
}

function Get-Task6ExternalEvidenceManifest {
    param(
        [string]$ManifestPath,
        [datetime]$NotBeforeUtc = [DateTime]::MinValue
    )

    if ([string]::IsNullOrWhiteSpace($ManifestPath)) {
        throw 'BASE_EXTERNAL_EVIDENCE_MANIFEST_REQUIRED'
    }
    if (-not (Test-Path -LiteralPath $ManifestPath -PathType Leaf)) {
        throw "BASE_EXTERNAL_EVIDENCE_MANIFEST_MISSING path=$ManifestPath"
    }
    $fields = @{}
    $fileRows = [Collections.Generic.List[string]]::new()
    foreach ($line in @(Get-Content -LiteralPath $ManifestPath)) {
        if ([string]::IsNullOrWhiteSpace($line)) { continue }
        $parts = $line -split '=', 2
        if ($parts.Count -ne 2 -or [string]::IsNullOrWhiteSpace($parts[0])) {
            throw "BASE_EXTERNAL_EVIDENCE_MALFORMED label=line value=$line"
        }
        if ($parts[0] -ceq 'file') {
            $fileRows.Add($parts[1])
            continue
        }
        if ($fields.ContainsKey($parts[0])) {
            throw "BASE_EXTERNAL_EVIDENCE_MALFORMED label=duplicate_field value=$($parts[0])"
        }
        $fields[$parts[0]] = $parts[1]
    }
    foreach ($required in @(
        'schema', 'run_id', 'candidate_root', 'evidence_root', 'created_after_utc', 'log_count'
    )) {
        if (-not $fields.ContainsKey($required) -or [string]::IsNullOrWhiteSpace([string]$fields[$required])) {
            throw "BASE_EXTERNAL_EVIDENCE_MALFORMED label=missing_field value=$required"
        }
    }
    if ($fields['schema'] -cne 'task6-base-external-evidence-v1') {
        throw "BASE_EXTERNAL_EVIDENCE_MALFORMED label=schema value=$($fields['schema'])"
    }
    $runId = [guid]::Empty
    if (-not [guid]::TryParse([string]$fields['run_id'], [ref]$runId)) {
        throw "BASE_EXTERNAL_EVIDENCE_MALFORMED label=run_id value=$($fields['run_id'])"
    }
    $candidateRoot = Get-Task6AbsolutePath -Path ([string]$fields['candidate_root'])
    if (-not $candidateRoot.Equals((Get-Task6AbsolutePath -Path $project), [StringComparison]::OrdinalIgnoreCase)) {
        throw "BASE_EXTERNAL_EVIDENCE_CANDIDATE_MISMATCH expected=$project actual=$candidateRoot"
    }
    $evidenceRoot = Get-Task6AbsolutePath -Path ([string]$fields['evidence_root'])
    if (Test-Task6PathWithinRoot -Path $evidenceRoot -Root $project) {
        throw "BASE_EXTERNAL_EVIDENCE_ROOT_INSIDE_PROJECT root=$evidenceRoot project=$project"
    }
    $resolvedManifest = Get-Task6AbsolutePath -Path ((Resolve-Path -LiteralPath $ManifestPath).Path)
    if (-not (Test-Task6PathWithinRoot -Path $resolvedManifest -Root $evidenceRoot)) {
        throw "BASE_EXTERNAL_EVIDENCE_PATH_OUTSIDE_ROOT path=$resolvedManifest root=$evidenceRoot"
    }
    $createdAfterUtc = ConvertTo-Task6UtcTimestamp -Value ([string]$fields['created_after_utc']) `
        -Label 'created_after_utc'
    $requiredFreshness = $NotBeforeUtc.ToUniversalTime()
    if ($requiredFreshness -ne [DateTime]::MinValue -and $createdAfterUtc -lt $requiredFreshness) {
        throw "BASE_EXTERNAL_EVIDENCE_STALE created_after_utc=$($createdAfterUtc.ToString('o')) not_before_utc=$($requiredFreshness.ToString('o'))"
    }
    $declaredCount = 0
    if (-not [int]::TryParse([string]$fields['log_count'], [ref]$declaredCount) -or $declaredCount -lt 1 -or
        $declaredCount -ne $fileRows.Count) {
        throw "BASE_EXTERNAL_EVIDENCE_MALFORMED label=log_count value=$($fields['log_count']) actual=$($fileRows.Count)"
    }
    $files = @{}
    foreach ($row in $fileRows) {
        $parts = $row -split '\|', 3
        if ($parts.Count -ne 3 -or [string]::IsNullOrWhiteSpace($parts[0]) -or
            $parts[1] -notmatch '^[A-Fa-f0-9]{64}$') {
            throw "BASE_EXTERNAL_EVIDENCE_MALFORMED label=file value=$row"
        }
        $relative = $parts[0].Replace('/', '\')
        $path = Get-Task6AbsolutePath -Path (Join-Path $evidenceRoot $relative)
        if (-not (Test-Task6PathWithinRoot -Path $path -Root $evidenceRoot)) {
            throw "BASE_EXTERNAL_EVIDENCE_PATH_OUTSIDE_ROOT path=$path root=$evidenceRoot"
        }
        if ($files.ContainsKey($relative)) {
            throw "BASE_EXTERNAL_EVIDENCE_MALFORMED label=duplicate_file value=$relative"
        }
        if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
            throw "BASE_EXTERNAL_EVIDENCE_FILE_MISSING path=$path"
        }
        $recordedWriteUtc = ConvertTo-Task6UtcTimestamp -Value $parts[2] -Label 'file_last_write_utc'
        $actualWriteUtc = (Get-Item -LiteralPath $path).LastWriteTimeUtc
        if ($actualWriteUtc -lt $requiredFreshness -or $recordedWriteUtc -lt $requiredFreshness) {
            throw "BASE_EXTERNAL_EVIDENCE_STALE path=$path actual_utc=$($actualWriteUtc.ToString('o')) not_before_utc=$($requiredFreshness.ToString('o'))"
        }
        if ((Get-EvidenceSha256 -LiteralPath $path) -cne $parts[1].ToLowerInvariant()) {
            throw "BASE_EXTERNAL_EVIDENCE_HASH_MISMATCH path=$path"
        }
        $files[$relative] = $path
    }
    foreach ($required in @(
        'verify-prologue-attack-presentation.log',
        'verify-prologue-base-attack-presentation.log',
        'verify-prologue-smoke.log',
        'verify-prologue-generated-runtime-semantics.txt'
    )) {
        if (-not $files.ContainsKey($required)) {
            throw "BASE_EXTERNAL_EVIDENCE_REQUIRED_FILE_MISSING relative=$required"
        }
    }
    $generatedRuntimeSemantics = Assert-Task6GeneratedRuntimeSemanticEvidence `
        -Path $files['verify-prologue-generated-runtime-semantics.txt']
    if (-not $fields.ContainsKey('generated_runtime_sha') -or
        [string]$fields['generated_runtime_sha'] -cnotmatch '^[a-f0-9]{64}$' -or
        [string]$fields['generated_runtime_sha'] -cne $generatedRuntimeSemantics.Sha256) {
        throw "BASE_GENERATED_RUNTIME_SEMANTICS_HASH_MISMATCH manifest=$ManifestPath"
    }
    return [pscustomobject]@{
        Manifest = $resolvedManifest; Root = $evidenceRoot; RunId = $runId.ToString();
        CreatedAfterUtc = $createdAfterUtc; Files = $files
        GeneratedRuntimeSemantics = $generatedRuntimeSemantics
    }
}

function New-Task6ExternalEvidenceRoot {
    param([string]$Path)

    if ([string]::IsNullOrWhiteSpace($Path)) {
        throw 'BASE_EXTERNAL_EVIDENCE_ROOT_REQUIRED'
    }
    $resolved = Get-Task6AbsolutePath -Path $Path
    if (Test-Task6PathWithinRoot -Path $resolved -Root $project) {
        throw "BASE_EXTERNAL_EVIDENCE_ROOT_INSIDE_PROJECT root=$resolved project=$project"
    }
    if (Test-Path -LiteralPath $resolved) {
        throw "BASE_EXTERNAL_EVIDENCE_ROOT_NOT_FRESH root=$resolved"
    }
    New-Item -ItemType Directory -Path $resolved -Force | Out-Null
    return $resolved
}

function Write-Task6ExternalEvidenceManifest {
    param(
        [Parameter(Mandatory)][string]$Root,
        [Parameter(Mandatory)][DateTime]$StartedUtc
    )

    $resolvedRoot = Get-Task6AbsolutePath -Path $Root
    $files = @(Get-ChildItem -LiteralPath $resolvedRoot -File -Recurse | Sort-Object FullName)
    if ($files.Count -eq 0) { throw "BASE_EXTERNAL_EVIDENCE_MALFORMED label=empty_root value=$resolvedRoot" }
    $generatedRuntimeEvidencePath = Join-Path $resolvedRoot 'verify-prologue-generated-runtime-semantics.txt'
    $generatedRuntimeSemantics = Assert-Task6GeneratedRuntimeSemanticEvidence -Path $generatedRuntimeEvidencePath
    $prefix = $resolvedRoot + '\'
    $lines = [Collections.Generic.List[string]]::new()
    $lines.Add('schema=task6-base-external-evidence-v1')
    $lines.Add('run_id=' + [guid]::NewGuid().ToString())
    $lines.Add('candidate_root=' + (Get-Task6AbsolutePath -Path $project))
    $lines.Add('evidence_root=' + $resolvedRoot)
    $lines.Add('created_after_utc=' + $StartedUtc.ToUniversalTime().ToString('o'))
    $lines.Add('generated_runtime_sha=' + $generatedRuntimeSemantics.Sha256)
    $lines.Add('log_count=' + $files.Count)
    foreach ($file in $files) {
        $path = Get-Task6AbsolutePath -Path $file.FullName
        if (-not $path.StartsWith($prefix, [StringComparison]::OrdinalIgnoreCase)) {
            throw "BASE_EXTERNAL_EVIDENCE_PATH_OUTSIDE_ROOT path=$path root=$resolvedRoot"
        }
        if ($file.LastWriteTimeUtc -lt $StartedUtc.ToUniversalTime()) {
            throw "BASE_EXTERNAL_EVIDENCE_STALE path=$path actual_utc=$($file.LastWriteTimeUtc.ToString('o')) not_before_utc=$($StartedUtc.ToUniversalTime().ToString('o'))"
        }
        $relative = $path.Substring($prefix.Length).Replace('\\', '/')
        $lines.Add('file=' + $relative + '|' + (Get-EvidenceSha256 -LiteralPath $path) + '|' +
            $file.LastWriteTimeUtc.ToString('o'))
    }
    $manifest = Join-Path $resolvedRoot 'base-evidence-manifest.txt'
    [IO.File]::WriteAllLines($manifest, $lines.ToArray(), [Text.UTF8Encoding]::new($false))
    return $manifest
}

function Assert-OrderedExactLogMarkers {
    param(
        [Parameter(Mandatory)][string]$Path,
        [Parameter(Mandatory)][string[]]$Markers
    )
    $lines = @(Get-Content -LiteralPath $Path)
    $previous = -1
    foreach ($marker in $Markers) {
        $markerIndices = @()
        for ($index = 0; $index -lt $lines.Count; $index++) {
            if ($lines[$index] -ceq $marker) { $markerIndices += $index }
        }
        if ($markerIndices.Count -ne 1) {
            throw "Expected exactly one ordered '$marker' line in $Path; found $($markerIndices.Count)."
        }
        if ($markerIndices[0] -le $previous) {
            throw "Ordered marker '$marker' appears at line $($markerIndices[0] + 1) after prior line $($previous + 1)."
        }
        $previous = $markerIndices[0]
    }
}

function Assert-OrderedRegexLogMarkers {
    param(
        [Parameter(Mandatory)][string]$Path,
        [Parameter(Mandatory)][string[]]$Patterns
    )
    $lines = @(Get-Content -LiteralPath $Path)
    $previous = -1
    foreach ($pattern in $Patterns) {
        $markerIndices = @()
        for ($index = 0; $index -lt $lines.Count; $index++) {
            if ($lines[$index] -match $pattern) { $markerIndices += $index }
        }
        if ($markerIndices.Count -ne 1) {
            throw "Expected exactly one ordered /$pattern/ line in $Path; found $($markerIndices.Count)."
        }
        if ($markerIndices[0] -le $previous) {
            throw "Ordered /$pattern/ appears at line $($markerIndices[0] + 1) after prior line $($previous + 1)."
        }
        $previous = $markerIndices[0]
    }
}

function Test-ExactBytes {
    param(
        [Parameter(Mandatory)][byte[]]$Expected,
        [Parameter(Mandatory)][byte[]]$Actual
    )
    if ($Expected.Length -gt 8388608 -or $Actual.Length -gt 8388608) {
        throw 'BASE exact-byte protection is limited to small generated scenes and project.godot.'
    }
    if ($Expected.Length -ne $Actual.Length) { return $false }
    for ($index = 0; $index -lt $Expected.Length; $index++) {
        if ($Expected[$index] -ne $Actual[$index]) { return $false }
    }
    return $true
}

function Join-PhaseLogsDeterministically {
    param(
        [Parameter(Mandatory)][string]$OutputPath,
        [Parameter(Mandatory)][string[]]$Paths
    )

    $chunks = foreach ($path in $Paths) {
        if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
            throw "Required prologue phase log is missing: $path"
        }
        Get-Content -LiteralPath $path -Raw
    }
    [System.IO.File]::WriteAllText($OutputPath, ($chunks -join [Environment]::NewLine),
        [System.Text.UTF8Encoding]::new($false))
}

function Assert-BasePresentationEvidence {
    param([Parameter(Mandatory)][string]$Path)
    # Schema v2 replaces the obsolete mandatory-Dash stage with the verified generated-main
    # no-Dash traversal. This gate refuses both a missing migration terminal and the legacy
    # air_dash_consumed marker, so the two contracts cannot accidentally be green together.
    Assert-ExactLogMarker -Path $Path `
        -Marker 'PROLOGUE_PRESENTATION_INPUTMAP_PROOF_OK route=generated_main inputmap=True post_proof_reposition=True'
    Assert-ExactLogMarker -Path $Path -Marker 'TASK6_NO_DASH_TRAVERSAL_OK mandatory_dash=False route=generated_main'
    Assert-ExactLogMarker -Path $Path `
        -Marker 'TASK6_INPUT_VIEWPORT_BASELINE_OK sampled=(0, 0) events=viewport+inputmap os_warp=False'
    Assert-ExactLogMarker -Path $Path `
        -Marker 'TASK6_INPUT_VIEWPORT_SAMPLE_OK sampled=(1120, 620) intended=(1120, 620) events=viewport+inputmap os_warp=False'
    Assert-ExactLogMarker -Path $Path `
        -Marker 'TASK6_INPUT_RETICLE_BINDING_OK sampled=(1120, 620) reticle=(1120, 620) adapter=True reticle_visible=True events=viewport+inputmap os_warp=False'
    if (@(Get-Content -LiteralPath $Path | Where-Object {
        $_ -match 'air_dash_consumed|TRAVERSAL_PROOF_STAGE=.*dash|events=os-warp\+viewport\+inputmap'
    }).Count -ne 0) {
        throw 'BASE Task6 migration rejects legacy mandatory-Dash or OS-warp input markers.'
    }
    Assert-OrderedRegexLogMarkers -Path $Path -Patterns @(
        '^PROLOGUE_PRESENTATION_STAGE=canyon_descent frame=\d+$',
        '^PROLOGUE_PRESENTATION_STAGE=ascent_one frame=\d+$',
        '^PROLOGUE_PRESENTATION_STAGE=ascent_two frame=\d+$',
        '^PROLOGUE_PRESENTATION_STAGE=ascent_three frame=\d+$',
        '^PROLOGUE_PRESENTATION_STAGE=boost_attached frame=\d+$',
        '^PROLOGUE_PRESENTATION_STAGE=boost_start frame=\d+$',
        '^PROLOGUE_PRESENTATION_STAGE=attached_boost_consumed frame=\d+$',
        '^PROLOGUE_PRESENTATION_STAGE=boost_arc frame=\d+$',
        '^PROLOGUE_PRESENTATION_STAGE=boost_second_rejected frame=\d+$',
        '^PROLOGUE_PRESENTATION_STAGE=boost_land frame=\d+$'
    )
    foreach ($marker in @(
        'PROLOGUE_PRESENTATION_ATTACHED_BOOST_OK cast_retained=True first=True second_rejected=True normal_air_jump=False inputmap=True',
        'PROLOGUE_RETICLE_DWELL_OK count=6 states=Attack,Grippable,Ice,Forbidden,Obstructed,OutOfRange inputmap=True',
        'TASK6_CLAW_STATE_OK state=Latched inputmap=True',
        'TASK6_CLAW_STATE_OK state=Boost inputmap=True',
        'TASK6_CLAW_STATE_OK state=Idle inputmap=True',
        'TASK6_CLAW_FAILURE_DWELL_OK failure=IceSlip visible=True inputmap=True',
        'TASK6_CLAW_FAILURE_DWELL_OK failure=Forbidden visible=True inputmap=True',
        'TASK6_CLAW_FAILURE_DWELL_OK failure=Obstructed visible=True inputmap=True',
        'TASK6_CLAW_RECOVERY_OK state=Idle post_boost=True',
        'PROLOGUE_PRESENTATION_RECOVERY_OK checkpoints=2 pre_checkpoint_start=True boss_death=True retry=True completion=True fall_context_version=3',
        'TASK6_CLAW_PRESENTATION_OK states=Latched,Boost,Idle failures=IceSlip,Forbidden,Obstructed dwells=6 recovery=True',
        'PROLOGUE_PRESENTATION_BOSS_ATTACKS_OK live=True attacks=Sweep,LeapSlam,GroundWave,HammerThrow inputmap=True',
        'PROLOGUE_PRESENTATION_STAGES_OK count=30 reticles=6 boost=True checkpoints=2 boss_attacks=4'
    )) { Assert-ExactLogMarker -Path $Path -Marker $marker }
    $complete = @(Get-Content -LiteralPath $Path | Where-Object {
        $_ -match '^PROLOGUE_PRESENTATION_COMPLETE frame=\d+ route=generated_main inputmap=True presentation_reposition=post_proof title_return=True$'
    })
    if ($complete.Count -ne 1) { throw "Task6 presentation completion must occur exactly once; actual=$($complete.Count)" }
    Assert-NoEngineLogErrors -Paths @($Path)
}

function Assert-BaseAttackPresentationEvidence {
    param([Parameter(Mandatory)][string]$Path)
    foreach ($marker in @(
        'PROLOGUE_ATTACK_SEQUENCE_OK RustShell/RustShellThrust',
        'PROLOGUE_ATTACK_SEQUENCE_OK GraveLampMoth/MothDive',
        'PROLOGUE_ATTACK_SEQUENCE_OK BellBackLaborer/LaborerSweep',
        'PROLOGUE_ATTACK_SEQUENCE_OK Boss/BossSweep',
        'PROLOGUE_ATTACK_SEQUENCE_OK Boss/BossLeapSlam',
        'PROLOGUE_ATTACK_SEQUENCE_OK Boss/BossGroundWave',
        'PROLOGUE_ATTACK_SEQUENCE_OK Boss/BossHammerThrow',
        'TASK6_BASE_ATTACK_PRESENTATION_OK sequences=7 runtime_packed_scenes=True'
    )) { Assert-ExactLogMarker -Path $Path -Marker $marker }
    Assert-NoEngineLogErrors -Paths @($Path)
}

function Assert-BaseCriticalSmokeEvidence {
    param([Parameter(Mandatory)][string]$Path)

    # The six-room ThreadAbility smoke was retired when the canonical main became the
    # five-sector InputMap mechanical-claw route.  This is a schema migration, not a second
    # green branch: require the generated-main proof, its real physical-death cleanup, and the
    # new full-route headroom contract.  The legacy source remains available only for historical
    # review and is never accepted as BASE runtime evidence.
    foreach ($marker in @(
        'PROLOGUE_INPUT_PROOF_OK q=True e=True mouse=True slot1=True',
        'PROLOGUE_ENERGY_SHIELD_PROOF_OK attack=True normal=True heavy=True overheat=True',
        'PROLOGUE_SURFACE_PROOF_OK material=GripStone',
        'PROLOGUE_SURFACE_PROOF_OK material=TempleMetal',
        'PROLOGUE_SURFACE_PROOF_OK material=DedicatedAnchor',
        'PROLOGUE_SURFACE_PROOF_OK material=Ice',
        'PROLOGUE_SURFACE_PROOF_OK material=Forbidden',
        'PROLOGUE_CANYON_TRAVERSAL_OK sectors=5 grapples=3 mechanism=True',
        'PROLOGUE_CHECKPOINT_RECOVERY_OK basin=True temple=True physical_death=True',
        'PROLOGUE_BOSS_RETRY_OK attacks=4 retry=True completion=True',
        'PROLOGUE_TERRAIN_GRAPPLE_OK wall=True ceiling=True underside=True edge=True ice=IceSlip forbidden=Forbidden range=620',
        'PROLOGUE_ATTACHED_BOOST_OK cast_retained=True first=True second_rejected=True no_air_double_jump=True',
        'PROLOGUE_MICROSCENES_OK count=15',
        'PROLOGUE_FALL_CONTEXT_OK version=3 fields=position,sector,last_support,checkpoint,claw,inputs',
        'PROLOGUE_CATCH_RETURN_OK catches=4 dash=False',
        'PROLOGUE_CLAW_MECHANISMS_OK roles=4',
        'PROLOGUE_CHECKPOINT_PROOF_OK basin=True temple=True pre_checkpoint_start=True',
        'TASK6_CLAW_DEATH_CLEANUP_OK physical=True idle=True generated_main=True',
        'TASK6_BASE_CRITICAL_MIGRATION_OK schema=2 five_sector=True legacy_thread_smoke=superseded inputmap=True',
        'PROLOGUE_CRITICAL_PATH_OK'
    )) { Assert-ExactLogMarker -Path $Path -Marker $marker }
    Assert-OrderedRegexLogMarkers -Path $Path -Patterns @(
        '^TASK9_GENERATED_MAIN_DEATH_OK marker=before-basin sector=CanyonEntrance activated=False same_frame=True position=.+$',
        '^TASK9_GENERATED_MAIN_DEATH_OK marker=after-basin sector=CanyonBasin activated=True same_frame=True position=.+$',
        '^TASK9_GENERATED_MAIN_DEATH_OK marker=after-temple sector=LostTemple activated=True same_frame=True position=.+$',
        '^TASK9_GENERATED_MAIN_DEATH_OK marker=boss-retry sector=LostTemple activated=True same_frame=True position=.+$',
        '^TASK9_GENERATED_MAIN_BOSS_RETRY_OK physical=True completion=True$'
    )
    $budgetMatches = @(Get-Content -LiteralPath $Path | Where-Object {
        $_ -match '^PROLOGUE_FRAME_BUDGET_OK used=(\d+) total=12000 headroom_percent=(\d+(?:\.\d+)?)$'
    })
    if ($budgetMatches.Count -ne 1) {
        throw "Expected exactly one five-sector bounded full-route frame marker; found $($budgetMatches.Count)."
    }
    $usedFrames = [int]([regex]::Match($budgetMatches[0], 'used=(\d+)').Groups[1].Value)
    $headroomPercent = [double]([regex]::Match($budgetMatches[0], 'headroom_percent=(\d+(?:\.\d+)?)').Groups[1].Value)
    if ($usedFrames -gt 10800 -or $headroomPercent -lt 10d) {
        throw "Five-sector route used=$usedFrames headroom_percent=$headroomPercent and exhausted its required 10% headroom."
    }
    Assert-NoEngineLogErrors -Paths @($Path)
}

if ($PresentationEvidenceOnly) {
    if ([string]::IsNullOrWhiteSpace($PresentationEvidenceLog)) {
        throw 'BASE presentation evidence-only gate requires -PresentationEvidenceLog.'
    }
    Assert-BasePresentationEvidence -Path $PresentationEvidenceLog
    "PROLOGUE_BASE_PRESENTATION_EVIDENCE_OK log=$PresentationEvidenceLog"
    return
}

if ($AttackPresentationEvidenceOnly) {
    if ([string]::IsNullOrWhiteSpace($AttackPresentationEvidenceLog)) {
        throw 'BASE attack-presentation evidence-only gate requires -AttackPresentationEvidenceLog.'
    }
    Assert-BaseAttackPresentationEvidence -Path $AttackPresentationEvidenceLog
    "PROLOGUE_BASE_ATTACK_PRESENTATION_EVIDENCE_OK log=$AttackPresentationEvidenceLog"
    return
}

if ($CriticalSmokeEvidenceOnly) {
    if ([string]::IsNullOrWhiteSpace($CriticalSmokeEvidenceLog)) {
        throw 'BASE critical-smoke evidence-only gate requires -CriticalSmokeEvidenceLog.'
    }
    Assert-BaseCriticalSmokeEvidence -Path $CriticalSmokeEvidenceLog
    "PROLOGUE_BASE_CRITICAL_SMOKE_EVIDENCE_OK log=$CriticalSmokeEvidenceLog"
    return
}

if ($ExternalEvidenceManifestOnly) {
    $externalEvidence = Get-Task6ExternalEvidenceManifest -ManifestPath $ExternalEvidenceManifest `
        -NotBeforeUtc $ExternalEvidenceNotBeforeUtc
    "PROLOGUE_BASE_EXTERNAL_EVIDENCE_OK manifest=$($externalEvidence.Manifest) root=$($externalEvidence.Root) " +
        "run_id=$($externalEvidence.RunId) files=$($externalEvidence.Files.Count) " +
        "created_after_utc=$($externalEvidence.CreatedAfterUtc.ToString('o'))"
    return
}

$baseEvidenceStartedUtc = [DateTime]::UtcNow
$externalEvidenceRoot = New-Task6ExternalEvidenceRoot -Path $ExternalEvidenceRoot
$dotnetVersion = (& "$dotnetRoot\dotnet.exe" --version).Trim()
$dotnetMajor = 0
if (-not [int]::TryParse(($dotnetVersion -split '\.')[0], [ref]$dotnetMajor) -or $dotnetMajor -ne 9) {
    throw "Prologue verification requires .NET SDK 9.x; found '$dotnetVersion' at $dotnetRoot"
}

$generatedScenes = @(
    'scenes\presentation\PlayerFormalVisual.tscn',
    'scenes\player\Player.tscn',
    'scenes\presentation\RustShellFormalVisual.tscn',
    'scenes\presentation\GraveLampMothFormalVisual.tscn',
    'scenes\presentation\BellBackLaborerFormalVisual.tscn',
    'scenes\presentation\FallenBellkeeperFormalVisual.tscn',
    'scenes\presentation\PrologueEnvironmentKit.tscn',
    'scenes\presentation\GraveLampVisual.tscn',
    'scenes\presentation\ThreadAnchorVisual.tscn',
    'scenes\presentation\BellChainVisual.tscn',
    'scenes\presentation\ShortcutDoorVisual.tscn',
    'scenes\vfx\PrologueVfx.tscn',
    'scenes\enemies\RustShellSquire.tscn',
    'scenes\enemies\GraveLampMoth.tscn',
    'scenes\enemies\BellBackLaborer.tscn',
    'scenes\boss\FallenBellkeeper.tscn',
    'scenes\mechanisms\BellChainMechanism.tscn',
    'scenes\prologue\rooms\AwakeningCourtyard.tscn',
    'scenes\prologue\rooms\BrokenToothCorridor.tscn',
    'scenes\prologue\rooms\UpperPendulum.tscn',
    'scenes\prologue\rooms\LowerBuriedMachinery.tscn',
    'scenes\prologue\rooms\BellChainMechanism.tscn',
    'scenes\prologue\rooms\FallenBellkeeperArena.tscn',
    'scenes\ui\PrologueUi.tscn',
    'scenes\prologue\PrologueLevel.tscn'
)
# These five leaf outputs are deliberately not part of BASE's historical 25 generated
# scenes.  The Task 6 main scene resolves them at runtime, so certification must build,
# bind, and restore them independently instead of silently consuming committed stale bytes.
$activeCanyonScenes = @(
    'scenes\prologue\sectors\CanyonEntrance.tscn',
    'scenes\prologue\sectors\CanyonDescent.tscn',
    'scenes\prologue\sectors\CanyonBasin.tscn',
    'scenes\prologue\sectors\CanyonAscent.tscn',
    'scenes\prologue\sectors\LostTemple.tscn'
)
# These are the actual leaf-to-parent bytes loaded by the Task 6 runtime path.
# They intentionally have no committed semantic equality requirement: Godot may
# regenerate unique IDs, and the certified canyon main supersedes the stale
# legacy room graph.  Their ordered normalized semantic digest is bound below.
$generatedRuntimeScenes = @(
    'scenes\prologue\sectors\CanyonEntrance.tscn',
    'scenes\prologue\sectors\CanyonDescent.tscn',
    'scenes\prologue\sectors\CanyonBasin.tscn',
    'scenes\prologue\sectors\CanyonAscent.tscn',
    'scenes\prologue\sectors\LostTemple.tscn',
    'scenes\prologue\PrologueLevel.tscn'
)
$builders = @(
    @{ Name = 'BuildPlayerFormalVisual'; Script = 'res://tools/build/art/BuildPlayerFormalVisual.cs' },
    @{ Name = 'BuildPlayer'; Script = 'res://tools/build/BuildPlayer.cs' },
    @{ Name = 'BuildEnemyFormalVisuals'; Script = 'res://tools/build/art/BuildEnemyFormalVisuals.cs' },
    @{ Name = 'BuildRustShellSquire'; Script = 'res://tools/build/BuildRustShellSquire.cs' },
    @{ Name = 'BuildGraveLampMoth'; Script = 'res://tools/build/prologue/BuildGraveLampMoth.cs' },
    @{ Name = 'BuildBellBackLaborer'; Script = 'res://tools/build/prologue/BuildBellBackLaborer.cs' },
    @{ Name = 'BuildFallenBellkeeperFormalVisual'; Script = 'res://tools/build/art/BuildFallenBellkeeperFormalVisual.cs' },
    @{ Name = 'BuildFallenBellkeeper'; Script = 'res://tools/build/prologue/BuildFallenBellkeeper.cs' },
    @{ Name = 'BuildPrologueMechanismVisuals'; Script = 'res://tools/build/art/BuildPrologueMechanismVisuals.cs' },
    @{ Name = 'BuildBellChainMechanism'; Script = 'res://tools/build/prologue/BuildBellChainMechanism.cs' },
    @{ Name = 'BuildPrologueEnvironmentKit'; Script = 'res://tools/build/art/BuildPrologueEnvironmentKit.cs' },
    @{ Name = 'BuildAwakeningCourtyard'; Script = 'res://tools/build/prologue/BuildAwakeningCourtyard.cs' },
    @{ Name = 'BuildBrokenToothCorridor'; Script = 'res://tools/build/prologue/BuildBrokenToothCorridor.cs' },
    @{ Name = 'BuildUpperPendulum'; Script = 'res://tools/build/prologue/BuildUpperPendulum.cs' },
    @{ Name = 'BuildLowerBuriedMachinery'; Script = 'res://tools/build/prologue/BuildLowerBuriedMachinery.cs' },
    @{ Name = 'BuildBellChainRoom'; Script = 'res://tools/build/prologue/BuildBellChainRoom.cs' },
    @{ Name = 'BuildFallenBellkeeperArena'; Script = 'res://tools/build/prologue/BuildFallenBellkeeperArena.cs' },
    @{ Name = 'BuildPrologueUi'; Script = 'res://tools/build/ui/BuildPrologueUi.cs' },
    @{ Name = 'BuildPrologueVfx'; Script = 'res://tools/build/art/BuildPrologueVfx.cs' },
    @{ Name = 'BuildCanyonEntrance'; Script = 'res://tools/build/prologue/BuildCanyonEntrance.cs' },
    @{ Name = 'BuildCanyonDescent'; Script = 'res://tools/build/prologue/BuildCanyonDescent.cs' },
    @{ Name = 'BuildCanyonBasin'; Script = 'res://tools/build/prologue/BuildCanyonBasin.cs' },
    @{ Name = 'BuildCanyonAscent'; Script = 'res://tools/build/prologue/BuildCanyonAscent.cs' },
    @{ Name = 'BuildLostTemple'; Script = 'res://tools/build/prologue/BuildLostTemple.cs' },
    @{ Name = 'BuildPrologueLevel'; Script = 'res://tools/build/prologue/BuildPrologueLevel.cs' }
)
$builderNames = @($builders | ForEach-Object { $_.Name })
Assert-PrologueBuilderPlan -BuilderNames $builderNames

$levelBuilderText = Get-Content -LiteralPath (Join-Path $project 'tools\build\prologue\BuildPrologueLevel.cs') -Raw
# BASE used six linked rooms.  The certified Task 3/6 revision intentionally replaces that
# legacy graph with the authored five-sector seamless canyon, so retain the hard *route*
# gate without requiring mutually exclusive legacy declarations.
$legacyRooms = @(
    'PrologueRoomId.AwakeningCourtyard', 'PrologueRoomId.BrokenToothCorridor',
    'PrologueRoomId.UpperPendulum', 'PrologueRoomId.LowerBuriedMachinery',
    'PrologueRoomId.BellChainMechanism', 'PrologueRoomId.FallenBellkeeperArena'
)
$legacyRoomCounts = @($legacyRooms | ForEach-Object {
    ([regex]::Matches($levelBuilderText, [regex]::Escape($_))).Count
})
$legacyRoute = @($legacyRoomCounts | Where-Object { $_ -ge 2 }).Count -eq $legacyRooms.Count
$sectorRoute = $levelBuilderText.Contains('PopulateCanyonDefinitions(sectors);') -and
    $levelBuilderText.Contains('PrologueSectorId[] route') -and
    $levelBuilderText.Contains('world.Definitions.Count != 5')
if (-not $legacyRoute -and -not $sectorRoute) {
    throw 'BuildPrologueLevel must preserve either the BASE six-room route or the certified five-sector canyon route.'
}
"PROLOGUE_BASE_ROUTE_TOPOLOGY_OK profile=$(if ($sectorRoute) { 'sectors' } else { 'legacy-rooms' })"

$baseSnapshotPaths = @($generatedScenes) + @('project.godot')
$snapshotPaths = @($baseSnapshotPaths) + @($activeCanyonScenes)
$snapshots = @{}
$semanticSnapshots = @{}
$committedSemanticSnapshots = @{}
$preExistingDirtyScenes = @{}
$committedResourceTexts = @{}
$logs = [System.Collections.Generic.List[string]]::new()
$intentionalNegativeTestWarnings = @(
    'WARNING: PresentationProfile: VisualScene root must implement IEntityVisual2D for VisualRoot.',
    "WARNING: PresentationProfile: cue 'Hurt' root must be Node2D.",
    "WARNING: RoomPresentationProfile: VisualScene root must be Node2D for 'VisualRoot'.",
    'WARNING: MechanicalClawController isolated a throwing ClawFrameChanged observer: intentional Task7 observer fault',
    'WARNING: Rejected prologue flow transition Completed->Recovering.'
)
foreach ($relativePath in $snapshotPaths) {
    $path = Join-Path $project $relativePath
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Required prologue artifact is missing before verification: $path"
    }
    $snapshots[$relativePath] = [System.IO.File]::ReadAllBytes($path)
    if ($relativePath.EndsWith('.tscn', [StringComparison]::OrdinalIgnoreCase)) {
        $semanticSnapshots[$relativePath] = [System.IO.File]::ReadAllText($path)
        $gitRelativePath = $relativePath.Replace('\', '/')
        $preExistingDirtyScenes[$relativePath] = @(
            & git -C $project status --porcelain=v1 -- $gitRelativePath
        ).Count -gt 0
        if ($preExistingDirtyScenes[$relativePath]) {
            $committedLines = @(& git -C $project show ("HEAD:$gitRelativePath") 2>$null)
            if ($LASTEXITCODE) {
                throw "Dirty generated scene has no committed canonical baseline: $relativePath"
            }
            $committedSemanticSnapshots[$relativePath] = $committedLines -join "`n"
        }
        else {
            $committedSemanticSnapshots[$relativePath] = $semanticSnapshots[$relativePath]
        }
    }
}
foreach ($relativePath in $generatedScenes) {
    $resourcePath = 'res://' + $relativePath.Replace('\', '/')
    $committedResourceTexts[$resourcePath] = $committedSemanticSnapshots[$relativePath]
}

try {
    $childHost = Get-Task6CompatiblePowerShellHost
    & $childHost -NoProfile -ExecutionPolicy Bypass -File `
        (Join-Path $project 'tests\powershell\Task6BaseHardGates.Tests.ps1')
    if ($LASTEXITCODE) { throw "BASE hard-gate source fixture failed with exit code $LASTEXITCODE" }

    & $childHost -NoProfile -ExecutionPolicy Bypass -File `
        (Join-Path $project 'tests\powershell\PlayPrologue.Tests.ps1')
    if ($LASTEXITCODE) { throw "Prologue launcher fixture failed with exit code $LASTEXITCODE" }

    # A native C0000005 after the editor's GC-drain terminal is a known intermittent host
    # failure.  The retry helper owns only this attempt's completed child process and permits
    # exactly one retry after restoring the pre-import candidate bytes and deleting its cache.
    # Run its physical-Git fixture first so the production path cannot silently weaken that
    # cleanup or retry contract.
    $importRetryPesterRoot = Join-Path $externalEvidenceRoot 'pester-import-native-retry'
    $importRetryPesterConsole = Join-Path $externalEvidenceRoot 'pester-import-native-retry.console.log'
    $importRetryPesterPath = Join-Path $project 'tests\powershell\Task6ImportNativeRetry.Tests.ps1'
    $escapedImportRetryPesterPath = $importRetryPesterPath.Replace("'", "''")
    $escapedImportRetryProject = $project.Replace("'", "''")
    $escapedImportRetryRoot = $importRetryPesterRoot.Replace("'", "''")
    $importRetryPesterCommand = "& { `$p = @{ Path = '$escapedImportRetryPesterPath'; Parameters = @{ ProjectRoot = '$escapedImportRetryProject'; EvidenceRoot = '$escapedImportRetryRoot' } }; Import-Module Pester -MinimumVersion 3.4.0; `$result = Invoke-Pester -Script `$p -PassThru; if (`$result.FailedCount -ne 0) { exit 1 } }"
    $importRetryPester = Invoke-MergedNativeCommand -FilePath $childHost -ArgumentList @(
        '-NoProfile', '-ExecutionPolicy', 'Bypass', '-Command', $importRetryPesterCommand
    ) -ConsolePath $importRetryPesterConsole
    if ($importRetryPester.ExitCode -ne 0) {
        throw "Task 6 import-native retry Pester failed with exit code $($importRetryPester.ExitCode)"
    }
    if ((Get-Content -LiteralPath $importRetryPesterConsole -Raw) -notmatch 'Passed:\s+6\s+Failed:\s+0') {
        throw 'Task 6 import-native retry Pester did not report six passing retry cases.'
    }
    "TASK6_IMPORT_NATIVE_RETRY_PESTER_OK cases=6 console=$importRetryPesterConsole"

    # Godot's generated project can otherwise report a stale incremental DLL after an isolated
    # candidate source materialization.  Verification must execute the bytes it fingerprints.
    $importPrepare = {
        param([int]$Attempt)
        & "$dotnetRoot\dotnet.exe" build --no-incremental (Join-Path $project 'Knight.csproj')
        if ($LASTEXITCODE) {
            throw "Prologue C# build failed before import attempt=$Attempt with exit code $LASTEXITCODE"
        }
    }.GetNewClosure()
    $importNative = {
        param([int]$Attempt, [string]$LogPath, [string]$ConsolePath)
        Invoke-MergedNativeCommand -FilePath $godot `
            -ArgumentList @('--headless', '--editor', '--path', $project, '--quit-after', '1200',
                '--log-file', $LogPath) `
            -ConsolePath $ConsolePath
    }.GetNewClosure()
    $importOutputs = @(Invoke-Task6ImportWithBoundedNativeRetry -ProjectRoot $project `
        -EvidenceRoot $externalEvidenceRoot -PrepareAttempt $importPrepare -InvokeAttempt $importNative `
        -GodotPath $godot)
    foreach ($output in $importOutputs) {
        if ($output -is [string]) { $output }
    }
    $importObjects = @($importOutputs | Where-Object {
            $null -ne $_ -and $_.PSObject.Properties.Match('FinalLog').Count -eq 1
        })
    if ($importObjects.Count -ne 1) {
        throw "Task 6 import-native retry did not return one final result object; actual=$($importObjects.Count)"
    }
    $importResult = $importObjects[0]
    $importLog = $importResult.FinalLog
    $importConsole = $importResult.FinalConsole
    $importEngineLogs = @($importResult.EvidencePaths | Where-Object { $_ -match '\.log$' })
    if ($importEngineLogs.Count -lt 2) {
        throw "Task 6 import-native retry has too few engine log paths; actual=$($importEngineLogs.Count)"
    }
    foreach ($path in $importEngineLogs) {
        if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
            throw "Task 6 import-native retry evidence path is missing: $path"
        }
        $logs.Add($path)
    }
    Assert-ExactLogMarker -Path $importLog -Marker 'VERIFICATION_EDITOR_GC_DRAIN_OK'
    Assert-NoEngineLogErrors -Paths $importEngineLogs

    # The six legacy-room emitters rewrite rooms that Task 3 replaced with generated sectors.
    # They remain in the 25-file immutable snapshot (and are restored byte-for-byte), while
    # the active leaf-to-main sector builders are run by the Task 6 verifier.  Executing both
    # families would manufacture semantic drift rather than test the final candidate.
    $supersededLegacyRoomBuilders = @(
        'BuildAwakeningCourtyard', 'BuildBrokenToothCorridor', 'BuildUpperPendulum',
        'BuildLowerBuriedMachinery', 'BuildBellChainRoom', 'BuildFallenBellkeeperArena'
    )
    $buildersToExecute = @($builders | Where-Object { $_.Name -notin $supersededLegacyRoomBuilders })
    $builderLogs = [System.Collections.Generic.List[string]]::new()
    foreach ($builder in $buildersToExecute) {
        $log = Join-Path $externalEvidenceRoot ("verify-prologue-builder-{0}.log" -f $builder.Name)
        $console = Join-Path $externalEvidenceRoot ("verify-prologue-builder-{0}.console.log" -f $builder.Name)
        $builderLogs.Add($log)
        $logs.Add($log)
        $logs.Add($console)
        $builderResult = Invoke-MergedNativeCommand -FilePath $godot `
            -ArgumentList @('--headless', '--path', $project, '--script', $builder.Script, '--log-file', $log) `
            -ConsolePath $console
        $builderResult.Lines | ForEach-Object { $_ }
        if ($builderResult.ExitCode) { throw "Prologue scene builder failed: $($builder.Name)" }
        Assert-NoEngineLogErrors -Paths @($log, $console)
    }
    Assert-RequiredFiles -Paths $builderLogs.ToArray() -Label 'builder log'

    # The leaf-to-main chain is a runtime dependency, not a source-string assertion: bind the
    # actual outputs that BuildPrologueLevel subsequently packs and that pre/post tests load.
    $activeCanyonBuiltHashes = [ordered]@{}
    $activeCanyonTexts = @{}
    foreach ($relativePath in $activeCanyonScenes) {
        $artifactPath = Join-Path $project $relativePath
        if (-not (Test-Path -LiteralPath $artifactPath -PathType Leaf)) {
            throw "Active canyon builder did not create required scene: $relativePath"
        }
        $text = [System.IO.File]::ReadAllText($artifactPath)
        $activeCanyonTexts[$relativePath] = $text
        $activeCanyonBuiltHashes[$relativePath] = Get-EvidenceSha256 -LiteralPath $artifactPath
        "TASK6_ACTIVE_CANYON_SCENE_HASH path=$relativePath sha256=$($activeCanyonBuiltHashes[$relativePath])"
    }
    $descentText = $activeCanyonTexts['scenes\prologue\sectors\CanyonDescent.tscn']
    $descentStaticBodies = ([regex]::Matches($descentText,
        '(?m)^\[node name="[^"]+" type="StaticBody2D" parent="Gameplay/Collision(?:/[^"]*)?"[^\r\n]*\]$')).Count
    $descentProfiles = ([regex]::Matches($descentText,
        '(?m)^\[node name="GrappleSurfaceProfile" type="Node2D" parent="Gameplay/Collision/[^"]+"[^\r\n]*\]$')).Count
    $craneProfile = [regex]::IsMatch($descentText,
        '(?m)^\[node name="GrappleSurfaceProfile" type="Node2D" parent="Gameplay/Collision/CraneShaftWall"[^\r\n]*\]$')
    $iceProfile = [regex]::IsMatch($descentText,
        '(?m)^\[node name="GrappleSurfaceProfile" type="Node2D" parent="Gameplay/Collision/VisibleIceWall"[^\r\n]*\]$')
    if ($descentStaticBodies -ne 15 -or $descentProfiles -ne 15 -or
        -not $craneProfile -or -not $iceProfile) {
        throw "Active CanyonDescent builder contract failed: static_bodies=$descentStaticBodies " +
            "profiles=$descentProfiles crane_profile=$craneProfile ice_profile=$iceProfile"
    }
    "TASK6_ACTIVE_CANYON_BUILD_OK scenes=$($activeCanyonScenes.Count) " +
        "descent_static_bodies=$descentStaticBodies descent_profiles=$descentProfiles " +
        "crane_profile=$craneProfile ice_profile=$iceProfile"

    $generatedRuntimeSemantics = Get-Task6GeneratedRuntimeSemantics -Root $project -RelativePaths $generatedRuntimeScenes
    if ($generatedRuntimeSemantics.Count -ne $generatedRuntimeScenes.Count) {
        throw "Generated runtime semantic count mismatch: expected=$($generatedRuntimeScenes.Count) actual=$($generatedRuntimeSemantics.Count)"
    }
    for ($index = 0; $index -lt $generatedRuntimeScenes.Count; $index++) {
        $expectedRuntimePath = $generatedRuntimeScenes[$index].Replace('\', '/')
        if ($generatedRuntimeSemantics.Paths[$index] -cne $expectedRuntimePath) {
            throw "Generated runtime semantic path order mismatch: expected=$expectedRuntimePath actual=$($generatedRuntimeSemantics.Paths[$index]) index=$index"
        }
    }
    $generatedRuntimeMarker = "PROLOGUE_GENERATED_RUNTIME_SEMANTICS_OK sha=$($generatedRuntimeSemantics.Sha256) count=$($generatedRuntimeSemantics.Count)"
    $generatedRuntimeEvidencePath = Join-Path $externalEvidenceRoot 'verify-prologue-generated-runtime-semantics.txt'
    $generatedRuntimeEvidenceLines = [Collections.Generic.List[string]]::new()
    $generatedRuntimeEvidenceLines.Add($generatedRuntimeMarker)
    foreach ($runtimePath in $generatedRuntimeSemantics.Paths) {
        $generatedRuntimeEvidenceLines.Add(
            "path=$runtimePath sha256=$($generatedRuntimeSemantics.SceneSha256[$runtimePath])")
    }
    [IO.File]::WriteAllLines($generatedRuntimeEvidencePath, $generatedRuntimeEvidenceLines.ToArray(),
        [Text.UTF8Encoding]::new($false))
    $logs.Add($generatedRuntimeEvidencePath)
    $generatedRuntimeMarker

    $builderByteDriftCount = 0
    $sharedDirtySemanticChecks = 0
    $builtResourceTexts = @{}
    foreach ($relativePath in $generatedScenes) {
        $resourcePath = 'res://' + $relativePath.Replace('\', '/')
        $builtResourceTexts[$resourcePath] = [System.IO.File]::ReadAllText(
            (Join-Path $project $relativePath))
    }
    foreach ($relativePath in $generatedScenes) {
        $artifactPath = Join-Path $project $relativePath
        $resourcePath = 'res://' + $relativePath.Replace('\', '/')
        $actual = $builtResourceTexts[$resourcePath]
        if ($relativePath -cne 'scenes\prologue\PrologueLevel.tscn') {
            Assert-StableGeneratedSceneSemantics -StartupText $semanticSnapshots[$relativePath] `
                -CommittedText $committedSemanticSnapshots[$relativePath] -ActualText $actual `
                -StartedDirty $preExistingDirtyScenes[$relativePath] -Label $relativePath `
                -ExpectedResourceTexts $committedResourceTexts `
                -ActualResourceTexts $builtResourceTexts
        }
        if ($preExistingDirtyScenes[$relativePath]) {
            $sharedDirtySemanticChecks++
            "PROLOGUE_SHARED_DIRTY_SCENE_SEMANTICS_OK path=$relativePath"
        }
        $actualBytes = [System.IO.File]::ReadAllBytes($artifactPath)
        if (-not (Test-ExactBytes -Expected $snapshots[$relativePath] -Actual $actualBytes)) {
            $builderByteDriftCount++
        }
    }
    $projectArtifactPath = Join-Path $project 'project.godot'
    $projectArtifactBytes = [System.IO.File]::ReadAllBytes($projectArtifactPath)
    $projectByteDrift = if (Test-ExactBytes -Expected $snapshots['project.godot'] `
        -Actual $projectArtifactBytes) { 0 } else { 1 }

    $testsPreLog = Join-Path $externalEvidenceRoot 'verify-prologue-tests-pre.log'
    $testsPreConsole = Join-Path $externalEvidenceRoot 'verify-prologue-tests-pre.console.log'
    $testsPostLog = Join-Path $externalEvidenceRoot 'verify-prologue-tests-post.log'
    $testsPostConsole = Join-Path $externalEvidenceRoot 'verify-prologue-tests-post.console.log'
    $testsLog = Join-Path $externalEvidenceRoot 'verify-prologue-tests.log'
    $testsConsole = Join-Path $externalEvidenceRoot 'verify-prologue-tests.console.log'
    $logs.Add($testsLog)
    $logs.Add($testsConsole)
    foreach ($phaseRun in @(
        @{ Phase = 'pre'; Log = $testsPreLog; Console = $testsPreConsole },
        @{ Phase = 'post'; Log = $testsPostLog; Console = $testsPostConsole })) {
        $testsResult = Invoke-MergedNativeCommand -FilePath $godot `
            -ArgumentList @('--headless', '--path', $project, '--script', 'res://tests/RunPrologueTests.cs',
                '--log-file', $phaseRun.Log, '--', "--phase=$($phaseRun.Phase)") `
            -ConsolePath $phaseRun.Console
        $testsResult.Lines | ForEach-Object { $_ }
        if ($testsResult.ExitCode) {
            throw "Prologue test phase '$($phaseRun.Phase)' failed with exit code $($testsResult.ExitCode)"
        }
        Assert-ExactLogMarker -Path $phaseRun.Log -Marker "PROLOGUE_TEST_PHASE_EXIT_DRAIN_OK phase=$($phaseRun.Phase)"
        Assert-ExactLogMarker -Path $phaseRun.Log -Marker "PROLOGUE_TEST_PHASE_OK phase=$($phaseRun.Phase)"
        Assert-ExactLogMarker -Path $phaseRun.Console -Marker "PROLOGUE_TEST_PHASE_EXIT_DRAIN_OK phase=$($phaseRun.Phase)"
        Assert-ExactLogMarker -Path $phaseRun.Console -Marker "PROLOGUE_TEST_PHASE_OK phase=$($phaseRun.Phase)"
        $overallCount = @((Get-Content -LiteralPath $phaseRun.Log) |
            Where-Object { $_ -ceq 'PROLOGUE_TESTS_OK' }).Count
        $expectedOverallCount = if ($phaseRun.Phase -eq 'post') { 1 } else { 0 }
        if ($overallCount -ne $expectedOverallCount) {
            throw "Prologue test phase '$($phaseRun.Phase)' expected PROLOGUE_TESTS_OK count $expectedOverallCount; found $overallCount."
        }
        $consoleOverallCount = @((Get-Content -LiteralPath $phaseRun.Console) |
            Where-Object { $_ -ceq 'PROLOGUE_TESTS_OK' }).Count
        if ($consoleOverallCount -ne $expectedOverallCount) {
            throw "Prologue test phase '$($phaseRun.Phase)' Console expected PROLOGUE_TESTS_OK count $expectedOverallCount; found $consoleOverallCount."
        }
    }

    $bossRuntimeLog = Join-Path $externalEvidenceRoot 'verify-prologue-boss-runtime.log'
    $bossRuntimeConsole = Join-Path $externalEvidenceRoot 'verify-prologue-boss-runtime.console.log'
    $logs.Add($bossRuntimeLog)
    $logs.Add($bossRuntimeConsole)
    $bossRuntimeResult = Invoke-MergedNativeCommand -FilePath $godot `
        -ArgumentList @('--headless', '--path', $project, '--script',
            'res://tests/RunFallenBellkeeperRuntimeTests.cs', '--log-file', $bossRuntimeLog) `
        -ConsolePath $bossRuntimeConsole
    $bossRuntimeResult.Lines | ForEach-Object { $_ }
    if ($bossRuntimeResult.ExitCode) {
        throw "Isolated Fallen Bellkeeper runtime suite failed with exit code $($bossRuntimeResult.ExitCode)"
    }
    Assert-ExactLogMarker -Path $bossRuntimeLog `
        -Marker 'PROLOGUE_TEST_PHASE_EXIT_DRAIN_OK phase=boss-runtime'
    Assert-ExactLogMarker -Path $bossRuntimeConsole `
        -Marker 'PROLOGUE_TEST_PHASE_EXIT_DRAIN_OK phase=boss-runtime'
    Assert-ExactLogMarker -Path $bossRuntimeLog -Marker 'FALLEN_BELLKEEPER_RUNTIME_TESTS_OK passes=2'
    Assert-ExactLogMarker -Path $bossRuntimeConsole -Marker 'FALLEN_BELLKEEPER_RUNTIME_TESTS_OK passes=2'
    Assert-NoEngineLogErrors -Paths @($bossRuntimeLog, $bossRuntimeConsole)

    Join-PhaseLogsDeterministically -OutputPath $testsLog -Paths @($testsPreLog, $testsPostLog)
    Join-PhaseLogsDeterministically -OutputPath $testsConsole -Paths @($testsPreConsole, $testsPostConsole)
    Assert-ExactLogMarker -Path $testsLog -Marker 'PROLOGUE_TESTS_OK'
    Assert-ExactLogMarker -Path $testsConsole -Marker 'PROLOGUE_TESTS_OK'
    Assert-EngineLogContract -Path $testsLog `
        -ExpectedWarnings $intentionalNegativeTestWarnings
    Assert-EngineLogContract -Path $testsConsole `
        -ExpectedWarnings $intentionalNegativeTestWarnings

    $teardownLog = Join-Path $externalEvidenceRoot 'verify-prologue-audio-teardown.log'
    $teardownConsole = Join-Path $externalEvidenceRoot 'verify-prologue-audio-teardown.console.log'
    $logs.Add($teardownLog)
    $logs.Add($teardownConsole)
    $teardownResult = Invoke-MergedNativeCommand -FilePath $godot `
        -ArgumentList @('--headless', '--path', $project, '--script',
            'res://tests/integration/PrologueAudioTeardownProbe.cs', '--log-file', $teardownLog) `
        -ConsolePath $teardownConsole
    $teardownResult.Lines | ForEach-Object { $_ }
    if ($teardownResult.ExitCode) {
        throw "Prologue audio teardown probe failed with exit code $($teardownResult.ExitCode)"
    }
    Assert-ExactLogMarker -Path $teardownLog -Marker 'PROLOGUE_AUDIO_TEARDOWN_OK'
    Assert-NoEngineLogErrors -Paths @($teardownLog, $teardownConsole)

    $mothTeardownLog = Join-Path $externalEvidenceRoot 'verify-prologue-moth-teardown.log'
    $mothTeardownConsole = Join-Path $externalEvidenceRoot 'verify-prologue-moth-teardown.console.log'
    $logs.Add($mothTeardownLog)
    $logs.Add($mothTeardownConsole)
    $mothTeardownResult = Invoke-MergedNativeCommand -FilePath $godot `
        -ArgumentList @('--headless', '--path', $project, '--script',
            'res://tests/integration/GraveLampMothTeardownProbe.cs', '--log-file', $mothTeardownLog) `
        -ConsolePath $mothTeardownConsole
    $mothTeardownResult.Lines | ForEach-Object { $_ }
    if ($mothTeardownResult.ExitCode) {
        throw "Prologue Moth teardown probe failed with exit code $($mothTeardownResult.ExitCode)"
    }
    Assert-ExactLogMarker -Path $mothTeardownLog -Marker 'PROLOGUE_MOTH_TEARDOWN_OK count=12'
    Assert-NoEngineLogErrors -Paths @($mothTeardownLog, $mothTeardownConsole)

    # BASE historically ran a ThreadAbility/Dash recorder here.  The certified Task 6 route
    # supersedes that incompatible recorder with a display-backed InputMap proof.  Keep the
    # same hard evidence responsibilities, but make the schema migration explicit: no legacy
    # air_dash_consumed marker can participate in a green result.
    $presentationLog = Join-Path $externalEvidenceRoot 'verify-prologue-attack-presentation.log'
    $presentationConsole = Join-Path $externalEvidenceRoot 'verify-prologue-attack-presentation.console.log'
    $logs.Add($presentationLog)
    $logs.Add($presentationConsole)
    $presentationResult = Invoke-MergedNativeCommand -FilePath $godot `
        -ArgumentList @('--path', $project, '--fixed-fps', '60', '--display-driver', 'windows',
            '--rendering-method', 'gl_compatibility', '--audio-driver', 'Dummy', '--script',
            'res://tests/presentation/ProloguePresentationTask6.cs', '--log-file', $presentationLog) `
        -ConsolePath $presentationConsole
    $presentationResult.Lines | ForEach-Object { $_ }
    if ($presentationResult.ExitCode) {
        throw "Task 6 InputMap presentation failed with exit code $($presentationResult.ExitCode)"
    }
    Assert-BasePresentationEvidence -Path $presentationLog
    Assert-NoEngineLogErrors -Paths @($presentationConsole)

    # Attack telegraphs remain independently runnable against packed production scenes.  This
    # prevents the traversal-schema migration from silently dropping the seven BASE attack
    # sequences, including the Boss hammer throw.
    $attackPresentationLog = Join-Path $externalEvidenceRoot 'verify-prologue-base-attack-presentation.log'
    $attackPresentationConsole = Join-Path $externalEvidenceRoot 'verify-prologue-base-attack-presentation.console.log'
    $logs.Add($attackPresentationLog)
    $logs.Add($attackPresentationConsole)
    $attackPresentationResult = Invoke-MergedNativeCommand -FilePath $godot `
        -ArgumentList @('--headless', '--path', $project, '--fixed-fps', '60', '--script',
            'res://tests/presentation/PrologueBaseAttackPresentation.cs', '--log-file', $attackPresentationLog) `
        -ConsolePath $attackPresentationConsole
    $attackPresentationResult.Lines | ForEach-Object { $_ }
    if ($attackPresentationResult.ExitCode) {
        throw "BASE packed-scene attack presentation failed with exit code $($attackPresentationResult.ExitCode)"
    }
    Assert-BaseAttackPresentationEvidence -Path $attackPresentationLog
    Assert-NoEngineLogErrors -Paths @($attackPresentationConsole)
    'TASK6_BASE_PRESENTATION_SCHEMA_OK version=2 mandatory_dash=False claw_runtime=True attack_sequences=7'

    $auditInvocationLog = Join-Path $externalEvidenceRoot 'verify-prologue-production-audit.log'
    $logs.Add($auditInvocationLog)
    $auditOutput = & $childHost -NoProfile -ExecutionPolicy Bypass -File `
        (Join-Path $project 'tools\audit-prologue-production.ps1') -SkipBuild `
        -BaseAuditRoot (Join-Path $externalEvidenceRoot 'production-audit') 2>&1
    $auditExit = $LASTEXITCODE
    $auditOutput | Set-Content -LiteralPath $auditInvocationLog -Encoding UTF8
    $auditOutput | ForEach-Object { $_ }
    if ($auditExit) { throw "Prologue production audit failed with exit code $auditExit" }
    Assert-ExactLogMarker -Path $auditInvocationLog -Marker 'PROLOGUE_UI_RESOLUTION_OK'
    Assert-ExactLogMarker -Path $auditInvocationLog -Marker 'PROLOGUE_VISUAL_EVIDENCE_OK'
    Assert-ExactLogMarker -Path $auditInvocationLog -Marker 'PROLOGUE_VISUAL_PERFORMANCE_OK'
    Assert-ExactLogMarker -Path $auditInvocationLog -Marker 'PROLOGUE_VISUAL_AUDIT_OK'
    Assert-ExactLogMarker -Path $auditInvocationLog `
        -Marker 'PROLOGUE_DISPLAY_BACKED_AUDIT_OK runs=4 driver=windows'
    Assert-ExactLogMarker -Path $auditInvocationLog -Marker 'PROLOGUE_PRODUCTION_AUDIT_OK'

    $forcedFailureLog = Join-Path $externalEvidenceRoot 'verify-prologue-smoke-forced-failure.log'
    $forcedFailureConsole = Join-Path $externalEvidenceRoot 'verify-prologue-smoke-forced-failure.console.log'
    $priorForcedFailure = [Environment]::GetEnvironmentVariable('KNIGHT_FORCE_SMOKE_FAILURE', 'Process')
    try {
        $env:KNIGHT_FORCE_SMOKE_FAILURE = '1'
        $forcedFailureResult = Invoke-MergedNativeCommand -FilePath $godot `
            -ArgumentList @('--headless', '--path', $project, '--script',
                'res://tests/smoke/PrologueCriticalPathSmokeTask6.cs', '--log-file', $forcedFailureLog) `
            -ConsolePath $forcedFailureConsole
    }
    finally {
        if ($null -eq $priorForcedFailure) { Remove-Item Env:\KNIGHT_FORCE_SMOKE_FAILURE -ErrorAction SilentlyContinue }
        else { $env:KNIGHT_FORCE_SMOKE_FAILURE = $priorForcedFailure }
    }
    $forcedFailureResult.Lines | ForEach-Object { $_ }
    if ($forcedFailureResult.ExitCode -ne 1) {
        throw "Prologue smoke failure path did not preserve managed exit 1; " +
            "actual=$($forcedFailureResult.ExitCode)"
    }
    # The shared fail-closed filter permits exactly the intended error and rejects any
    # fatal/native/RID signature independently in both the Godot log and merged console.
    Assert-OnlyExpectedEngineError -Path $forcedFailureLog -ExpectedPattern 'FORCED_SMOKE_FAILURE' `
        -ExpectedContext 'PROLOGUE_CRITICAL_PATH_FAIL'
    Assert-OnlyExpectedEngineError -Path $forcedFailureConsole -ExpectedPattern 'FORCED_SMOKE_FAILURE' `
        -ExpectedContext 'PROLOGUE_CRITICAL_PATH_FAIL'

    $smokeLog = Join-Path $externalEvidenceRoot 'verify-prologue-smoke.log'
    $smokeConsole = Join-Path $externalEvidenceRoot 'verify-prologue-smoke.console.log'
    $logs.Add($smokeLog)
    $logs.Add($smokeConsole)
    $smokeResult = Invoke-MergedNativeCommand -FilePath $godot `
        -ArgumentList @('--headless', '--path', $project, '--script',
            'res://tests/smoke/PrologueCriticalPathSmokeTask6.cs', '--log-file', $smokeLog) `
        -ConsolePath $smokeConsole
    $smokeResult.Lines | ForEach-Object { $_ }
    if ($smokeResult.ExitCode) {
        throw "Prologue critical-path smoke failed with exit code $($smokeResult.ExitCode)"
    }
    Assert-BaseCriticalSmokeEvidence -Path $smokeLog
    foreach ($scanPath in $logs.ToArray()) {
        if ($scanPath -ceq $testsLog -or $scanPath -ceq $testsConsole) {
            Assert-EngineLogContract -Path $scanPath `
                -ExpectedWarnings $intentionalNegativeTestWarnings
        }
        else { Assert-EngineLogContract -Path $scanPath }
    }

    $projectText = [System.IO.File]::ReadAllText((Join-Path $project 'project.godot'))
    if (-not $projectText.Contains('run/main_scene="res://scenes/prologue/PrologueLevel.tscn"')) {
        throw 'The playable main scene is not the complete prologue level.'
    }
    # Keep the active canyon artifacts available through every runtime/presentation/smoke gate,
    # then put the clean candidate back byte-for-byte before declaring success.
    $activeCanyonByteDrift = 0
    foreach ($relativePath in $activeCanyonScenes) {
        $artifactPath = Join-Path $project $relativePath
        $actualBytes = [System.IO.File]::ReadAllBytes($artifactPath)
        if (-not (Test-ExactBytes -Expected $snapshots[$relativePath] -Actual $actualBytes)) {
            $activeCanyonByteDrift++
        }
    }
    # Leave the generated leaf-to-main chain in place for every runtime consumer
    # above, then restore all protected source bytes atomically before the final
    # hash gate.  The finally block repeats this exact restoration on failures.
    Restore-ProtectedFileSnapshots -Root $project -RelativePaths $snapshotPaths `
        -Snapshots $snapshots
    foreach ($relativePath in $snapshotPaths) {
        $protectedBytes = [System.IO.File]::ReadAllBytes((Join-Path $project $relativePath))
        if (-not (Test-ExactBytes -Expected $snapshots[$relativePath] -Actual $protectedBytes)) {
            throw "Protected artifact drifted before verification completion: $relativePath"
        }
    }
    "PROLOGUE_GENERATED_SCENES_RESTORED byte_drift=$builderByteDriftCount " +
        "semantic_drift=0 shared_dirty_checked=$sharedDirtySemanticChecks project_drift=$projectByteDrift"
    "TASK6_ACTIVE_CANYON_ARTIFACT_HASHES_OK count=$($activeCanyonScenes.Count) byte_drift=$activeCanyonByteDrift"
    "PROLOGUE_PROTECTED_ARTIFACT_HASHES_OK count=$($baseSnapshotPaths.Count)"
    $externalEvidenceManifestPath = Write-Task6ExternalEvidenceManifest -Root $externalEvidenceRoot `
        -StartedUtc $baseEvidenceStartedUtc
    $externalEvidence = Get-Task6ExternalEvidenceManifest -ManifestPath $externalEvidenceManifestPath `
        -NotBeforeUtc $baseEvidenceStartedUtc
    "PROLOGUE_BASE_EXTERNAL_EVIDENCE_WRITTEN manifest=$($externalEvidence.Manifest) root=$($externalEvidence.Root) " +
        "run_id=$($externalEvidence.RunId) files=$($externalEvidence.Files.Count)"
    if ($SuppressTerminal) { 'PROLOGUE_BASE_HARD_GATES_OK' }
    else { 'PROLOGUE_VERIFY_OK' }
}
finally {
    Restore-ProtectedFileSnapshots -Root $project -RelativePaths $snapshotPaths `
        -Snapshots $snapshots
}
