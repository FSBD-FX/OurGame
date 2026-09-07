param(
    [string]$AuditRoot,
    [switch]$SkipBuild,
    [switch]$UiResolutionOnly,
    [switch]$UiResolutionManifestOnly,
    [datetime]$UiResolutionNotBeforeUtc = [DateTime]::MinValue
)

function Get-Task6CompatiblePowerShellHost {
    $current = $null
    try { $current = (Get-Process -Id $PID -ErrorAction Stop).Path } catch { }
    if (-not [string]::IsNullOrWhiteSpace($current) -and
        (Test-Path -LiteralPath $current -PathType Leaf)) { return $current }
    foreach ($name in @('pwsh', 'powershell.exe', 'powershell')) {
        $command = Get-Command $name -ErrorAction SilentlyContinue
        if ($null -ne $command) { return $command.Source }
    }
    throw 'Production audit cannot resolve a compatible PowerShell host.'
}

if ($PSVersionTable.PSVersion.Major -lt 7 -and -not $env:KNIGHT_PRODUCTION_AUDIT_CHILD) {
    $env:KNIGHT_PRODUCTION_AUDIT_CHILD = '1'
    $arguments = @('-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', $PSCommandPath)
    if (-not [string]::IsNullOrWhiteSpace($AuditRoot)) { $arguments += @('-AuditRoot', $AuditRoot) }
    if ($SkipBuild) { $arguments += '-SkipBuild' }
    if ($UiResolutionOnly) { $arguments += '-UiResolutionOnly' }
    if ($UiResolutionManifestOnly) { $arguments += '-UiResolutionManifestOnly' }
    if ($UiResolutionNotBeforeUtc -ne [DateTime]::MinValue) {
        $arguments += @('-UiResolutionNotBeforeUtc', $UiResolutionNotBeforeUtc.ToString('o'))
    }
    & (Get-Task6CompatiblePowerShellHost) @arguments
    exit $LASTEXITCODE
}

$ErrorActionPreference = 'Stop'
$project = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
. (Join-Path $PSScriptRoot 'EvidenceChecks.ps1')
$dotnetRoot = 'G:\godot\dotnet-sdk'
$godot = 'G:\godot\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64_console.exe'
$env:DOTNET_ROOT = $dotnetRoot
$env:DOTNET_ROOT_X64 = $dotnetRoot
$env:DOTNET_ROLL_FORWARD = 'LatestMajor'

if ([string]::IsNullOrWhiteSpace($AuditRoot)) {
    throw 'BASE production audit requires an explicit external -AuditRoot.'
}
$AuditRoot = [IO.Path]::GetFullPath($AuditRoot).TrimEnd('\')
$projectRoot = [IO.Path]::GetFullPath($project).TrimEnd('\')
if ($AuditRoot.Equals($projectRoot, [StringComparison]::OrdinalIgnoreCase) -or
    $AuditRoot.StartsWith(($projectRoot + '\'), [StringComparison]::OrdinalIgnoreCase)) {
    throw "BASE production audit root must be outside the candidate worktree: $AuditRoot"
}
if ((Test-Path -LiteralPath $AuditRoot) -and -not $UiResolutionManifestOnly) {
    throw "BASE production audit root must be fresh: $AuditRoot"
}
if ($UiResolutionManifestOnly -and -not (Test-Path -LiteralPath $AuditRoot -PathType Container)) {
    throw "PROLOGUE_UI_CAPTURE_MANIFEST_ROOT_MISSING root=$AuditRoot"
}
if (-not $UiResolutionManifestOnly) { New-Item -ItemType Directory -Path $AuditRoot -Force | Out-Null }
$auditStartedUtc = [DateTime]::UtcNow

$requiredTask4Cues = @(
    'RustShellTelegraph', 'MothTelegraph', 'LaborerTelegraph',
    'BossSweepTelegraph', 'BossLeapTelegraph', 'BossGroundWaveTelegraph', 'BossHammerTelegraph',
    'ThreadPreview', 'ThreadWindup', 'ThreadTravel', 'ThreadLatch', 'ThreadTension'
)
$displayBackedAuditRuns = 0

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
            throw "Ordered marker '$marker' appears before its required predecessor in $Path."
        }
        $previous = $markerIndices[0]
    }
}

function Get-WavFacts {
    param([Parameter(Mandatory)][string]$Path)

    $stream = [IO.File]::OpenRead($Path)
    $reader = [IO.BinaryReader]::new($stream)
    try {
        $riff = [Text.Encoding]::ASCII.GetString($reader.ReadBytes(4))
        $reader.BaseStream.Position = 8
        $wave = [Text.Encoding]::ASCII.GetString($reader.ReadBytes(4))
        $reader.BaseStream.Position = 22
        $channels = $reader.ReadInt16()
        $sampleRate = $reader.ReadInt32()
        $reader.BaseStream.Position = 34
        $bits = $reader.ReadInt16()
        $reader.BaseStream.Position = 40
        $dataBytes = $reader.ReadInt32()
        if ($riff -cne 'RIFF' -or $wave -cne 'WAVE' -or $channels -ne 1 -or $bits -ne 16) {
            throw "Unsupported production WAV format: $Path"
        }
        if ($dataBytes -le 0 -or $dataBytes -gt ($reader.BaseStream.Length - 44)) {
            throw "Invalid or empty WAV payload: $Path"
        }
        $peak = 0
        for ($read = 0; $read -lt $dataBytes; $read += 2) {
            $sample = [Math]::Abs([int]$reader.ReadInt16())
            if ($sample -gt $peak) { $peak = $sample }
        }
        $peakDbfs = 20.0 * [Math]::Log10([Math]::Max(1, $peak) / 32767.0)
        [pscustomobject]@{
            SampleRate = $sampleRate
            DurationSeconds = $dataBytes / ($sampleRate * $channels * ($bits / 8.0))
            PeakDbfs = $peakDbfs
        }
    }
    finally {
        $reader.Dispose()
        $stream.Dispose()
    }
}

function Test-FiniteDouble {
    param([Parameter(Mandatory)][double]$Value)
    # Windows PowerShell 5.1 targets a framework without Double.IsFinite. Keep the same
    # fail-closed rule using APIs available in both Windows PowerShell and pwsh.
    return -not ([double]::IsNaN($Value) -or [double]::IsInfinity($Value))
}

function Get-Task6FileSha256 {
    param([Parameter(Mandatory)][string]$Path)

    # Do not depend on command auto-loading in the Windows PowerShell compatible-child
    # process.  This is bounded-memory hashing as well: audio files and manifests are
    # read through a fixed buffer instead of materialized as byte arrays.
    $stream = [IO.File]::Open($Path, [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::Read)
    $sha256 = [Security.Cryptography.SHA256]::Create()
    $buffer = New-Object byte[] 65536
    try {
        while (($read = $stream.Read($buffer, 0, $buffer.Length)) -gt 0) {
            [void]$sha256.TransformBlock($buffer, 0, $read, $buffer, 0)
        }
        [void]$sha256.TransformFinalBlock($buffer, 0, 0)
        return ([BitConverter]::ToString($sha256.Hash)).Replace('-', '').ToLowerInvariant()
    }
    finally {
        $sha256.Dispose()
        $stream.Dispose()
    }
}

function Assert-Task4AudioManifest {
    param([Parameter(Mandatory)][string]$ManifestPath)

    if (-not (Test-Path -LiteralPath $ManifestPath -PathType Leaf)) {
        throw "Audio manifest missing: $ManifestPath"
    }
    $manifest = Get-Content -LiteralPath $ManifestPath -Raw | ConvertFrom-Json
    foreach ($cue in $requiredTask4Cues) {
        $cueProperty = $manifest.cues.PSObject.Properties[$cue]
        if ($null -eq $cueProperty -or @($cueProperty.Value).Count -eq 0) {
            throw "Audio manifest missing required cue '$cue'."
        }
    }
    if ($null -eq $manifest.fileMetadata) {
        throw 'Audio manifest missing deterministic fileMetadata.'
    }

    $resourcePaths = @()
    foreach ($property in $manifest.music.PSObject.Properties) { $resourcePaths += @($property.Value) }
    foreach ($property in $manifest.ambience.PSObject.Properties) { $resourcePaths += [string]$property.Value }
    foreach ($property in $manifest.cues.PSObject.Properties) { $resourcePaths += @($property.Value) }
    $resourcePaths = @($resourcePaths | Sort-Object -Unique)
    foreach ($resourcePath in $resourcePaths) {
        $metadataProperty = $manifest.fileMetadata.PSObject.Properties[$resourcePath]
        if ($null -eq $metadataProperty) {
            throw "Audio metadata missing exact resource '$resourcePath'."
        }
        $metadata = $metadataProperty.Value
        $relative = $resourcePath -replace '^res://', '' -replace '/', [IO.Path]::DirectorySeparatorChar
        $absolute = Join-Path $project $relative
        if (-not (Test-Path -LiteralPath $absolute -PathType Leaf)) {
            throw "Audio resource missing with no production fallback: $resourcePath"
        }
        $facts = Get-WavFacts -Path $absolute
        if ($facts.SampleRate -ne 48000 -or [double]$metadata.sampleRate -ne 48000) {
            throw "Audio resource is not 48 kHz: $resourcePath"
        }
        if (-not (Test-FiniteDouble $facts.DurationSeconds) -or $facts.DurationSeconds -le 0 -or
            [Math]::Abs($facts.DurationSeconds - [double]$metadata.durationSeconds) -gt 0.00001) {
            throw "Audio duration metadata mismatch: $resourcePath"
        }
        if (-not (Test-FiniteDouble $facts.PeakDbfs) -or $facts.PeakDbfs -gt -0.9 -or
            [Math]::Abs($facts.PeakDbfs - [double]$metadata.peakDbfs) -gt 0.001) {
            throw "Audio headroom metadata mismatch: $resourcePath actual=$($facts.PeakDbfs)"
        }
        if ([string]::IsNullOrWhiteSpace([string]$metadata.family)) {
            throw "Audio family metadata missing: $resourcePath"
        }
        $actualHash = Get-Task6FileSha256 -Path $absolute
        if ($actualHash -cne [string]$metadata.sha256) {
            throw "Audio deterministic hash mismatch: $resourcePath"
        }
    }
    return $manifest
}

function Get-ManifestHashSnapshot {
    param([Parameter(Mandatory)]$Manifest)
    $pairs = foreach ($property in $Manifest.fileMetadata.PSObject.Properties) {
        '{0}={1}' -f $property.Name, ([string]$property.Value.sha256)
    }
    return ($pairs | Sort-Object) -join "`n"
}

function Invoke-Task4AudioDeterminismAudit {
    $manifestPath = Join-Path $project 'assets\audio\prologue\audio-manifest.json'
    $null = Assert-Task4AudioManifest -ManifestPath $manifestPath
    $renderer = Join-Path $project 'tools\audio\render_knight_audio.py'
    & python $renderer | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "First deterministic audio render failed with exit code $LASTEXITCODE" }
    $first = Assert-Task4AudioManifest -ManifestPath $manifestPath
    $firstSnapshot = Get-ManifestHashSnapshot -Manifest $first
    $firstManifestHash = Get-Task6FileSha256 -Path $manifestPath
    & python $renderer | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "Second deterministic audio render failed with exit code $LASTEXITCODE" }
    $second = Assert-Task4AudioManifest -ManifestPath $manifestPath
    $secondSnapshot = Get-ManifestHashSnapshot -Manifest $second
    $secondManifestHash = Get-Task6FileSha256 -Path $manifestPath
    if ($firstSnapshot -cne $secondSnapshot -or $firstManifestHash -cne $secondManifestHash) {
        throw 'Audio renderer is not deterministic across two fixed-seed regenerations.'
    }
    $metadataProperties = @($second.fileMetadata.PSObject.Properties)
    $peak = ($metadataProperties | ForEach-Object { [double]$_.Value.peakDbfs } | Measure-Object -Maximum).Maximum
    "PROLOGUE_AUDIO_AUDIT files=$($metadataProperties.Count) rate=48000 peak_dbfs=$peak deterministic=TRUE"
}

if (-not $SkipBuild) {
    & (Join-Path $dotnetRoot 'dotnet.exe') build (Join-Path $project 'Knight.csproj')
    if ($LASTEXITCODE) { throw "Production audit build failed with exit code $LASTEXITCODE" }
}

if (-not $UiResolutionOnly -and -not $UiResolutionManifestOnly) { Invoke-Task4AudioDeterminismAudit }

function Invoke-ProductionAudit {
    param(
        [Parameter(Mandatory)][string]$Name,
        [Parameter(Mandatory)][string]$Script,
        [Parameter(Mandatory)][string]$Marker,
        [string[]]$AdditionalArguments = @(),
        [hashtable]$EnvironmentOverrides = @{}
    )

    $log = Join-Path $AuditRoot ("audit-prologue-{0}.log" -f $Name)
    $consolePath = Join-Path $AuditRoot ("audit-prologue-{0}.console.log" -f $Name)

    $arguments = @(
        '--path', $project, '--fixed-fps', '60',
        '--display-driver', 'windows', '--rendering-method', 'gl_compatibility', '--audio-driver', 'Dummy',
        '--script', $Script, '--log-file', $log
    ) + $AdditionalArguments
    if ($arguments -contains '--headless' -or
        $arguments[([Array]::IndexOf($arguments, '--display-driver') + 1)] -cne 'windows') {
        throw "Production audit '$Name' must preserve the Windows display-backed contract."
    }
    $priorEnvironment = @{}
    foreach ($key in $EnvironmentOverrides.Keys) {
        $priorEnvironment[$key] = [Environment]::GetEnvironmentVariable([string]$key, 'Process')
        [Environment]::SetEnvironmentVariable([string]$key,
            [string]$EnvironmentOverrides[$key], 'Process')
    }
    $priorPreference = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    try {
        $consoleLines = & $godot @arguments 2>&1
        $exitCode = $LASTEXITCODE
    }
    finally {
        $ErrorActionPreference = $priorPreference
        foreach ($key in $priorEnvironment.Keys) {
            [Environment]::SetEnvironmentVariable([string]$key, $priorEnvironment[$key], 'Process')
        }
    }
    $consoleLines | Set-Content -LiteralPath $consolePath -Encoding UTF8
    if ($exitCode -ne 0) { throw "Production audit '$Name' failed with exit code $exitCode." }

    Assert-ExactLogMarker -Path $log -Marker $Marker
    Assert-ExactLogMarker -Path $consolePath -Marker $Marker
    Assert-NoEngineLogErrors -Paths @($log, $consolePath)
    $console = Get-Content -LiteralPath $consolePath -Raw
    $forbidden = @(
        'Leaked instance:', 'ObjectDB instances were leaked', 'resources still in use',
        'Resource still in use:', 'GCHandle', 'RID allocations', 'FATAL',
        'SCRIPT ERROR:', 'Parse Error', 'ERROR:'
    )
    foreach ($pattern in $forbidden) {
        if ($console.IndexOf($pattern, [StringComparison]::OrdinalIgnoreCase) -ge 0) {
            throw "Production audit '$Name' console contains forbidden output '$pattern'."
        }
    }
    Get-Content -LiteralPath $log | Where-Object {
        $_ -like 'PROLOGUE_UI_RESOLUTION=*' -or $_ -like 'PROLOGUE_PERFORMANCE*' -or $_ -ceq $Marker
    }
    $script:displayBackedAuditRuns++
}

function Get-Task6UiExpectedCapturePaths {
    $commonStates = @(
        'entrance-guidance-expanded-en', 'entrance-guidance-expanded-zh',
        'claw-guidance-expanded-en', 'claw-guidance-expanded-zh',
        'ordinary-keyboard', 'action-hud-en', 'action-hud-zh',
        'reticle-attack', 'reticle-grippable', 'reticle-ice', 'reticle-forbidden',
        'reticle-obstructed', 'reticle-outofrange', 'boss-layout-safe',
        'pause-en', 'pause-zh', 'completion-clear-en', 'completion-clear-zh'
    )
    $expected = [Collections.Generic.List[string]]::new()
    foreach ($resolution in @('1280x720', '1920x1080', '2560x1440', '3840x2160')) {
        $states = [Collections.Generic.List[string]]::new()
        foreach ($state in $commonStates) { $states.Add($state) }
        if ($resolution -ceq '1280x720') {
            $states.Add('ui-scale-150')
            $states.Add('reduced-flash-two-channel')
        }
        foreach ($state in $states) {
            foreach ($index in 0..2) {
                $expected.Add(($resolution + '\' + $state + ('-frame{0:D2}.png' -f $index)))
            }
        }
    }
    return @($expected.ToArray())
}

function Write-Task6UiCaptureManifest {
    param(
        [Parameter(Mandatory)][string]$CaptureRoot,
        [Parameter(Mandatory)][string]$UiAuditLog,
        [Parameter(Mandatory)][DateTime]$StartedUtc
    )

    $resolvedCaptureRoot = [IO.Path]::GetFullPath($CaptureRoot).TrimEnd('\')
    $resolvedAuditRoot = [IO.Path]::GetFullPath($AuditRoot).TrimEnd('\')
    if (-not $resolvedCaptureRoot.StartsWith(($resolvedAuditRoot + '\'),
            [StringComparison]::OrdinalIgnoreCase)) {
        throw "PROLOGUE_UI_CAPTURE_ROOT_OUTSIDE_EVIDENCE root=$resolvedCaptureRoot evidence=$resolvedAuditRoot"
    }
    $expected = @(Get-Task6UiExpectedCapturePaths)
    if ($expected.Count -ne 222) { throw "UI capture contract expected 222 PNG paths; actual=$($expected.Count)" }
    $files = @((Get-ChildItem -LiteralPath $resolvedCaptureRoot -File -Recurse -Filter '*.png') |
        Sort-Object FullName)
    if ($files.Count -ne $expected.Count) {
        throw "PROLOGUE_UI_CAPTURE_PNG_COUNT_MISMATCH expected=$($expected.Count) actual=$($files.Count)"
    }
    $actual = @{}
    foreach ($file in $files) {
        $full = [IO.Path]::GetFullPath($file.FullName)
        if (-not $full.StartsWith(($resolvedCaptureRoot + '\'), [StringComparison]::OrdinalIgnoreCase)) {
            throw "PROLOGUE_UI_CAPTURE_PATH_OUTSIDE_ROOT path=$full root=$resolvedCaptureRoot"
        }
        if ($file.Length -le 0 -or $file.LastWriteTimeUtc -lt $StartedUtc.ToUniversalTime()) {
            throw "PROLOGUE_UI_CAPTURE_NOT_FRESH path=$full size=$($file.Length) started_utc=$($StartedUtc.ToString('o'))"
        }
        $relative = $full.Substring(($resolvedCaptureRoot + '\').Length).Replace('/', '\')
        if ($actual.ContainsKey($relative)) { throw "PROLOGUE_UI_CAPTURE_DUPLICATE path=$relative" }
        $actual[$relative] = $file
    }
    $logLines = @(Get-Content -LiteralPath $UiAuditLog)
    $manifestLines = [Collections.Generic.List[string]]::new()
    $manifestLines.Add('schema=task6-ui-resolution-capture-v1')
    $manifestLines.Add('evidence_root=' + $resolvedAuditRoot)
    $manifestLines.Add('capture_root=' + $resolvedCaptureRoot)
    $manifestLines.Add('audit_log=audit-prologue-ui-resolution.log')
    $manifestLines.Add('created_after_utc=' + $StartedUtc.ToUniversalTime().ToString('o'))
    $manifestLines.Add('resolution_count=4')
    $manifestLines.Add('png_count=' + $expected.Count)
    foreach ($relative in $expected) {
        if (-not $actual.ContainsKey($relative)) { throw "PROLOGUE_UI_CAPTURE_FILE_MISSING path=$relative" }
        $file = $actual[$relative]
        $expectedLog = 'PROLOGUE_UI_CAPTURE=' + (Join-Path $resolvedCaptureRoot $relative)
        if (@($logLines | Where-Object { $_ -ceq $expectedLog }).Count -ne 1) {
            throw "PROLOGUE_UI_CAPTURE_LOG_BINDING_MISMATCH path=$relative"
        }
        $manifestLines.Add('file=' + $relative.Replace('\', '/') + '|' +
            (Get-Task6FileSha256 -Path $file.FullName) + '|' + $file.Length + '|' +
            $file.LastWriteTimeUtc.ToString('o'))
    }
    $manifest = Join-Path $resolvedAuditRoot 'ui-resolution-capture-manifest.txt'
    [IO.File]::WriteAllLines($manifest, $manifestLines.ToArray(), [Text.UTF8Encoding]::new($false))
    $verified = Assert-Task6UiCaptureManifest -Root $resolvedAuditRoot -NotBeforeUtc $StartedUtc
    return [pscustomobject]@{ Path = $manifest; PngCount = $verified.PngCount }
}

function Assert-Task6UiCaptureManifest {
    param(
        [Parameter(Mandatory)][string]$Root,
        [datetime]$NotBeforeUtc = [DateTime]::MinValue
    )

    $resolvedRoot = [IO.Path]::GetFullPath($Root).TrimEnd('\')
    $manifest = Join-Path $resolvedRoot 'ui-resolution-capture-manifest.txt'
    if (-not (Test-Path -LiteralPath $manifest -PathType Leaf)) {
        throw "PROLOGUE_UI_CAPTURE_MANIFEST_MISSING path=$manifest"
    }
    $fields = @{}
    $fileRows = [Collections.Generic.List[string]]::new()
    foreach ($line in @(Get-Content -LiteralPath $manifest)) {
        if ($line.StartsWith('file=', [StringComparison]::Ordinal)) { $fileRows.Add($line.Substring(5)); continue }
        $parts = $line -split '=', 2
        if ($parts.Count -ne 2 -or [string]::IsNullOrWhiteSpace($parts[0]) -or
            $fields.ContainsKey($parts[0])) {
            throw "PROLOGUE_UI_CAPTURE_MANIFEST_MALFORMED value=$line"
        }
        $fields[$parts[0]] = $parts[1]
    }
    foreach ($required in @('schema', 'evidence_root', 'capture_root', 'audit_log',
            'created_after_utc', 'resolution_count', 'png_count')) {
        if (-not $fields.ContainsKey($required)) {
            throw "PROLOGUE_UI_CAPTURE_MANIFEST_MISSING_FIELD field=$required"
        }
    }
    if ($fields['schema'] -cne 'task6-ui-resolution-capture-v1') {
        throw "PROLOGUE_UI_CAPTURE_MANIFEST_SCHEMA_MISMATCH actual=$($fields['schema'])"
    }
    if ([IO.Path]::GetFullPath([string]$fields['evidence_root']).TrimEnd('\') -cne $resolvedRoot) {
        throw "PROLOGUE_UI_CAPTURE_MANIFEST_ROOT_MISMATCH expected=$resolvedRoot actual=$($fields['evidence_root'])"
    }
    $captureRoot = [IO.Path]::GetFullPath([string]$fields['capture_root']).TrimEnd('\')
    if (-not $captureRoot.StartsWith(($resolvedRoot + '\'), [StringComparison]::OrdinalIgnoreCase)) {
        throw "PROLOGUE_UI_CAPTURE_ROOT_OUTSIDE_EVIDENCE root=$captureRoot evidence=$resolvedRoot"
    }
    $createdUtc = [DateTime]::Parse([string]$fields['created_after_utc'],
        [Globalization.CultureInfo]::InvariantCulture, [Globalization.DateTimeStyles]::RoundtripKind).ToUniversalTime()
    if ($createdUtc -lt $NotBeforeUtc.ToUniversalTime()) {
        throw "PROLOGUE_UI_CAPTURE_STALE created_utc=$($createdUtc.ToString('o')) not_before_utc=$($NotBeforeUtc.ToUniversalTime().ToString('o'))"
    }
    $resolutionCount = 0; $pngCount = 0
    if (-not [int]::TryParse([string]$fields['resolution_count'], [ref]$resolutionCount) -or $resolutionCount -ne 4 -or
        -not [int]::TryParse([string]$fields['png_count'], [ref]$pngCount) -or $pngCount -ne 222 -or
        $fileRows.Count -ne 222) {
        throw "PROLOGUE_UI_CAPTURE_MANIFEST_COUNT_MISMATCH resolutions=$($fields['resolution_count']) pngs=$($fields['png_count']) rows=$($fileRows.Count)"
    }
    $expected = @{}
    foreach ($relative in @(Get-Task6UiExpectedCapturePaths)) { $expected[$relative] = $true }
    $seen = @{}
    foreach ($row in $fileRows) {
        $parts = $row -split '\|', 4
        if ($parts.Count -ne 4 -or $parts[1] -notmatch '^[a-f0-9]{64}$') {
            throw "PROLOGUE_UI_CAPTURE_MANIFEST_MALFORMED value=file=$row"
        }
        $relative = $parts[0].Replace('/', '\')
        if (-not $expected.ContainsKey($relative) -or $seen.ContainsKey($relative)) {
            throw "PROLOGUE_UI_CAPTURE_MANIFEST_PATH_MISMATCH path=$relative"
        }
        $length = 0
        if (-not [long]::TryParse($parts[2], [ref]$length) -or $length -le 0) {
            throw "PROLOGUE_UI_CAPTURE_MANIFEST_LENGTH_MISMATCH path=$relative value=$($parts[2])"
        }
        $recordedUtc = [DateTime]::Parse($parts[3], [Globalization.CultureInfo]::InvariantCulture,
            [Globalization.DateTimeStyles]::RoundtripKind).ToUniversalTime()
        $path = [IO.Path]::GetFullPath((Join-Path $captureRoot $relative))
        if (-not $path.StartsWith(($captureRoot + '\'), [StringComparison]::OrdinalIgnoreCase) -or
            -not (Test-Path -LiteralPath $path -PathType Leaf)) {
            throw "PROLOGUE_UI_CAPTURE_FILE_MISSING path=$relative"
        }
        $item = Get-Item -LiteralPath $path
        if ($item.Length -ne $length) { throw "PROLOGUE_UI_CAPTURE_LENGTH_MISMATCH path=$relative" }
        if ($item.LastWriteTimeUtc -lt $createdUtc -or $item.LastWriteTimeUtc -lt $NotBeforeUtc.ToUniversalTime() -or
            $recordedUtc -lt $createdUtc -or $recordedUtc -lt $NotBeforeUtc.ToUniversalTime()) {
            throw "PROLOGUE_UI_CAPTURE_NOT_FRESH path=$relative"
        }
        if ((Get-Task6FileSha256 -Path $path) -cne $parts[1]) {
            throw "PROLOGUE_UI_CAPTURE_HASH_MISMATCH path=$relative"
        }
        $seen[$relative] = $true
    }
    if (@((Get-ChildItem -LiteralPath $captureRoot -File -Recurse -Filter '*.png')).Count -ne 222) {
        throw 'PROLOGUE_UI_CAPTURE_PNG_COUNT_MISMATCH expected=222'
    }
    $auditLog = [IO.Path]::GetFullPath((Join-Path $resolvedRoot ([string]$fields['audit_log'])))
    if (-not $auditLog.StartsWith(($resolvedRoot + '\'), [StringComparison]::OrdinalIgnoreCase) -or
        -not (Test-Path -LiteralPath $auditLog -PathType Leaf)) {
        throw "PROLOGUE_UI_CAPTURE_AUDIT_LOG_MISSING path=$auditLog"
    }
    $auditLines = @(Get-Content -LiteralPath $auditLog)
    foreach ($relative in $expected.Keys) {
        $marker = 'PROLOGUE_UI_CAPTURE=' + (Join-Path $captureRoot $relative)
        if (@($auditLines | Where-Object { $_ -ceq $marker }).Count -ne 1) {
            throw "PROLOGUE_UI_CAPTURE_LOG_BINDING_MISMATCH path=$relative"
        }
    }
    return [pscustomobject]@{ Path = $manifest; PngCount = 222; CaptureRoot = $captureRoot }
}

if ($UiResolutionManifestOnly) {
    $result = Assert-Task6UiCaptureManifest -Root $AuditRoot -NotBeforeUtc $UiResolutionNotBeforeUtc
    "TASK6_UI_CAPTURE_MANIFEST_OK manifest=$($result.Path) pngs=$($result.PngCount) root=$($result.CaptureRoot)"
    return
}

$uiCaptureRoot = Join-Path $AuditRoot 'ui-resolution-captures'
if (Test-Path -LiteralPath $uiCaptureRoot) {
    throw "PROLOGUE_UI_CAPTURE_ROOT_NOT_FRESH root=$uiCaptureRoot"
}
Invoke-ProductionAudit -Name 'ui-resolution' `
    -Script 'res://tests/presentation/PrologueUiResolutionCapture.cs' `
    -Marker 'PROLOGUE_UI_RESOLUTION_OK' `
    -EnvironmentOverrides @{
        KNIGHT_UI_CAPTURE_ROOT = $uiCaptureRoot
        KNIGHT_UI_CAPTURE_EVIDENCE_ROOT = $AuditRoot
        KNIGHT_UI_CAPTURE_CERTIFICATION = '1'
    }
$uiCaptureManifest = Write-Task6UiCaptureManifest -CaptureRoot $uiCaptureRoot `
    -UiAuditLog (Join-Path $AuditRoot 'audit-prologue-ui-resolution.log') -StartedUtc $auditStartedUtc
$uiCaptureManifestHash = Get-Task6FileSha256 -Path $uiCaptureManifest.Path
"TASK6_UI_CAPTURE_EXTERNAL_OK root=$uiCaptureRoot pngs=$($uiCaptureManifest.PngCount) " +
    "manifest=$($uiCaptureManifest.Path) sha256=$uiCaptureManifestHash"
if ($UiResolutionOnly) { return }
Invoke-ProductionAudit -Name 'task4-audio-vfx' `
    -Script 'res://tests/RunTask4AudioVfxTests.cs' `
    -Marker 'TASK4_AUDIO_VFX_TESTS_OK'
$task4DrainMarkers = @(
    'PROLOGUE_TEST_PHASE_EXIT_DRAIN_OK phase=task4-audio-vfx',
    'TASK4_AUDIO_VFX_TESTS_OK'
)
foreach ($task4Log in @(
    (Join-Path $AuditRoot 'audit-prologue-task4-audio-vfx.log'),
    (Join-Path $AuditRoot 'audit-prologue-task4-audio-vfx.console.log'))) {
    Assert-OrderedExactLogMarkers -Path $task4Log -Markers $task4DrainMarkers
}
    # The old six-room visual recorder cannot mount the certified generated-sector level.
    # Reuse Task6's display-backed mounted-sector audit for both retained BASE visual slots;
    # it emits their terminals only after real topology and callback measurements complete.
Invoke-ProductionAudit -Name 'visual-evidence' `
    -Script 'res://tests/presentation/PrologueVisualAuditTask6.cs' `
    -Marker 'PROLOGUE_VISUAL_EVIDENCE_OK'
Invoke-ProductionAudit -Name 'visual-performance' `
    -Script 'res://tests/presentation/PrologueVisualAuditTask6.cs' `
    -Marker 'PROLOGUE_VISUAL_PERFORMANCE_OK'
if ($displayBackedAuditRuns -ne 4) {
    throw "Expected four display-backed production audits; actual=$displayBackedAuditRuns"
}
'PROLOGUE_VISUAL_AUDIT_OK'
'PROLOGUE_DISPLAY_BACKED_AUDIT_OK runs=4 driver=windows'
'PROLOGUE_PRODUCTION_AUDIT_OK'
