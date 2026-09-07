[CmdletBinding()]
param(
    [string]$VerificationRoot,
    [string]$CaptureRoot,
    [string]$ProjectRoot,
    [switch]$CaptureEvidenceOnly,
    [switch]$CaptureMaterializationManifestOnly,
    [string]$CaptureMaterializationManifest,
    [switch]$MaterializationPreflightOnly,
    [switch]$CleanCandidatePreflightOnly,
    [switch]$CleanCandidateRestoreOnly,
    [string]$CleanCandidateStateLog,
    [string]$VerifierTerminalLog,
    [switch]$VerifierTerminalOnly,
    [switch]$SkipCapture
)

$ErrorActionPreference = 'Stop'
$toolProject = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
if ([string]::IsNullOrWhiteSpace($ProjectRoot)) { $project = $toolProject }
else { $project = (Resolve-Path -LiteralPath $ProjectRoot).Path }
. (Join-Path $PSScriptRoot 'EvidenceChecks.ps1')
. (Join-Path $PSScriptRoot 'Task6AviStreaming.ps1')
$dotnetRoot = 'G:\godot\dotnet-sdk'
$godot = 'G:\godot\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64_console.exe'
$env:DOTNET_ROOT = $dotnetRoot
$env:DOTNET_ROOT_X64 = $dotnetRoot
$env:DOTNET_ROLL_FORWARD = 'LatestMajor'
$env:DOTNET_CLI_UI_LANGUAGE = 'en'

function Get-Task6CompatiblePowerShellHost {
    $current = Get-Process -Id $PID -ErrorAction SilentlyContinue
    if ($null -ne $current -and -not [string]::IsNullOrWhiteSpace($current.Path) -and
        (Test-Path -LiteralPath $current.Path -PathType Leaf)) { return $current.Path }
    foreach ($name in @('pwsh', 'powershell.exe', 'powershell')) {
        $command = Get-Command $name -ErrorAction SilentlyContinue
        if ($null -ne $command -and -not [string]::IsNullOrWhiteSpace($command.Source)) {
            return $command.Source
        }
    }
    throw 'No compatible PowerShell child host is available.'
}

if ([string]::IsNullOrWhiteSpace($VerificationRoot)) {
    $VerificationRoot = Join-Path $project ('artifacts\task6-verify-' +
        (Get-Date -Format 'yyyyMMddTHHmmssfffZ') + '-' + [guid]::NewGuid().ToString('N'))
}
if (($CleanCandidatePreflightOnly -or $CleanCandidateRestoreOnly) -and
    [IO.Path]::GetFullPath($VerificationRoot).TrimEnd('\').StartsWith(
        ($project.TrimEnd('\') + '\'), [StringComparison]::OrdinalIgnoreCase)) {
    throw 'Task 6 clean-candidate state and cleanup evidence must be outside the candidate worktree.'
}
if (Test-Path -LiteralPath $VerificationRoot) {
    throw "Task 6 verifier root must be fresh; refusing to reuse or overwrite: $VerificationRoot"
}
if (-not $MaterializationPreflightOnly -and -not $CleanCandidatePreflightOnly -and -not $CleanCandidateRestoreOnly -and -not $VerifierTerminalOnly -and
    -not (Test-Path -LiteralPath $CaptureRoot -PathType Container)) {
    throw "Task 6 verifier requires the final capture bundle: $CaptureRoot"
}
New-Item -ItemType Directory -Path $VerificationRoot -Force | Out-Null

function Assert-OneExactMarker {
    param([string]$Path, [string]$Marker, [string]$Label)
    $count = @((Get-Content -LiteralPath $Path) | Where-Object { $_ -ceq $Marker }).Count
    if ($count -ne 1) { throw "$Label must occur exactly once; actual=$count marker=$Marker" }
}

function Assert-OneRegexMarker {
    param([string]$Path, [string]$Pattern, [string]$Label)
    $count = @([regex]::Matches((Get-Content -LiteralPath $Path -Raw), $Pattern,
        [Text.RegularExpressions.RegexOptions]::Multiline)).Count
    if ($count -ne 1) { throw "$Label must occur exactly once; actual=$count pattern=$Pattern" }
}

function Assert-NonEmptyFiles {
    param([string[]]$Paths, [string]$Label)
    foreach ($path in $Paths) {
        if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { throw "$Label missing: $path" }
        if ((Get-Item -LiteralPath $path).Length -le 0) { throw "$Label empty: $path" }
    }
}

function Get-Task6CaptureMaterializationRoot {
    param([Parameter(Mandatory)][string]$Root)
    if ([string]::IsNullOrWhiteSpace($Root) -or -not (Test-Path -LiteralPath $Root -PathType Container)) {
        throw 'capture_materialization_capture_root_required'
    }
    $item = Get-Item -LiteralPath $Root -Force -ErrorAction Stop
    if (-not $item.PSIsContainer -or ($item.Attributes -band [IO.FileAttributes]::ReparsePoint)) {
        throw "capture_materialization_capture_root_invalid path=$Root"
    }
    return (Resolve-Path -LiteralPath $Root).Path.TrimEnd('\')
}

function Assert-Task6CaptureMaterializationContainedFile {
    param(
        [Parameter(Mandatory)][string]$Root,
        [Parameter(Mandatory)][string]$Path,
        [Parameter(Mandatory)][string]$Label
    )
    try { $absolute = [IO.Path]::GetFullPath($Path) }
    catch { throw "$Label invalid_path=$Path" }
    if (-not $absolute.StartsWith(($Root + '\'), [StringComparison]::OrdinalIgnoreCase)) {
        throw "$Label path=$absolute root=$Root"
    }
    if (-not (Test-Path -LiteralPath $absolute -PathType Leaf)) {
        throw "$Label missing=$absolute"
    }
    $item = Get-Item -LiteralPath $absolute -Force -ErrorAction Stop
    if ($item.Attributes -band [IO.FileAttributes]::ReparsePoint) {
        throw "$Label reparse_point=$absolute"
    }
    $resolved = (Resolve-Path -LiteralPath $absolute).Path
    if (-not $resolved.StartsWith(($Root + '\'), [StringComparison]::OrdinalIgnoreCase)) {
        throw "$Label resolved_outside=$resolved root=$Root"
    }
    if ($item.Length -le 0) { throw "$Label empty=$resolved" }
    return $resolved
}

function Get-Task6CaptureMaterializationSingleValue {
    param(
        [Parameter(Mandatory)][string[]]$Lines,
        [Parameter(Mandatory)][string]$Name,
        [Parameter(Mandatory)][string]$Label
    )
    $prefix = $Name + '='
    $matches = @($Lines | Where-Object { $_.StartsWith($prefix, [StringComparison]::Ordinal) })
    if ($matches.Count -ne 1) { throw "$Label actual=$($matches.Count)" }
    $value = $matches[0].Substring($prefix.Length)
    if ([string]::IsNullOrWhiteSpace($value)) { throw "$Label empty_value=True" }
    return $value
}

function Assert-Task6CaptureMaterializationBoundFile {
    param(
        [Parameter(Mandatory)][string[]]$Lines,
        [Parameter(Mandatory)][string]$Root,
        [Parameter(Mandatory)][string]$Name
    )
    $path = Get-Task6CaptureMaterializationSingleValue -Lines $Lines -Name $Name `
        -Label "capture_materialization_referenced_file_required key=$Name"
    $hash = Get-Task6CaptureMaterializationSingleValue -Lines $Lines -Name ($Name + '_sha256') `
        -Label "capture_materialization_referenced_hash_required key=$Name"
    if ($hash -notmatch '^[A-Fa-f0-9]{64}$') {
        throw "capture_materialization_referenced_hash_required key=$Name malformed=True"
    }
    $resolved = Assert-Task6CaptureMaterializationContainedFile -Root $Root -Path $path `
        -Label "capture_materialization_referenced_path_outside_capture_root key=$Name"
    $actual = Get-EvidenceSha256 -LiteralPath $resolved
    if ($actual -ine $hash) {
        throw "capture_materialization_referenced_hash_mismatch key=$Name expected=$hash actual=$actual"
    }
    return $resolved
}

function Assert-Task6CaptureMaterializationManifest {
    param(
        [Parameter(Mandatory)][string]$Root,
        [Parameter(Mandatory)][string]$ManifestPath
    )
    $captureRoot = Get-Task6CaptureMaterializationRoot -Root $Root
    if ([string]::IsNullOrWhiteSpace($ManifestPath)) {
        throw 'capture_materialization_manifest_required'
    }
    try { $manifestAbsolute = [IO.Path]::GetFullPath($ManifestPath) }
    catch { throw "capture_materialization_manifest_outside_capture_root path=$ManifestPath root=$captureRoot" }
    if (-not $manifestAbsolute.StartsWith(($captureRoot + '\'), [StringComparison]::OrdinalIgnoreCase)) {
        throw "capture_materialization_manifest_outside_capture_root path=$manifestAbsolute root=$captureRoot"
    }
    $expectedManifest = Join-Path $captureRoot 'task6-capture-materialization.manifest.txt'
    if (-not $manifestAbsolute.Equals($expectedManifest, [StringComparison]::OrdinalIgnoreCase)) {
        throw "capture_materialization_manifest_path_required expected=$expectedManifest actual=$manifestAbsolute"
    }
    $manifest = Assert-Task6CaptureMaterializationContainedFile -Root $captureRoot -Path $manifestAbsolute `
        -Label 'capture_materialization_manifest_outside_capture_root'
    $lines = @((Get-Content -LiteralPath $manifest) | ForEach-Object { [string]$_ })
    if (@($lines | Where-Object { $_ -ceq 'schema=task6-capture-materialization-v1' }).Count -ne 1) {
        throw 'capture_materialization_schema_required'
    }

    $importTerminal = Get-Task6CaptureMaterializationSingleValue -Lines $lines -Name 'import_terminal' `
        -Label 'capture_materialization_import_terminal_required'
    $importMatch = [regex]::Match($importTerminal,
        '^PROLOGUE_IMPORT_NATIVE_RETRY_OK attempts=(?<attempts>[12]) first_exit=(?<exit>-?\d+) restored=(?<restored>True|False)$')
    if (-not $importMatch.Success) { throw 'capture_materialization_import_terminal_required malformed=True' }
    $importLog = Assert-Task6CaptureMaterializationBoundFile -Lines $lines -Root $captureRoot -Name 'import_log'
    $importConsole = Assert-Task6CaptureMaterializationBoundFile -Lines $lines -Root $captureRoot -Name 'import_console'
    $importSummary = Assert-Task6CaptureMaterializationBoundFile -Lines $lines -Root $captureRoot -Name 'import_summary'
    Assert-OneExactMarker -Path $importLog -Marker 'VERIFICATION_EDITOR_GC_DRAIN_OK' -Label 'capture materialization import GC drain'
    Assert-NoEngineLogErrors -Paths @($importLog, $importConsole)
    if (@((Get-Content -LiteralPath $importSummary) | Where-Object { $_ -ceq $importTerminal }).Count -ne 1) {
        throw 'capture_materialization_import_terminal_summary_mismatch'
    }

    $builderCount = Get-Task6CaptureMaterializationSingleValue -Lines $lines -Name 'builder_count' `
        -Label 'capture_materialization_builder_count_required'
    if ($builderCount -cne '6') { throw "capture_materialization_builder_count_required expected=6 actual=$builderCount" }
    $builders = @(
        @{ Name = 'BuildCanyonEntrance'; Script = 'res://tools/build/prologue/BuildCanyonEntrance.cs' },
        @{ Name = 'BuildCanyonDescent'; Script = 'res://tools/build/prologue/BuildCanyonDescent.cs' },
        @{ Name = 'BuildCanyonBasin'; Script = 'res://tools/build/prologue/BuildCanyonBasin.cs' },
        @{ Name = 'BuildCanyonAscent'; Script = 'res://tools/build/prologue/BuildCanyonAscent.cs' },
        @{ Name = 'BuildLostTemple'; Script = 'res://tools/build/prologue/BuildLostTemple.cs' },
        @{ Name = 'BuildPrologueLevel'; Script = 'res://tools/build/prologue/BuildPrologueLevel.cs' }
    )
    $builderDescriptorLines = @($lines | Where-Object { $_ -match '^builder_\d{2}=' })
    if ($builderDescriptorLines.Count -ne $builders.Count) {
        throw "capture_materialization_builder_order_required expected=$($builders.Count) actual=$($builderDescriptorLines.Count)"
    }
    for ($index = 0; $index -lt $builders.Count; $index++) {
        $ordinal = ($index + 1).ToString('00')
        $name = 'builder_' + $ordinal
        $expected = $builders[$index].Name + '|script=' + $builders[$index].Script
        $expectedRecord = $name + '=' + $expected
        if ($builderDescriptorLines[$index] -cne $expectedRecord) {
            throw "capture_materialization_builder_order_required ordinal=$ordinal expected=$expectedRecord actual=$($builderDescriptorLines[$index])"
        }
        $actual = Get-Task6CaptureMaterializationSingleValue -Lines $lines -Name $name `
            -Label "capture_materialization_builder_order_required ordinal=$ordinal"
        if ($actual -cne $expected) {
            throw "capture_materialization_builder_order_required ordinal=$ordinal expected=$expected actual=$actual"
        }
        $builderLog = Assert-Task6CaptureMaterializationBoundFile -Lines $lines -Root $captureRoot -Name ($name + '_log')
        $builderConsole = Assert-Task6CaptureMaterializationBoundFile -Lines $lines -Root $captureRoot -Name ($name + '_console')
        Assert-NoEngineLogErrors -Paths @($builderLog, $builderConsole)
    }

    $semanticSha = Get-Task6CaptureMaterializationSingleValue -Lines $lines -Name 'generated_runtime_semantics_sha256' `
        -Label 'capture_materialization_semantic_sha_required'
    if ($semanticSha -notmatch '^[A-Fa-f0-9]{64}$') { throw 'capture_materialization_semantic_sha_required malformed=True' }
    $semanticCount = Get-Task6CaptureMaterializationSingleValue -Lines $lines -Name 'generated_runtime_semantics_count' `
        -Label 'capture_materialization_semantic_count_required'
    if ($semanticCount -cne '6') { throw "capture_materialization_semantic_count_required expected=6 actual=$semanticCount" }
    $pathLines = @($lines | Where-Object { $_.StartsWith('generated_runtime_semantic_path=', [StringComparison]::Ordinal) })
    $scenePaths = @(
        'scenes/prologue/sectors/CanyonEntrance.tscn',
        'scenes/prologue/sectors/CanyonDescent.tscn',
        'scenes/prologue/sectors/CanyonBasin.tscn',
        'scenes/prologue/sectors/CanyonAscent.tscn',
        'scenes/prologue/sectors/LostTemple.tscn',
        'scenes/prologue/PrologueLevel.tscn'
    )
    if ($pathLines.Count -ne $scenePaths.Count) {
        throw "capture_materialization_semantic_paths_required expected=$($scenePaths.Count) actual=$($pathLines.Count)"
    }
    for ($index = 0; $index -lt $scenePaths.Count; $index++) {
        $pathMatch = [regex]::Match($pathLines[$index],
            '^generated_runtime_semantic_path=(?<path>[^|]+)\|sha256=(?<sha>[A-Fa-f0-9]{64})$')
        if (-not $pathMatch.Success -or $pathMatch.Groups['path'].Value -cne $scenePaths[$index]) {
            throw "capture_materialization_semantic_paths_required ordinal=$($index + 1) expected=$($scenePaths[$index])"
        }
    }

    $terminalPath = Assert-Task6CaptureMaterializationContainedFile -Root $captureRoot `
        -Path (Join-Path $captureRoot 'task6-capture-materialization.log') `
        -Label 'capture_materialization_terminal_log_required'
    $terminalPattern = '^TASK6_CAPTURE_MATERIALIZATION_OK import_attempts=(?<attempts>[12]) import_first_exit=(?<exit>-?\d+) import_restored=(?<restored>True|False) builders=6 runtime_semantics_sha256=(?<sha>[A-Fa-f0-9]{64}) runtime_semantics_count=6$'
    $terminalMatches = @([regex]::Matches((Get-Content -LiteralPath $terminalPath -Raw), $terminalPattern,
            [Text.RegularExpressions.RegexOptions]::Multiline))
    if ($terminalMatches.Count -ne 1) { throw "capture_materialization_semantic_terminal_required actual=$($terminalMatches.Count)" }
    $terminal = $terminalMatches[0]
    if ($terminal.Groups['sha'].Value -ine $semanticSha) {
        throw "capture_materialization_semantic_sha_mismatch manifest=$semanticSha terminal=$($terminal.Groups['sha'].Value)"
    }
    foreach ($field in @('attempts', 'exit', 'restored')) {
        if ($terminal.Groups[$field].Value -cne $importMatch.Groups[$field].Value) {
            throw "capture_materialization_import_terminal_mismatch field=$field import=$($importMatch.Groups[$field].Value) terminal=$($terminal.Groups[$field].Value)"
        }
    }
    return [pscustomobject]@{
        Root = $captureRoot
        Manifest = $manifest
        ImportLog = $importLog
        ImportConsole = $importConsole
        ImportSummary = $importSummary
        TerminalLog = $terminalPath
        RuntimeSemanticSha = $semanticSha.ToUpperInvariant()
        RuntimeSemanticCount = 6
        RuntimeSemanticPaths = $scenePaths
    }
}

if ($CaptureMaterializationManifestOnly) {
    $materialization = Assert-Task6CaptureMaterializationManifest -Root $CaptureRoot `
        -ManifestPath $CaptureMaterializationManifest
    "TASK6_CAPTURE_MATERIALIZATION_MANIFEST_OK root=$($materialization.Root) manifest=$($materialization.Manifest) " +
        "builders=6 runtime_semantics_sha256=$($materialization.RuntimeSemanticSha) runtime_semantics_count=6"
    return
}

function Test-ExactBytes {
    param([byte[]]$Expected, [byte[]]$Actual)
    if ($Expected.Length -gt 8388608 -or $Actual.Length -gt 8388608) {
        throw 'Test-ExactBytes is restricted to small source/scene snapshots; use streaming for large evidence.'
    }
    if ($Expected.Length -ne $Actual.Length) { return $false }
    for ($index = 0; $index -lt $Expected.Length; $index++) {
        if ($Expected[$index] -ne $Actual[$index]) { return $false }
    }
    return $true
}

function Get-Task6AviMainHeader {
    param([string]$Path)
    # Task6AviStreaming owns all large-container parsing; keep this compatibility wrapper so
    # callers cannot regress to a whole-file byte read.
    return (Get-Task6AviMjpegIndex -Path $Path).Header
}

function Get-Task6Fingerprint {
    param([string[]]$RelativePaths)
    $orderedRelativePaths = @($RelativePaths)
    [Array]::Sort($orderedRelativePaths, [StringComparer]::Ordinal)
    $lines = foreach ($relative in $orderedRelativePaths) {
        $path = Join-Path $project $relative
        if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
            throw "candidate fingerprint source is missing: $relative"
        }
        "$relative=$(Get-EvidenceSha256 -LiteralPath $path)"
    }
    $bytes = [Text.Encoding]::UTF8.GetBytes((($lines -join "`n") + "`n"))
    $algorithm = [Security.Cryptography.SHA256]::Create()
    try { return ([BitConverter]::ToString($algorithm.ComputeHash($bytes)) -replace '-', '') }
    finally { $algorithm.Dispose() }
}

function Get-CaptureValue {
    param([hashtable]$Values, [string]$Name)
    if (-not $Values.ContainsKey($Name) -or [string]::IsNullOrWhiteSpace($Values[$Name])) {
        throw "capture manifest misses '$Name'"
    }
    return [string]$Values[$Name]
}

function Get-Task6CaptureManifestValues {
    param([string]$Root)
    $manifest = Join-Path $Root 'capture-manifest.txt'
    Assert-NonEmptyFiles -Paths @($manifest) -Label 'capture manifest'
    $values = @{}
    foreach ($line in Get-Content -LiteralPath $manifest) {
        $parts = $line -split '=', 2
        if ($parts.Count -eq 2 -and -not [string]::IsNullOrWhiteSpace($parts[0])) {
            $values[$parts[0]] = $parts[1]
        }
    }
    return [pscustomobject]@{ Path = $manifest; Values = $values }
}

function Get-Task6CaptureManifestSingleValue {
    param(
        [Parameter(Mandatory)]$Manifest,
        [Parameter(Mandatory)][string]$Name,
        [Parameter(Mandatory)][string]$Label
    )
    $prefix = $Name + '='
    $matches = @((Get-Content -LiteralPath $Manifest.Path) | Where-Object {
            $_.StartsWith($prefix, [StringComparison]::Ordinal)
        })
    if ($matches.Count -ne 1) { throw "$Label actual=$($matches.Count) key=$Name" }
    $value = $matches[0].Substring($prefix.Length)
    if ([string]::IsNullOrWhiteSpace($value)) { throw "$Label empty_value=True key=$Name" }
    return $value
}

function Assert-Task6CaptureMaterializationCaptureBinding {
    param(
        [Parameter(Mandatory)][string]$Root,
        [Parameter(Mandatory)]$Manifest
    )
    $captureRoot = Get-Task6CaptureMaterializationRoot -Root $Root
    $outerManifestPath = Get-Task6CaptureManifestSingleValue -Manifest $Manifest `
        -Name 'task6_capture_materialization_manifest' -Label 'capture_materialization_outer_manifest_required'
    $outerManifestHash = Get-Task6CaptureManifestSingleValue -Manifest $Manifest `
        -Name 'task6_capture_materialization_manifest_sha256' -Label 'capture_materialization_outer_manifest_hash_required'
    if ($outerManifestHash -notmatch '^[A-Fa-f0-9]{64}$') {
        throw 'capture_materialization_outer_manifest_hash_required malformed=True'
    }
    $outerManifest = Assert-Task6CaptureMaterializationContainedFile -Root $captureRoot -Path $outerManifestPath `
        -Label 'capture_materialization_manifest_outside_capture_root'
    $actualOuterManifestHash = Get-EvidenceSha256 -LiteralPath $outerManifest
    if ($actualOuterManifestHash -ine $outerManifestHash) {
        throw "capture_materialization_outer_manifest_hash_mismatch expected=$outerManifestHash actual=$actualOuterManifestHash"
    }
    $materialization = Assert-Task6CaptureMaterializationManifest -Root $captureRoot -ManifestPath $outerManifest

    $outerLogPath = Get-Task6CaptureManifestSingleValue -Manifest $Manifest `
        -Name 'task6_capture_materialization_log' -Label 'capture_materialization_outer_terminal_required'
    $outerLogHash = Get-Task6CaptureManifestSingleValue -Manifest $Manifest `
        -Name 'task6_capture_materialization_log_sha256' -Label 'capture_materialization_outer_terminal_hash_required'
    if ($outerLogHash -notmatch '^[A-Fa-f0-9]{64}$') {
        throw 'capture_materialization_outer_terminal_hash_required malformed=True'
    }
    $outerLog = Assert-Task6CaptureMaterializationContainedFile -Root $captureRoot -Path $outerLogPath `
        -Label 'capture_materialization_terminal_log_outside_capture_root'
    $actualOuterLogHash = Get-EvidenceSha256 -LiteralPath $outerLog
    if ($actualOuterLogHash -ine $outerLogHash) {
        throw "capture_materialization_outer_terminal_hash_mismatch expected=$outerLogHash actual=$actualOuterLogHash"
    }
    if ((Resolve-Path -LiteralPath $outerLog).Path -cne (Resolve-Path -LiteralPath $materialization.TerminalLog).Path) {
        throw "capture_materialization_terminal_log_mismatch outer=$outerLog manifest=$($materialization.TerminalLog)"
    }

    $outerSemanticSha = Get-Task6CaptureManifestSingleValue -Manifest $Manifest `
        -Name 'task6_generated_runtime_semantics_sha256' -Label 'capture_materialization_outer_semantic_sha_required'
    if ($outerSemanticSha -notmatch '^[A-Fa-f0-9]{64}$') {
        throw 'capture_materialization_outer_semantic_sha_required malformed=True'
    }
    if ($outerSemanticSha -ine $materialization.RuntimeSemanticSha) {
        throw "capture_materialization_semantic_sha_mismatch outer=$outerSemanticSha manifest=$($materialization.RuntimeSemanticSha)"
    }
    $outerSemanticCount = Get-Task6CaptureManifestSingleValue -Manifest $Manifest `
        -Name 'task6_generated_runtime_semantics_count' -Label 'capture_materialization_outer_semantic_count_required'
    if ($outerSemanticCount -cne [string]$materialization.RuntimeSemanticCount) {
        throw "capture_materialization_semantic_count_mismatch outer=$outerSemanticCount manifest=$($materialization.RuntimeSemanticCount)"
    }
    return $materialization
}

function Assert-Task6ManifestFileHash {
    param([hashtable]$Values, [string]$Name)
    $path = Get-CaptureValue $Values $Name
    Assert-NonEmptyFiles -Paths @($path) -Label "capture-bound $Name"
    $expected = Get-CaptureValue $Values ($Name + '_sha256')
    $actual = Get-EvidenceSha256 -LiteralPath $path
    if ($actual -cne $expected) {
        throw "capture manifest hash mismatch for ${Name}: expected=$expected actual=$actual"
    }
    return $path
}

function Get-Task6ManifestFrameCount {
    param(
        [Parameter(Mandatory)][hashtable]$Values,
        [Parameter(Mandatory)][string]$Name,
        [Parameter(Mandatory)][string]$Label,
        [Parameter(Mandatory)][uint32]$ExpectedFrames
    )

    $raw = Get-CaptureValue -Values $Values -Name $Name
    if ($raw -notmatch '^\d+$') {
        throw "formal_frame_manifest_invalid label=$Label expected=$ExpectedFrames actual=$raw field=$Name"
    }
    try { return [uint32]$raw }
    catch { throw "formal_frame_manifest_invalid label=$Label expected=$ExpectedFrames actual=$raw field=$Name" }
}

function Assert-Task6FormalManifestFrameBinding {
    param(
        [Parameter(Mandatory)][hashtable]$Values,
        [Parameter(Mandatory)][string]$Label,
        [Parameter(Mandatory)][uint32]$ExpectedFrames,
        [Parameter(Mandatory)][uint32]$ActualFrames,
        [Parameter(Mandatory)][string]$ExpectedField,
        [Parameter(Mandatory)][string]$ObservedField
    )

    $manifestExpected = Get-Task6ManifestFrameCount -Values $Values -Name $ExpectedField `
        -Label $Label -ExpectedFrames $ExpectedFrames
    if ($manifestExpected -ne $ExpectedFrames) {
        throw "formal_frame_contract_mismatch label=$Label expected=$ExpectedFrames actual=$manifestExpected field=$ExpectedField"
    }
    $manifestObserved = Get-Task6ManifestFrameCount -Values $Values -Name $ObservedField `
        -Label $Label -ExpectedFrames $ExpectedFrames
    if ($manifestObserved -ne $ActualFrames) {
        throw "formal_frame_manifest_mismatch label=$Label expected=$ActualFrames actual=$manifestObserved field=$ObservedField"
    }
    return [pscustomobject]@{ Expected = $manifestExpected; Observed = $manifestObserved }
}

function Assert-Task6CanonicalPrefixManifestBinding {
    param(
        [Parameter(Mandatory)][hashtable]$Values,
        [Parameter(Mandatory)][string]$CaptureRoot,
        [Parameter(Mandatory)][string]$Prefix,
        [Parameter(Mandatory)][string]$Label,
        [Parameter(Mandatory)][string]$RawPath,
        [Parameter(Mandatory)][string]$CanonicalPath,
        [Parameter(Mandatory)][uint32]$ExpectedFrames,
        [int]$MinimumRawTailFrames = 30
    )

    $resolvedRoot = Get-Task6CaptureMaterializationRoot -Root $CaptureRoot
    $raw = Assert-Task6CaptureMaterializationContainedFile -Root $resolvedRoot -Path $RawPath `
        -Label "$Label raw AVI outside capture root"
    $canonical = Assert-Task6CaptureMaterializationContainedFile -Root $resolvedRoot -Path $CanonicalPath `
        -Label "$Label canonical AVI outside capture root"
    $log = Assert-Task6ManifestFileHash -Values $Values -Name ($Prefix + '_canonical_prefix_log')
    $log = Assert-Task6CaptureMaterializationContainedFile -Root $resolvedRoot -Path $log `
        -Label "$Label canonical-prefix log outside capture root"
    $contract = Assert-Task6CanonicalAviPrefix -RawPath $raw -CanonicalPath $canonical `
        -ExpectedFrames $ExpectedFrames -MinimumRawTailFrames $MinimumRawTailFrames -Label $Label

    $numericFields = @{
        ($Prefix + '_raw_avi_frames') = [uint32]$contract.RawFrames
        ($Prefix + '_trimmed_frames') = [uint32]$contract.TrimmedFrames
    }
    foreach ($field in $numericFields.Keys) {
        $recorded = Get-Task6ManifestFrameCount -Values $Values -Name $field -Label $Label `
            -ExpectedFrames $ExpectedFrames
        if ($recorded -ne $numericFields[$field]) {
            throw "canonical_prefix_manifest_mismatch label=$Label field=$field expected=$($numericFields[$field]) actual=$recorded"
        }
    }
    $hashFields = @{
        ($Prefix + '_prefix_payload_sha256') = [string]$contract.PrefixPayloadSha256
        ($Prefix + '_first_frame_sha256') = [string]$contract.FirstFrameSha256
        ($Prefix + '_last_frame_sha256') = [string]$contract.LastFrameSha256
    }
    foreach ($field in $hashFields.Keys) {
        $recorded = Get-CaptureValue -Values $Values -Name $field
        if ($recorded -notmatch '^[A-Fa-f0-9]{64}$' -or $recorded -ine $hashFields[$field]) {
            throw "canonical_prefix_hash_mismatch label=$Label field=$field expected=$($hashFields[$field]) actual=$recorded"
        }
    }
    $marker = "TASK6_CANONICAL_AVI_PREFIX_OK label=$Label raw_frames=$($contract.RawFrames) " +
        "canonical_frames=$($contract.CanonicalFrames) trimmed_frames=$($contract.TrimmedFrames) " +
        "prefix_payload_sha256=$($contract.PrefixPayloadSha256) first_frame_sha256=$($contract.FirstFrameSha256) " +
        "last_frame_sha256=$($contract.LastFrameSha256)"
    Assert-OneExactMarker -Path $log -Marker $marker -Label "$Label canonical-prefix marker"
    return $contract
}

function Assert-Task6NoProhibitedAuthority {
    $task6ProofSources = @(
        'tests\smoke\PrologueCriticalPathSmokeTask6.cs',
        'tests\presentation\ProloguePresentationTask6.cs',
        'tests\presentation\PrologueBaseAttackPresentation.cs',
        'tests\presentation\BossRetryPresentationTask6.cs',
        'tests\presentation\PrologueVisualAuditTask6.cs'
    )
    foreach ($relative in $task6ProofSources) {
        $path = Join-Path $project $relative
        if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
            throw "Task 6 proof source is missing: $relative"
        }
        $text = Get-Content -LiteralPath $path -Raw
        if ($text -match 'InputActions\.Dash') {
            throw "Task 6 proof source must not require Dash: $relative"
        }
        if ($text -match '(?:FallenBellkeeperModelTests|MechanicalClawModelTests)\.Run\s*\(') {
            throw "Task 6 proof source bypasses the InputMap runtime with a direct model suite: $relative"
        }
        if ($text -match '\.Traverse\s*\(') {
            throw "Task 6 proof source bypasses traversal with a direct Traverse call: $relative"
        }
    }

    # The capture contract intentionally uses the normal MouseMotion event route. OS cursor
    # warps are host-window-manager dependent and must never become hidden test authority.
    # The production reader retains a pre-first-motion fallback for real gameplay startup, but
    # once a mouse event arrives both human input and replay consume the same cached observation.
    $eventRouteSources = @(
        'scripts\player\PlayerInputReader.cs',
        'tests\integration\PrologueCanyonTraversalTests.cs',
        'tests\integration\PrologueCanyonGeneratedMainRouteTests.cs',
        'tests\integration\PrologueTask9GeneratedMainTests.cs',
        'tests\presentation\ProloguePresentationTask6.cs'
    )
    foreach ($relative in $eventRouteSources) {
        $path = Join-Path $project $relative
        if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
            throw "Task 6 event-route source is missing: $relative"
        }
        $text = Get-Content -LiteralPath $path -Raw
        if ($text -match '(?:Input\.|\.)WarpMouse\s*\(|WindowMoveToForeground\s*\(|SetCursorPos\s*\(') {
            throw "Task 6 event-route source must not require OS cursor warp: $relative"
        }
    }
    $inputAdapter = Get-Content -LiteralPath (Join-Path $project 'scripts\player\PlayerInputReader.cs') -Raw
    if ($inputAdapter -notmatch 'InputEventMouseMotion' -or
        $inputAdapter -notmatch 'LastObservedMouseViewportPosition' -or
        $inputAdapter -notmatch 'HasObservedMouseMotion') {
        throw 'Task 6 production PlayerInputReader must cache normal MouseMotion events.'
    }
    if ($inputAdapter -match 'IsEditorHint|test-only|Task6') {
        throw 'Task 6 production PlayerInputReader must not branch on test-only state.'
    }

    $critical = Join-Path $project 'tests\smoke\PrologueCriticalPathSmokeTask6.cs'
    $criticalText = Get-Content -LiteralPath $critical -Raw
    if (-not $criticalText.Contains('INPUTMAP_FIXTURE_BOUNDARY') -or
        $criticalText -notmatch 'PrologueCanyonTraversalTests\.Run\(this\)') {
        throw 'Task 6 critical proof must retain its explicit InputMap fixture boundary.'
    }

    $presentation = Join-Path $project 'tests\presentation\ProloguePresentationTask6.cs'
    $presentationText = Get-Content -LiteralPath $presentation -Raw
    if ($presentationText -match 'PrologueCanyonTraversalTests\.Run\s*\(') {
        throw 'Task 6 presentation cannot replay the direct fixture traversal as visible route proof.'
    }
    if (-not $presentationText.Contains('RepositionAfterInputMapProof') -or
        -not $presentationText.Contains('PROLOGUE_PRESENTATION_INPUTMAP_PROOF_OK') -or
        -not $presentationText.Contains('TASK6_NO_DASH_TRAVERSAL_OK mandatory_dash=False route=generated_main')) {
        throw 'Task 6 presentation must declare the runtime InputMap proof boundary before visual repositioning.'
    }
    if ($presentationText -match 'air_dash_consumed|TRAVERSAL_PROOF_STAGE=.*dash') {
        throw 'Task 6 presentation must not retain the superseded mandatory-Dash evidence schema.'
    }
    # Evaluate each physical source line independently.  The former \s* lookahead was able to
    # consume a newline, then backtrack into this deliberately allowed post-proof helper and
    # report it as forbidden.  Any other direct player relocation still fails closed.
    $forbiddenPresentationReposition = @($presentationText -split "`r?`n" | Where-Object {
        $_ -match '^\s*player\.(?:Position|GlobalPosition)\s*=' -and
        $_ -notmatch '^\s*player\.GlobalPosition\s*=\s*position;\s*$'
    })
    if ($forbiddenPresentationReposition.Count -ne 0) {
        throw 'Task 6 presentation has a direct player reposition outside RepositionAfterInputMapProof.'
    }
    $validation = Join-Path $project 'docs\playtests\2026-08-29-terrain-grapple-dense-canyon-validation.md'
    if (-not (Test-Path -LiteralPath $validation -PathType Leaf)) {
        throw 'Task 6 validation document is missing.'
    }
    $validationText = Get-Content -LiteralPath $validation -Raw
    if (-not $validationText.Contains('No automatic HandsOnLauncher report') -or
        $validationText -match 'Automated HandsOnLauncher report') {
        throw 'Task 6 must preserve the explicit human HandsOnLauncher boundary.'
    }
}

function Get-Task6KeyframeManifestEntries {
    param([Parameter(Mandatory)][string]$ManifestPath)
    $directory = Split-Path -Parent $ManifestPath
    $entries = [Collections.Generic.List[object]]::new()
    foreach ($line in Get-Content -LiteralPath $ManifestPath) {
        $match = [regex]::Match($line,
            '^(?<stage>[^|]+)\|avi_frame=(?<frame>\d+)\|png=(?<png>[^\\/|]+\.png)\|sha256=(?<hash>[A-Fa-f0-9]{64})$')
        if (-not $match.Success) { continue }
        $png = Join-Path $directory $match.Groups['png'].Value
        if (-not (Test-Path -LiteralPath $png -PathType Leaf)) {
            throw "keyframe manifest references a missing PNG: $png"
        }
        $actualHash = Get-EvidenceSha256 -LiteralPath $png
        $expectedHash = $match.Groups['hash'].Value
        if ($actualHash -ine $expectedHash) {
            throw "keyframe PNG hash mismatch: png=$png manifest=$expectedHash actual=$actualHash"
        }
        $entries.Add([pscustomobject]@{
            Stage = $match.Groups['stage'].Value; Frame = [long]$match.Groups['frame'].Value;
            Png = $png; Sha256 = $actualHash; Sample = (Get-Task6ImageSample -Path $png)
        })
    }
    if ($entries.Count -eq 0) { throw "keyframe manifest has no valid PNG entries: $ManifestPath" }
    return $entries
}

function Assert-Task6KeyframeManifestBinding {
    param(
        [hashtable]$Values,
        [string]$Name,
        [string]$AviPath,
        [uint32]$ExpectedFrames,
        [string[]]$ExpectedStages = @(),
        [object]$ExpectedStageFrames = $null
    )
    $manifest = Assert-Task6ManifestFileHash -Values $Values -Name $Name
    $lines = @(Get-Content -LiteralPath $manifest)
    $aviHash = Get-EvidenceSha256 -LiteralPath $AviPath
    if (@($lines | Where-Object { $_ -ceq "avi_sha256=$aviHash" }).Count -ne 1) {
        throw "$Name does not bind its AVI SHA256."
    }
    if (@($lines | Where-Object { $_ -ceq "avi_frames=$ExpectedFrames" }).Count -ne 1) {
        throw "$Name does not bind its exact AVI frame count."
    }
    $entries = Get-Task6KeyframeManifestEntries -ManifestPath $manifest
    if ($ExpectedStages.Count -gt 0) {
        if ($null -eq $ExpectedStageFrames) {
            throw "$Name cannot validate named keyframes without the runtime stage-frame map."
        }
        foreach ($stage in $ExpectedStages) {
            if (-not $ExpectedStageFrames.Contains($stage)) {
                throw "$Name runtime stage-frame map misses '$stage'."
            }
            [long]$frame = [long]$ExpectedStageFrames[$stage]
            $matches = @($entries | Where-Object { $_.Stage -ceq $stage -and $_.Frame -eq $frame })
            if ($matches.Count -ne 1) {
                throw "$Name must bind exactly one '$stage' keyframe to AVI frame $frame; actual=$($matches.Count)"
            }
        }
    }
    return $manifest
}

function Assert-Task6GlobalKeyframeMotion {
    param(
        [Parameter(Mandatory)][string]$ManifestPath,
        [Parameter(Mandatory)][string[]]$Stages,
        [Parameter(Mandatory)][string]$Label
    )
    $entries = Get-Task6KeyframeManifestEntries -ManifestPath $ManifestPath
    $ordered = @()
    foreach ($stage in $Stages) {
        $matches = @($entries | Where-Object { $_.Stage -ceq $stage })
        if ($matches.Count -ne 1) { throw "$Label has no unique center keyframe for '$stage'" }
        $ordered += $matches[0]
    }
    $sampleHashes = @($ordered | ForEach-Object { $_.Sample.Sha256 } | Sort-Object -Unique)
    [long]$changedTransitions = 0
    [long]$totalSampleDelta = 0
    for ($index = 0; $index -lt $ordered.Count - 1; $index++) {
        $delta = Get-Task6ImageSampleDelta -Left $ordered[$index].Sample.Bytes -Right $ordered[$index + 1].Sample.Bytes
        if ($delta.ChangedPoints -gt 0 -and $delta.AbsoluteDelta -gt 0) { $changedTransitions++ }
        $totalSampleDelta += $delta.AbsoluteDelta
    }
    # Keep verifier semantics byte-for-byte aligned with extraction: the two-stage Boss
    # retry needs one real transition, while the thirty-stage full-flow still needs four.
    $minimumDistinct = [Math]::Min(8, $Stages.Count)
    $minimumTransitions = [Math]::Min(4, [Math]::Max(0, $Stages.Count - 1))
    if ($sampleHashes.Count -lt $minimumDistinct -or $changedTransitions -lt $minimumTransitions -or
        $totalSampleDelta -le 0) {
        throw "$Label is globally frozen at decoded samples: distinct=$($sampleHashes.Count) changed=$changedTransitions delta=$totalSampleDelta"
    }
    $expected = "TASK6_GLOBAL_MOTION|stages=$($Stages.Count)|distinct_sample_hashes=$($sampleHashes.Count)|changed_transitions=$changedTransitions|total_sample_delta=$totalSampleDelta|sample_points=$($ordered[0].Sample.Points)"
    $count = @((Get-Content -LiteralPath $ManifestPath) | Where-Object { $_ -ceq $expected }).Count
    if ($count -ne 1) { throw "$Label global-motion manifest binding must occur exactly once; actual=$count" }
}

function Assert-Task6BoostNeighborMotion {
    param(
        [Parameter(Mandatory)][string]$ManifestPath,
        [Parameter(Mandatory)][object]$StageFrames,
        [Parameter(Mandatory)][string]$Label
    )
    $entries = Get-Task6KeyframeManifestEntries -ManifestPath $ManifestPath
    $lines = @(Get-Content -LiteralPath $ManifestPath)
    foreach ($stage in @('boost_start', 'boost_arc', 'boost_second_rejected', 'boost_land')) {
        [long]$centerFrame = [long]$StageFrames[$stage]
        $center = @($entries | Where-Object { $_.Stage -ceq $stage -and $_.Frame -eq $centerFrame })
        $previous = @($entries | Where-Object { $_.Stage -ceq ($stage + '__previous') -and $_.Frame -eq ($centerFrame - 1L) })
        $next = @($entries | Where-Object { $_.Stage -ceq ($stage + '__next') -and $_.Frame -eq ($centerFrame + 1L) })
        if ($center.Count -ne 1 -or $previous.Count -ne 1 -or $next.Count -ne 1) {
            throw "$Label lacks an exact real-motion keyframe trio for '$stage': center=$($center.Count) previous=$($previous.Count) next=$($next.Count)"
        }
        $previousDelta = Get-Task6ImageSampleDelta -Left $previous[0].Sample.Bytes -Right $center[0].Sample.Bytes
        $nextDelta = Get-Task6ImageSampleDelta -Left $center[0].Sample.Bytes -Right $next[0].Sample.Bytes
        if ($previousDelta.ChangedPoints -le 0 -or $previousDelta.AbsoluteDelta -le 0 -or
            $nextDelta.ChangedPoints -le 0 -or $nextDelta.AbsoluteDelta -le 0) {
            throw "$Label '$stage' has no decoded neighbouring-frame movement."
        }
        $expected = "${stage}__motion|center_frame=$centerFrame|previous_frame=$($centerFrame - 1L)|next_frame=$($centerFrame + 1L)|previous_png_sha256=$($previous[0].Sha256)|center_png_sha256=$($center[0].Sha256)|next_png_sha256=$($next[0].Sha256)|previous_sample_delta=$($previousDelta.AbsoluteDelta)|previous_changed_points=$($previousDelta.ChangedPoints)|next_sample_delta=$($nextDelta.AbsoluteDelta)|next_changed_points=$($nextDelta.ChangedPoints)|sample_points=$($center[0].Sample.Points)"
        $count = @($lines | Where-Object { $_ -ceq $expected }).Count
        if ($count -ne 1) { throw "$Label '$stage' motion manifest binding must occur exactly once; actual=$count" }
    }
}

function Assert-Task6DistinctKeyframeStages {
    param([string]$ManifestPath, [string[]]$Stages, [string]$Label)
    $lines = @(Get-Content -LiteralPath $ManifestPath)
    $hashes = @()
    foreach ($stage in $Stages) {
        $pattern = '^' + [regex]::Escape($stage) +
            '\|avi_frame=\d+\|png=.+\.png\|sha256=(?<hash>[A-Fa-f0-9]{64})$'
        $matches = @([regex]::Matches(($lines -join "`n"), $pattern,
            [Text.RegularExpressions.RegexOptions]::Multiline))
        if ($matches.Count -ne 1) {
            throw "$Label requires exactly one '$stage' keyframe; actual=$($matches.Count)"
        }
        $hashes += $matches[0].Groups['hash'].Value
    }
    if (@($hashes | Sort-Object -Unique).Count -ne $Stages.Count) {
        throw "$Label keyframes are not independently visible; stages=$($Stages -join ',')"
    }
}

function Get-Task6PresentationStageNames {
    return @(
        'title_start', 'mode_hud', 'attack_energy', 'recovery', 'canyon_descent', 'grapple_reticles',
        'canyon_basin', 'checkpoint_basin', 'shield_overheat', 'ascent_one', 'ascent_two', 'ascent_three',
        'optional_alcove', 'lost_temple', 'checkpoint_temple', 'boss_intro', 'death_retry', 'completion',
        'boost_attached', 'boost_start', 'attached_boost_consumed', 'boost_arc', 'boost_second_rejected',
        'boost_land', 'boss_sweep', 'boss_leap_slam', 'boss_ground_wave', 'boss_hammer_throw',
        'boss_attack_contract', 'completion_title_return'
    )
}

function Get-Task6ReticleStageNames {
    return @('Attack', 'Grippable', 'Ice', 'Forbidden', 'Obstructed', 'OutOfRange')
}

function Assert-Task6OrderedPresentationStages {
    param([string]$Path)
    # These are engine-log stages, not a source declaration.  Require each once and in the
    # order captured from the live InputMap route plus post-proof visual dwells.
    $expected = Get-Task6PresentationStageNames
    $lines = @(Get-Content -LiteralPath $Path)
    $stageLines = @($lines | Where-Object { $_ -match '^PROLOGUE_PRESENTATION_STAGE=' })
    if ($stageLines.Count -ne $expected.Count) {
        throw "full-flow stage count must be $($expected.Count); actual=$($stageLines.Count)"
    }
    $previous = -1
    foreach ($stage in $expected) {
        $stageIndices = @()
        for ($index = 0; $index -lt $lines.Count; $index++) {
            if ($lines[$index] -match ('^PROLOGUE_PRESENTATION_STAGE=' + [regex]::Escape($stage) + ' frame=\d+$')) {
                $stageIndices += $index
            }
        }
        if ($stageIndices.Count -ne 1 -or $stageIndices[0] -le $previous) {
            throw "full-flow ordered stage contract fails at $stage; matches=$($stageIndices.Count) previous=$previous"
        }
        $previous = $stageIndices[0]
    }
    if (@($lines | Where-Object { $_ -match 'air_dash_consumed|TRAVERSAL_PROOF_STAGE=.*dash' }).Count -ne 0) {
        throw 'full-flow capture retains a superseded mandatory-Dash traversal marker.'
    }
}

function Get-Task6CompletionRenderedFrame {
    param([Parameter(Mandatory)][string]$Path)
    # The completion signal is emitted by gameplay before Godot necessarily presents the
    # updated HUD.  This runtime-only marker is emitted after FramePostDraw and names the next
    # one-indexed Movie Maker AVI sample, which is independently decoded and pixel-checked
    # below.  The conversion is explicit so an engine-frame label cannot self-certify a stale
    # Boss-HUD image.
    $pattern = '^TASK6_COMPLETION_DISPLAY_OK frame=(?<frame>\d+) completion_visible=True boss_visible=False rendered_post_draw=True movie_frame_offset=1$'
    $matches = @([regex]::Matches((Get-Content -LiteralPath $Path -Raw), $pattern,
        [Text.RegularExpressions.RegexOptions]::Multiline))
    if ($matches.Count -ne 1) {
        throw "full-flow completion display marker must occur exactly once; actual=$($matches.Count)"
    }
    return [long]$matches[0].Groups['frame'].Value
}

function Get-Task6ModeHudRenderedFrame {
    param([Parameter(Mandatory)][string]$Path)
    $pattern = '^TASK6_MODE_HUD_DISPLAY_OK frame=(?<frame>\d+) formal_hud_visible=True rendered_post_draw=True movie_frame_offset=1$'
    $text = Get-Content -LiteralPath $Path -Raw
    $matches = @([regex]::Matches($text, $pattern, [Text.RegularExpressions.RegexOptions]::Multiline))
    if ($matches.Count -ne 1) {
        throw "full-flow mode HUD display marker must occur exactly once; actual=$($matches.Count)"
    }
    return [long]$matches[0].Groups['frame'].Value
}

function Get-Task6BoostRenderedFrames {
    param([Parameter(Mandatory)][string]$Path, [Parameter(Mandatory)][object]$StageFrames)
    $expected = @(
        [pscustomobject]@{ Stage = 'boost_attached'; State = 'Swing'; Consumed = 'False'; Rejected = 'False'; Offset = '1' },
        [pscustomobject]@{ Stage = 'boost_start'; State = 'Boost'; Consumed = 'True'; Rejected = 'False'; Offset = '1' },
        [pscustomobject]@{ Stage = 'attached_boost_consumed'; State = 'Reel'; Consumed = 'True'; Rejected = 'False'; Offset = '1' },
        [pscustomobject]@{ Stage = 'boost_arc'; State = 'Reel'; Consumed = 'True'; Rejected = 'False'; Offset = '1' },
        [pscustomobject]@{ Stage = 'boost_second_rejected'; State = 'Reel'; Consumed = 'True'; Rejected = 'True'; Offset = '2' },
        [pscustomobject]@{ Stage = 'boost_land'; State = 'Idle'; Consumed = 'False'; Rejected = 'False'; Offset = '1' }
    )
    $text = Get-Content -LiteralPath $Path -Raw
    $frames = [ordered]@{}
    [long]$cast = 0L
    foreach ($item in $expected) {
        $pattern = '^TASK6_BOOST_DISPLAY_OK stage=' + [regex]::Escape($item.Stage) +
            ' frame=(?<frame>\d+) state=' + [regex]::Escape($item.State) +
            ' cast=(?<cast>\d+) boost_consumed=' + $item.Consumed +
            ' second_rejected=' + $item.Rejected +
            ' rendered_post_draw=True movie_frame_offset=' + $item.Offset + '$'
        Assert-OneRegexMarker -Path $Path -Pattern $pattern -Label "full-flow/$($item.Stage)-display"
        $match = [regex]::Match($text, $pattern, [Text.RegularExpressions.RegexOptions]::Multiline)
        [long]$frame = [long]$match.Groups['frame'].Value
        if ($frame -ne [long]$StageFrames[$item.Stage]) {
            throw "full-flow $($item.Stage) display marker does not bind its stage: display=$frame stage=$($StageFrames[$item.Stage])"
        }
        [long]$markerCast = [long]$match.Groups['cast'].Value
        if ($markerCast -le 0L) { throw "full-flow $($item.Stage) visual binding has no cast id" }
        if ($cast -eq 0L) { $cast = $markerCast }
        elseif ($cast -ne $markerCast) { throw "full-flow boost display cast drift: expected=$cast actual=$markerCast stage=$($item.Stage)" }
        $frames[$item.Stage] = $frame
    }
    return [pscustomobject]@{ Frames = $frames; Cast = $cast }
}

function Get-Task6BossAttackRenderedFrames {
    param([Parameter(Mandatory)][string]$Path, [Parameter(Mandatory)][object]$StageFrames)
    $expected = @(
        [pscustomobject]@{ Attack = 'Sweep'; Stage = 'boss_sweep'; Pose = 'boss_sweep_windup' },
        [pscustomobject]@{ Attack = 'LeapSlam'; Stage = 'boss_leap_slam'; Pose = 'boss_leap_crouch' },
        [pscustomobject]@{ Attack = 'GroundWave'; Stage = 'boss_ground_wave'; Pose = 'boss_groundwave_raise' },
        [pscustomobject]@{ Attack = 'HammerThrow'; Stage = 'boss_hammer_throw'; Pose = 'boss_hammer_windup' }
    )
    $text = Get-Content -LiteralPath $Path -Raw
    $frames = [ordered]@{}
    foreach ($item in $expected) {
        $pattern = '^TASK6_BOSS_ATTACK_DISPLAY_OK attack=' + [regex]::Escape($item.Attack) +
            ' stage=' + [regex]::Escape($item.Stage) + ' frame=(?<frame>\d+) formal_pose=' +
            [regex]::Escape($item.Pose) + ' telegraph=True rendered_post_draw=True movie_frame_offset=1$'
        Assert-OneRegexMarker -Path $Path -Pattern $pattern -Label "full-flow/$($item.Stage)-display"
        $match = [regex]::Match($text, $pattern, [Text.RegularExpressions.RegexOptions]::Multiline)
        [long]$frame = [long]$match.Groups['frame'].Value
        if ($frame -ne [long]$StageFrames[$item.Stage]) {
            throw "full-flow $($item.Stage) display marker does not bind its stage: display=$frame stage=$($StageFrames[$item.Stage])"
        }
        $frames[$item.Stage] = $frame
    }
    return $frames
}

function Get-Task6ExactKeyframeEntry {
    param(
        [Parameter(Mandatory)][object[]]$Entries,
        [Parameter(Mandatory)][string]$Stage,
        [Parameter(Mandatory)][long]$Frame,
        [Parameter(Mandatory)][string]$Label
    )
    $matches = @($Entries | Where-Object { $_.Stage -ceq $Stage -and $_.Frame -eq $Frame })
    if ($matches.Count -ne 1) {
        throw "$Label must bind exactly one decoded keyframe to AVI frame $Frame; actual=$($matches.Count)"
    }
    return $matches[0]
}

function Assert-Task6CaptureEvidence {
    param([string]$Root)
    $manifest = Get-Task6CaptureManifestValues -Root $Root
    $values = $manifest.Values
    # Reject structurally coherent but wrong-length movies before trusting any higher-level
    # marker, path, or candidate metadata. This preserves a precise fail-closed diagnosis for
    # the +1/-1 mutations while every valid bundle proceeds into the stronger provenance gates.
    $formalFrameContract = Get-Task6FormalCaptureFrameContract
    $formalContractLine = "formal_capture_frame_contract=full_flow=$($formalFrameContract.FullFlow) boss_retry=$($formalFrameContract.BossRetry)"
    if (@((Get-Content -LiteralPath $manifest.Path) | Where-Object { $_ -ceq $formalContractLine }).Count -ne 1) {
        throw "formal_frame_contract_manifest_required label=capture expected=$formalContractLine actual=missing_or_duplicate"
    }
    $fullAvi = Assert-Task6ManifestFileHash -Values $values -Name 'full_flow_avi'
    $bossAvi = Assert-Task6ManifestFileHash -Values $values -Name 'boss_retry_avi'
    $fullIndex = Assert-Task6FormalAviMovieContract -Path $fullAvi -ExpectedFrames $formalFrameContract.FullFlow -Label 'full-flow'
    $bossIndex = Assert-Task6FormalAviMovieContract -Path $bossAvi -ExpectedFrames $formalFrameContract.BossRetry -Label 'boss-retry'
    $requiredLogs = @(
        'task11_materialization_log', 'task11_materialization_console', 'task11_critical_log', 'task11_critical_console',
        'task11_ui_log', 'task11_ui_console', 'audit_log', 'audit_console',
        'full_flow_log', 'full_flow_console', 'boss_retry_log', 'boss_retry_console'
    )
    $bound = @{}
    foreach ($name in $requiredLogs) { $bound[$name] = Assert-Task6ManifestFileHash -Values $values -Name $name }
    Assert-NoEngineLogErrors -Paths @($bound.Values)
    $materialization = Assert-Task6CaptureMaterializationCaptureBinding -Root $Root -Manifest $manifest

    $manifestHead = Get-CaptureValue $values 'base_head'
    $headLines = @(& git -C $project rev-parse HEAD)
    if ($LASTEXITCODE -ne 0 -or $headLines.Count -ne 1 -or $headLines[0] -notmatch '^[A-Fa-f0-9]{40}$') {
        throw 'capture candidate HEAD cannot be resolved by the verifier'
    }
    $actualHead = $headLines[0].Trim()
    if ($manifestHead -ine $actualHead) {
        throw "capture base HEAD mismatch: capture=$manifestHead verifier=$actualHead"
    }

    $runtimeSources = Get-Task6RuntimeSourceList
    $scenes = Get-Task6SceneSourceList
    $candidate = Get-CaptureValue $values 'candidate_runtime_sha256'
    $actualCandidate = Get-Task6Fingerprint ($runtimeSources + $scenes)
    if ($candidate -cne $actualCandidate) {
        throw "capture runtime candidate mismatch: capture=$candidate verifier=$actualCandidate"
    }
    Assert-Task6NoProhibitedAuthority

    Assert-OneExactMarker -Path $bound['task11_materialization_log'] `
        -Marker 'TASK11_MATERIALIZATION_PRECHECK_OK main=PrologueLevel sectors=5 generated_scenes=25 project_snapshot=1' `
        -Label 'Task11 materialization marker'
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
        'PROLOGUE_CRITICAL_PATH_OK'
    )) { Assert-OneExactMarker -Path $bound['task11_critical_log'] -Marker $marker -Label "Task11 critical $marker" }
    Assert-OneExactMarker -Path $bound['task11_ui_log'] `
        -Marker 'TASK11_UI_READABILITY_OK localized=True wrapped=True target_protected=True' -Label 'Task11 UI marker'
    Assert-OneExactMarker -Path $bound['audit_log'] `
        -Marker 'PROLOGUE_AUDIT_TOPOLOGY_OK sectors=5 micro_scenes=15 claw_roles=4' -Label 'audit topology'
    Assert-OneExactMarker -Path $bound['audit_log'] -Marker 'PROLOGUE_PRODUCTION_AUDIT_OK' -Label 'audit terminal'
    Assert-OneRegexMarker -Path $bound['audit_log'] `
        -Pattern '^PROLOGUE_PERFORMANCE peak_ms=(?:[0-9](?:\.\d+)?|[1-4]\d(?:\.\d+)?) ceiling_ms=50 basis=callback_wall_clock headroom_ms=\d+(?:\.\d+)?\s*$' `
        -Label 'audit callback budget'
    foreach ($marker in @(
        'PROLOGUE_PRESENTATION_INPUTMAP_PROOF_OK route=generated_main inputmap=True post_proof_reposition=True',
        'TASK6_NO_DASH_TRAVERSAL_OK mandatory_dash=False route=generated_main',
        'PROLOGUE_PRESENTATION_STAGES_OK count=30 reticles=6 boost=True checkpoints=2 boss_attacks=4',
        'TASK6_INPUT_VIEWPORT_BASELINE_OK sampled=(0, 0) events=viewport+inputmap os_warp=False',
        'TASK6_INPUT_VIEWPORT_SAMPLE_OK sampled=(1120, 620) intended=(1120, 620) events=viewport+inputmap os_warp=False',
        'TASK6_INPUT_RETICLE_BINDING_OK sampled=(1120, 620) reticle=(1120, 620) adapter=True reticle_visible=True events=viewport+inputmap os_warp=False',
        'PROLOGUE_RETICLE_DWELL_OK count=6 states=Attack,Grippable,Ice,Forbidden,Obstructed,OutOfRange inputmap=True',
        'TASK6_CLAW_STATE_OK state=Latched inputmap=True',
        'TASK6_CLAW_STATE_OK state=Boost inputmap=True',
        'TASK6_CLAW_STATE_OK state=Idle inputmap=True',
        'TASK6_CLAW_FAILURE_DWELL_OK failure=IceSlip visible=True inputmap=True',
        'TASK6_CLAW_FAILURE_DWELL_OK failure=Forbidden visible=True inputmap=True',
        'TASK6_CLAW_FAILURE_DWELL_OK failure=Obstructed visible=True inputmap=True',
        'TASK6_CLAW_RECOVERY_OK state=Idle post_boost=True',
        'PROLOGUE_PRESENTATION_BOSS_ATTACKS_OK live=True attacks=Sweep,LeapSlam,GroundWave,HammerThrow inputmap=True',
        'PROLOGUE_PRESENTATION_RECOVERY_OK checkpoints=2 pre_checkpoint_start=True boss_death=True retry=True completion=True fall_context_version=3',
        'TASK6_CLAW_PRESENTATION_OK states=Latched,Boost,Idle failures=IceSlip,Forbidden,Obstructed dwells=6 recovery=True',
        'TASK6_CANONICAL_TIMELINE_BOUNDARY_OK canonical_frames=4053 process_frame=4053 title_visible=True audio_released=True',
        'TASK6_RAW_PRODUCER_FRAME_OK canonical_frames=4053 raw_terminal_process_frame=4113 minimum_trim_tail_frames=30 title_visible=True audio_released=True'
    )) { Assert-OneExactMarker -Path $bound['full_flow_log'] -Marker $marker -Label "full-flow $marker" }
    $presentationStages = Get-Task6PresentationStageNames
    $reticleStageNames = Get-Task6ReticleStageNames
    $completionDisplayFrame = Get-Task6CompletionRenderedFrame -Path $bound['full_flow_log']
    Assert-Task6OrderedPresentationStages -Path $bound['full_flow_log']
    Assert-OneExactMarker -Path $bound['boss_retry_log'] -Marker 'PROLOGUE_BOSS_RETRY_COMPLETE' -Label 'Boss retry terminal'
    Assert-OneExactMarker -Path $bound['boss_retry_log'] `
        -Marker 'TASK6_BOSS_RETRY_RAW_PRODUCER_OK canonical_frames=2719 raw_tail_process_frames=90' `
        -Label 'Boss retry raw producer'

    $fullRawAvi = Assert-Task6ManifestFileHash -Values $values -Name 'full_flow_raw_avi'
    $bossRawAvi = Assert-Task6ManifestFileHash -Values $values -Name 'boss_retry_raw_avi'
    $fullRawProducer = Assert-Task6RawProducerFrameContract -LogPath $bound['full_flow_log'] -RawPath $fullRawAvi `
        -ExpectedCanonicalFrames $formalFrameContract.FullFlow -MinimumRawTailFrames 30 -Label 'full-flow'
    foreach ($binding in @(
        @{ Field = 'full_flow_raw_terminal_process_frame'; Actual = $fullRawProducer.TerminalProcessFrame },
        @{ Field = 'full_flow_writer_shutdown_frames'; Actual = $fullRawProducer.WriterShutdownFrames }
    )) {
        $recorded = Get-Task6ManifestFrameCount -Values $values -Name $binding.Field -Label 'full-flow raw producer' `
            -ExpectedFrames $formalFrameContract.FullFlow
        if ($recorded -ne [uint32]$binding.Actual) {
            throw "raw_producer_manifest_mismatch field=$($binding.Field) expected=$($binding.Actual) actual=$recorded"
        }
    }
    $fullPrefix = Assert-Task6CanonicalPrefixManifestBinding -Values $values -CaptureRoot $Root `
        -Prefix 'full_flow' -Label 'full-flow' -RawPath $fullRawAvi -CanonicalPath $fullAvi `
        -ExpectedFrames $formalFrameContract.FullFlow -MinimumRawTailFrames 30
    $bossPrefix = Assert-Task6CanonicalPrefixManifestBinding -Values $values -CaptureRoot $Root `
        -Prefix 'boss_retry' -Label 'boss-retry' -RawPath $bossRawAvi -CanonicalPath $bossAvi `
        -ExpectedFrames $formalFrameContract.BossRetry -MinimumRawTailFrames 30
    Assert-Task6FormalManifestFrameBinding -Values $values -Label 'full-flow' `
        -ExpectedFrames $formalFrameContract.FullFlow -ActualFrames $fullIndex.Header.TotalFrames `
        -ExpectedField 'full_flow_expected_frames' -ObservedField 'full_flow_avi_frames' | Out-Null
    Assert-Task6FormalManifestFrameBinding -Values $values -Label 'boss-retry' `
        -ExpectedFrames $formalFrameContract.BossRetry -ActualFrames $bossIndex.Header.TotalFrames `
        -ExpectedField 'boss_retry_expected_frames' -ObservedField 'boss_retry_avi_frames' | Out-Null
    $timeline = Assert-Task6FullFlowTimelineContract -LogPath $bound['full_flow_log'] `
        -TotalFrames $fullIndex.Header.TotalFrames -ExpectedFrames $formalFrameContract.FullFlow -ExpectedStages $presentationStages `
        -ExpectedReticles $reticleStageNames -Label 'full-flow'
    $modeHudDisplayFrame = Get-Task6ModeHudRenderedFrame -Path $bound['full_flow_log']
    if ($modeHudDisplayFrame -ne [long]$timeline.StageFrames['mode_hud']) {
        throw "full-flow mode HUD display frame does not bind its stage: display=$modeHudDisplayFrame stage=$($timeline.StageFrames['mode_hud'])"
    }
    $boostDisplay = Get-Task6BoostRenderedFrames -Path $bound['full_flow_log'] -StageFrames $timeline.StageFrames
    $bossAttackDisplayFrames = Get-Task6BossAttackRenderedFrames -Path $bound['full_flow_log'] -StageFrames $timeline.StageFrames
    if ($completionDisplayFrame -ne [long]$timeline.StageFrames['completion']) {
        throw "full-flow completion display frame does not bind its stage: display=$completionDisplayFrame stage=$($timeline.StageFrames['completion'])"
    }
    $fullKeyframeManifest = Assert-Task6KeyframeManifestBinding -Values $values `
        -Name 'full_flow_keyframe_manifest' -AviPath $fullAvi -ExpectedFrames $fullIndex.Header.TotalFrames `
        -ExpectedStages $presentationStages -ExpectedStageFrames $timeline.StageFrames
    $fullKeyframeEntries = Get-Task6KeyframeManifestEntries -ManifestPath $fullKeyframeManifest
    $modeHudEntry = Get-Task6ExactKeyframeEntry -Entries @($fullKeyframeEntries) -Stage 'mode_hud' `
        -Frame $modeHudDisplayFrame -Label 'full-flow mode HUD'
    $modeHudPath = Assert-Task6ManifestFileHash -Values $values -Name 'full_flow_mode_hud_png'
    if ((Resolve-Path -LiteralPath $modeHudPath).Path -cne (Resolve-Path -LiteralPath $modeHudEntry.Png).Path) {
        throw "capture mode-HUD path does not bind the manifest mode-HUD PNG: capture=$modeHudPath keyframe=$($modeHudEntry.Png)"
    }
    $modeHudVisual = Assert-Task6ModeHudVisual -Path $modeHudPath
    [int]$recordedCreamSamples = [int](Get-CaptureValue $values 'full_flow_mode_hud_cream_samples')
    [int]$recordedInkSamples = [int](Get-CaptureValue $values 'full_flow_mode_hud_ink_samples')
    if ($recordedCreamSamples -ne [int]$modeHudVisual.CreamSamples -or
        $recordedInkSamples -ne [int]$modeHudVisual.InkSamples) {
        throw "capture mode-HUD visual sample binding mismatch: recorded=$recordedCreamSamples/$recordedInkSamples actual=$($modeHudVisual.CreamSamples)/$($modeHudVisual.InkSamples)"
    }
    $secondBoostRejectedEntry = Get-Task6ExactKeyframeEntry -Entries @($fullKeyframeEntries) `
        -Stage 'boost_second_rejected' -Frame ([long]$boostDisplay.Frames['boost_second_rejected']) `
        -Label 'full-flow second boost rejection'
    $secondBoostRejectedPath = Assert-Task6ManifestFileHash -Values $values -Name 'full_flow_second_boost_rejected_png'
    if ((Resolve-Path -LiteralPath $secondBoostRejectedPath).Path -cne
        (Resolve-Path -LiteralPath $secondBoostRejectedEntry.Png).Path) {
        throw "capture second-boost-rejection path does not bind its manifest PNG: capture=$secondBoostRejectedPath keyframe=$($secondBoostRejectedEntry.Png)"
    }
    $secondBoostRejectedVisual = Assert-Task6SecondBoostRejectedVisual -Path $secondBoostRejectedPath
    [int]$recordedRopeCyanSamples = [int](Get-CaptureValue $values 'full_flow_second_boost_rejected_rope_cyan_samples')
    [int]$recordedPurpleRingSamples = [int](Get-CaptureValue $values 'full_flow_second_boost_rejected_purple_ring_samples')
    if ($recordedRopeCyanSamples -ne [int]$secondBoostRejectedVisual.RopeCyanSamples -or
        $recordedPurpleRingSamples -ne [int]$secondBoostRejectedVisual.PurpleRingSamples) {
        throw "capture second-boost-rejection visual sample binding mismatch: recorded=$recordedRopeCyanSamples/$recordedPurpleRingSamples actual=$($secondBoostRejectedVisual.RopeCyanSamples)/$($secondBoostRejectedVisual.PurpleRingSamples)"
    }
    foreach ($stage in @('boss_sweep', 'boss_leap_slam', 'boss_ground_wave', 'boss_hammer_throw')) {
        Get-Task6ExactKeyframeEntry -Entries @($fullKeyframeEntries) -Stage $stage `
            -Frame ([long]$bossAttackDisplayFrames[$stage]) -Label "full-flow $stage formal Telegraph" | Out-Null
    }
    $completionEntry = Get-Task6ExactKeyframeEntry -Entries @($fullKeyframeEntries) -Stage 'completion' `
        -Frame ([long]$timeline.StageFrames['completion']) -Label 'full-flow completion card'
    $completionCardPath = Assert-Task6ManifestFileHash -Values $values -Name 'full_flow_completion_card_png'
    if ((Resolve-Path -LiteralPath $completionCardPath).Path -cne
        (Resolve-Path -LiteralPath $completionEntry.Png).Path) {
        throw "capture completion-card path does not bind the manifest completion PNG: capture=$completionCardPath keyframe=$($completionEntry.Png)"
    }
    $completionCardVisual = Assert-Task6CompletionCardVisual -Path $completionCardPath
    [int]$recordedPanelSamples = [int](Get-CaptureValue $values 'full_flow_completion_card_panel_samples')
    [int]$recordedGoldBorderSamples = [int](Get-CaptureValue $values 'full_flow_completion_card_gold_border_samples')
    if ($recordedPanelSamples -ne [int]$completionCardVisual.PanelSamples -or
        $recordedGoldBorderSamples -ne [int]$completionCardVisual.GoldBorderSamples) {
        throw "capture completion-card visual sample binding mismatch: recorded=$recordedPanelSamples/$recordedGoldBorderSamples actual=$($completionCardVisual.PanelSamples)/$($completionCardVisual.GoldBorderSamples)"
    }
    Assert-Task6GlobalKeyframeMotion -ManifestPath $fullKeyframeManifest -Stages $presentationStages `
        -Label 'full-flow decoded AVI'
    Assert-Task6BoostNeighborMotion -ManifestPath $fullKeyframeManifest -StageFrames $timeline.StageFrames `
        -Label 'full-flow attached boost'
    $reticleFrameMap = Get-Task6ExactStageFrameMap -LogPath $bound['full_flow_log'] `
        -StagePrefix 'PROLOGUE_RETICLE_DWELL' -ExpectedStages $reticleStageNames -Label 'full-flow reticles'
    $reticleKeyframeManifest = Assert-Task6KeyframeManifestBinding -Values $values `
        -Name 'full_flow_reticle_keyframe_manifest' -AviPath $fullAvi -ExpectedFrames $fullIndex.Header.TotalFrames `
        -ExpectedStages $reticleStageNames -ExpectedStageFrames $reticleFrameMap.Frames
    $bossStageNames = @('route_started', 'completed_real_retry')
    $bossFrameMap = Get-Task6ExactStageFrameMap -LogPath $bound['boss_retry_log'] `
        -StagePrefix 'PROLOGUE_BOSS_RETRY_STAGE' -ExpectedStages $bossStageNames -Label 'boss-retry'
    $bossKeyframeManifest = Assert-Task6KeyframeManifestBinding -Values $values `
        -Name 'boss_retry_keyframe_manifest' -AviPath $bossAvi -ExpectedFrames $bossIndex.Header.TotalFrames `
        -ExpectedStages $bossStageNames -ExpectedStageFrames $bossFrameMap.Frames
    Assert-Task6GlobalKeyframeMotion -ManifestPath $bossKeyframeManifest -Stages $bossStageNames `
        -Label 'boss-retry decoded AVI'
    Assert-Task6DistinctKeyframeStages -ManifestPath $fullKeyframeManifest `
        -Stages @('boss_sweep', 'boss_leap_slam', 'boss_ground_wave', 'boss_hammer_throw') `
        -Label 'Boss attack proof'
    return [pscustomobject]@{
        Manifest = $manifest.Path
        Values = $values
        Bound = $bound
        BaseHead = $actualHead
        Candidate = $candidate
        Materialization = $materialization
        Full = $fullIndex
        Boss = $bossIndex
    }
}

function Assert-Task6MaterializationPreflight {
    $projectFile = Join-Path $project 'project.godot'
    $main = Join-Path $project 'scenes\prologue\PrologueLevel.tscn'
    if (-not (Test-Path -LiteralPath $projectFile -PathType Leaf) -or -not (Test-Path -LiteralPath $main -PathType Leaf)) {
        throw 'Task11 materialization misses project.godot or generated PrologueLevel.'
    }
    $projectText = Get-Content -LiteralPath $projectFile -Raw
    if ($projectText -notmatch 'run/main_scene="res://scenes/prologue/PrologueLevel.tscn"') {
        throw 'Task11 materialization requires the generated PrologueLevel as the candidate main scene.'
    }
    foreach ($relative in (Get-Task6SceneSourceList | Select-Object -First 5)) {
        if (-not (Test-Path -LiteralPath (Join-Path $project $relative) -PathType Leaf)) {
            throw "Task11 materialization misses sector scene $relative"
        }
    }
    'TASK11_MATERIALIZATION_PRECHECK_OK main=PrologueLevel sectors=5 generated_scenes=25 project_snapshot=1'
}

function Get-Task6RuntimeSourceList {
    return @(
        'project.godot', 'Knight.csproj',
        'tests\smoke\PrologueCriticalPathSmokeTask6.cs',
        'tests\presentation\ProloguePresentationTask6.cs',
        'tests\presentation\PrologueBaseAttackPresentation.cs',
        'tests\presentation\BossRetryPresentationTask6.cs',
        'tests\presentation\PrologueVisualAuditTask6.cs',
        'tests\presentation\PrologueUiResolutionCapture.cs',
        'tests\RunTask4AudioVfxTests.cs',
        'tests\integration\PrologueCanyonTraversalTests.cs',
        'tests\integration\PrologueCanyonGeneratedMainRouteTests.cs',
        'tests\integration\PrologueTask9GeneratedMainTests.cs',
        'tests\integration\FallenBellkeeperRuntimeTests.cs',
        # RunTask4UiTests is the committed, HEAD-only replacement for the historical
        # user-owned RunTask11UiTests runner.  Never let certification silently reach
        # an untracked runner.
        'tests\RunTask4UiTests.cs',
        'scripts\prologue\guidance\PrologueGuidanceTargetView.cs',
        'assets\localization\prologue-ui.json',
        'scripts\prologue\PrologueCompletionController.cs',
        'scripts\player\PlayerInputReader.cs',
        'scripts\ui\PrologueActionModeVisual.cs',
        'scripts\ui\PrologueFormalHud.cs',
        'scripts\ui\WorldTargetMarkerVisual.cs',
        'scripts\ui\ProtectedPromptPlacer.cs',
        'tests\powershell\Task6TerrainGrappleEvidence.Tests.ps1',
        'tests\powershell\Task6ExactFrameContract.Tests.ps1',
        'tests\powershell\Task6BaseExternalEvidence.Tests.ps1',
        'tests\powershell\Task6CaptureHostCompatibility.Tests.ps1',
        'tests\powershell\Task6CleanCandidateCertification.Tests.ps1',
        'tests\powershell\Task6ImportNativeRetry.Tests.ps1',
        'tests\powershell\Task6UiCaptureExternal.Tests.ps1',
        'tools\EvidenceChecks.ps1', 'tools\Task6AviStreaming.ps1', 'tools\audit-prologue-production.ps1',
        'tools\audit-prologue-base-hardgates.ps1', 'tools\audit-prologue-task6-focused.ps1',
        'tools\capture-prologue.ps1', 'tools\extract-prologue-keyframes.ps1',
        'tools\Task6ImportRetry.ps1', 'tools\verify-prologue.ps1', 'tools\verify-prologue-base-hardgates.ps1'
    )
}

function Get-Task6DirectProofDependencyList {
    # This is intentionally an explicit audit of every source launched directly by the
    # runtime/capture/verifier chain.  `git status --untracked-files=all` protects the
    # rest of the project glob; `git cat-file` below proves these entry points exist in
    # the candidate commit rather than merely in the working directory.
    $baseAndBuilderEntrypoints = @(
        'tests\powershell\Task6BaseHardGates.Tests.ps1',
        'tests\powershell\PlayPrologue.Tests.ps1',
        'tests\RunPrologueTests.cs',
        'tests\RunFallenBellkeeperRuntimeTests.cs',
        'tests\integration\PrologueAudioTeardownProbe.cs',
        'tests\integration\GraveLampMothTeardownProbe.cs',
        'tools\build\art\BuildPlayerFormalVisual.cs',
        'tools\build\BuildPlayer.cs',
        'tools\build\art\BuildEnemyFormalVisuals.cs',
        'tools\build\BuildRustShellSquire.cs',
        'tools\build\prologue\BuildGraveLampMoth.cs',
        'tools\build\prologue\BuildBellBackLaborer.cs',
        'tools\build\art\BuildFallenBellkeeperFormalVisual.cs',
        'tools\build\prologue\BuildFallenBellkeeper.cs',
        'tools\build\art\BuildPrologueMechanismVisuals.cs',
        'tools\build\prologue\BuildBellChainMechanism.cs',
        'tools\build\art\BuildPrologueEnvironmentKit.cs',
        'tools\build\prologue\BuildAwakeningCourtyard.cs',
        'tools\build\prologue\BuildBrokenToothCorridor.cs',
        'tools\build\prologue\BuildUpperPendulum.cs',
        'tools\build\prologue\BuildLowerBuriedMachinery.cs',
        'tools\build\prologue\BuildBellChainRoom.cs',
        'tools\build\prologue\BuildFallenBellkeeperArena.cs',
        'tools\build\ui\BuildPrologueUi.cs',
        'tools\build\art\BuildPrologueVfx.cs',
        'tools\build\prologue\BuildCanyonEntrance.cs',
        'tools\build\prologue\BuildCanyonDescent.cs',
        'tools\build\prologue\BuildCanyonBasin.cs',
        'tools\build\prologue\BuildCanyonAscent.cs',
        'tools\build\prologue\BuildLostTemple.cs',
        'tools\build\prologue\BuildPrologueLevel.cs'
    )
    return @(Get-Task6RuntimeSourceList) + @(Get-Task6SceneSourceList) + $baseAndBuilderEntrypoints |
        Sort-Object -Unique
}

function Assert-Task6CleanCandidatePreflight {
    $preflightStartedUtc = [DateTime]::UtcNow
    $status = @(& git -C $project status --porcelain=v1 --untracked-files=all)
    if ($LASTEXITCODE -ne 0) { throw 'Task 6 clean-candidate preflight cannot query git status.' }
    if ($status.Count -ne 0) {
        throw "Task 6 candidate working tree is dirty; certification requires a clean named worktree: $project"
    }

    $gitRootLines = @(& git -C $project rev-parse --show-toplevel)
    if ($LASTEXITCODE -ne 0 -or $gitRootLines.Count -ne 1) {
        throw 'Task 6 clean-candidate preflight cannot resolve the git worktree root.'
    }
    $gitRoot = (Resolve-Path -LiteralPath $gitRootLines[0]).Path
    if (-not $gitRoot.TrimEnd('\').Equals($project.TrimEnd('\'), [StringComparison]::OrdinalIgnoreCase)) {
        throw "Task 6 candidate root does not match its git worktree root: candidate=$project git_root=$gitRoot"
    }

    $branchLines = @(& git -C $project symbolic-ref --quiet --short HEAD 2>$null)
    if ($LASTEXITCODE -ne 0 -or $branchLines.Count -ne 1 -or
        [string]::IsNullOrWhiteSpace($branchLines[0])) {
        throw 'Task 6 candidate must be on a named branch; detached materializations are proof-only, not candidates.'
    }
    $branch = $branchLines[0].Trim()
    if (-not $branch.StartsWith('codex/', [StringComparison]::Ordinal)) {
        throw "Task 6 candidate branch must use the codex/ namespace: $branch"
    }
    $gitLinkPath = Join-Path $project '.git'
    $gitLink = Get-Item -LiteralPath $gitLinkPath -Force -ErrorAction SilentlyContinue
    if ($null -eq $gitLink -or $gitLink.PSIsContainer) {
        throw "TASK6_CLEAN_CANDIDATE_REQUIRES_LINKED_WORKTREE path=$gitLinkPath"
    }
    $cachePath = Get-Task6CleanCandidateCachePath
    if (Test-Path -LiteralPath $cachePath) {
        throw "TASK6_CLEAN_CANDIDATE_CACHE_PREEXISTING path=$cachePath"
    }

    $headLines = @(& git -C $project rev-parse HEAD)
    if ($LASTEXITCODE -ne 0 -or $headLines.Count -ne 1 -or $headLines[0] -notmatch '^[0-9a-fA-F]{40}$') {
        throw 'Task 6 clean-candidate preflight cannot resolve a 40-character HEAD.'
    }
    $head = $headLines[0].Trim()
    $dependencies = @(Get-Task6DirectProofDependencyList)
    $trackedState = @{}
    foreach ($line in @(& git -C $project ls-files -s)) {
        if ($line -notmatch '^\d+\s+([0-9a-fA-F]{40})\s+\d+\t(.+)$') {
            throw "Task 6 clean-candidate preflight cannot parse tracked git state: $line"
        }
        $trackedState[$Matches[2]] = $Matches[1].ToUpperInvariant()
    }
    if ($LASTEXITCODE -ne 0 -or $trackedState.Count -eq 0) {
        throw 'Task 6 clean-candidate preflight cannot snapshot tracked git blobs.'
    }
    $untrackedState = @(& git -C $project ls-files --others --exclude-standard)
    if ($LASTEXITCODE -ne 0) {
        throw 'Task 6 clean-candidate preflight cannot snapshot the untracked-file manifest.'
    }
    if ($untrackedState.Count -ne 0) {
        throw 'Task 6 clean-candidate preflight requires an empty untracked-file manifest.'
    }
    $logLines = [System.Collections.Generic.List[string]]::new()
    $logLines.Add("candidate_root=$project")
    $logLines.Add("git_root=$gitRoot")
    $logLines.Add("branch=$branch")
    $logLines.Add("head=$head")
    $logLines.Add("preflight_started_utc=$($preflightStartedUtc.ToString('o'))")
    $logLines.Add("cache_root=$cachePath")
    $logLines.Add('cache_preexisting=False')
    $logLines.Add('git_status_porcelain=<empty>')
    $logLines.Add("dependency_count=$($dependencies.Count)")
    $logLines.Add("tracked_git_blob_count=$($trackedState.Count)")
    foreach ($tracked in @($trackedState.Keys | Sort-Object)) {
        $logLines.Add("tracked=$tracked git_blob_sha1=$($trackedState[$tracked])")
    }
    $logLines.Add("untracked_manifest_count=$($untrackedState.Count)")
    foreach ($untracked in @($untrackedState | Sort-Object)) {
        $logLines.Add("untracked=$untracked")
    }
    foreach ($relative in $dependencies) {
        $workingPath = Join-Path $project $relative
        if (-not (Test-Path -LiteralPath $workingPath -PathType Leaf)) {
            throw "Task 6 committed dependency is missing from the working tree: $relative"
        }
        $gitRelative = $relative.Replace('\', '/')
        & git -C $project cat-file -e ("HEAD:$gitRelative") 2>$null
        if ($LASTEXITCODE -ne 0) {
            throw "Task 6 committed dependency is absent from HEAD: $relative"
        }
        $blobLines = @(& git -C $project rev-parse ("HEAD:$gitRelative"))
        if ($LASTEXITCODE -ne 0 -or $blobLines.Count -ne 1 -or $blobLines[0] -notmatch '^[0-9a-fA-F]{40}$') {
            throw "Task 6 committed dependency cannot resolve a HEAD blob: $relative"
        }
        $logLines.Add("dependency=$gitRelative blob=$($blobLines[0].Trim())")
    }
    [IO.File]::WriteAllLines((Join-Path $VerificationRoot 'candidate-clean-before.log'),
        $logLines.ToArray(), [Text.UTF8Encoding]::new($false))
    "TASK6_CLEAN_CANDIDATE_OK branch=$branch head=$head dependencies=$($dependencies.Count)"
}

function Get-Task6CleanCandidateState {
    param([string]$Path)
    if ([string]::IsNullOrWhiteSpace($Path) -or -not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        throw 'Task 6 clean-candidate restoration requires an existing preflight state log.'
    }
    $fullPath = (Resolve-Path -LiteralPath $Path).Path
    if ($fullPath.TrimEnd('\').StartsWith(($project.TrimEnd('\') + '\'), [StringComparison]::OrdinalIgnoreCase)) {
        throw 'Task 6 clean-candidate restoration refuses state evidence inside the candidate worktree.'
    }
    $state = [ordered]@{
        CandidateRoot = $null
        Head = $null
        PreflightStartedUtc = [DateTime]::MinValue
        CacheRoot = $null
        CachePreexisting = $null
        Tracked = @{}
        Untracked = @{}
    }
    foreach ($line in Get-Content -LiteralPath $fullPath) {
        if ($line -match '^candidate_root=(.+)$') { $state.CandidateRoot = $Matches[1]; continue }
        if ($line -match '^head=([0-9a-fA-F]{40})$') { $state.Head = $Matches[1].ToUpperInvariant(); continue }
        if ($line -match '^preflight_started_utc=(.+)$') {
            $parsed = [DateTime]::MinValue
            if (-not [DateTime]::TryParse($Matches[1], [Globalization.CultureInfo]::InvariantCulture,
                    [Globalization.DateTimeStyles]::RoundtripKind, [ref]$parsed)) {
                throw 'Task 6 clean-candidate restoration state has an invalid preflight timestamp.'
            }
            $state.PreflightStartedUtc = $parsed.ToUniversalTime()
            continue
        }
        if ($line -match '^cache_root=(.+)$') { $state.CacheRoot = $Matches[1]; continue }
        if ($line -match '^cache_preexisting=(True|False)$') {
            $state.CachePreexisting = [bool]::Parse($Matches[1])
            continue
        }
        if ($line -match '^tracked=(.+) git_blob_sha1=([0-9a-fA-F]{40})$') {
            $state.Tracked[$Matches[1]] = $Matches[2].ToUpperInvariant()
            continue
        }
        if ($line -match '^untracked=(.+)$') { $state.Untracked[$Matches[1]] = $true }
    }
    if ([string]::IsNullOrWhiteSpace($state.CandidateRoot) -or
        -not $state.CandidateRoot.TrimEnd('\').Equals($project.TrimEnd('\'), [StringComparison]::OrdinalIgnoreCase)) {
        throw 'Task 6 clean-candidate restoration state does not belong to this worktree.'
    }
    if ([string]::IsNullOrWhiteSpace($state.Head) -or $state.Tracked.Count -eq 0) {
        throw 'Task 6 clean-candidate restoration state lacks the required pre-import tracked hash manifest.'
    }
    $expectedCacheRoot = Get-Task6CleanCandidateCachePath
    if ([string]::IsNullOrWhiteSpace($state.CacheRoot) -or
        -not $state.CacheRoot.TrimEnd('\').Equals($expectedCacheRoot, [StringComparison]::OrdinalIgnoreCase) -or
        $null -eq $state.CachePreexisting -or $state.CachePreexisting -or
        $state.PreflightStartedUtc -eq [DateTime]::MinValue) {
        throw 'Task 6 clean-candidate restoration state lacks a cache-free registered-worktree preflight.'
    }
    return [pscustomobject]$state
}

function Get-Task6CandidateChildPath {
    param([string]$RelativePath, [switch]$RequireLeaf)
    if ([string]::IsNullOrWhiteSpace($RelativePath)) { throw 'Task 6 candidate path is empty.' }
    $fullPath = [IO.Path]::GetFullPath((Join-Path $project $RelativePath))
    $prefix = $project.TrimEnd('\') + '\'
    if (-not $fullPath.StartsWith($prefix, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Task 6 candidate path escapes its worktree: $RelativePath"
    }
    if ($RequireLeaf -and -not (Test-Path -LiteralPath $fullPath -PathType Leaf)) {
        throw "Task 6 candidate path is not an existing file: $RelativePath"
    }
    return $fullPath
}

function Get-Task6CleanCandidateCachePath {
    $candidateRoot = [IO.Path]::GetFullPath($project).TrimEnd('\')
    $cachePath = [IO.Path]::GetFullPath((Join-Path $candidateRoot '.godot')).TrimEnd('\')
    $expected = $candidateRoot + '\.godot'
    if (-not $cachePath.Equals($expected, [StringComparison]::OrdinalIgnoreCase) -or
        -not $cachePath.StartsWith(($candidateRoot + '\'), [StringComparison]::OrdinalIgnoreCase)) {
        throw "Task 6 clean-candidate cache path escapes its linked worktree: $cachePath"
    }
    return $cachePath
}

function Wait-Task6OwnedGodotDrain {
    $deadline = [DateTime]::UtcNow.AddSeconds(30)
    $projectArgument = [regex]::Escape($project)
    $pathArgumentPattern = ('--path\s+(?:"{0}"|''{0}''|{0})(?:\s|$)' -f $projectArgument)
    while ($true) {
        $owned = @(
            Get-CimInstance Win32_Process -ErrorAction Stop | Where-Object {
                $_.Name -like 'Godot*.exe' -and $null -ne $_.CommandLine -and
                $_.CommandLine -match $pathArgumentPattern
            }
        )
        if ($owned.Count -eq 0) { return }
        if ([DateTime]::UtcNow -ge $deadline) {
            throw "TASK6_CLEAN_CANDIDATE_GODOT_STILL_RUNNING pids=$($owned.ProcessId -join ',')"
        }
        Start-Sleep -Milliseconds 200
    }
}

function Get-Task6CleanCandidateCacheManifest {
    param([Parameter(Mandatory)][string]$CachePath)

    if (-not (Test-Path -LiteralPath $CachePath)) {
        return [pscustomobject]@{
            Exists = $false; RootCreationUtc = [DateTime]::MinValue; Files = @()
        }
    }
    $rootItem = Get-Item -LiteralPath $CachePath -Force -ErrorAction Stop
    if (-not $rootItem.PSIsContainer -or
        ($rootItem.Attributes -band [IO.FileAttributes]::ReparsePoint)) {
        throw "TASK6_CLEAN_CANDIDATE_CACHE_UNSAFE_ROOT path=$CachePath"
    }
    $stack = [Collections.Generic.Stack[string]]::new()
    $stack.Push($CachePath)
    $files = [Collections.Generic.List[string]]::new()
    while ($stack.Count -gt 0) {
        $directory = $stack.Pop()
        foreach ($item in @(Get-ChildItem -LiteralPath $directory -Force -ErrorAction Stop)) {
            $fullPath = [IO.Path]::GetFullPath($item.FullName)
            if (-not $fullPath.StartsWith(($CachePath + '\'), [StringComparison]::OrdinalIgnoreCase)) {
                throw "TASK6_CLEAN_CANDIDATE_CACHE_PATH_ESCAPE path=$fullPath root=$CachePath"
            }
            if ($item.Attributes -band [IO.FileAttributes]::ReparsePoint) {
                throw "TASK6_CLEAN_CANDIDATE_CACHE_REPARSE_POINT path=$fullPath"
            }
            if ($item.PSIsContainer) {
                $stack.Push($fullPath)
                continue
            }
            if (-not $item.PSIsContainer -and -not (Test-Path -LiteralPath $fullPath -PathType Leaf)) {
                throw "TASK6_CLEAN_CANDIDATE_CACHE_NONFILE path=$fullPath"
            }
            $relative = $fullPath.Substring(($CachePath + '\').Length).Replace('\', '/')
            $files.Add("file=$relative|$($item.Length)|$($item.LastWriteTimeUtc.ToString('o'))|$(Get-EvidenceSha256 -LiteralPath $fullPath)")
        }
    }
    return [pscustomobject]@{
        Exists = $true; RootCreationUtc = $rootItem.CreationTimeUtc; Files = @($files | Sort-Object)
    }
}

function Get-Task6PorcelainTrackedPaths {
    param([hashtable]$Tracked)
    $paths = [System.Collections.Generic.List[string]]::new()
    foreach ($line in @(& git -C $project status --porcelain=v1 --untracked-files=all)) {
        if ($line.StartsWith('?? ')) { continue }
        if ($line.Length -lt 4) { throw "Task 6 cleanup cannot parse porcelain status: $line" }
        $indexState = $line.Substring(0, 1)
        $worktreeState = $line.Substring(1, 1)
        if ($indexState -ne ' ') {
            throw "Task 6 cleanup refuses an indexed candidate mutation: $line"
        }
        if ($worktreeState -notin @('M', 'D', 'T')) {
            throw "Task 6 cleanup refuses an unsupported tracked candidate mutation: $line"
        }
        $relative = $line.Substring(3).Replace('\', '/')
        if (-not $Tracked.ContainsKey($relative)) {
            throw "Task 6 cleanup refuses a tracked path absent from the pre-import hash manifest: $relative"
        }
        [void](Get-Task6CandidateChildPath -RelativePath $relative)
        $paths.Add($relative)
    }
    if ($LASTEXITCODE -ne 0) { throw 'Task 6 cleanup cannot query tracked porcelain state.' }
    return @($paths | Sort-Object -Unique)
}

function Restore-Task6CleanCandidateDerivedFiles {
    param([string]$StateLog)
    $state = Get-Task6CleanCandidateState -Path $StateLog
    $headLines = @(& git -C $project rev-parse HEAD)
    if ($LASTEXITCODE -ne 0 -or $headLines.Count -ne 1 -or
        $headLines[0].Trim().ToUpperInvariant() -cne $state.Head) {
        throw 'Task 6 cleanup refuses a candidate whose HEAD changed after clean preflight.'
    }
    $restored = [System.Collections.Generic.List[string]]::new()
    foreach ($relative in Get-Task6PorcelainTrackedPaths -Tracked $state.Tracked) {
        $path = Get-Task6CandidateChildPath -RelativePath $relative
        $beforeBlobLines = @(& git -C $project hash-object -- $relative)
        if ($LASTEXITCODE -ne 0 -or $beforeBlobLines.Count -ne 1) {
            throw "Task 6 cleanup cannot hash tracked import side effect: $relative"
        }
        & git -C $project restore --source=HEAD --worktree -- $relative
        if ($LASTEXITCODE -ne 0) { throw "Task 6 cleanup cannot restore tracked import side effect: $relative" }
        $afterBlobLines = @(& git -C $project hash-object -- $relative)
        if ($LASTEXITCODE -ne 0 -or $afterBlobLines.Count -ne 1 -or
            $afterBlobLines[0].Trim().ToUpperInvariant() -cne $state.Tracked[$relative]) {
            throw "Task 6 cleanup restored bytes do not match the pre-import tracked hash: $relative"
        }
        $restored.Add("restored=$relative before_git_blob_sha1=$($beforeBlobLines[0].Trim().ToUpperInvariant()) after_git_blob_sha1=$($afterBlobLines[0].Trim().ToUpperInvariant())")
    }

    $created = [System.Collections.Generic.List[string]]::new()
    $untrackedNow = @(& git -C $project ls-files --others --exclude-standard)
    if ($LASTEXITCODE -ne 0) { throw 'Task 6 cleanup cannot query the post-import untracked-file manifest.' }
    foreach ($relative in @($untrackedNow | Sort-Object)) {
        $normalized = $relative.Replace('\', '/')
        if ($state.Untracked.ContainsKey($normalized)) { continue }
        if (-not ($normalized.EndsWith('.uid', [StringComparison]::OrdinalIgnoreCase) -or
                  $normalized.EndsWith('.import', [StringComparison]::OrdinalIgnoreCase))) {
            throw "Task 6 cleanup refuses a non-derived post-import file: $normalized"
        }
        $path = Get-Task6CandidateChildPath -RelativePath $normalized -RequireLeaf
        $sha256 = Get-EvidenceSha256 -LiteralPath $path
        Remove-Item -LiteralPath $path -Force
        if (Test-Path -LiteralPath $path) { throw "Task 6 cleanup did not remove derived import file: $normalized" }
        $created.Add("created=$normalized sha256=$sha256")
    }

    Wait-Task6OwnedGodotDrain
    $cachePath = Get-Task6CleanCandidateCachePath
    $cache = Get-Task6CleanCandidateCacheManifest -CachePath $cachePath
    $cacheDeleted = $false
    $cacheManifestHash = 'none'
    if ($cache.Exists) {
        if ($cache.RootCreationUtc -lt $state.PreflightStartedUtc) {
            throw "TASK6_CLEAN_CANDIDATE_CACHE_NOT_CREATED_THIS_RUN root=$cachePath " +
                "created_utc=$($cache.RootCreationUtc.ToString('o')) preflight_utc=$($state.PreflightStartedUtc.ToString('o'))"
        }
        $cacheManifestPath = Join-Path $VerificationRoot 'candidate-clean-cache-before-delete.manifest.txt'
        $cacheManifestLines = [Collections.Generic.List[string]]::new()
        $cacheManifestLines.Add("cache_root=$cachePath")
        $cacheManifestLines.Add("created_utc=$($cache.RootCreationUtc.ToString('o'))")
        $cacheManifestLines.Add("file_count=$($cache.Files.Count)")
        foreach ($line in $cache.Files) { $cacheManifestLines.Add($line) }
        [IO.File]::WriteAllLines($cacheManifestPath, $cacheManifestLines.ToArray(),
            [Text.UTF8Encoding]::new($false))
        $cacheManifestHash = Get-EvidenceSha256 -LiteralPath $cacheManifestPath
        Remove-Item -LiteralPath $cachePath -Recurse -Force
        if (Test-Path -LiteralPath $cachePath) {
            throw "Task 6 clean-candidate cleanup did not remove its owned cache: $cachePath"
        }
        $cacheDeleted = $true
    }

    $finalStatus = @(& git -C $project status --porcelain=v1 --untracked-files=all)
    if ($LASTEXITCODE -ne 0) { throw 'Task 6 cleanup cannot query final candidate status.' }
    if ($finalStatus.Count -ne 0) {
        throw "Task 6 cleanup failed to restore an empty candidate status: $($finalStatus -join '; ')"
    }
    & git -C $project diff --quiet
    if ($LASTEXITCODE -ne 0) { throw 'Task 6 cleanup detected remaining unstaged byte drift.' }
    & git -C $project diff --cached --quiet
    if ($LASTEXITCODE -ne 0) { throw 'Task 6 cleanup detected remaining staged byte drift.' }
    $logLines = [System.Collections.Generic.List[string]]::new()
    $logLines.Add("candidate_root=$project")
    $logLines.Add("head=$($state.Head)")
    $logLines.Add("state_log=$((Resolve-Path -LiteralPath $StateLog).Path)")
    $logLines.Add("cache_root=$cachePath")
    $logLines.Add("cache_preexisting=$($state.CachePreexisting)")
    $logLines.Add("cache_deleted=$cacheDeleted")
    $logLines.Add("cache_manifest_sha256=$cacheManifestHash")
    $logLines.Add("restored_count=$($restored.Count)")
    foreach ($line in $restored) { $logLines.Add($line) }
    $logLines.Add("created_count=$($created.Count)")
    foreach ($line in $created) { $logLines.Add($line) }
    $logLines.Add('git_status_porcelain=<empty>')
    [IO.File]::WriteAllLines((Join-Path $VerificationRoot 'candidate-clean-after.log'),
        $logLines.ToArray(), [Text.UTF8Encoding]::new($false))
    "TASK6_CLEAN_CANDIDATE_RESTORE_OK restored=$($restored.Count) created=$($created.Count) cache_deleted=$cacheDeleted"
}

function Get-Task6SceneSourceList {
    return @(
        'scenes\prologue\sectors\CanyonEntrance.tscn',
        'scenes\prologue\sectors\CanyonDescent.tscn',
        'scenes\prologue\sectors\CanyonBasin.tscn',
        'scenes\prologue\sectors\CanyonAscent.tscn',
        'scenes\prologue\sectors\LostTemple.tscn',
        'scenes\prologue\PrologueLevel.tscn'
    )
}

if ($MaterializationPreflightOnly) {
    Assert-Task6MaterializationPreflight
    return
}

if ($CleanCandidatePreflightOnly) {
    Assert-Task6CleanCandidatePreflight
    return
}

if ($CleanCandidateRestoreOnly) {
    Restore-Task6CleanCandidateDerivedFiles -StateLog $CleanCandidateStateLog
    return
}

if ($VerifierTerminalOnly) {
    if ([string]::IsNullOrWhiteSpace($VerifierTerminalLog)) {
        throw 'Task 6 verifier terminal evidence requires -VerifierTerminalLog.'
    }
    Assert-NonEmptyFiles -Paths @($VerifierTerminalLog) -Label 'verifier terminal evidence'
    Assert-NoEngineLogErrors -Paths @($VerifierTerminalLog)
    Assert-OneRegexMarker -Path $VerifierTerminalLog -Pattern '^PROLOGUE_VERIFY_OK\s+.+$' `
        -Label 'verifier terminal marker'
    "PROLOGUE_VERIFY_TERMINAL_EVIDENCE_OK log=$VerifierTerminalLog"
    return
}

if ($CaptureEvidenceOnly) {
    $evidence = Assert-Task6CaptureEvidence -Root $CaptureRoot
    "PROLOGUE_CAPTURE_EVIDENCE_OK root=$CaptureRoot manifest=$($evidence.Manifest) candidate_sha256=$($evidence.Candidate)"
    return
}

$task6RuntimeSources = Get-Task6RuntimeSourceList
# The validation document receives the resulting hashes after capture; it is policy-gated above,
# but deliberately excluded from the executable runtime fingerprint to avoid a circular hash claim.
$task6EvidenceSources = $task6RuntimeSources
$scenePaths = Get-Task6SceneSourceList
$runtimeCandidateBefore = Get-Task6Fingerprint ($task6RuntimeSources + $scenePaths)
$candidateBefore = Get-Task6Fingerprint ($task6EvidenceSources + $scenePaths)
$sceneSnapshots = @{}
$sceneSemanticSnapshots = @{}
foreach ($relative in $scenePaths) {
    $path = Join-Path $project $relative
    if ((Get-Item -LiteralPath $path).Length -gt 8388608) {
        throw "generated scene exact snapshot exceeds the small-file limit: $relative"
    }
    $sceneSnapshots[$relative] = [IO.File]::ReadAllBytes($path)
    $sceneSemanticSnapshots[$relative] = ConvertTo-GodotSceneSemanticText ([IO.File]::ReadAllText($path))
}
$sceneHashBefore = Get-Task6Fingerprint $scenePaths

$baseConsole = Join-Path $VerificationRoot 'base-hardgates.console.log'
$baseEvidenceRoot = Join-Path $VerificationRoot 'base-hardgates'
$baseEvidenceStartedUtc = [DateTime]::UtcNow
$baseHost = Get-Task6CompatiblePowerShellHost
$base = Invoke-MergedNativeCommand -FilePath $baseHost -ArgumentList @(
    '-NoProfile', '-ExecutionPolicy', 'Bypass', '-File',
    (Join-Path $toolProject 'tools\verify-prologue-base-hardgates.ps1'), '-SuppressTerminal',
    '-ExternalEvidenceRoot', $baseEvidenceRoot
) -ConsolePath $baseConsole
if ($base.ExitCode -ne 0) { throw "BASE hard gates failed with exit code $($base.ExitCode)" }
Assert-NonEmptyFiles -Paths @($baseConsole) -Label 'BASE hard gates console'
Assert-OneExactMarker -Path $baseConsole -Marker 'PROLOGUE_BASE_HARD_GATES_OK' -Label 'BASE hard-gate terminal'
$baseImportRetryPattern = '^PROLOGUE_IMPORT_NATIVE_RETRY_OK attempts=(?<attempts>[12]) first_exit=(?<firstExit>0|-1073741819) restored=(?<restored>True|False)$'
$baseImportRetryLines = @((Get-Content -LiteralPath $baseConsole) | Where-Object {
    $_ -cmatch $baseImportRetryPattern
})
if ($baseImportRetryLines.Count -ne 1) {
    throw "BASE import-native retry terminal must occur exactly once; actual=$($baseImportRetryLines.Count)"
}
$baseImportRetryMatch = [regex]::Match($baseImportRetryLines[0], $baseImportRetryPattern)
$baseImportRetryAttempts = [int]$baseImportRetryMatch.Groups['attempts'].Value
$baseImportRetryFirstExit = [int]$baseImportRetryMatch.Groups['firstExit'].Value
$baseImportRetryRestored = $baseImportRetryMatch.Groups['restored'].Value
if (($baseImportRetryAttempts -eq 1 -and ($baseImportRetryFirstExit -ne 0 -or $baseImportRetryRestored -cne 'False')) -or
    ($baseImportRetryAttempts -eq 2 -and ($baseImportRetryFirstExit -ne -1073741819 -or $baseImportRetryRestored -cne 'True'))) {
    throw "BASE import-native retry terminal has an invalid attempt/exit/restore tuple: $($baseImportRetryLines[0])"
}
$baseImportRetrySummary = Join-Path $baseEvidenceRoot 'verify-prologue-import-retry-summary.txt'
$baseImportRetryAttempt1Metadata = Join-Path $baseEvidenceRoot 'verify-prologue-import-attempt1.metadata.txt'
Assert-NonEmptyFiles -Paths @($baseImportRetrySummary, $baseImportRetryAttempt1Metadata) `
    -Label 'BASE import-native retry evidence'
if ($baseImportRetryAttempts -eq 2) {
    Assert-NonEmptyFiles -Paths @(Join-Path $baseEvidenceRoot 'verify-prologue-import-attempt2.metadata.txt') `
        -Label 'BASE import-native retry second-attempt evidence'
}
"TASK6_BASE_IMPORT_NATIVE_RETRY_BOUND_OK attempts=$baseImportRetryAttempts first_exit=$baseImportRetryFirstExit restored=$baseImportRetryRestored"
$baseRuntimeSemanticPattern = '^PROLOGUE_GENERATED_RUNTIME_SEMANTICS_OK sha=([a-f0-9]{64}) count=6$'
$baseRuntimeSemanticLines = @((Get-Content -LiteralPath $baseConsole) | Where-Object {
    $_ -cmatch $baseRuntimeSemanticPattern
})
if ($baseRuntimeSemanticLines.Count -ne 1) {
    throw "BASE generated-runtime semantic marker must occur exactly once; actual=$($baseRuntimeSemanticLines.Count)"
}
$baseRuntimeSemanticSha = [regex]::Match(
    $baseRuntimeSemanticLines[0], $baseRuntimeSemanticPattern).Groups[1].Value
$baseEvidenceManifest = Join-Path $baseEvidenceRoot 'base-evidence-manifest.txt'
$baseEvidenceConsole = Join-Path $VerificationRoot 'base-external-evidence.console.log'
$baseEvidence = Invoke-MergedNativeCommand -FilePath $baseHost -ArgumentList @(
    '-NoProfile', '-ExecutionPolicy', 'Bypass', '-File',
    (Join-Path $toolProject 'tools\verify-prologue-base-hardgates.ps1'), '-ExternalEvidenceManifestOnly',
    '-ExternalEvidenceManifest', $baseEvidenceManifest,
    '-ExternalEvidenceNotBeforeUtc', $baseEvidenceStartedUtc.ToString('o')
) -ConsolePath $baseEvidenceConsole
if ($baseEvidence.ExitCode -ne 0) {
    throw "BASE external evidence manifest failed with exit code $($baseEvidence.ExitCode)"
}
Assert-NonEmptyFiles -Paths @($baseEvidenceManifest, $baseEvidenceConsole) -Label 'BASE external evidence'
Assert-OneRegexMarker -Path $baseEvidenceConsole `
    -Pattern '^PROLOGUE_BASE_EXTERNAL_EVIDENCE_OK manifest=.+ root=.+ run_id=[0-9a-fA-F-]{36} files=\d+ created_after_utc=.+$' `
    -Label 'BASE external evidence terminal'
$baseUiCaptureRoot = Join-Path $baseEvidenceRoot 'production-audit'
$baseUiCaptureManifestConsole = Join-Path $VerificationRoot 'base-ui-capture-manifest.console.log'
$baseUiCaptureManifest = Invoke-MergedNativeCommand -FilePath $baseHost -ArgumentList @(
    '-NoProfile', '-ExecutionPolicy', 'Bypass', '-File',
    (Join-Path $toolProject 'tools\audit-prologue-base-hardgates.ps1'),
    '-AuditRoot', $baseUiCaptureRoot, '-UiResolutionManifestOnly',
    '-UiResolutionNotBeforeUtc', $baseEvidenceStartedUtc.ToString('o')
) -ConsolePath $baseUiCaptureManifestConsole
if ($baseUiCaptureManifest.ExitCode -ne 0) {
    throw "BASE UI-capture manifest evidence failed with exit code $($baseUiCaptureManifest.ExitCode)"
}
Assert-NonEmptyFiles -Paths @(
    (Join-Path $baseUiCaptureRoot 'ui-resolution-capture-manifest.txt'),
    $baseUiCaptureManifestConsole
) -Label 'BASE UI-capture manifest evidence'
Assert-OneRegexMarker -Path $baseUiCaptureManifestConsole `
    -Pattern '^TASK6_UI_CAPTURE_MANIFEST_OK manifest=.+ pngs=222 root=.+$' `
    -Label 'BASE UI-capture manifest terminal'
'TASK6_BASE_UI_CAPTURE_MANIFEST_BOUND_OK pngs=222 resolutions=4'
$baseRuntimeSemanticEvidencePath = Join-Path $baseEvidenceRoot 'verify-prologue-generated-runtime-semantics.txt'
Assert-NonEmptyFiles -Paths @($baseRuntimeSemanticEvidencePath) -Label 'BASE generated-runtime semantic evidence'
$baseRuntimeSemanticEvidenceLines = @((Get-Content -LiteralPath $baseRuntimeSemanticEvidencePath) | Where-Object {
    $_ -cmatch $baseRuntimeSemanticPattern
})
if ($baseRuntimeSemanticEvidenceLines.Count -ne 1) {
    throw "BASE generated-runtime semantic evidence marker must occur exactly once; actual=$($baseRuntimeSemanticEvidenceLines.Count)"
}
$baseRuntimeSemanticEvidenceSha = [regex]::Match(
    $baseRuntimeSemanticEvidenceLines[0], $baseRuntimeSemanticPattern).Groups[1].Value
if ($baseRuntimeSemanticEvidenceSha -cne $baseRuntimeSemanticSha) {
    throw "BASE generated-runtime semantic binding mismatch console=$baseRuntimeSemanticSha evidence=$baseRuntimeSemanticEvidenceSha"
}
'TASK6_BASE_GENERATED_RUNTIME_SEMANTICS_BOUND_OK sha=$baseRuntimeSemanticSha count=6'

$baseExternalPesterConsole = Join-Path $VerificationRoot 'pester-base-external-evidence.console.log'
$baseExternalPesterRoot = Join-Path $VerificationRoot 'pester-base-external-evidence'
$baseExternalPesterPath = Join-Path $project 'tests\powershell\Task6BaseExternalEvidence.Tests.ps1'
$escapedBaseExternalPesterPath = $baseExternalPesterPath.Replace("'", "''")
$escapedBaseExternalPesterRoot = $baseExternalPesterRoot.Replace("'", "''")
$escapedBaseExternalProject = $project.Replace("'", "''")
$baseExternalPesterCommand = "& { `$p = @{ Path = '$escapedBaseExternalPesterPath'; Parameters = @{ ProjectRoot = '$escapedBaseExternalProject'; EvidenceRoot = '$escapedBaseExternalPesterRoot' } }; Import-Module Pester -MinimumVersion 3.4.0; `$result = Invoke-Pester -Script `$p -PassThru; if (`$result.FailedCount -ne 0) { exit 1 } }"
$baseExternalPester = Invoke-MergedNativeCommand -FilePath (Get-Task6CompatiblePowerShellHost) -ArgumentList @(
    '-NoProfile', '-ExecutionPolicy', 'Bypass', '-Command', $baseExternalPesterCommand
) -ConsolePath $baseExternalPesterConsole
if ($baseExternalPester.ExitCode -ne 0) {
    throw "Task 6 BASE external-evidence Pester failed: $($baseExternalPester.ExitCode)"
}
Assert-NonEmptyFiles -Paths @(
    $baseExternalPesterConsole,
    (Join-Path $baseExternalPesterRoot 'normal-root-required.child.log'),
    (Join-Path $baseExternalPesterRoot 'missing-manifest.child.log'),
    (Join-Path $baseExternalPesterRoot 'stale-manifest.child.log'),
    (Join-Path $baseExternalPesterRoot 'project-root-fallback.child.log'),
    (Join-Path $baseExternalPesterRoot 'path-outside-root.child.log'),
    (Join-Path $baseExternalPesterRoot 'fresh-manifest.child.log'),
    (Join-Path $baseExternalPesterRoot 'semantic-digest-tamper.child.log'),
    (Join-Path $baseExternalPesterRoot 'topology-path-tamper.child.log')
) -Label 'BASE external-evidence Pester logs'
if ((Get-Content -LiteralPath $baseExternalPesterConsole -Raw) -notmatch 'Passed:\s+8\s+Failed:\s+0') {
    throw 'Task 6 BASE external-evidence Pester did not report eight passing external-evidence boundary cases.'
}
'TASK6_BASE_EXTERNAL_EVIDENCE_PESTER_OK red_cases=7 green_cases=1 child_gates=True project_root_fallback=False generated_runtime_semantics=True'

$buildConsole = Join-Path $VerificationRoot 'build.console.log'
$build = Invoke-MergedNativeCommand -FilePath (Join-Path $dotnetRoot 'dotnet.exe') -ArgumentList @(
    'build', '--no-incremental', (Join-Path $project 'Knight.csproj')) -ConsolePath $buildConsole
if ($build.ExitCode -ne 0) { throw "Task 6 verifier build failed: $($build.ExitCode)" }
Assert-NonEmptyFiles -Paths @($buildConsole) -Label 'build log'
$buildText = Get-Content -LiteralPath $buildConsole -Raw
if ($buildText -notmatch '0 Warning\(s\)' -or $buildText -notmatch '0 Error\(s\)') {
    throw 'Task 6 verifier requires build 0 warnings / 0 errors'
}

$builders = @(
    @{ Name = 'CanyonEntrance'; Script = 'res://tools/build/prologue/BuildCanyonEntrance.cs' },
    @{ Name = 'CanyonDescent'; Script = 'res://tools/build/prologue/BuildCanyonDescent.cs' },
    @{ Name = 'CanyonBasin'; Script = 'res://tools/build/prologue/BuildCanyonBasin.cs' },
    @{ Name = 'CanyonAscent'; Script = 'res://tools/build/prologue/BuildCanyonAscent.cs' },
    @{ Name = 'LostTemple'; Script = 'res://tools/build/prologue/BuildLostTemple.cs' },
    @{ Name = 'PrologueLevel'; Script = 'res://tools/build/prologue/BuildPrologueLevel.cs' }
)
$builderDrift = 0
try {
    foreach ($builder in $builders) {
        $log = Join-Path $VerificationRoot ("builder-$($builder.Name).log")
        $console = Join-Path $VerificationRoot ("builder-$($builder.Name).console.log")
        $result = Invoke-MergedNativeCommand -FilePath $godot -ArgumentList @(
            '--headless', '--path', $project, '--script', $builder.Script, '--log-file', $log
        ) -ConsolePath $console
        if ($result.ExitCode -ne 0) { throw "leaf-to-main builder failed: $($builder.Name)" }
        Assert-NonEmptyFiles -Paths @($log, $console) -Label "builder $($builder.Name) logs"
        Assert-NoEngineLogErrors -Paths @($log, $console)
    }
    foreach ($relative in $scenePaths) {
        $path = Join-Path $project $relative
        $semantic = ConvertTo-GodotSceneSemanticText ([IO.File]::ReadAllText($path))
        if ($semantic -cne $sceneSemanticSnapshots[$relative]) {
            throw "leaf-to-main builder changed scene semantics: $relative"
        }
        $actual = [IO.File]::ReadAllBytes($path)
        if (-not (Test-ExactBytes -Expected $sceneSnapshots[$relative] -Actual $actual)) { $builderDrift++ }
    }
}
finally {
    foreach ($relative in $scenePaths) {
        [IO.File]::WriteAllBytes((Join-Path $project $relative), $sceneSnapshots[$relative])
    }
}
$sceneHashAfter = Get-Task6Fingerprint $scenePaths
if ($sceneHashAfter -cne $sceneHashBefore) {
    throw "scene hash restoration failed: before=$sceneHashBefore after=$sceneHashAfter"
}

$pesterConsole = Join-Path $VerificationRoot 'pester.console.log'
$pesterEvidence = Join-Path $VerificationRoot 'pester-red-evidence.log'
$pesterMutationRoot = Join-Path $VerificationRoot 'pester-mutations'
$pesterSourceMutationRoot = Join-Path ([IO.Path]::GetTempPath()) `
    ('knight-task6-source-mutations-' + [guid]::NewGuid().ToString('N'))
$pesterPath = Join-Path $project 'tests\powershell\Task6TerrainGrappleEvidence.Tests.ps1'
$escapedPesterPath = $pesterPath.Replace("'", "''")
$escapedPesterEvidence = $pesterEvidence.Replace("'", "''")
$escapedCaptureRoot = $CaptureRoot.Replace("'", "''")
$escapedMutationRoot = $pesterMutationRoot.Replace("'", "''")
$escapedSourceMutationRoot = $pesterSourceMutationRoot.Replace("'", "''")
$escapedBaseEvidenceManifest = $baseEvidenceManifest.Replace("'", "''")
$escapedBaseEvidenceNotBefore = $baseEvidenceStartedUtc.ToString('o').Replace("'", "''")
$pesterCommand = "& { `$env:KNIGHT_TASK6_PESTER_EVIDENCE_LOG = '$escapedPesterEvidence'; `$env:KNIGHT_TASK6_MUTATION_CAPTURE_ROOT = '$escapedCaptureRoot'; `$env:KNIGHT_TASK6_MUTATION_ROOT = '$escapedMutationRoot'; `$env:KNIGHT_TASK6_SOURCE_MUTATION_ROOT = '$escapedSourceMutationRoot'; `$env:KNIGHT_TASK6_BASE_EVIDENCE_MANIFEST = '$escapedBaseEvidenceManifest'; `$env:KNIGHT_TASK6_BASE_EVIDENCE_NOT_BEFORE_UTC = '$escapedBaseEvidenceNotBefore'; Import-Module Pester -MinimumVersion 3.4.0; `$result = Invoke-Pester -Script '$escapedPesterPath' -PassThru; if (`$result.FailedCount -ne 0) { exit 1 } }"
$pester = Invoke-MergedNativeCommand -FilePath (Get-Task6CompatiblePowerShellHost) -ArgumentList @(
    '-NoProfile', '-ExecutionPolicy', 'Bypass', '-Command', $pesterCommand
) -ConsolePath $pesterConsole
if ($pester.ExitCode -ne 0) { throw "Task 6 Pester failed: $($pester.ExitCode)" }
Assert-NonEmptyFiles -Paths @($pesterConsole, $pesterEvidence) -Label 'Pester log'
Assert-OneExactMarker -Path $pesterEvidence -Marker 'TASK6_PESTER_CONTRACT_OK canonical=1 mutations=15 host_probes=2 human_gate=open' `
    -Label 'Pester terminal marker'
Assert-OneRegexMarker -Path $pesterEvidence `
    -Pattern '^TASK6_SOURCE_MUTATION_ROOT_OK root=.+ outside_project=True$' `
    -Label 'Pester external source-mutation root marker'
Assert-OneRegexMarker -Path $pesterEvidence `
    -Pattern '^TASK6_BASE_EXTERNAL_EVIDENCE_CONSUMED manifest=.+ manifest_sha256=[A-Fa-f0-9]{64} not_before_utc=.+$' `
    -Label 'Pester explicit BASE external-evidence manifest marker'
foreach ($mutation in @('mandatory-dash-stale-action', 'mandatory-dash-stale-action-human-boundary')) {
    Assert-OneRegexMarker -Path $pesterEvidence `
        -Pattern ('^TASK6_SOURCE_MUTATION_MATERIALIZATION_OK name=' + [regex]::Escape($mutation) +
            ' root=.+ candidate_runtime_sha256=[A-Fa-f0-9]{64} materialized_tree_sha256=[A-Fa-f0-9]{64}$') `
        -Label "Pester external source-mutation materialization $mutation"
}
foreach ($mutation in @(
    'terrain-marker-delete', 'task11-input-marker-delete', 'task11-input-mouse-false',
    'micro-scene-count', 'claw-role-count', 'mandatory-dash-stale-action', 'second-boost-reject',
    'base-presentation-order', 'boss-hammer-throw-delete', 'thread-death-cleanup-delete',
    'budget-exceeded', 'fatal-error-leak', 'avi-header-tamper', 'manifest-hash-tamper',
    'verify-terminal-duplicate')) {
    Assert-OneExactMarker -Path $pesterEvidence -Marker "TASK6_MUTATION_RED_OBSERVED name=$mutation" `
        -Label "Pester RED mutation $mutation"
}
Assert-OneRegexMarker -Path $pesterEvidence `
    -Pattern '^TASK6_FROZEN_MOVIE_RED_OBSERVED name=avi-frozen-title-tail frames=\d+,\d+ command=actual_child_extractor nonzero=True canonical_manifest_sha256=[A-Fa-f0-9]{64} canonical_candidate_sha256=[A-Fa-f0-9]{64}$' `
    -Label 'Pester decoded frozen-movie RED evidence'

# The historical terrain suite deliberately aggregates fifteen independent risk categories.
# Exact full/boss AVI count is an additional two real child-verifier mutations, kept in its own
# bounded streaming test so it can structurally alter header/index/movi/manifest bytes rather
# than asserting an in-memory source substring.
$exactPesterConsole = Join-Path $VerificationRoot 'pester-exact-frame.console.log'
$exactPesterEvidence = Join-Path $VerificationRoot 'pester-exact-frame-evidence.log'
$exactPesterMutationRoot = Join-Path $VerificationRoot 'pester-exact-frame-mutations'
$exactPesterPath = Join-Path $project 'tests\powershell\Task6ExactFrameContract.Tests.ps1'
$escapedExactPesterPath = $exactPesterPath.Replace("'", "''")
$escapedExactPesterEvidence = $exactPesterEvidence.Replace("'", "''")
$escapedExactPesterMutationRoot = $exactPesterMutationRoot.Replace("'", "''")
$escapedExactCaptureRoot = $CaptureRoot.Replace("'", "''")
$escapedExactProject = $project.Replace("'", "''")
$exactPesterCommand = "& { `$p = @{ Path = '$escapedExactPesterPath'; Parameters = @{ CaptureRoot = '$escapedExactCaptureRoot'; RedRoot = '$escapedExactPesterMutationRoot'; CandidateRoot = '$escapedExactProject'; EvidenceLog = '$escapedExactPesterEvidence' } }; Import-Module Pester -MinimumVersion 3.4.0; `$result = Invoke-Pester -Script `$p -PassThru; if (`$result.FailedCount -ne 0) { exit 1 } }"
$exactPester = Invoke-MergedNativeCommand -FilePath (Get-Task6CompatiblePowerShellHost) -ArgumentList @(
    '-NoProfile', '-ExecutionPolicy', 'Bypass', '-Command', $exactPesterCommand
) -ConsolePath $exactPesterConsole
if ($exactPester.ExitCode -ne 0) { throw "Task 6 exact-frame Pester failed: $($exactPester.ExitCode)" }
Assert-NonEmptyFiles -Paths @($exactPesterConsole, $exactPesterEvidence) -Label 'exact-frame Pester log'
Assert-OneExactMarker -Path $exactPesterEvidence `
    -Marker 'TASK6_EXACT_FRAME_CONTRACT_OK mutations=2 full_expected=4053 boss_expected=2719' `
    -Label 'exact-frame Pester terminal marker'
foreach ($mutation in @('avi-exact-frame-full-plus-one', 'avi-exact-frame-boss-minus-one')) {
    Assert-OneExactMarker -Path $exactPesterEvidence -Marker "TASK6_MUTATION_RED_OBSERVED name=$mutation" `
        -Label "exact-frame Pester RED mutation $mutation"
}
'TASK6_FORMAL_MUTATION_LEDGER_OK terrain_mutations=15 exact_frame_mutations=2 total_mutations=17'

$criticalLog = Join-Path $VerificationRoot 'critical-path.log'
$criticalConsole = Join-Path $VerificationRoot 'critical-path.console.log'
$critical = Invoke-MergedNativeCommand -FilePath $godot -ArgumentList @(
    '--path', $project, '--fixed-fps', '60', '--display-driver', 'windows',
    '--rendering-method', 'gl_compatibility', '--audio-driver', 'Dummy',
    '--script', 'res://tests/smoke/PrologueCriticalPathSmokeTask6.cs', '--log-file', $criticalLog
) -ConsolePath $criticalConsole
if ($critical.ExitCode -ne 0) { throw "Task 6 critical path failed: $($critical.ExitCode)" }
Assert-NonEmptyFiles -Paths @($criticalLog, $criticalConsole) -Label 'critical path logs'
Assert-NoEngineLogErrors -Paths @($criticalLog, $criticalConsole)
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
    'PROLOGUE_CRITICAL_PATH_OK')) {
    Assert-OneExactMarker -Path $criticalLog -Marker $marker -Label 'critical marker'
}
Assert-OneRegexMarker -Path $criticalLog `
    -Pattern '^PROLOGUE_FRAME_BUDGET_OK used=\d+ total=12000 headroom_percent=(?:1\d|[2-9]\d)\.\d{2}\s*$' `
    -Label 'critical headroom marker'

$captureEvidence = Assert-Task6CaptureEvidence -Root $CaptureRoot
$captureManifest = $captureEvidence.Manifest
$captureValues = $captureEvidence.Values
$capturedRuntimeCandidate = $captureEvidence.Candidate
if ($capturedRuntimeCandidate -cne $runtimeCandidateBefore) {
    throw "capture runtime candidate mismatch: capture=$capturedRuntimeCandidate verifier=$runtimeCandidateBefore"
}
$fullAvi = Get-CaptureValue $captureValues 'full_flow_avi'
$bossAvi = Get-CaptureValue $captureValues 'boss_retry_avi'
$fullKeyframes = Get-CaptureValue $captureValues 'full_flow_keyframe_manifest'
$reticleKeyframes = Get-CaptureValue $captureValues 'full_flow_reticle_keyframe_manifest'
$bossKeyframes = Get-CaptureValue $captureValues 'boss_retry_keyframe_manifest'
$fullLog = $captureEvidence.Bound['full_flow_log']
$fullConsole = $captureEvidence.Bound['full_flow_console']
$bossLog = $captureEvidence.Bound['boss_retry_log']
$bossConsole = $captureEvidence.Bound['boss_retry_console']
$fullStages = @(Get-Content -LiteralPath $fullLog | Where-Object { $_ -match '^PROLOGUE_PRESENTATION_STAGE=' })
    if ($fullStages.Count -ne 30) { throw "full-flow stage count must be 30; actual=$($fullStages.Count)" }
$reticleStages = @(Get-Content -LiteralPath $fullLog | Where-Object { $_ -match '^PROLOGUE_RETICLE_DWELL=(?:Attack|Grippable|Ice|Forbidden|Obstructed|OutOfRange) frame=\d+\s*$' })
if ($reticleStages.Count -ne 6) { throw "full-flow reticle dwell count must be 6; actual=$($reticleStages.Count)" }
foreach ($state in @('Attack', 'Grippable', 'Ice', 'Forbidden', 'Obstructed', 'OutOfRange')) {
    Assert-OneRegexMarker -Path $fullLog -Pattern ('^PROLOGUE_RETICLE_DWELL=' + $state + ' frame=\d+\s*$') `
        -Label "full-flow reticle dwell $state"
}
$fullPngs = @(Get-ChildItem -LiteralPath (Split-Path -Parent $fullKeyframes) -Filter '*.png' -File)
$reticlePngs = @(Get-ChildItem -LiteralPath (Split-Path -Parent $reticleKeyframes) -Filter '*.png' -File)
$bossPngs = @(Get-ChildItem -LiteralPath (Split-Path -Parent $bossKeyframes) -Filter '*.png' -File)
    # The full-flow bundle includes 30 named stages plus preceding/following frames for each of
    # four transient boost states.  Reticle/Boss bundles have only their named proof frames.
    if ($fullPngs.Count -ne 38 -or $reticlePngs.Count -ne 6 -or $bossPngs.Count -ne 2) {
    throw "keyframe counts fail: full=$($fullPngs.Count) reticle=$($reticlePngs.Count) boss=$($bossPngs.Count)"
}
if (@($fullPngs | ForEach-Object { Get-EvidenceSha256 -LiteralPath $_.FullName } | Sort-Object -Unique).Count -lt 2) {
    throw 'full-flow keyframes are frozen'
}
if (@($reticlePngs | ForEach-Object { Get-EvidenceSha256 -LiteralPath $_.FullName } | Sort-Object -Unique).Count -ne 6) {
    throw 'reticle keyframes are frozen or missing distinct states'
}

$runtimeCandidateAfter = Get-Task6Fingerprint ($task6RuntimeSources + $scenePaths)
if ($runtimeCandidateAfter -cne $runtimeCandidateBefore) {
    throw "Task 6 runtime candidate drifted during verifier: before=$runtimeCandidateBefore after=$runtimeCandidateAfter"
}
$candidateAfter = Get-Task6Fingerprint ($task6EvidenceSources + $scenePaths)
if ($candidateAfter -cne $candidateBefore) {
    throw "Task 6 candidate drifted during verifier: before=$candidateBefore after=$candidateAfter"
}
$summary = Join-Path $VerificationRoot 'verify-summary.txt'
[IO.File]::WriteAllText($summary, (@(
        "base_head=$($captureEvidence.BaseHead)",
    "candidate_sha256=$candidateBefore", "runtime_candidate_sha256=$runtimeCandidateBefore",
    "capture_runtime_candidate_sha256=$capturedRuntimeCandidate", "capture_root=$CaptureRoot",
    "capture_manifest_sha256=$(Get-EvidenceSha256 -LiteralPath $captureManifest)",
    "scene_sha256=$sceneHashAfter", "builder_byte_drift=$builderDrift",
    'build_warnings=0', 'build_errors=0', 'critical_markers=19', 'capture_stages=30', 'capture_reticle_stages=6',
    'completion_display_post_draw=True',
    'human_acceptance=open'
) -join "`n") + "`n", [Text.UTF8Encoding]::new($false))
$summaryHash = Get-EvidenceSha256 -LiteralPath $summary
"PROLOGUE_VERIFY_OK root=$VerificationRoot capture=$CaptureRoot candidate_sha256=$candidateBefore build_warnings=0 build_errors=0 scene_sha256=$sceneHashAfter summary_sha256=$summaryHash"
