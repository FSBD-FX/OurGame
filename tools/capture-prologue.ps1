[CmdletBinding()]
param(
    [string]$CaptureRoot,
    [string]$AuditRoot,
    [switch]$SkipBuild,
    [switch]$HostProbeOnly,
    [switch]$RuntimeFingerprintProbeOnly,
    # Executes every runtime/audit step before AVI recording and then stops. This keeps the
    # materialization/input/audit entry path independently certifiable before a long capture.
    [switch]$PreRecordProbeOnly
)

$ErrorActionPreference = 'Stop'
$project = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
. (Join-Path $PSScriptRoot 'EvidenceChecks.ps1')
. (Join-Path $PSScriptRoot 'Task6AviStreaming.ps1')
. (Join-Path $PSScriptRoot 'Task6ImportRetry.ps1')
$godot = 'G:\godot\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64_console.exe'
$dotnetRoot = 'G:\godot\dotnet-sdk'
$env:DOTNET_ROOT = $dotnetRoot
$env:DOTNET_ROOT_X64 = $dotnetRoot
$env:DOTNET_ROLL_FORWARD = 'LatestMajor'
$env:DOTNET_CLI_UI_LANGUAGE = 'en'

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
    throw 'No compatible PowerShell child host is available.'
}

if ($HostProbeOnly) {
    $resolvedHost = Get-Task6CompatiblePowerShellHost
    "TASK6_CAPTURE_HOST_OK ps_major=$($PSVersionTable.PSVersion.Major) child_host=$([IO.Path]::GetFileName($resolvedHost))"
    return
}

if (-not $RuntimeFingerprintProbeOnly -and [string]::IsNullOrWhiteSpace($CaptureRoot)) {
    $CaptureRoot = Join-Path $project ('artifacts\task6-capture-' +
        (Get-Date -Format 'yyyyMMddTHHmmssfffZ') + '-' + [guid]::NewGuid().ToString('N'))
}
if (-not $RuntimeFingerprintProbeOnly) {
    if (Test-Path -LiteralPath $CaptureRoot) {
        throw "Task 6 capture root must be fresh; refusing to reuse or overwrite: $CaptureRoot"
    }
    New-Item -ItemType Directory -Path $CaptureRoot -Force | Out-Null
}

# Runtime candidate identity deliberately excludes post-capture human-gate documentation:
# the capture writes the hashes that the validation document subsequently records.  It binds
# every executable/test/capture/verifier source and every generated traversal scene instead.
function Get-Task6Fingerprint {
    param([string[]]$RelativePaths)
    $orderedRelativePaths = @($RelativePaths)
    [Array]::Sort($orderedRelativePaths, [StringComparer]::Ordinal)
    $lines = foreach ($relative in $orderedRelativePaths) {
        $path = Join-Path $project $relative
        if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
            throw "runtime candidate source is missing: $relative"
        }
        "$relative=$(Get-EvidenceSha256 -LiteralPath $path)"
    }
    $bytes = [Text.Encoding]::UTF8.GetBytes((($lines -join "`n") + "`n"))
    $algorithm = [Security.Cryptography.SHA256]::Create()
    try { return ([BitConverter]::ToString($algorithm.ComputeHash($bytes)) -replace '-', '') }
    finally { $algorithm.Dispose() }
}

$task6RuntimeSources = @(
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
$task6SceneSources = @(
    'scenes\prologue\sectors\CanyonEntrance.tscn',
    'scenes\prologue\sectors\CanyonDescent.tscn',
    'scenes\prologue\sectors\CanyonBasin.tscn',
    'scenes\prologue\sectors\CanyonAscent.tscn',
    'scenes\prologue\sectors\LostTemple.tscn',
    'scenes\prologue\PrologueLevel.tscn'
)
$runtimeCandidateBefore = Get-Task6Fingerprint ($task6RuntimeSources + $task6SceneSources)
if ($RuntimeFingerprintProbeOnly) {
    "TASK6_CAPTURE_RUNTIME_FINGERPRINT_OK ps_major=$($PSVersionTable.PSVersion.Major) " +
        "candidate_runtime_sha256=$runtimeCandidateBefore files=$($task6RuntimeSources.Count + $task6SceneSources.Count)"
    return
}

function Assert-OneRegexMarker {
    param([string]$Text, [string]$Pattern, [string]$Label)
    $markerMatches = @([regex]::Matches($Text, $Pattern, [Text.RegularExpressions.RegexOptions]::Multiline))
    if ($markerMatches.Count -ne 1) {
        throw "$Label marker must occur exactly once: pattern=$Pattern actual=$($markerMatches.Count)"
    }
    return $markerMatches[0]
}

function Get-Task6AviMainHeader {
    param([string]$Path)
    return (Get-Task6AviMjpegIndex -Path $Path).Header
}

function Assert-WindowMovieContract {
    param([string]$Path, [uint32]$ExpectedFrames, [string]$Label)
    return (Assert-Task6FormalAviMovieContract -Path $Path -ExpectedFrames $ExpectedFrames -Label $Label).Header
}

function Assert-MovieIsNotFrozen {
    param([string]$KeyframeDirectory, [int]$MinimumDistinctFrames, [string]$Label)
    $frames = @(Get-ChildItem -LiteralPath $KeyframeDirectory -Filter '*.png' -File | Sort-Object Name)
    if ($frames.Count -lt $MinimumDistinctFrames) {
        throw "$Label lacks enough extracted keyframes for a frozen-movie gate"
    }
    $hashes = @($frames | ForEach-Object { Get-EvidenceSha256 -LiteralPath $_.FullName } |
        Sort-Object -Unique)
    if ($hashes.Count -lt $MinimumDistinctFrames) {
        throw "$Label keyframes are frozen or reused: distinct=$($hashes.Count) required=$MinimumDistinctFrames"
    }
    Add-Type -AssemblyName System.Drawing
    $visualSignatures = [Collections.Generic.List[string]]::new()
    foreach ($frame in $frames) {
        $image = [Drawing.Bitmap]::new($frame.FullName)
        try {
            if ($image.Width -ne 1280 -or $image.Height -ne 720) {
                throw "$Label keyframe is not 1280x720: $($frame.FullName)"
            }
            # A static movie can have distinct container bytes. Sample a distributed image grid,
            # rather than four arbitrary pixels or a per-frame color-count heuristic: a valid
            # dark/flat end card is allowed, but two visually identical proof frames are not.
            $signature = [Text.StringBuilder]::new()
            for ($y = 10; $y -lt $image.Height; $y += 20) {
                for ($x = 10; $x -lt $image.Width; $x += 20) {
                    [void]$signature.Append($image.GetPixel($x, $y).ToArgb()).Append(',')
                }
            }
            $visualSignatures.Add($signature.ToString())
        }
        finally { $image.Dispose() }
    }
    if (@($visualSignatures | Sort-Object -Unique).Count -lt $MinimumDistinctFrames) {
        throw "$Label keyframes are frozen at distributed samples: distinct=$(@($visualSignatures | Sort-Object -Unique).Count) required=$MinimumDistinctFrames"
    }
}

function Invoke-Task6KeyframeExtraction {
    param(
        [string]$Name,
        [string]$Avi,
        [string]$Log,
        [string]$StagePrefix,
        [string[]]$StageBeats,
        [DateTime]$StartedUtc,
        [int]$MinimumDistinctFrames
    )

    $keyframes = Join-Path $CaptureRoot ($Name + '-keyframes')
    $extractLog = Join-Path $CaptureRoot ($Name + '-keyframes.log')
    $childHost = Get-Task6CompatiblePowerShellHost
    $extractOutput = & $childHost -NoProfile -ExecutionPolicy Bypass -File `
        (Join-Path $project 'tools\extract-prologue-keyframes.ps1') `
        -AviPath $avi -StageLogPath $log -OutputPath $keyframes -StagePrefix $StagePrefix `
        -BeatsCsv ($StageBeats -join ',') 2>&1
    $extractExit = $LASTEXITCODE
    $extractOutput | Set-Content -LiteralPath $extractLog -Encoding UTF8
    if ($extractExit -ne 0) {
        throw "$Name keyframe extraction failed with exit code ${extractExit}: $($extractOutput -join "`n")"
    }
    # Four instantaneous boost states carry their real preceding/following AVI frames in
    # addition to their center proof frame.  Those neighbours are decoded and compared by the
    # extractor/verifier; they are not paused-frame dwell padding.
    $motionBeatCount = @($StageBeats | Where-Object {
        $_ -in @('boost_start', 'boost_arc', 'boost_second_rejected', 'boost_land')
    }).Count
    $expectedCount = $StageBeats.Count + (2 * $motionBeatCount)
    $pngs = @(Get-ChildItem -LiteralPath $keyframes -Filter '*.png' -File | Sort-Object Name)
    $null = Assert-PngCaptureContract -Paths @($pngs | ForEach-Object { $_.FullName }) `
        -ExpectedCount $expectedCount -ExpectedWidth 1280 -ExpectedHeight 720 -Label $Name
    Assert-MovieIsNotFrozen -KeyframeDirectory $keyframes -MinimumDistinctFrames $MinimumDistinctFrames -Label $Name
    $keyframeManifest = Join-Path $keyframes 'manifest.txt'
    Assert-FreshNonEmptyOutputs -Paths @($extractLog, $keyframeManifest) -StartedUtc $StartedUtc
    return [pscustomobject]@{
        Keyframes = $keyframes; KeyframeManifest = $keyframeManifest; ExtractLog = $extractLog
    }
}

function Invoke-Task6Capture {
    param(
        [string]$Name,
        [string]$Script,
        [string[]]$RequiredMarkers,
        [string[]]$StageBeats,
        [string]$StagePrefix,
        [uint32]$ExpectedFrames,
        [int]$MinimumRawTailFrames = 30
    )

    $rawAvi = Join-Path $CaptureRoot ($Name + '.raw.avi')
    $avi = Join-Path $CaptureRoot ($Name + '.avi')
    $normalizationLog = Join-Path $CaptureRoot ($Name + '-canonical-prefix.log')
    $log = Join-Path $CaptureRoot ($Name + '.log')
    $console = Join-Path $CaptureRoot ($Name + '.console.log')
    $started = [DateTime]::UtcNow
    $result = Invoke-MergedNativeCommand -FilePath $godot -ArgumentList @(
        '--path', $project, '--fixed-fps', '60', '--display-driver', 'windows',
        '--rendering-method', 'gl_compatibility', '--audio-driver', 'Dummy',
        '--write-movie', $rawAvi, '--script', $Script, '--log-file', $log
    ) -ConsolePath $console
    if ($result.ExitCode -ne 0) {
        throw "$Name display-backed MJPEG capture failed with exit code $($result.ExitCode)"
    }
    Assert-FreshNonEmptyOutputs -Paths @($rawAvi, $log, $console) -StartedUtc $started
    Assert-NoEngineLogErrors -Paths @($log, $console)
    # Godot deliberately mirrors the same marker stream to --log-file and stdout. Exactness is
    # measured in the engine log, while the console is separately scanned for failures above.
    $text = Get-Content -LiteralPath $log -Raw
    foreach ($marker in $RequiredMarkers) {
        Assert-OneRegexMarker -Text $text -Pattern ('^' + [regex]::Escape($marker) + '$') -Label "$Name/$marker" | Out-Null
    }
    $stageLines = @(Get-Content -LiteralPath $log)
    $previousStageIndex = -1
    foreach ($beat in $StageBeats) {
        Assert-OneRegexMarker -Text $text -Pattern ('^' + [regex]::Escape($StagePrefix + '=' + $beat) +
            ' frame=\d+\s*$') -Label "$Name/stage-$beat" | Out-Null
        $indices = @()
        for ($index = 0; $index -lt $stageLines.Count; $index++) {
            if ($stageLines[$index] -match ('^' + [regex]::Escape($StagePrefix + '=' + $beat) + ' frame=\d+\s*$')) {
                $indices += $index
            }
        }
        if ($indices.Count -ne 1 -or $indices[0] -le $previousStageIndex) {
            throw "$Name ordered stage contract fails at $beat; actual_indices=$($indices -join ',') previous=$previousStageIndex"
        }
        $previousStageIndex = $indices[0]
    }
    $normalization = New-Task6CanonicalAviPrefix -SourcePath $rawAvi -Destination $avi `
        -ExpectedFrames $ExpectedFrames -MinimumRawTailFrames $MinimumRawTailFrames -Label $Name
    $normalization = Assert-Task6CanonicalAviPrefix -RawPath $rawAvi -CanonicalPath $avi `
        -ExpectedFrames $ExpectedFrames -MinimumRawTailFrames $MinimumRawTailFrames -Label $Name
    $normalizationMarker = "TASK6_CANONICAL_AVI_PREFIX_OK label=$Name raw_frames=$($normalization.RawFrames) " +
        "canonical_frames=$($normalization.CanonicalFrames) trimmed_frames=$($normalization.TrimmedFrames) " +
        "prefix_payload_sha256=$($normalization.PrefixPayloadSha256) " +
        "first_frame_sha256=$($normalization.FirstFrameSha256) last_frame_sha256=$($normalization.LastFrameSha256)"
    [IO.File]::WriteAllText($normalizationLog, ($normalizationMarker + "`n"), [Text.UTF8Encoding]::new($false))
    Assert-FreshNonEmptyOutputs -Paths @($avi, $normalizationLog) -StartedUtc $started
    Assert-ExactLogMarker -Path $normalizationLog -Marker $normalizationMarker
    $rawProducer = $null
    if ($Name -ceq 'full-flow') {
        $rawProducer = Assert-Task6RawProducerFrameContract -LogPath $log -RawPath $rawAvi `
            -ExpectedCanonicalFrames $ExpectedFrames -MinimumRawTailFrames $MinimumRawTailFrames -Label $Name
    }
    $header = Assert-WindowMovieContract -Path $avi -ExpectedFrames $ExpectedFrames -Label $Name
    $minimumDistinct = if ($Name -ceq 'full-flow') { 8 } else { [Math]::Min(2, $StageBeats.Count) }
    $keyframeEvidence = Invoke-Task6KeyframeExtraction -Name $Name -Avi $avi -Log $log `
        -StagePrefix $StagePrefix -StageBeats $StageBeats -StartedUtc $started `
        -MinimumDistinctFrames $minimumDistinct
    return [pscustomobject]@{
        Name = $Name; RawAvi = $rawAvi; Avi = $avi; NormalizationLog = $normalizationLog;
        Normalization = $normalization; RawProducer = $rawProducer; Log = $log; Console = $console;
        Keyframes = $keyframeEvidence.Keyframes;
        KeyframeManifest = $keyframeEvidence.KeyframeManifest; Header = $header; StartedUtc = $started;
    }
}

function Assert-Task6CompletionDisplayBinding {
    param(
        [Parameter(Mandatory)][string]$LogPath,
        [Parameter(Mandatory)][object]$StageFrames
    )
    # Completion is only valid when the AVI frame that names it was observed after the renderer
    # presented the formal completion surface and removed the Boss HUD.  The live observer
    # records the one-frame movie-writer coordinate conversion explicitly, then capture decodes
    # that exact named AVI image below.
    $text = Get-Content -LiteralPath $LogPath -Raw
    $marker = Assert-OneRegexMarker -Text $text `
        -Pattern '^TASK6_COMPLETION_DISPLAY_OK frame=(?<frame>\d+) completion_visible=True boss_visible=False rendered_post_draw=True movie_frame_offset=1$' `
        -Label 'full-flow/completion-display'
    [long]$frame = [long]$marker.Groups['frame'].Value
    if ($frame -ne [long]$StageFrames['completion']) {
        throw "full-flow completion display frame does not bind the completion stage: display=$frame stage=$($StageFrames['completion'])"
    }
    return $frame
}

function Assert-Task6ModeHudDisplayBinding {
    param(
        [Parameter(Mandatory)][string]$LogPath,
        [Parameter(Mandatory)][object]$StageFrames
    )
    $text = Get-Content -LiteralPath $LogPath -Raw
    $marker = Assert-OneRegexMarker -Text $text `
        -Pattern '^TASK6_MODE_HUD_DISPLAY_OK frame=(?<frame>\d+) formal_hud_visible=True rendered_post_draw=True movie_frame_offset=1$' `
        -Label 'full-flow/mode-hud-display'
    [long]$frame = [long]$marker.Groups['frame'].Value
    if ($frame -ne [long]$StageFrames['mode_hud']) {
        throw "full-flow mode HUD display frame does not bind the mode_hud stage: display=$frame stage=$($StageFrames['mode_hud'])"
    }
    return $frame
}

function Assert-Task6BoostDisplayBindings {
    param(
        [Parameter(Mandatory)][string]$LogPath,
        [Parameter(Mandatory)][object]$StageFrames
    )
    # Transient physics states are named only at FramePostDraw.  Check the exact state schema
    # independently of stage order so a stale tick, renamed stage, or duplicate visual marker
    # cannot select a neighbouring AVI image.
    $expected = @(
        [pscustomobject]@{ Stage = 'boost_attached'; State = 'Swing'; Consumed = 'False'; Rejected = 'False'; Offset = '1' },
        [pscustomobject]@{ Stage = 'boost_start'; State = 'Boost'; Consumed = 'True'; Rejected = 'False'; Offset = '1' },
        [pscustomobject]@{ Stage = 'attached_boost_consumed'; State = 'Reel'; Consumed = 'True'; Rejected = 'False'; Offset = '1' },
        [pscustomobject]@{ Stage = 'boost_arc'; State = 'Reel'; Consumed = 'True'; Rejected = 'False'; Offset = '1' },
        [pscustomobject]@{ Stage = 'boost_second_rejected'; State = 'Reel'; Consumed = 'True'; Rejected = 'True'; Offset = '2' },
        [pscustomobject]@{ Stage = 'boost_land'; State = 'Idle'; Consumed = 'False'; Rejected = 'False'; Offset = '1' }
    )
    $text = Get-Content -LiteralPath $LogPath -Raw
    $frames = [ordered]@{}
    [long]$cast = 0L
    foreach ($item in $expected) {
        $pattern = '^TASK6_BOOST_DISPLAY_OK stage=' + [regex]::Escape($item.Stage) +
            ' frame=(?<frame>\d+) state=' + [regex]::Escape($item.State) +
            ' cast=(?<cast>\d+) boost_consumed=' + $item.Consumed +
            ' second_rejected=' + $item.Rejected +
            ' rendered_post_draw=True movie_frame_offset=' + $item.Offset + '$'
        $marker = Assert-OneRegexMarker -Text $text -Pattern $pattern -Label "full-flow/$($item.Stage)-display"
        [long]$frame = [long]$marker.Groups['frame'].Value
        if ($frame -ne [long]$StageFrames[$item.Stage]) {
            throw "full-flow $($item.Stage) display frame does not bind its stage: display=$frame stage=$($StageFrames[$item.Stage])"
        }
        [long]$markerCast = [long]$marker.Groups['cast'].Value
        if ($markerCast -le 0L) { throw "full-flow $($item.Stage) display marker has no retained cast id" }
        if ($cast -eq 0L) { $cast = $markerCast }
        elseif ($markerCast -ne $cast) { throw "full-flow boost display markers disagree on cast id: expected=$cast actual=$markerCast stage=$($item.Stage)" }
        $frames[$item.Stage] = $frame
    }
    return [pscustomobject]@{ Frames = $frames; Cast = $cast }
}

function Assert-Task6BossAttackDisplayBindings {
    param(
        [Parameter(Mandatory)][string]$LogPath,
        [Parameter(Mandatory)][object]$StageFrames
    )
    $expected = @(
        [pscustomobject]@{ Attack = 'Sweep'; Stage = 'boss_sweep'; Pose = 'boss_sweep_windup' },
        [pscustomobject]@{ Attack = 'LeapSlam'; Stage = 'boss_leap_slam'; Pose = 'boss_leap_crouch' },
        [pscustomobject]@{ Attack = 'GroundWave'; Stage = 'boss_ground_wave'; Pose = 'boss_groundwave_raise' },
        [pscustomobject]@{ Attack = 'HammerThrow'; Stage = 'boss_hammer_throw'; Pose = 'boss_hammer_windup' }
    )
    $text = Get-Content -LiteralPath $LogPath -Raw
    $frames = [ordered]@{}
    foreach ($item in $expected) {
        $pattern = '^TASK6_BOSS_ATTACK_DISPLAY_OK attack=' + [regex]::Escape($item.Attack) +
            ' stage=' + [regex]::Escape($item.Stage) + ' frame=(?<frame>\d+) formal_pose=' +
            [regex]::Escape($item.Pose) + ' telegraph=True rendered_post_draw=True movie_frame_offset=1$'
        $marker = Assert-OneRegexMarker -Text $text -Pattern $pattern -Label "full-flow/$($item.Stage)-display"
        [long]$frame = [long]$marker.Groups['frame'].Value
        if ($frame -ne [long]$StageFrames[$item.Stage]) {
            throw "full-flow $($item.Stage) display frame does not bind its stage: display=$frame stage=$($StageFrames[$item.Stage])"
        }
        $frames[$item.Stage] = $frame
    }
    return $frames
}

function Get-Task6BoundKeyframe {
    param(
        [Parameter(Mandatory)][string]$ManifestPath,
        [Parameter(Mandatory)][string]$KeyframeRoot,
        [Parameter(Mandatory)][string]$Stage,
        [Parameter(Mandatory)][long]$ExpectedFrame,
        [Parameter(Mandatory)][string]$Label
    )
    $text = Get-Content -LiteralPath $ManifestPath -Raw
    $pattern = '^' + [regex]::Escape($Stage) +
        # EvidenceChecks emits lowercase SHA-256 hex.  Hex case has no semantic meaning, but
        # rejecting the extractor's own streamed manifest would make a completed AVI fail for
        # a parser-only reason rather than an evidence defect.
        '\|avi_frame=(?<frame>\d+)\|png=(?<png>[^|\\/]+\.png)\|sha256=(?<hash>[A-Fa-f0-9]{64})$'
    $marker = Assert-OneRegexMarker -Text $text -Pattern $pattern -Label $Label
    [long]$frame = [long]$marker.Groups['frame'].Value
    if ($frame -ne $ExpectedFrame) {
        throw "$Label keyframe does not bind the post-draw stage frame: keyframe=$frame display=$ExpectedFrame"
    }
    $path = Join-Path $KeyframeRoot $marker.Groups['png'].Value
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { throw "$Label keyframe PNG is missing: $path" }
    $actualHash = Get-EvidenceSha256 -LiteralPath $path
    if ($actualHash -ine $marker.Groups['hash'].Value) {
        throw "$Label keyframe PNG hash does not bind manifest: expected=$($marker.Groups['hash'].Value) actual=$actualHash"
    }
    return [pscustomobject]@{ Frame = $frame; Path = $path; Sha256 = $actualHash }
}

function Invoke-Task6RuntimeLog {
    param([string]$Name, [string]$Script, [string[]]$RequiredMarkers)
    $log = Join-Path $CaptureRoot ($Name + '.log')
    $console = Join-Path $CaptureRoot ($Name + '.console.log')
    $started = [DateTime]::UtcNow
    $result = Invoke-MergedNativeCommand -FilePath $godot -ArgumentList @(
        '--path', $project, '--fixed-fps', '60', '--display-driver', 'windows',
        '--rendering-method', 'gl_compatibility', '--audio-driver', 'Dummy',
        '--script', $Script, '--log-file', $log
    ) -ConsolePath $console
    if ($result.ExitCode -ne 0) { throw "$Name runtime evidence failed with exit code $($result.ExitCode)" }
    Assert-FreshNonEmptyOutputs -Paths @($log, $console) -StartedUtc $started
    Assert-NoEngineLogErrors -Paths @($log, $console)
    foreach ($marker in $RequiredMarkers) {
        $text = Get-Content -LiteralPath $log -Raw
        Assert-OneRegexMarker -Text $text -Pattern ('^' + [regex]::Escape($marker) + '$') `
            -Label "$Name/$marker" | Out-Null
    }
    return [pscustomobject]@{ Log = $log; Console = $console }
}

function Invoke-Task6MaterializationPreflight {
    $log = Join-Path $CaptureRoot 'task11-materialization.log'
    $console = Join-Path $CaptureRoot 'task11-materialization.console.log'
    $runRoot = Join-Path $CaptureRoot 'task11-materialization-run'
    $childHost = Get-Task6CompatiblePowerShellHost
    $started = [DateTime]::UtcNow
    $result = Invoke-MergedNativeCommand -FilePath $childHost -ArgumentList @(
        '-NoProfile', '-ExecutionPolicy', 'Bypass', '-File',
        (Join-Path $project 'tools\verify-prologue.ps1'), '-ProjectRoot', $project,
        '-MaterializationPreflightOnly', '-VerificationRoot', $runRoot
    ) -ConsolePath $console
    if ($result.ExitCode -ne 0) { throw "Task11 materialization preflight failed with exit code $($result.ExitCode)" }
    [IO.File]::WriteAllText($log, (($result.Lines -join "`n") + "`n"), [Text.UTF8Encoding]::new($false))
    Assert-FreshNonEmptyOutputs -Paths @($log, $console) -StartedUtc $started
    Assert-ExactLogMarker -Path $log `
        -Marker 'TASK11_MATERIALIZATION_PRECHECK_OK main=PrologueLevel sectors=5 generated_scenes=25 project_snapshot=1'
    return [pscustomobject]@{ Log = $log; Console = $console }
}

function Invoke-Task6CaptureCleanCandidatePreflight {
    $runRoot = Join-Path $CaptureRoot 'task6-capture-clean-preflight'
    $log = Join-Path $CaptureRoot 'task6-capture-clean-preflight.log'
    $console = Join-Path $CaptureRoot 'task6-capture-clean-preflight.console.log'
    $childHost = Get-Task6CompatiblePowerShellHost
    $started = [DateTime]::UtcNow
    $result = Invoke-MergedNativeCommand -FilePath $childHost -ArgumentList @(
        '-NoProfile', '-ExecutionPolicy', 'Bypass', '-File',
        (Join-Path $project 'tools\verify-prologue.ps1'), '-ProjectRoot', $project,
        '-CleanCandidatePreflightOnly', '-VerificationRoot', $runRoot
    ) -ConsolePath $console
    if ($result.ExitCode -ne 0) {
        throw "Task 6 capture clean-candidate preflight failed with exit code $($result.ExitCode)"
    }
    [IO.File]::WriteAllText($log, (($result.Lines -join "`n") + "`n"), [Text.UTF8Encoding]::new($false))
    Assert-FreshNonEmptyOutputs -Paths @($log, $console) -StartedUtc $started
    Assert-OneRegexMarker -Text (Get-Content -LiteralPath $log -Raw) `
        -Pattern '^TASK6_CLEAN_CANDIDATE_OK branch=codex/[^\s]+ head=[A-Fa-f0-9]{40} dependencies=\d+$' `
        -Label 'capture clean-candidate preflight' | Out-Null
    $stateLog = Join-Path $runRoot 'candidate-clean-before.log'
    if (-not (Test-Path -LiteralPath $stateLog -PathType Leaf)) {
        throw 'Task 6 capture clean-candidate preflight did not persist its restoration state.'
    }
    return [pscustomobject]@{ StateLog = $stateLog; Log = $log; Console = $console; RunRoot = $runRoot }
}

function Invoke-Task6CaptureCleanCandidateRestore {
    param([Parameter(Mandatory)][object]$State)

    $runRoot = Join-Path $CaptureRoot 'task6-capture-cleanup'
    $log = Join-Path $CaptureRoot 'task6-capture-cleanup.log'
    $childHost = Get-Task6CompatiblePowerShellHost
    $started = [DateTime]::UtcNow
    $result = Invoke-MergedNativeCommand -FilePath $childHost -ArgumentList @(
        '-NoProfile', '-ExecutionPolicy', 'Bypass', '-File',
        (Join-Path $project 'tools\verify-prologue.ps1'), '-ProjectRoot', $project,
        '-CleanCandidateRestoreOnly', '-CleanCandidateStateLog', $State.StateLog,
        '-VerificationRoot', $runRoot
    ) -ConsolePath $log
    if ($result.ExitCode -ne 0) {
        throw "Task 6 capture clean-candidate restore failed with exit code $($result.ExitCode)"
    }
    $restoreMarker = Assert-OneRegexMarker -Text (($result.Lines -join "`n")) `
        -Pattern '^TASK6_CLEAN_CANDIDATE_RESTORE_OK restored=(?<restored>\d+) created=(?<created>\d+) cache_deleted=(?<cache>True|False)$' `
        -Label 'capture clean-candidate restore'
    if ($restoreMarker.Groups['cache'].Value -cne 'True') {
        throw 'Task 6 capture clean-candidate restore did not delete the cache created by this run.'
    }
    $afterLog = Join-Path $runRoot 'candidate-clean-after.log'
    Assert-FreshNonEmptyOutputs -Paths @($log, $afterLog) -StartedUtc $started
    $captureMarker = 'TASK6_CAPTURE_CLEANUP_OK restored=' + $restoreMarker.Groups['restored'].Value +
        ' created=' + $restoreMarker.Groups['created'].Value + ' cache_deleted=True status=empty'
    [IO.File]::WriteAllText($log, (($result.Lines + @($captureMarker) -join "`n") + "`n"),
        [Text.UTF8Encoding]::new($false))
    Assert-ExactLogMarker -Path $log -Marker $captureMarker
    return [pscustomobject]@{ Log = $log; AfterLog = $afterLog; Marker = $captureMarker }
}

function Invoke-Task6CaptureMaterialization {
    $materializationRoot = Join-Path $CaptureRoot 'task6-capture-materialization'
    if (Test-Path -LiteralPath $materializationRoot) {
        throw "Task 6 capture materialization root must be fresh: $materializationRoot"
    }
    New-Item -ItemType Directory -Path $materializationRoot -Force | Out-Null
    $started = [DateTime]::UtcNow
    $importPrepare = {
        param([int]$Attempt)
        if (-not $SkipBuild) {
            & (Join-Path $dotnetRoot 'dotnet.exe') build --no-incremental (Join-Path $project 'Knight.csproj')
            if ($LASTEXITCODE -ne 0) {
                throw "Task 6 capture build failed before import attempt=$Attempt with exit code $LASTEXITCODE"
            }
        }
    }.GetNewClosure()
    $importNative = {
        param([int]$Attempt, [string]$LogPath, [string]$ConsolePath)
        Invoke-MergedNativeCommand -FilePath $godot -ArgumentList @(
            '--headless', '--editor', '--path', $project, '--quit-after', '1200', '--log-file', $LogPath
        ) -ConsolePath $ConsolePath
    }.GetNewClosure()
    $importOutputs = @(Invoke-Task6ImportWithBoundedNativeRetry -ProjectRoot $project `
        -EvidenceRoot $materializationRoot -PrepareAttempt $importPrepare -InvokeAttempt $importNative `
        -GodotPath $godot)
    $importObjects = @($importOutputs | Where-Object {
            $null -ne $_ -and $_.PSObject.Properties.Match('FinalLog').Count -eq 1
        })
    if ($importObjects.Count -ne 1) {
        throw "Task 6 capture import did not return one final result object; actual=$($importObjects.Count)"
    }
    $import = $importObjects[0]
    Assert-FreshNonEmptyOutputs -Paths @($import.FinalLog, $import.FinalConsole, $import.Summary) -StartedUtc $started
    Assert-ExactLogMarker -Path $import.FinalLog -Marker 'VERIFICATION_EDITOR_GC_DRAIN_OK'
    Assert-NoEngineLogErrors -Paths @($import.FinalLog, $import.FinalConsole)
    $importTerminalCandidates = @($importOutputs | Where-Object {
            $_ -is [string] -and $_ -match '^PROLOGUE_IMPORT_NATIVE_RETRY_OK attempts=(?:1|2) first_exit=-?\d+ restored=(?:True|False)$'
        })
    if ($importTerminalCandidates.Count -ne 1) {
        throw "Task 6 capture import terminal marker must occur exactly once; actual=$($importTerminalCandidates.Count)"
    }
    $importTerminal = $importTerminalCandidates[0]

    $builders = @(
        @{ Name = 'BuildCanyonEntrance'; Script = 'res://tools/build/prologue/BuildCanyonEntrance.cs' },
        @{ Name = 'BuildCanyonDescent'; Script = 'res://tools/build/prologue/BuildCanyonDescent.cs' },
        @{ Name = 'BuildCanyonBasin'; Script = 'res://tools/build/prologue/BuildCanyonBasin.cs' },
        @{ Name = 'BuildCanyonAscent'; Script = 'res://tools/build/prologue/BuildCanyonAscent.cs' },
        @{ Name = 'BuildLostTemple'; Script = 'res://tools/build/prologue/BuildLostTemple.cs' },
        @{ Name = 'BuildPrologueLevel'; Script = 'res://tools/build/prologue/BuildPrologueLevel.cs' }
    )
    $builderLogs = [Collections.Generic.List[object]]::new()
    for ($index = 0; $index -lt $builders.Count; $index++) {
        $builder = $builders[$index]
        $ordinal = ($index + 1).ToString('00')
        $log = Join-Path $materializationRoot ('builder-' + $ordinal + '-' + $builder.Name + '.log')
        $console = Join-Path $materializationRoot ('builder-' + $ordinal + '-' + $builder.Name + '.console.log')
        $result = Invoke-MergedNativeCommand -FilePath $godot -ArgumentList @(
            '--headless', '--path', $project, '--script', $builder.Script, '--log-file', $log
        ) -ConsolePath $console
        if ($result.ExitCode -ne 0) { throw "Task 6 capture builder failed: $($builder.Name)" }
        Assert-FreshNonEmptyOutputs -Paths @($log, $console) -StartedUtc $started
        Assert-NoEngineLogErrors -Paths @($log, $console)
        $builderLogs.Add([pscustomobject]@{ Ordinal = $ordinal; Name = $builder.Name; Script = $builder.Script; Log = $log; Console = $console })
    }
    $scenePaths = @(
        'scenes\prologue\sectors\CanyonEntrance.tscn',
        'scenes\prologue\sectors\CanyonDescent.tscn',
        'scenes\prologue\sectors\CanyonBasin.tscn',
        'scenes\prologue\sectors\CanyonAscent.tscn',
        'scenes\prologue\sectors\LostTemple.tscn',
        'scenes\prologue\PrologueLevel.tscn'
    )
    $semantics = Get-Task6GeneratedRuntimeSemantics -Root $project -RelativePaths $scenePaths
    if ($semantics.Count -ne 6) {
        throw "Task 6 capture generated runtime semantic count mismatch: expected=6 actual=$($semantics.Count)"
    }
    for ($index = 0; $index -lt $scenePaths.Count; $index++) {
        $expectedPath = $scenePaths[$index].Replace('\', '/')
        if ($semantics.Paths[$index] -cne $expectedPath) {
            throw "Task 6 capture generated runtime semantic order mismatch: expected=$expectedPath actual=$($semantics.Paths[$index])"
        }
    }
    $terminal = 'TASK6_CAPTURE_MATERIALIZATION_OK import_attempts=' + $import.Attempts +
        ' import_first_exit=' + $import.FirstExit + ' import_restored=' + $import.Restored +
        ' builders=6 runtime_semantics_sha256=' + $semantics.Sha256 + ' runtime_semantics_count=6'
    $manifest = Join-Path $CaptureRoot 'task6-capture-materialization.manifest.txt'
    $manifestLines = [Collections.Generic.List[string]]::new()
    $manifestLines.Add('schema=task6-capture-materialization-v1')
    $manifestLines.Add('import_terminal=' + $importTerminal)
    $manifestLines.Add('import_log=' + $import.FinalLog)
    $manifestLines.Add('import_log_sha256=' + (Get-EvidenceSha256 -LiteralPath $import.FinalLog))
    $manifestLines.Add('import_console=' + $import.FinalConsole)
    $manifestLines.Add('import_console_sha256=' + (Get-EvidenceSha256 -LiteralPath $import.FinalConsole))
    $manifestLines.Add('import_summary=' + $import.Summary)
    $manifestLines.Add('import_summary_sha256=' + (Get-EvidenceSha256 -LiteralPath $import.Summary))
    $manifestLines.Add('builder_count=6')
    foreach ($builder in $builderLogs) {
        $manifestLines.Add('builder_' + $builder.Ordinal + '=' + $builder.Name + '|script=' + $builder.Script)
        $manifestLines.Add('builder_' + $builder.Ordinal + '_log=' + $builder.Log)
        $manifestLines.Add('builder_' + $builder.Ordinal + '_log_sha256=' + (Get-EvidenceSha256 -LiteralPath $builder.Log))
        $manifestLines.Add('builder_' + $builder.Ordinal + '_console=' + $builder.Console)
        $manifestLines.Add('builder_' + $builder.Ordinal + '_console_sha256=' + (Get-EvidenceSha256 -LiteralPath $builder.Console))
    }
    $manifestLines.Add('generated_runtime_semantics_sha256=' + $semantics.Sha256)
    $manifestLines.Add('generated_runtime_semantics_count=6')
    foreach ($path in $semantics.Paths) {
        $manifestLines.Add('generated_runtime_semantic_path=' + $path + '|sha256=' + $semantics.SceneSha256[$path])
    }
    [IO.File]::WriteAllLines($manifest, $manifestLines.ToArray(), [Text.UTF8Encoding]::new($false))
    $log = Join-Path $CaptureRoot 'task6-capture-materialization.log'
    [IO.File]::WriteAllText($log, ($terminal + "`n"), [Text.UTF8Encoding]::new($false))
    Assert-FreshNonEmptyOutputs -Paths @($manifest, $log) -StartedUtc $started
    Assert-ExactLogMarker -Path $log -Marker $terminal
    return [pscustomobject]@{ Manifest = $manifest; Log = $log; Semantics = $semantics; Import = $import; Builders = @($builderLogs) }
}

$task6CaptureCandidateState = Invoke-Task6CaptureCleanCandidatePreflight
$task6CaptureCleanup = $null
try {
$candidateHead = (& git -C $project rev-parse HEAD).Trim()
if ($LASTEXITCODE -ne 0 -or $candidateHead -notmatch '^[A-Fa-f0-9]{40}$') {
    throw "Task 6 capture could not bind the named candidate HEAD: $candidateHead"
}
$task11Materialization = Invoke-Task6MaterializationPreflight
$task6Materialization = Invoke-Task6CaptureMaterialization
$task11Critical = Invoke-Task6RuntimeLog -Name 'task11-critical' `
    -Script 'res://tests/smoke/PrologueCriticalPathSmokeTask6.cs' -RequiredMarkers @(
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
    )
$task11Ui = Invoke-Task6RuntimeLog -Name 'task11-ui' -Script 'res://tests/RunTask4UiTests.cs' `
    -RequiredMarkers @('TASK11_UI_READABILITY_OK')

if ([string]::IsNullOrWhiteSpace($AuditRoot)) {
    $AuditRoot = Join-Path $CaptureRoot 'focused-audit'
}
if (Test-Path -LiteralPath $AuditRoot) { throw "Task 6 capture audit root must be fresh: $AuditRoot" }
$auditArguments = @('-NoProfile', '-ExecutionPolicy', 'Bypass', '-File',
    (Join-Path $project 'tools\audit-prologue-production.ps1'), '-AuditRoot', $AuditRoot)
if ($SkipBuild) { $auditArguments += '-SkipBuild' }
# Run the same focused audit for both implicit and explicitly supplied fresh roots.
$auditHost = Get-Task6CompatiblePowerShellHost
& $auditHost @auditArguments
if ($LASTEXITCODE -ne 0) { throw "Task 6 focused production audit failed with exit code $LASTEXITCODE" }
$auditLog = Join-Path $AuditRoot 'production-audit.log'
$auditConsole = Join-Path $AuditRoot 'production-audit.console.log'
$auditManifest = Join-Path $AuditRoot 'audit-manifest.txt'
Assert-FreshNonEmptyOutputs -Paths @($auditLog, $auditConsole, $auditManifest) -StartedUtc ([DateTime]::UtcNow.AddHours(-24))
Assert-NoEngineLogErrors -Paths @($auditLog, $auditConsole)
$auditText = Get-Content -LiteralPath $auditLog -Raw
foreach ($marker in @(
    'PROLOGUE_AUDIT_TOPOLOGY_OK sectors=5 micro_scenes=15 claw_roles=4',
    'PROLOGUE_PRODUCTION_AUDIT_OK'
)) { Assert-OneRegexMarker -Text $auditText -Pattern ('^' + [regex]::Escape($marker) + '$') -Label "audit/$marker" | Out-Null }
$performance = Assert-OneRegexMarker -Text $auditText `
    -Pattern '^PROLOGUE_PERFORMANCE peak_ms=(?<peak>\d+(?:\.\d+)?) ceiling_ms=50 basis=callback_wall_clock headroom_ms=\d+(?:\.\d+)?\s*$' `
    -Label 'audit/performance'
$peak = [double]$performance.Groups['peak'].Value
if ($peak -ge 50d) { throw "Task 6 callback budget fails: peak_ms=$peak" }

if ($PreRecordProbeOnly) {
    "TASK6_CAPTURE_PRERECORD_PROBE_OK root=$CaptureRoot candidate_runtime_sha256=$runtimeCandidateBefore peak_ms=$peak"
    return
}

$fullStages = @(
    'title_start', 'mode_hud', 'attack_energy', 'recovery', 'canyon_descent', 'grapple_reticles',
    'canyon_basin', 'checkpoint_basin', 'shield_overheat',
    'ascent_one', 'ascent_two', 'ascent_three', 'optional_alcove', 'lost_temple',
    'checkpoint_temple', 'boss_intro', 'death_retry', 'completion',
    'boost_attached', 'boost_start', 'attached_boost_consumed', 'boost_arc', 'boost_second_rejected', 'boost_land', 'boss_sweep',
    'boss_leap_slam', 'boss_ground_wave', 'boss_hammer_throw', 'boss_attack_contract',
    'completion_title_return'
)
$reticleStages = @('Attack', 'Grippable', 'Ice', 'Forbidden', 'Obstructed', 'OutOfRange')
$formalFrameContract = Get-Task6FormalCaptureFrameContract
$full = Invoke-Task6Capture -Name 'full-flow' -Script 'res://tests/presentation/ProloguePresentationTask6.cs' `
    -RequiredMarkers @(
        'PROLOGUE_PRESENTATION_INPUTMAP_PROOF_OK route=generated_main inputmap=True post_proof_reposition=True',
        'TASK6_NO_DASH_TRAVERSAL_OK mandatory_dash=False route=generated_main',
        'PROLOGUE_PRESENTATION_ATTACHED_BOOST_OK cast_retained=True first=True second_rejected=True normal_air_jump=False inputmap=True',
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
        'PROLOGUE_PRESENTATION_RECOVERY_OK checkpoints=2 pre_checkpoint_start=True boss_death=True retry=True completion=True fall_context_version=3',
        'PROLOGUE_PRESENTATION_BOSS_ATTACKS_OK live=True attacks=Sweep,LeapSlam,GroundWave,HammerThrow inputmap=True',
        'TASK6_CLAW_PRESENTATION_OK states=Latched,Boost,Idle failures=IceSlip,Forbidden,Obstructed dwells=6 recovery=True',
        'TASK6_CANONICAL_TIMELINE_BOUNDARY_OK canonical_frames=4053 process_frame=4053 title_visible=True audio_released=True',
        'TASK6_RAW_PRODUCER_FRAME_OK canonical_frames=4053 raw_terminal_process_frame=4113 minimum_trim_tail_frames=30 title_visible=True audio_released=True',
        'PROLOGUE_PRESENTATION_STAGES_OK count=30 reticles=6 boost=True checkpoints=2 boss_attacks=4'
    ) -StageBeats $fullStages -StagePrefix 'PROLOGUE_PRESENTATION_STAGE' -ExpectedFrames $formalFrameContract.FullFlow

$fullTimeline = Assert-Task6FullFlowTimelineContract -LogPath $full.Log -TotalFrames $full.Header.TotalFrames `
    -ExpectedFrames $formalFrameContract.FullFlow -ExpectedStages $fullStages -ExpectedReticles $reticleStages -Label 'full-flow'
$modeHudDisplayFrame = Assert-Task6ModeHudDisplayBinding -LogPath $full.Log -StageFrames $fullTimeline.StageFrames
$boostDisplay = Assert-Task6BoostDisplayBindings -LogPath $full.Log -StageFrames $fullTimeline.StageFrames
$bossAttackDisplayFrames = Assert-Task6BossAttackDisplayBindings -LogPath $full.Log -StageFrames $fullTimeline.StageFrames
$completionDisplayFrame = Assert-Task6CompletionDisplayBinding -LogPath $full.Log -StageFrames $fullTimeline.StageFrames
$modeHudKeyframe = Get-Task6BoundKeyframe -ManifestPath $full.KeyframeManifest -KeyframeRoot $full.Keyframes `
    -Stage 'mode_hud' -ExpectedFrame $modeHudDisplayFrame -Label 'full-flow/mode-hud'
$modeHudVisual = Assert-Task6ModeHudVisual -Path $modeHudKeyframe.Path
$secondBoostRejectedKeyframe = Get-Task6BoundKeyframe -ManifestPath $full.KeyframeManifest -KeyframeRoot $full.Keyframes `
    -Stage 'boost_second_rejected' -ExpectedFrame ([long]$boostDisplay.Frames['boost_second_rejected']) `
    -Label 'full-flow/second-boost-rejected'
$secondBoostRejectedVisual = Assert-Task6SecondBoostRejectedVisual -Path $secondBoostRejectedKeyframe.Path
foreach ($stage in @('boss_sweep', 'boss_leap_slam', 'boss_ground_wave', 'boss_hammer_throw')) {
    Get-Task6BoundKeyframe -ManifestPath $full.KeyframeManifest -KeyframeRoot $full.Keyframes `
        -Stage $stage -ExpectedFrame ([long]$bossAttackDisplayFrames[$stage]) -Label "full-flow/$stage" | Out-Null
}
$completionKeyframe = Get-Task6BoundKeyframe -ManifestPath $full.KeyframeManifest -KeyframeRoot $full.Keyframes `
    -Stage 'completion' -ExpectedFrame $completionDisplayFrame -Label 'full-flow/completion'
$completionCardPath = $completionKeyframe.Path
$completionCardVisual = Assert-Task6CompletionCardVisual -Path $completionCardPath
"TASK6_MODE_HUD_VISUAL_OK avi_frame=$modeHudDisplayFrame png=$(Split-Path -Leaf $modeHudKeyframe.Path) " +
    "cream_samples=$($modeHudVisual.CreamSamples) ink_samples=$($modeHudVisual.InkSamples)"
"TASK6_SECOND_BOOST_REJECTED_VISUAL_OK avi_frame=$($secondBoostRejectedKeyframe.Frame) " +
    "png=$(Split-Path -Leaf $secondBoostRejectedKeyframe.Path) rope_cyan_samples=$($secondBoostRejectedVisual.RopeCyanSamples) " +
    "purple_ring_samples=$($secondBoostRejectedVisual.PurpleRingSamples)"
"TASK6_COMPLETION_CARD_VISUAL_OK avi_frame=$completionDisplayFrame png=$(Split-Path -Leaf $completionCardPath) " +
    "panel_samples=$($completionCardVisual.PanelSamples) gold_border_samples=$($completionCardVisual.GoldBorderSamples)"
"TASK6_FULL_FLOW_TIMELINE_OK frames=$($fullTimeline.TotalFrames) title_tail=$($fullTimeline.TitleTail) " +
    "completion_frame=$($fullTimeline.CompletionFrame) stage_count=$($fullStages.Count) reticles=$($reticleStages.Count)"

$reticle = Invoke-Task6KeyframeExtraction -Name 'full-flow-reticles' -Avi $full.Avi -Log $full.Log `
    -StagePrefix 'PROLOGUE_RETICLE_DWELL' -StageBeats $reticleStages -StartedUtc $full.StartedUtc `
    -MinimumDistinctFrames 6

$bossStages = @('route_started', 'completed_real_retry')
$boss = Invoke-Task6Capture -Name 'boss-retry' -Script 'res://tests/presentation/BossRetryPresentationTask6.cs' `
    -RequiredMarkers @(
        'TASK9_GENERATED_MAIN_BOSS_RETRY_OK physical=True completion=True',
        'PROLOGUE_BOSS_RETRY_COMPLETE',
        'TASK6_BOSS_RETRY_RAW_PRODUCER_OK canonical_frames=2719 raw_tail_process_frames=90'
    ) -StageBeats $bossStages -StagePrefix 'PROLOGUE_BOSS_RETRY_STAGE' -ExpectedFrames $formalFrameContract.BossRetry

$task6CaptureCleanup = Invoke-Task6CaptureCleanCandidateRestore -State $task6CaptureCandidateState
$runtimeCandidateAfter = Get-Task6Fingerprint ($task6RuntimeSources + $task6SceneSources)
if ($runtimeCandidateAfter -cne $runtimeCandidateBefore) {
    throw "Task 6 runtime candidate drifted during capture: before=$runtimeCandidateBefore after=$runtimeCandidateAfter"
}

$manifestLines = @(
    "capture_root=$CaptureRoot",
    "base_head=$candidateHead",
    "candidate_runtime_sha256=$runtimeCandidateBefore",
    "driver=windows fixed_fps=60 resolution=1280x720 callback_peak_ms=$peak",
    "formal_capture_frame_contract=full_flow=$($formalFrameContract.FullFlow) boss_retry=$($formalFrameContract.BossRetry)",
    "task6_capture_clean_preflight_log=$($task6CaptureCandidateState.Log)",
    "task6_capture_clean_preflight_log_sha256=$(Get-EvidenceSha256 -LiteralPath $task6CaptureCandidateState.Log)",
    "task6_capture_materialization_manifest=$($task6Materialization.Manifest)",
    "task6_capture_materialization_manifest_sha256=$(Get-EvidenceSha256 -LiteralPath $task6Materialization.Manifest)",
    "task6_capture_materialization_log=$($task6Materialization.Log)",
    "task6_capture_materialization_log_sha256=$(Get-EvidenceSha256 -LiteralPath $task6Materialization.Log)",
    "task6_generated_runtime_semantics_sha256=$($task6Materialization.Semantics.Sha256)",
    "task6_generated_runtime_semantics_count=$($task6Materialization.Semantics.Count)",
    "task6_capture_cleanup_log=$($task6CaptureCleanup.Log)",
    "task6_capture_cleanup_log_sha256=$(Get-EvidenceSha256 -LiteralPath $task6CaptureCleanup.Log)",
    "task6_capture_cleanup_after_log=$($task6CaptureCleanup.AfterLog)",
    "task6_capture_cleanup_after_log_sha256=$(Get-EvidenceSha256 -LiteralPath $task6CaptureCleanup.AfterLog)",
    "task11_materialization_log=$($task11Materialization.Log)",
    "task11_materialization_log_sha256=$(Get-EvidenceSha256 -LiteralPath $task11Materialization.Log)",
    "task11_materialization_console=$($task11Materialization.Console)",
    "task11_materialization_console_sha256=$(Get-EvidenceSha256 -LiteralPath $task11Materialization.Console)",
    "task11_critical_log=$($task11Critical.Log)",
    "task11_critical_log_sha256=$(Get-EvidenceSha256 -LiteralPath $task11Critical.Log)",
    "task11_critical_console=$($task11Critical.Console)",
    "task11_critical_console_sha256=$(Get-EvidenceSha256 -LiteralPath $task11Critical.Console)",
    "task11_ui_log=$($task11Ui.Log)",
    "task11_ui_log_sha256=$(Get-EvidenceSha256 -LiteralPath $task11Ui.Log)",
    "task11_ui_console=$($task11Ui.Console)",
    "task11_ui_console_sha256=$(Get-EvidenceSha256 -LiteralPath $task11Ui.Console)",
    "audit_log=$auditLog",
    "audit_log_sha256=$(Get-EvidenceSha256 -LiteralPath $auditLog)",
    "audit_console=$auditConsole",
    "audit_console_sha256=$(Get-EvidenceSha256 -LiteralPath $auditConsole)",
    "full_flow_log=$($full.Log)",
    "full_flow_log_sha256=$(Get-EvidenceSha256 -LiteralPath $full.Log)",
    "full_flow_console=$($full.Console)",
    "full_flow_console_sha256=$(Get-EvidenceSha256 -LiteralPath $full.Console)",
    "full_flow_avi=$($full.Avi)",
    "full_flow_avi_sha256=$(Get-EvidenceSha256 -LiteralPath $full.Avi)",
    "full_flow_raw_avi=$($full.RawAvi)",
    "full_flow_raw_avi_sha256=$(Get-EvidenceSha256 -LiteralPath $full.RawAvi)",
    "full_flow_raw_avi_frames=$($full.Normalization.RawFrames)",
    "full_flow_raw_terminal_process_frame=$($full.RawProducer.TerminalProcessFrame)",
    "full_flow_writer_shutdown_frames=$($full.RawProducer.WriterShutdownFrames)",
    "full_flow_canonical_prefix_log=$($full.NormalizationLog)",
    "full_flow_canonical_prefix_log_sha256=$(Get-EvidenceSha256 -LiteralPath $full.NormalizationLog)",
    "full_flow_trimmed_frames=$($full.Normalization.TrimmedFrames)",
    "full_flow_prefix_payload_sha256=$($full.Normalization.PrefixPayloadSha256)",
    "full_flow_first_frame_sha256=$($full.Normalization.FirstFrameSha256)",
    "full_flow_last_frame_sha256=$($full.Normalization.LastFrameSha256)",
    "full_flow_expected_frames=$($formalFrameContract.FullFlow)",
    "full_flow_avi_frames=$($full.Header.TotalFrames)",
    "full_flow_keyframe_manifest=$($full.KeyframeManifest)",
    "full_flow_keyframe_manifest_sha256=$(Get-EvidenceSha256 -LiteralPath $full.KeyframeManifest)",
    "full_flow_mode_hud_png=$($modeHudKeyframe.Path)",
    "full_flow_mode_hud_png_sha256=$($modeHudKeyframe.Sha256)",
    "full_flow_mode_hud_cream_samples=$($modeHudVisual.CreamSamples)",
    "full_flow_mode_hud_ink_samples=$($modeHudVisual.InkSamples)",
    "full_flow_second_boost_rejected_png=$($secondBoostRejectedKeyframe.Path)",
    "full_flow_second_boost_rejected_png_sha256=$($secondBoostRejectedKeyframe.Sha256)",
    "full_flow_second_boost_rejected_rope_cyan_samples=$($secondBoostRejectedVisual.RopeCyanSamples)",
    "full_flow_second_boost_rejected_purple_ring_samples=$($secondBoostRejectedVisual.PurpleRingSamples)",
    "full_flow_completion_card_png=$completionCardPath",
    "full_flow_completion_card_png_sha256=$(Get-EvidenceSha256 -LiteralPath $completionCardPath)",
    "full_flow_completion_card_panel_samples=$($completionCardVisual.PanelSamples)",
    "full_flow_completion_card_gold_border_samples=$($completionCardVisual.GoldBorderSamples)",
    "full_flow_reticle_keyframe_manifest=$($reticle.KeyframeManifest)",
    "full_flow_reticle_keyframe_manifest_sha256=$(Get-EvidenceSha256 -LiteralPath $reticle.KeyframeManifest)",
    "boss_retry_avi=$($boss.Avi)",
    "boss_retry_avi_sha256=$(Get-EvidenceSha256 -LiteralPath $boss.Avi)",
    "boss_retry_raw_avi=$($boss.RawAvi)",
    "boss_retry_raw_avi_sha256=$(Get-EvidenceSha256 -LiteralPath $boss.RawAvi)",
    "boss_retry_raw_avi_frames=$($boss.Normalization.RawFrames)",
    "boss_retry_canonical_prefix_log=$($boss.NormalizationLog)",
    "boss_retry_canonical_prefix_log_sha256=$(Get-EvidenceSha256 -LiteralPath $boss.NormalizationLog)",
    "boss_retry_trimmed_frames=$($boss.Normalization.TrimmedFrames)",
    "boss_retry_prefix_payload_sha256=$($boss.Normalization.PrefixPayloadSha256)",
    "boss_retry_first_frame_sha256=$($boss.Normalization.FirstFrameSha256)",
    "boss_retry_last_frame_sha256=$($boss.Normalization.LastFrameSha256)",
    "boss_retry_expected_frames=$($formalFrameContract.BossRetry)",
    "boss_retry_avi_frames=$($boss.Header.TotalFrames)",
    "boss_retry_keyframe_manifest=$($boss.KeyframeManifest)",
    "boss_retry_keyframe_manifest_sha256=$(Get-EvidenceSha256 -LiteralPath $boss.KeyframeManifest)",
    "boss_retry_log=$($boss.Log)",
    "boss_retry_log_sha256=$(Get-EvidenceSha256 -LiteralPath $boss.Log)",
    "boss_retry_console=$($boss.Console)",
    "boss_retry_console_sha256=$(Get-EvidenceSha256 -LiteralPath $boss.Console)"
)
$manifestPath = Join-Path $CaptureRoot 'capture-manifest.txt'
[IO.File]::WriteAllText($manifestPath, (($manifestLines -join "`n") + "`n"),
    [Text.UTF8Encoding]::new($false))
$manifestHash = Get-EvidenceSha256 -LiteralPath $manifestPath
"PROLOGUE_CAPTURE_AUDIT_OK driver=windows fixed_fps=60 sectors=5 micro_scenes=15 claw_roles=4 peak_ms=$peak"
"TASK6_FORMAL_FRAME_CONTRACT_OK full_flow_expected=$($formalFrameContract.FullFlow) full_flow_actual=$($full.Header.TotalFrames) boss_retry_expected=$($formalFrameContract.BossRetry) boss_retry_actual=$($boss.Header.TotalFrames)"
"PROLOGUE_CAPTURE_OK root=$CaptureRoot manifest=$manifestPath sha256=$manifestHash"
} finally {
    if ($null -ne $task6CaptureCandidateState -and $null -eq $task6CaptureCleanup) {
        Invoke-Task6CaptureCleanCandidateRestore -State $task6CaptureCandidateState | Out-Null
    }
}
