[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$CaptureRoot,
    [Parameter(Mandatory)][string]$RedRoot,
    [string]$CandidateRoot,
    [switch]$HistoricalHeadCandidate,
    [string]$EvidenceLog
)

$ErrorActionPreference = 'Stop'
$ProjectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
. (Join-Path $ProjectRoot 'tools\EvidenceChecks.ps1')
. (Join-Path $ProjectRoot 'tools\Task6AviStreaming.ps1')

function Assert-Task6ExactFrame([bool]$Condition, [string]$Message) {
    if (-not $Condition) { throw "Task 6 exact-frame assertion failed: $Message" }
}

function Get-Task6ExactFrameChildHost {
    $processPath = $null
    try { $processPath = (Get-Process -Id $PID -ErrorAction Stop).Path } catch { }
    if (-not [string]::IsNullOrWhiteSpace($processPath) -and
        (Test-Path -LiteralPath $processPath -PathType Leaf)) { return $processPath }
    foreach ($name in @('pwsh', 'powershell.exe', 'powershell')) {
        $command = Get-Command $name -ErrorAction SilentlyContinue
        if ($null -ne $command -and -not [string]::IsNullOrWhiteSpace($command.Source)) {
            return $command.Source
        }
    }
    throw 'Task 6 exact-frame test cannot locate a PowerShell child host.'
}

function Get-Task6ExactFrameManifestValues([string]$Path) {
    Assert-Task6ExactFrame (Test-Path -LiteralPath $Path -PathType Leaf) "manifest exists: $Path"
    $values = @{}
    foreach ($line in Get-Content -LiteralPath $Path) {
        $parts = $line -split '=', 2
        if ($parts.Count -eq 2) { $values[$parts[0]] = $parts[1] }
    }
    return $values
}

function Set-Task6ExactFrameManifestValue([string]$Path, [string]$Name, [string]$Value) {
    $lines = @((Get-Content -LiteralPath $Path) | ForEach-Object {
        if ($_ -match ('^' + [regex]::Escape($Name) + '=')) { "$Name=$Value" } else { $_ }
    })
    Assert-Task6ExactFrame ((@($lines | Where-Object { $_ -ceq "$Name=$Value" }).Count) -eq 1) `
        "manifest rewrites exactly one $Name"
    [IO.File]::WriteAllText($Path, (($lines -join "`n") + "`n"), [Text.UTF8Encoding]::new($false))
}

function Set-Task6ExactFrameManifestRequiredValue([string]$Path, [string]$Name, [string]$Value) {
    $values = Get-Task6ExactFrameManifestValues $Path
    if ($values.ContainsKey($Name)) {
        Set-Task6ExactFrameManifestValue -Path $Path -Name $Name -Value $Value
        return
    }
    [IO.File]::AppendAllText($Path, "$Name=$Value`n", [Text.UTF8Encoding]::new($false))
    Assert-Task6ExactFrame ((@(Get-Content -LiteralPath $Path | Where-Object { $_ -ceq "$Name=$Value" }).Count) -eq 1) `
        "manifest adds exactly one required $Name"
}

function Set-Task6ExactFrameManifestHash([string]$Manifest, [string]$Name, [string]$Path) {
    Set-Task6ExactFrameManifestValue -Path $Manifest -Name ($Name + '_sha256') `
        -Value (Get-EvidenceSha256 -LiteralPath $Path)
}

function Read-Task6ExactFrameBytes([System.IO.Stream]$Stream, [int]$Count, [string]$Label) {
    if ($Count -lt 0 -or $Count -gt 1048576) { throw "unsafe bounded read for ${Label}: $Count" }
    $bytes = New-Object byte[] $Count
    $offset = 0
    while ($offset -lt $Count) {
        $read = $Stream.Read($bytes, $offset, $Count - $offset)
        if ($read -le 0) { throw "unexpected EOF while reading $Label" }
        $offset += $read
    }
    return $bytes
}

function Read-Task6ExactFrameFourCc([System.IO.Stream]$Stream, [string]$Label) {
    return [Text.Encoding]::ASCII.GetString((Read-Task6ExactFrameBytes $Stream 4 $Label))
}

function Read-Task6ExactFrameUInt32([System.IO.Stream]$Stream, [string]$Label) {
    return [BitConverter]::ToUInt32((Read-Task6ExactFrameBytes $Stream 4 $Label), 0)
}

function Get-Task6ExactFramePaddedEnd([long]$PayloadStart, [uint32]$Size) {
    return $PayloadStart + [long]$Size + ([long]$Size % 2L)
}

function Get-Task6ExactFrameContainerOffsets([string]$Path) {
    $index = Get-Task6AviMjpegIndex -Path $Path
    $stream = [IO.FileStream]::new((Resolve-Path -LiteralPath $Path).Path, [IO.FileMode]::Open,
        [IO.FileAccess]::Read, [IO.FileShare]::Read)
    try {
        Assert-Task6ExactFrame ((Read-Task6ExactFrameFourCc $stream 'RIFF') -ceq 'RIFF') 'AVI has RIFF signature'
        $null = Read-Task6ExactFrameUInt32 $stream 'RIFF size'
        Assert-Task6ExactFrame ((Read-Task6ExactFrameFourCc $stream 'AVI form') -ceq 'AVI ') 'AVI has AVI form'
        $pending = New-Object System.Collections.Stack
        $pending.Push([pscustomobject]@{ Start = [long]12; End = [long]$stream.Length })
        $avihPayloadStart = -1L
        $videoStreamHeaderPayloadStart = -1L
        $moviListChunkStart = -1L
        $indexChunkStart = -1L
        while ($pending.Count -gt 0) {
            $range = $pending.Pop()
            $stream.Position = $range.Start
            while ($stream.Position + 8L -le $range.End) {
                $chunkStart = [long]$stream.Position
                $chunkId = Read-Task6ExactFrameFourCc $stream 'AVI chunk id'
                $chunkSize = Read-Task6ExactFrameUInt32 $stream "AVI $chunkId size"
                $payloadStart = [long]$stream.Position
                $paddedEnd = Get-Task6ExactFramePaddedEnd $payloadStart $chunkSize
                Assert-Task6ExactFrame ($paddedEnd -le $range.End -and $paddedEnd -le $stream.Length) `
                    "AVI chunk stays in bounds: $chunkId"
                if ($chunkId -ceq 'LIST') {
                    Assert-Task6ExactFrame ($chunkSize -ge 4) 'AVI LIST is not truncated'
                    $listType = Read-Task6ExactFrameFourCc $stream 'AVI LIST type'
                    if ($listType -ceq 'movi') { $moviListChunkStart = $chunkStart }
                    else {
                        $pending.Push([pscustomobject]@{
                            Start = [long]$stream.Position; End = [long]$payloadStart + [long]$chunkSize
                        })
                    }
                }
                elseif ($chunkId -ceq 'avih') { $avihPayloadStart = $payloadStart }
                elseif ($chunkId -ceq 'strh') {
                    $stream.Position = $payloadStart
                    if ((Read-Task6ExactFrameFourCc $stream 'AVI stream type') -ceq 'vids') {
                        $videoStreamHeaderPayloadStart = $payloadStart
                    }
                }
                elseif ($chunkId -ceq 'idx1') { $indexChunkStart = $chunkStart }
                $stream.Position = $paddedEnd
            }
        }
        Assert-Task6ExactFrame ($avihPayloadStart -ge 0 -and $videoStreamHeaderPayloadStart -ge 0 -and
            $moviListChunkStart -ge 0 -and $indexChunkStart -ge 0) `
            'AVI exposes mutable avih/video-strh/movi/idx1 offsets'
        return [pscustomobject]@{
            Index = $index; AvihPayloadStart = $avihPayloadStart;
            VideoStreamHeaderPayloadStart = $videoStreamHeaderPayloadStart; MoviListChunkStart = $moviListChunkStart;
            IndexChunkStart = $indexChunkStart
        }
    }
    finally { $stream.Dispose() }
}

function Copy-Task6ExactFrameRange([System.IO.Stream]$InputStream, [System.IO.Stream]$OutputStream,
    [long]$Start, [long]$Length) {
    Assert-Task6ExactFrame ($Start -ge 0 -and $Length -ge 0 -and $Start + $Length -le $InputStream.Length) `
        "bounded copy range stays in source: start=$Start length=$Length"
    $InputStream.Position = $Start
    $remaining = $Length
    $buffer = New-Object byte[] 65536
    while ($remaining -gt 0) {
        $requested = [int][Math]::Min([long]$buffer.Length, $remaining)
        $read = $InputStream.Read($buffer, 0, $requested)
        if ($read -le 0) { throw 'unexpected EOF while streaming AVI mutation range' }
        $OutputStream.Write($buffer, 0, $read)
        $remaining -= $read
    }
}

function Write-Task6ExactFrameUInt32At([System.IO.Stream]$Stream, [long]$Offset, [uint32]$Value) {
    Assert-Task6ExactFrame ($Offset -ge 0 -and $Offset + 4L -le $Stream.Length) "AVI write offset is in bounds: $Offset"
    $Stream.Position = $Offset
    $bytes = [BitConverter]::GetBytes($Value)
    $Stream.Write($bytes, 0, $bytes.Length)
}

function Get-Task6ExactFrameLastVideoIndexEntry([string]$Path, $Layout) {
    $stream = [IO.FileStream]::new((Resolve-Path -LiteralPath $Path).Path, [IO.FileMode]::Open,
        [IO.FileAccess]::Read, [IO.FileShare]::Read)
    try {
        $stream.Position = $Layout.IndexStart
        $end = [long]$Layout.IndexStart + [long]$Layout.IndexSize
        $last = $null
        while ($stream.Position -lt $end) {
            $position = [long]$stream.Position
            $entry = Read-Task6ExactFrameBytes $stream 16 'idx1 entry'
            $kind = [Text.Encoding]::ASCII.GetString($entry, 0, 4)
            if ($kind -ceq '00dc' -or $kind -ceq '00db') {
                $last = [pscustomobject]@{ Position = $position; Bytes = $entry }
            }
        }
        Assert-Task6ExactFrame ($null -ne $last) 'AVI idx1 has a video entry'
        return $last
    }
    finally { $stream.Dispose() }
}

function New-Task6ExactFrameAviMutation([string]$SourcePath, [string]$Destination, [ValidateSet(-1, 1)][int]$Delta) {
    $container = Get-Task6ExactFrameContainerOffsets $SourcePath
    $layout = $container.Index.Layout
    $lastFrame = $container.Index.Frames[$container.Index.Frames.Count - 1]
    $frameStart = [long]$lastFrame.Offset - 8L
    $frameLength = 8L + [long]$lastFrame.Size + ([long]$lastFrame.Size % 2L)
    $frameEnd = $frameStart + $frameLength
    Assert-Task6ExactFrame ($frameEnd -le $layout.MoviEnd) 'AVI final MJPEG chunk stays inside movi list'
    $lastIndex = Get-Task6ExactFrameLastVideoIndexEntry -Path $SourcePath -Layout $layout
    $indexChunkStart = [long]$container.IndexChunkStart
    $indexEnd = [long]$layout.IndexStart + [long]$layout.IndexSize
    $inputStream = [IO.FileStream]::new((Resolve-Path -LiteralPath $SourcePath).Path, [IO.FileMode]::Open,
        [IO.FileAccess]::Read, [IO.FileShare]::Read)
    $output = [IO.FileStream]::new($Destination, [IO.FileMode]::CreateNew, [IO.FileAccess]::ReadWrite,
        [IO.FileShare]::None)
    try {
        if ($Delta -eq 1) {
            # The recorder interleaves a terminal audio chunk after the final video chunk.  Add
            # the duplicated video immediately before that audio and insert its idx1 entry at
            # the same logical point; this preserves a structurally coherent AVI rather than
            # assuming the last media/index record is always video.
            Copy-Task6ExactFrameRange $inputStream $output 0 $frameEnd
            Copy-Task6ExactFrameRange $inputStream $output $frameStart $frameLength
            Copy-Task6ExactFrameRange $inputStream $output $frameEnd ($indexChunkStart - $frameEnd)
            Copy-Task6ExactFrameRange $inputStream $output $indexChunkStart 8
            Copy-Task6ExactFrameRange $inputStream $output $layout.IndexStart (($lastIndex.Position + 16L) - $layout.IndexStart)
            $newIndexEntry = [byte[]]$lastIndex.Bytes.Clone()
            $newOffset = [BitConverter]::GetBytes([uint32]($frameEnd - ($layout.MoviStart - 4L)))
            [Array]::Copy($newOffset, 0, $newIndexEntry, 8, 4)
            $output.Write($newIndexEntry, 0, $newIndexEntry.Length)
            Copy-Task6ExactFrameRange $inputStream $output ($lastIndex.Position + 16L) ($indexEnd - ($lastIndex.Position + 16L))
            Copy-Task6ExactFrameRange $inputStream $output $indexEnd ($inputStream.Length - $indexEnd)
        }
        else {
            Assert-Task6ExactFrame ($container.Index.Frames.Count -gt 1) 'AVI has a removable final frame'
            Copy-Task6ExactFrameRange $inputStream $output 0 $frameStart
            Copy-Task6ExactFrameRange $inputStream $output $frameEnd ($indexChunkStart - $frameEnd)
            Copy-Task6ExactFrameRange $inputStream $output $indexChunkStart 8
            Copy-Task6ExactFrameRange $inputStream $output $layout.IndexStart ($lastIndex.Position - $layout.IndexStart)
            Copy-Task6ExactFrameRange $inputStream $output ($lastIndex.Position + 16L) ($indexEnd - ($lastIndex.Position + 16L))
            Copy-Task6ExactFrameRange $inputStream $output $indexEnd ($inputStream.Length - $indexEnd)
        }

        $oldMoviSize = [uint32](Read-Task6ExactFrameUInt32At $inputStream ($container.MoviListChunkStart + 4L))
        $oldIndexSize = [uint32](Read-Task6ExactFrameUInt32At $inputStream ($indexChunkStart + 4L))
        $newFrameCount = [uint32]([long]$container.Index.Header.TotalFrames + $Delta)
        $newMoviSize = [uint32]([long]$oldMoviSize + ($Delta * $frameLength))
        $newIndexSize = [uint32]([long]$oldIndexSize + ($Delta * 16L))
        $newIndexChunkStart = if ($Delta -eq 1) { $indexChunkStart + $frameLength } else { $indexChunkStart - $frameLength }
        Write-Task6ExactFrameUInt32At $output 4 ([uint32]($output.Length - 8L))
        Write-Task6ExactFrameUInt32At $output ($container.AvihPayloadStart + 16L) $newFrameCount
        Write-Task6ExactFrameUInt32At $output ($container.VideoStreamHeaderPayloadStart + 32L) $newFrameCount
        Write-Task6ExactFrameUInt32At $output ($container.MoviListChunkStart + 4L) $newMoviSize
        Write-Task6ExactFrameUInt32At $output ($newIndexChunkStart + 4L) $newIndexSize
    }
    finally {
        $output.Dispose()
        $inputStream.Dispose()
    }
    $mutated = Get-Task6AviMjpegIndex -Path $Destination
    Assert-Task6ExactFrame ($mutated.Header.TotalFrames -eq [uint32]($container.Index.Header.TotalFrames + $Delta)) `
        'mutated AVI keeps header/index/movi frame counts internally consistent'
    return $mutated
}

function Read-Task6ExactFrameUInt32At([System.IO.Stream]$Stream, [long]$Offset) {
    $Stream.Position = $Offset
    return [BitConverter]::ToUInt32((Read-Task6ExactFrameBytes $Stream 4 "AVI uint32 at $Offset"), 0)
}

function Update-Task6ExactFrameKeyframeManifest([string]$SourceManifest, [string]$DestinationDirectory,
    [string]$AviSha256, [uint32]$Frames) {
    $sourceDirectory = Split-Path -Parent $SourceManifest
    Copy-Item -LiteralPath $sourceDirectory -Destination $DestinationDirectory -Recurse -Force
    $manifest = Join-Path $DestinationDirectory 'manifest.txt'
    $text = [IO.File]::ReadAllText($manifest)
    $text = [regex]::Replace($text, '(?m)^avi_sha256=[A-Fa-f0-9]{64}$', "avi_sha256=$AviSha256", 1)
    $text = [regex]::Replace($text, '(?m)^avi_frames=\d+$', "avi_frames=$Frames", 1)
    Assert-Task6ExactFrame ($text -match ("(?m)^avi_sha256=" + [regex]::Escape($AviSha256) + '$')) 'copied keyframe manifest binds mutated AVI hash'
    Assert-Task6ExactFrame ($text -match "(?m)^avi_frames=$Frames`$") 'copied keyframe manifest binds mutated AVI frame count'
    [IO.File]::WriteAllText($manifest, $text, [Text.UTF8Encoding]::new($false))
    return $manifest
}

function Get-Task6ExactFramePng([string]$Manifest, [string]$Stage) {
    $line = @((Get-Content -LiteralPath $Manifest) | Where-Object { $_ -match ('^' + [regex]::Escape($Stage) + '\|avi_frame=\d+\|png=([^|]+)\|sha256=[A-Fa-f0-9]{64}$') })
    Assert-Task6ExactFrame ($line.Count -eq 1) "copied keyframe manifest binds one $Stage PNG"
    $png = [regex]::Match($line[0], '\|png=(?<png>[^|]+)\|').Groups['png'].Value
    return (Join-Path (Split-Path -Parent $Manifest) $png)
}

function Get-Task6ExactFrameCandidateFingerprint([string]$CandidateRoot) {
    $capture = Join-Path $CandidateRoot 'tools\capture-prologue.ps1'
    $childHost = Get-Task6ExactFrameChildHost
    $prior = $ErrorActionPreference
    try {
        $ErrorActionPreference = 'Continue'
        $output = & $childHost -NoProfile -ExecutionPolicy Bypass -File $capture -RuntimeFingerprintProbeOnly 2>&1
        $exitCode = $LASTEXITCODE
    }
    finally { $ErrorActionPreference = $prior }
    Assert-Task6ExactFrame ($exitCode -eq 0) 'historical min-only candidate exposes a runtime fingerprint'
    $marker = @($output | Where-Object { $_.ToString() -match '^TASK6_CAPTURE_RUNTIME_FINGERPRINT_OK ' })
    Assert-Task6ExactFrame ($marker.Count -eq 1) 'historical min-only candidate emits one runtime fingerprint marker'
    $match = [regex]::Match($marker[0].ToString(), 'candidate_runtime_sha256=(?<hash>[A-Fa-f0-9]{64})')
    Assert-Task6ExactFrame $match.Success 'historical runtime fingerprint marker contains SHA-256'
    return $match.Groups['hash'].Value
}

function Invoke-Task6ExactFrameEvidenceGate([string]$Name, [string]$CandidateRoot, [string]$EvidenceRoot) {
    $verify = Join-Path $CandidateRoot 'tools\verify-prologue.ps1'
    $runRoot = Join-Path $RedRoot ('verify-' + $Name)
    $childHost = Get-Task6ExactFrameChildHost
    $prior = $ErrorActionPreference
    try {
        $ErrorActionPreference = 'Continue'
        $output = & $childHost -NoProfile -ExecutionPolicy Bypass -File $verify -ProjectRoot $CandidateRoot `
            -CaptureEvidenceOnly -CaptureRoot $EvidenceRoot -VerificationRoot $runRoot 2>&1
        $exitCode = $LASTEXITCODE
    }
    finally { $ErrorActionPreference = $prior }
    [IO.File]::WriteAllText((Join-Path $RedRoot ($Name + '.legacy-minimum.verify.log')),
        (($output | ForEach-Object { $_.ToString() }) -join "`n") + "`n", [Text.UTF8Encoding]::new($false))
    return [pscustomobject]@{ ExitCode = $exitCode; Output = @($output | ForEach-Object { $_.ToString() }) }
}

function New-Task6ExactFrameMutationBundle([string]$Name, [string]$CandidateHash,
    [string]$AviField, [string]$FrameField, [ValidateSet(-1, 1)][int]$Delta) {
    $root = Join-Path $RedRoot $Name
    New-Item -ItemType Directory -Path $root -ErrorAction Stop | Out-Null
    $sourceManifest = Join-Path $CaptureRoot 'capture-manifest.txt'
    $manifest = Join-Path $root 'capture-manifest.txt'
    Copy-Item -LiteralPath $sourceManifest -Destination $manifest -Force
    $values = Get-Task6ExactFrameManifestValues $sourceManifest
    $sourceAvi = [string]$values[$AviField]
    Assert-Task6ExactFrame (Test-Path -LiteralPath $sourceAvi -PathType Leaf) "$Name source AVI exists"
    $mutatedAvi = Join-Path $root ('mutated-' + (Split-Path -Leaf $sourceAvi))
    $mutated = New-Task6ExactFrameAviMutation -SourcePath $sourceAvi -Destination $mutatedAvi -Delta $Delta
    Set-Task6ExactFrameManifestValue $manifest 'capture_root' $root
    Set-Task6ExactFrameManifestValue $manifest 'candidate_runtime_sha256' $CandidateHash
    Set-Task6ExactFrameManifestRequiredValue $manifest 'formal_capture_frame_contract' 'full_flow=4053 boss_retry=2719'
    Set-Task6ExactFrameManifestRequiredValue $manifest 'full_flow_expected_frames' '4053'
    Set-Task6ExactFrameManifestRequiredValue $manifest 'boss_retry_expected_frames' '2719'
    Set-Task6ExactFrameManifestValue $manifest $AviField $mutatedAvi
    Set-Task6ExactFrameManifestHash $manifest $AviField $mutatedAvi
    Set-Task6ExactFrameManifestValue $manifest $FrameField ([string]$mutated.Header.TotalFrames)
    $aviHash = Get-EvidenceSha256 -LiteralPath $mutatedAvi
    if ($AviField -ceq 'full_flow_avi') {
        $fullManifest = Update-Task6ExactFrameKeyframeManifest $values['full_flow_keyframe_manifest'] `
            (Join-Path $root 'full-flow-keyframes') $aviHash $mutated.Header.TotalFrames
        $reticleManifest = Update-Task6ExactFrameKeyframeManifest $values['full_flow_reticle_keyframe_manifest'] `
            (Join-Path $root 'full-flow-reticles-keyframes') $aviHash $mutated.Header.TotalFrames
        Set-Task6ExactFrameManifestValue $manifest 'full_flow_keyframe_manifest' $fullManifest
        Set-Task6ExactFrameManifestHash $manifest 'full_flow_keyframe_manifest' $fullManifest
        Set-Task6ExactFrameManifestValue $manifest 'full_flow_reticle_keyframe_manifest' $reticleManifest
        Set-Task6ExactFrameManifestHash $manifest 'full_flow_reticle_keyframe_manifest' $reticleManifest
        foreach ($pointer in @(
            @{ Name = 'full_flow_mode_hud_png'; Stage = 'mode_hud' },
            @{ Name = 'full_flow_second_boost_rejected_png'; Stage = 'boost_second_rejected' },
            @{ Name = 'full_flow_completion_card_png'; Stage = 'completion' }
        )) {
            $png = Get-Task6ExactFramePng $fullManifest $pointer.Stage
            Set-Task6ExactFrameManifestValue $manifest $pointer.Name $png
            Set-Task6ExactFrameManifestHash $manifest $pointer.Name $png
        }
    }
    else {
        $bossManifest = Update-Task6ExactFrameKeyframeManifest $values['boss_retry_keyframe_manifest'] `
            (Join-Path $root 'boss-retry-keyframes') $aviHash $mutated.Header.TotalFrames
        Set-Task6ExactFrameManifestValue $manifest 'boss_retry_keyframe_manifest' $bossManifest
        Set-Task6ExactFrameManifestHash $manifest 'boss_retry_keyframe_manifest' $bossManifest
    }
    return [pscustomobject]@{ Root = $root; Manifest = $manifest; Frames = [uint32]$mutated.Header.TotalFrames }
}

Describe 'Task 6 full-flow producer frame binding' -Tag 'ProducerFrameContract' {
    It 'over-records real terminal frames for deterministic 4053/2719 canonical prefixes' {
        $producerPath = Join-Path $ProjectRoot 'tests\presentation\ProloguePresentationTask6.cs'
        $bossProducerPath = Join-Path $ProjectRoot 'tests\presentation\BossRetryPresentationTask6.cs'
        $capturePath = Join-Path $ProjectRoot 'tools\capture-prologue.ps1'
        $verifierPath = Join-Path $ProjectRoot 'tools\verify-prologue.ps1'
        $producer = Get-Content -LiteralPath $producerPath -Raw
        $bossProducer = Get-Content -LiteralPath $bossProducerPath -Raw
        $capture = Get-Content -LiteralPath $capturePath -Raw
        $verifier = Get-Content -LiteralPath $verifierPath -Raw
        $canonicalMarker = 'TASK6_CANONICAL_TIMELINE_BOUNDARY_OK canonical_frames=4053 process_frame=4053 title_visible=True audio_released=True'
        $marker = 'TASK6_RAW_PRODUCER_FRAME_OK canonical_frames=4053 raw_terminal_process_frame=4113 minimum_trim_tail_frames=30 title_visible=True audio_released=True'
        $bossMarker = 'TASK6_BOSS_RETRY_RAW_PRODUCER_OK canonical_frames=2719 raw_tail_process_frames=90'

        Assert-Task6ExactFrame ($producer -match 'private const int FormalCanonicalMovieFrameContract\s*=\s*4053\s*;') `
            'full-flow producer declares the 4053-frame canonical contract'
        Assert-Task6ExactFrame ($producer -match 'private const int MinimumRawTrimTailFrames\s*=\s*30\s*;') `
            'full-flow producer declares the minimum auditable trim tail'
        Assert-Task6ExactFrame ($producer -match 'private const int FormalRawTerminalProcessFrame\s*=\s*FormalCanonicalMovieFrameContract\s*\+\s*60\s*;') `
            'full-flow producer records well past the canonical boundary'
        Assert-Task6ExactFrame ($producer -match 'private const int CanonicalTimelineProcessFrame\s*=\s*FormalCanonicalMovieFrameContract\s*;') `
            'full-flow producer binds a distinct canonical timeline terminal'
        Assert-Task6ExactFrame ($producer -match 'await RenderVisibleTitleToCanonicalBoundary\(finalTitle\)\s*;') `
            'full-flow producer renders the live title to the canonical boundary'
        Assert-Task6ExactFrame ($producer -match 'await RenderVisibleTitleThroughRawTail\(finalTitle\)\s*;') `
            'full-flow producer keeps the live final title visible through the raw tail'
        Assert-Task6ExactFrame ($producer.Contains($canonicalMarker)) `
            'full-flow producer emits the exact canonical timeline marker'
        $canonicalMarkerIndex = $producer.IndexOf($canonicalMarker, [StringComparison]::Ordinal)
        $timelineTerminalIndex = $producer.IndexOf('PROLOGUE_PRESENTATION_COMPLETE frame=', [StringComparison]::Ordinal)
        $rawTailCallIndex = $producer.IndexOf('await RenderVisibleTitleThroughRawTail(finalTitle);', [StringComparison]::Ordinal)
        Assert-Task6ExactFrame ($canonicalMarkerIndex -ge 0 -and $timelineTerminalIndex -gt $canonicalMarkerIndex -and
            $rawTailCallIndex -gt $timelineTerminalIndex) `
            'canonical timeline terminal is emitted before the raw over-record tail'
        Assert-Task6ExactFrame ($producer.Contains($marker)) `
            'full-flow producer emits the exact raw-tail marker'
        Assert-Task6ExactFrame ($bossProducer -match 'private const int CanonicalMovieFrameContract\s*=\s*2719\s*;') `
            'boss-retry producer declares the 2719-frame canonical contract'
        Assert-Task6ExactFrame ($bossProducer -match 'private const int RawTailProcessFrames\s*=\s*90\s*;') `
            'boss-retry producer records a deterministic real-frame tail'
        Assert-Task6ExactFrame ($bossProducer.Contains($bossMarker)) `
            'boss-retry producer emits the exact raw-tail marker'
        Assert-Task6ExactFrame ($capture.Contains("'$marker'")) `
            'formal capture requires the full-flow raw-tail marker'
        Assert-Task6ExactFrame ($capture.Contains("'$canonicalMarker'")) `
            'formal capture requires the canonical timeline marker'
        Assert-Task6ExactFrame ($capture -match 'Assert-Task6RawProducerFrameContract') `
            'formal capture binds the raw producer terminal to the raw AVI'
        Assert-Task6ExactFrame ($capture.Contains("'$bossMarker'")) `
            'formal capture requires the boss-retry raw-tail marker'
        Assert-Task6ExactFrame ($verifier.Contains("'$marker'")) `
            'full evidence verifier requires the full-flow raw-tail marker'
        Assert-Task6ExactFrame ($verifier.Contains("'$canonicalMarker'")) `
            'full evidence verifier requires the canonical timeline marker'
        Assert-Task6ExactFrame ($verifier -match 'Assert-Task6RawProducerFrameContract') `
            'full evidence verifier independently binds the raw producer terminal to the raw AVI'
        Assert-Task6ExactFrame ($verifier.Contains("'$bossMarker'")) `
            'full evidence verifier requires the boss-retry raw-tail marker'
    }
}

Describe 'Task 6 canonical AVI prefix normalization' -Tag 'CanonicalPrefixContract' {
    It 'streams a longer raw AVI into an exact real-frame prefix without padding or re-encoding' {
        $values = Get-Task6ExactFrameManifestValues (Join-Path $CaptureRoot 'capture-manifest.txt')
        $source = [string]$values['full_flow_avi']
        Assert-Task6ExactFrame (Test-Path -LiteralPath $source -PathType Leaf) `
            'canonical-prefix fixture AVI exists'
        $prefixRoot = Join-Path $RedRoot 'canonical-prefix'
        Assert-Task6ExactFrame (-not (Test-Path -LiteralPath $prefixRoot)) `
            'canonical-prefix evidence root is fresh'
        New-Item -ItemType Directory -Path $prefixRoot -Force -ErrorAction Stop | Out-Null
        $raw = Join-Path $prefixRoot 'raw-plus-one.avi'
        $canonical = Join-Path $prefixRoot 'canonical.avi'
        $rawIndex = New-Task6ExactFrameAviMutation -SourcePath $source -Destination $raw -Delta 1
        Assert-Task6ExactFrame ($rawIndex.Header.TotalFrames -eq 4054) `
            'fixture exposes one real container frame beyond the canonical contract'

        $normalized = New-Task6CanonicalAviPrefix -SourcePath $raw -Destination $canonical `
            -ExpectedFrames 4053 -MinimumRawTailFrames 1 -Label 'canonical-prefix-test'
        $verified = Assert-Task6CanonicalAviPrefix -RawPath $raw -CanonicalPath $canonical `
            -ExpectedFrames 4053 -MinimumRawTailFrames 1 -Label 'canonical-prefix-test'
        Assert-Task6ExactFrame ($normalized.RawFrames -eq 4054 -and
            $normalized.CanonicalFrames -eq 4053 -and $normalized.TrimmedFrames -eq 1) `
            'normalizer reports the exact raw/canonical/trim counts'
        Assert-Task6ExactFrame ($verified.FirstFrameSha256 -ceq $normalized.FirstFrameSha256 -and
            $verified.LastFrameSha256 -ceq $normalized.LastFrameSha256) `
            'independent prefix verifier binds the first and final retained MJPEG payloads'
        $container = Get-Task6AviContainerOffsets -Path $canonical
        Assert-Task6ExactFrame ($container.VideoStreamHeaderPayloadStart -gt 0) `
            'canonical-prefix parser binds the video stream header'
        $stream = [IO.FileStream]::new($canonical, [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::Read)
        try {
            $stream.Position = [long]$container.VideoStreamHeaderPayloadStart + 32L
            $videoStreamLength = Read-Task6AviUInt32 -Stream $stream -Label 'canonical video stream length'
        }
        finally { $stream.Dispose() }
        Assert-Task6ExactFrame ($videoStreamLength -eq 4053) `
            'canonical AVI patches the video stream length as well as the main header'
    }
}

Describe 'Task 6 formal exact AVI-frame contract' {
    It 'rejects internally consistent +1 and -1 AVI evidence through a real child verifier' {
        $CaptureRoot = (Resolve-Path -LiteralPath $CaptureRoot).Path
        Assert-Task6ExactFrame (Test-Path -LiteralPath (Join-Path $CaptureRoot 'capture-manifest.txt')) `
            'development capture has a manifest'
        $mutationArtifacts = @(
            'full-flow-plus-one', 'boss-retry-minus-one',
            'verify-full-flow-plus-one', 'verify-boss-retry-minus-one',
            'full-flow-plus-one.legacy-minimum.verify.log',
            'boss-retry-minus-one.legacy-minimum.verify.log'
        )
        Assert-Task6ExactFrame (-not @($mutationArtifacts | Where-Object {
                    Test-Path -LiteralPath (Join-Path $RedRoot $_)
                })) 'exact-frame mutation evidence paths are fresh'
        New-Item -ItemType Directory -Path $RedRoot -Force -ErrorAction Stop | Out-Null
        $repo = (& git -C $ProjectRoot rev-parse --show-toplevel).Trim()
        $suffix = [guid]::NewGuid().ToString('N')
        $candidateBranch = $null
        $temporaryCandidateRoot = $null
        $ownsCandidate = $false
        try {
            if ($HistoricalHeadCandidate) {
                $candidateBranch = 'codex/task6-exact-frame-red-' + $suffix
                $temporaryCandidateRoot = Join-Path ([IO.Path]::GetTempPath()) ('knight-task6-exact-frame-red-' + $suffix)
                & git -C $repo worktree add -b $candidateBranch $temporaryCandidateRoot HEAD | Out-Null
                Assert-Task6ExactFrame ($LASTEXITCODE -eq 0) 'historical min-only candidate worktree materializes'
                Assert-Task6ExactFrame ((@(& git -C $temporaryCandidateRoot status --porcelain=v1 --untracked-files=all)).Count -eq 0) `
                    'historical min-only candidate starts clean'
                $candidateRoot = $temporaryCandidateRoot
                $ownsCandidate = $true
            }
            else {
                if ([string]::IsNullOrWhiteSpace($CandidateRoot)) { $CandidateRoot = $ProjectRoot }
                $candidateRoot = (Resolve-Path -LiteralPath $CandidateRoot).Path
            }
            $candidateHash = Get-Task6ExactFrameCandidateFingerprint $candidateRoot
            $full = New-Task6ExactFrameMutationBundle -Name 'full-flow-plus-one' -CandidateHash $candidateHash `
                -AviField 'full_flow_avi' -FrameField 'full_flow_avi_frames' -Delta 1
            $boss = New-Task6ExactFrameMutationBundle -Name 'boss-retry-minus-one' -CandidateHash $candidateHash `
                -AviField 'boss_retry_avi' -FrameField 'boss_retry_avi_frames' -Delta -1
            Assert-Task6ExactFrame ($full.Frames -eq 4054) 'full-flow mutation is header/index/manifest count 4054'
            Assert-Task6ExactFrame ($boss.Frames -eq 2718) 'boss-retry mutation is header/index/manifest count 2718'
            $fullResult = Invoke-Task6ExactFrameEvidenceGate 'full-flow-plus-one' $candidateRoot $full.Root
            $bossResult = Invoke-Task6ExactFrameEvidenceGate 'boss-retry-minus-one' $candidateRoot $boss.Root
            Assert-Task6ExactFrame (($fullResult.ExitCode -ne 0) -and ($bossResult.ExitCode -ne 0)) (
                'RED: minimum-only formal verifier incorrectly accepted exact-frame deviations; ' +
                "full_exit=$($fullResult.ExitCode) boss_exit=$($bossResult.ExitCode)")
            $fullText = $fullResult.Output -join "`n"
            $bossText = $bossResult.Output -join "`n"
            Assert-Task6ExactFrame ($fullText -match 'formal_frame_count_mismatch label=full-flow expected=4053 actual=4054') `
                'full-flow +1 child verifier reports the exact formal mismatch label'
            Assert-Task6ExactFrame ($bossText -match 'formal_frame_count_mismatch label=boss-retry expected=2719 actual=2718') `
                'boss-retry -1 child verifier reports the exact formal mismatch label'
            if (-not [string]::IsNullOrWhiteSpace($EvidenceLog)) {
                Assert-Task6ExactFrame (-not (Test-Path -LiteralPath $EvidenceLog)) 'exact-frame evidence log is fresh'
                $evidence = @(
                    "TASK6_MUTATION_RED_OBSERVED name=avi-exact-frame-full-plus-one",
                    "TASK6_MUTATION_LEDGER name=avi-exact-frame-full-plus-one path=$($full.Root) command=actual_child_verifier nonzero=True expected=formal_frame_count_mismatch_expected_4053_actual_4054",
                    "TASK6_MUTATION_RED_OBSERVED name=avi-exact-frame-boss-minus-one",
                    "TASK6_MUTATION_LEDGER name=avi-exact-frame-boss-minus-one path=$($boss.Root) command=actual_child_verifier nonzero=True expected=formal_frame_count_mismatch_expected_2719_actual_2718",
                    'TASK6_EXACT_FRAME_CONTRACT_OK mutations=2 full_expected=4053 boss_expected=2719'
                )
                [IO.File]::WriteAllLines($EvidenceLog, $evidence, [Text.UTF8Encoding]::new($false))
            }
        }
        finally {
            if ($ownsCandidate -and (Test-Path -LiteralPath $temporaryCandidateRoot)) {
                & git -C $repo worktree remove --force $temporaryCandidateRoot 2>$null
            }
            if ($ownsCandidate -and -not [string]::IsNullOrWhiteSpace($candidateBranch)) {
                & git -C $repo branch -D $candidateBranch 2>$null
            }
        }
    }
}
