Set-StrictMode -Version Latest

# Bounded-memory RIFF/MJPEG helpers shared by Task 6 capture, extraction, and verification.
# AVI evidence can exceed 100 MB; none of these helpers reads the container as one byte array.
$script:Task6AviBufferBytes = 65536
$script:Task6AviMaximumFrameBytes = 33554432

function Read-Task6StreamBytes {
    param(
        [Parameter(Mandatory)][System.IO.Stream]$Stream,
        [Parameter(Mandatory)][int]$Count,
        [string]$Label = 'stream data'
    )

    if ($Count -lt 0 -or $Count -gt 1048576) {
        throw "$Label requested an unsafe in-memory read of $Count bytes."
    }
    $buffer = New-Object byte[] $Count
    $offset = 0
    while ($offset -lt $Count) {
        $read = $Stream.Read($buffer, $offset, $Count - $offset)
        if ($read -le 0) { throw "Unexpected EOF while reading $Label." }
        $offset += $read
    }
    return $buffer
}

function Read-Task6AviFourCc {
    param([Parameter(Mandatory)][System.IO.Stream]$Stream, [string]$Label = 'AVI FourCC')
    return [Text.Encoding]::ASCII.GetString((Read-Task6StreamBytes -Stream $Stream -Count 4 -Label $Label))
}

function Read-Task6AviUInt32 {
    param([Parameter(Mandatory)][System.IO.Stream]$Stream, [string]$Label = 'AVI uint32')
    return [BitConverter]::ToUInt32((Read-Task6StreamBytes -Stream $Stream -Count 4 -Label $Label), 0)
}

function Get-Task6AviPaddedEnd {
    param([long]$PayloadStart, [uint32]$Size)
    return [long]$PayloadStart + [long]$Size + ([long]$Size % 2L)
}

function Get-Task6AviLayout {
    param([Parameter(Mandatory)][string]$Path)

    $resolved = (Resolve-Path -LiteralPath $Path).Path
    $stream = [System.IO.FileStream]::new($resolved, [System.IO.FileMode]::Open,
        [System.IO.FileAccess]::Read, [System.IO.FileShare]::Read)
    try {
        if ($stream.Length -lt 1024) { throw "AVI proof is too small: $resolved" }
        $riff = Read-Task6AviFourCc -Stream $stream -Label 'RIFF signature'
        $null = Read-Task6AviUInt32 -Stream $stream -Label 'RIFF length'
        $form = Read-Task6AviFourCc -Stream $stream -Label 'AVI form type'
        if ($riff -cne 'RIFF' -or $form -cne 'AVI ') {
            throw "AVI RIFF signature is invalid: $resolved"
        }

        $pending = New-Object System.Collections.Stack
        $pending.Push([pscustomobject]@{ Start = [long]12; End = [long]$stream.Length })
        $header = $null
        $moviStart = -1L
        $moviEnd = -1L
        $indexStart = -1L
        $indexSize = 0L

        while ($pending.Count -gt 0) {
            $range = $pending.Pop()
            $stream.Position = $range.Start
            while ($stream.Position + 8L -le $range.End) {
                $chunkStart = [long]$stream.Position
                $chunkId = Read-Task6AviFourCc -Stream $stream -Label 'AVI chunk id'
                $chunkSize = Read-Task6AviUInt32 -Stream $stream -Label "AVI chunk $chunkId size"
                $payloadStart = [long]$stream.Position
                $paddedEnd = Get-Task6AviPaddedEnd -PayloadStart $payloadStart -Size $chunkSize
                if ($paddedEnd -gt $range.End -or $paddedEnd -gt $stream.Length) {
                    throw "AVI chunk '$chunkId' exceeds its RIFF boundary at byte $chunkStart."
                }

                if ($chunkId -ceq 'LIST') {
                    if ($chunkSize -lt 4) { throw "AVI LIST at byte $chunkStart is truncated." }
                    $listType = Read-Task6AviFourCc -Stream $stream -Label 'AVI LIST type'
                    if ($listType -ceq 'movi') {
                        $moviStart = [long]$stream.Position
                        $moviEnd = [long]$payloadStart + [long]$chunkSize
                    }
                    else {
                        $childStart = [long]$stream.Position
                        $pending.Push([pscustomobject]@{ Start = $childStart; End = [long]$payloadStart + [long]$chunkSize })
                    }
                }
                elseif ($chunkId -ceq 'avih') {
                    if ($chunkSize -lt 40) { throw "AVI main header is truncated: $resolved" }
                    $data = Read-Task6StreamBytes -Stream $stream -Count 40 -Label 'AVI main header'
                    $header = [pscustomobject]@{
                        MicrosecondsPerFrame = [BitConverter]::ToUInt32($data, 0)
                        TotalFrames = [BitConverter]::ToUInt32($data, 16)
                        Width = [BitConverter]::ToUInt32($data, 32)
                        Height = [BitConverter]::ToUInt32($data, 36)
                    }
                }
                elseif ($chunkId -ceq 'idx1') {
                    $indexStart = $payloadStart
                    $indexSize = [long]$chunkSize
                }
                $stream.Position = $paddedEnd
            }
        }

        if ($null -eq $header -or $moviStart -lt 0 -or $moviEnd -le $moviStart -or
            $indexStart -lt 0 -or $indexSize -le 0) {
            throw "AVI main/media/index structure is invalid: $resolved"
        }
        if ($header.MicrosecondsPerFrame -eq 0 -or $header.TotalFrames -eq 0) {
            throw "AVI main header has an invalid frame contract: $resolved"
        }
        if (($indexSize % 16L) -ne 0L -or $indexSize -lt ([long]$header.TotalFrames * 16L)) {
            throw "AVI index does not account for declared frames: $resolved"
        }
        return [pscustomobject]@{
            Path = $resolved; Header = $header; MoviStart = $moviStart; MoviEnd = $moviEnd;
            IndexStart = $indexStart; IndexSize = $indexSize; Length = [long]$stream.Length
        }
    }
    finally { $stream.Dispose() }
}

function Add-Task6MjpegFramesFromRange {
    param(
        [Parameter(Mandatory)][System.IO.Stream]$Stream,
        [Parameter(Mandatory)][long]$Start,
        [Parameter(Mandatory)][long]$End,
        [Parameter(Mandatory)][AllowEmptyCollection()][System.Collections.Generic.List[object]]$Frames
    )

    $stream.Position = $Start
    while ($stream.Position + 8L -le $End) {
        $chunkStart = [long]$stream.Position
        $chunkId = Read-Task6AviFourCc -Stream $stream -Label 'movi chunk id'
        $chunkSize = Read-Task6AviUInt32 -Stream $stream -Label "movi chunk $chunkId size"
        $payloadStart = [long]$stream.Position
        $paddedEnd = Get-Task6AviPaddedEnd -PayloadStart $payloadStart -Size $chunkSize
        if ($paddedEnd -gt $End) {
            throw "AVI movi chunk '$chunkId' exceeds media boundary at byte $chunkStart."
        }
        if ($chunkId -ceq 'LIST') {
            if ($chunkSize -lt 4) { throw "AVI nested LIST at byte $chunkStart is truncated." }
            $null = Read-Task6AviFourCc -Stream $stream -Label 'nested movi LIST type'
            Add-Task6MjpegFramesFromRange -Stream $stream -Start ([long]$stream.Position) `
                -End ([long]$payloadStart + [long]$chunkSize) -Frames $Frames
        }
        elseif ($chunkId -ceq '00dc' -or $chunkId -ceq '00db') {
            if ($chunkSize -lt 4 -or $chunkSize -gt $script:Task6AviMaximumFrameBytes) {
                throw "AVI video frame at byte $chunkStart has unsafe size $chunkSize."
            }
            $signature = Read-Task6StreamBytes -Stream $stream -Count 2 -Label 'MJPEG signature'
            if ($signature[0] -ne 0xff -or $signature[1] -ne 0xd8) {
                throw "AVI video chunk $($Frames.Count + 1) is not an MJPEG frame."
            }
            $Frames.Add([pscustomobject]@{ Offset = $payloadStart; Size = [long]$chunkSize })
        }
        $stream.Position = $paddedEnd
    }
}

function Get-Task6AviMjpegIndex {
    param([Parameter(Mandatory)][string]$Path)

    $layout = Get-Task6AviLayout -Path $Path
    $stream = [System.IO.FileStream]::new($layout.Path, [System.IO.FileMode]::Open,
        [System.IO.FileAccess]::Read, [System.IO.FileShare]::Read)
    try {
        $indexEnd = [long]$layout.IndexStart + [long]$layout.IndexSize
        if ($indexEnd -gt $layout.Length) { throw "AVI idx1 extends past EOF: $($layout.Path)" }
        $stream.Position = $layout.IndexStart
        $indexedVideoFrames = 0
        while ($stream.Position -lt $indexEnd) {
            $entry = Read-Task6StreamBytes -Stream $stream -Count 16 -Label 'AVI idx1 entry'
            $kind = [Text.Encoding]::ASCII.GetString($entry, 0, 4)
            if ($kind -ceq '00dc' -or $kind -ceq '00db') {
                if ([BitConverter]::ToUInt32($entry, 12) -le 0) {
                    throw "AVI idx1 contains an empty video frame: $($layout.Path)"
                }
                $indexedVideoFrames++
            }
        }
        if ($indexedVideoFrames -ne $layout.Header.TotalFrames) {
            throw "AVI indexed frame count mismatch: expected=$($layout.Header.TotalFrames) actual=$indexedVideoFrames"
        }

        $frames = [System.Collections.Generic.List[object]]::new()
        Add-Task6MjpegFramesFromRange -Stream $stream -Start $layout.MoviStart -End $layout.MoviEnd -Frames $frames
        if ($frames.Count -ne $layout.Header.TotalFrames) {
            throw "AVI streamed frame count mismatch: expected=$($layout.Header.TotalFrames) actual=$($frames.Count)"
        }
        return [pscustomobject]@{ Header = $layout.Header; Frames = $frames; Layout = $layout }
    }
    finally { $stream.Dispose() }
}

function Copy-Task6AviStreamRange {
    param(
        [Parameter(Mandatory)][System.IO.Stream]$InputStream,
        [Parameter(Mandatory)][System.IO.Stream]$OutputStream,
        [Parameter(Mandatory)][long]$Start,
        [Parameter(Mandatory)][long]$Length
    )

    if ($Start -lt 0 -or $Length -lt 0 -or $Start + $Length -gt $InputStream.Length) {
        throw "AVI bounded copy exceeds source: start=$Start length=$Length source=$($InputStream.Length)"
    }
    $InputStream.Position = $Start
    $remaining = $Length
    $buffer = New-Object byte[] $script:Task6AviBufferBytes
    while ($remaining -gt 0) {
        $requested = [int][Math]::Min([long]$buffer.Length, $remaining)
        $read = $InputStream.Read($buffer, 0, $requested)
        if ($read -le 0) { throw 'Unexpected EOF while streaming an AVI range.' }
        $OutputStream.Write($buffer, 0, $read)
        $remaining -= $read
    }
}

function Write-Task6AviUInt32At {
    param(
        [Parameter(Mandatory)][System.IO.Stream]$Stream,
        [Parameter(Mandatory)][long]$Offset,
        [Parameter(Mandatory)][uint32]$Value
    )
    if ($Offset -lt 0 -or $Offset + 4L -gt $Stream.Length) {
        throw "AVI uint32 write offset is out of bounds: $Offset"
    }
    $Stream.Position = $Offset
    $bytes = [BitConverter]::GetBytes($Value)
    $Stream.Write($bytes, 0, $bytes.Length)
}

function Get-Task6AviUInt32At {
    param(
        [Parameter(Mandatory)][string]$Path,
        [Parameter(Mandatory)][long]$Offset,
        [Parameter(Mandatory)][string]$Label
    )
    $stream = [IO.FileStream]::new((Resolve-Path -LiteralPath $Path).Path, [IO.FileMode]::Open,
        [IO.FileAccess]::Read, [IO.FileShare]::Read)
    try {
        if ($Offset -lt 0 -or $Offset + 4L -gt $stream.Length) {
            throw "$Label offset is out of bounds: $Offset"
        }
        $stream.Position = $Offset
        return Read-Task6AviUInt32 -Stream $stream -Label $Label
    }
    finally { $stream.Dispose() }
}

function Get-Task6AviContainerOffsets {
    param([Parameter(Mandatory)][string]$Path)

    $index = Get-Task6AviMjpegIndex -Path $Path
    $stream = [IO.FileStream]::new($index.Layout.Path, [IO.FileMode]::Open,
        [IO.FileAccess]::Read, [IO.FileShare]::Read)
    try {
        if ((Read-Task6AviFourCc -Stream $stream -Label 'RIFF') -cne 'RIFF') {
            throw 'AVI canonical-prefix source has no RIFF signature.'
        }
        $null = Read-Task6AviUInt32 -Stream $stream -Label 'RIFF size'
        if ((Read-Task6AviFourCc -Stream $stream -Label 'AVI form') -cne 'AVI ') {
            throw 'AVI canonical-prefix source has no AVI form.'
        }
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
                $chunkId = Read-Task6AviFourCc -Stream $stream -Label 'AVI chunk id'
                $chunkSize = Read-Task6AviUInt32 -Stream $stream -Label "AVI $chunkId size"
                $payloadStart = [long]$stream.Position
                $paddedEnd = Get-Task6AviPaddedEnd -PayloadStart $payloadStart -Size $chunkSize
                if ($paddedEnd -gt $range.End -or $paddedEnd -gt $stream.Length) {
                    throw "AVI chunk exceeds its canonical-prefix boundary: id=$chunkId start=$chunkStart"
                }
                if ($chunkId -ceq 'LIST') {
                    if ($chunkSize -lt 4) { throw 'AVI canonical-prefix LIST is truncated.' }
                    $listType = Read-Task6AviFourCc -Stream $stream -Label 'AVI LIST type'
                    if ($listType -ceq 'movi') { $moviListChunkStart = $chunkStart }
                    else {
                        $pending.Push([pscustomobject]@{
                            Start = [long]$stream.Position
                            End = [long]$payloadStart + [long]$chunkSize
                        })
                    }
                }
                elseif ($chunkId -ceq 'avih') { $avihPayloadStart = $payloadStart }
                elseif ($chunkId -ceq 'strh') {
                    $stream.Position = $payloadStart
                    $streamType = Read-Task6AviFourCc -Stream $stream -Label 'AVI stream type'
                    if ($streamType -ceq 'vids') { $videoStreamHeaderPayloadStart = $payloadStart }
                }
                elseif ($chunkId -ceq 'idx1') { $indexChunkStart = $chunkStart }
                $stream.Position = $paddedEnd
            }
        }
        if ($avihPayloadStart -lt 0 -or $videoStreamHeaderPayloadStart -lt 0 -or
            $moviListChunkStart -lt 0 -or $indexChunkStart -lt 0) {
            throw 'AVI canonical-prefix source lacks avih/video-strh/movi/idx1 offsets.'
        }
        return [pscustomobject]@{
            Index = $index
            AvihPayloadStart = $avihPayloadStart
            VideoStreamHeaderPayloadStart = $videoStreamHeaderPayloadStart
            MoviListChunkStart = $moviListChunkStart
            IndexChunkStart = $indexChunkStart
        }
    }
    finally { $stream.Dispose() }
}

function Get-Task6AviPayloadSequenceSha256 {
    param(
        [Parameter(Mandatory)][string]$Path,
        [Parameter(Mandatory)][object]$Index,
        [Parameter(Mandatory)][int]$StartIndex,
        [Parameter(Mandatory)][int]$Count
    )
    if ($StartIndex -lt 0 -or $Count -le 0 -or $StartIndex + $Count -gt $Index.Frames.Count) {
        throw "AVI payload digest range is invalid: start=$StartIndex count=$Count frames=$($Index.Frames.Count)"
    }
    $stream = [IO.FileStream]::new((Resolve-Path -LiteralPath $Path).Path, [IO.FileMode]::Open,
        [IO.FileAccess]::Read, [IO.FileShare]::Read)
    $sha = [Security.Cryptography.SHA256]::Create()
    try {
        $buffer = New-Object byte[] $script:Task6AviBufferBytes
        for ($frameIndex = $StartIndex; $frameIndex -lt $StartIndex + $Count; $frameIndex++) {
            $frame = $Index.Frames[$frameIndex]
            $sizeBytes = [BitConverter]::GetBytes([uint32]$frame.Size)
            $null = $sha.TransformBlock($sizeBytes, 0, $sizeBytes.Length, $sizeBytes, 0)
            $stream.Position = [long]$frame.Offset
            $remaining = [long]$frame.Size
            while ($remaining -gt 0) {
                $requested = [int][Math]::Min([long]$buffer.Length, $remaining)
                $read = $stream.Read($buffer, 0, $requested)
                if ($read -le 0) { throw "Unexpected EOF while hashing AVI frame $frameIndex." }
                $null = $sha.TransformBlock($buffer, 0, $read, $buffer, 0)
                $remaining -= $read
            }
        }
        $empty = New-Object byte[] 0
        $null = $sha.TransformFinalBlock($empty, 0, 0)
        return ([BitConverter]::ToString($sha.Hash)).Replace('-', '').ToLowerInvariant()
    }
    finally {
        $sha.Dispose()
        $stream.Dispose()
    }
}

function Assert-Task6CanonicalAviPrefix {
    param(
        [Parameter(Mandatory)][string]$RawPath,
        [Parameter(Mandatory)][string]$CanonicalPath,
        [Parameter(Mandatory)][uint32]$ExpectedFrames,
        [int]$MinimumRawTailFrames = 1,
        [string]$Label = 'AVI'
    )
    if ($MinimumRawTailFrames -lt 1) { throw "$Label canonical prefix requires a positive raw tail." }
    $raw = Get-Task6AviMjpegIndex -Path $RawPath
    $canonical = Get-Task6AviMjpegIndex -Path $CanonicalPath
    [long]$minimumRawFrames = [long]$ExpectedFrames + [long]$MinimumRawTailFrames
    if ([long]$raw.Header.TotalFrames -lt $minimumRawFrames) {
        throw "$Label raw AVI lacks a real trim tail: expected>=$minimumRawFrames actual=$($raw.Header.TotalFrames)"
    }
    if ($canonical.Header.TotalFrames -ne $ExpectedFrames) {
        throw "$Label canonical AVI frame mismatch: expected=$ExpectedFrames actual=$($canonical.Header.TotalFrames)"
    }
    $rawContainer = Get-Task6AviContainerOffsets -Path $RawPath
    $canonicalContainer = Get-Task6AviContainerOffsets -Path $CanonicalPath
    $rawStreamFrames = Get-Task6AviUInt32At -Path $RawPath `
        -Offset ($rawContainer.VideoStreamHeaderPayloadStart + 32L) -Label "$Label raw video stream length"
    $canonicalStreamFrames = Get-Task6AviUInt32At -Path $CanonicalPath `
        -Offset ($canonicalContainer.VideoStreamHeaderPayloadStart + 32L) -Label "$Label canonical video stream length"
    if ($rawStreamFrames -ne $raw.Header.TotalFrames) {
        throw "$Label raw AVI stream/header frame mismatch: stream=$rawStreamFrames main=$($raw.Header.TotalFrames)"
    }
    if ($canonicalStreamFrames -ne $ExpectedFrames) {
        throw "$Label canonical AVI stream frame mismatch: expected=$ExpectedFrames actual=$canonicalStreamFrames"
    }
    foreach ($field in @('Width', 'Height', 'MicrosecondsPerFrame')) {
        if ($raw.Header.$field -ne $canonical.Header.$field) {
            throw "$Label canonical AVI changed ${field}: raw=$($raw.Header.$field) canonical=$($canonical.Header.$field)"
        }
    }
    for ($index = 0; $index -lt [int]$ExpectedFrames; $index++) {
        $rawFrame = $raw.Frames[$index]
        $canonicalFrame = $canonical.Frames[$index]
        if ($rawFrame.Offset -ne $canonicalFrame.Offset -or $rawFrame.Size -ne $canonicalFrame.Size) {
            throw "$Label canonical AVI is not the raw media prefix at frame=$($index + 1)"
        }
    }
    $rawPrefixSha = Get-Task6AviPayloadSequenceSha256 -Path $RawPath -Index $raw -StartIndex 0 `
        -Count ([int]$ExpectedFrames)
    $canonicalPrefixSha = Get-Task6AviPayloadSequenceSha256 -Path $CanonicalPath -Index $canonical -StartIndex 0 `
        -Count ([int]$ExpectedFrames)
    if ($rawPrefixSha -cne $canonicalPrefixSha) {
        throw "$Label canonical AVI payloads are not a byte-identical raw prefix."
    }
    $firstSha = Get-Task6AviPayloadSequenceSha256 -Path $CanonicalPath -Index $canonical -StartIndex 0 -Count 1
    $lastSha = Get-Task6AviPayloadSequenceSha256 -Path $CanonicalPath -Index $canonical `
        -StartIndex ([int]$ExpectedFrames - 1) -Count 1
    return [pscustomobject]@{
        RawFrames = [uint32]$raw.Header.TotalFrames
        CanonicalFrames = [uint32]$canonical.Header.TotalFrames
        TrimmedFrames = [uint32]([long]$raw.Header.TotalFrames - [long]$ExpectedFrames)
        PrefixPayloadSha256 = $canonicalPrefixSha
        FirstFrameSha256 = $firstSha
        LastFrameSha256 = $lastSha
    }
}

function Assert-Task6RawProducerFrameContract {
    param(
        [Parameter(Mandatory)][string]$LogPath,
        [Parameter(Mandatory)][string]$RawPath,
        [Parameter(Mandatory)][uint32]$ExpectedCanonicalFrames,
        [int]$MinimumRawTailFrames = 30,
        [string]$Label = 'full-flow'
    )
    if (-not (Test-Path -LiteralPath $LogPath -PathType Leaf)) {
        throw "$Label raw producer log is missing: $LogPath"
    }
    $raw = Get-Task6AviMjpegIndex -Path $RawPath
    [long]$minimumRawFrames = [long]$ExpectedCanonicalFrames + [long]$MinimumRawTailFrames
    if ([long]$raw.Header.TotalFrames -lt $minimumRawFrames) {
        throw "$Label raw producer AVI lacks the declared trim tail: expected>=$minimumRawFrames actual=$($raw.Header.TotalFrames)"
    }
    $pattern = '^TASK6_RAW_PRODUCER_FRAME_OK canonical_frames=(?<canonical>\d+) ' +
        'raw_terminal_process_frame=(?<terminal>\d+) minimum_trim_tail_frames=(?<minimum>\d+) ' +
        'title_visible=True audio_released=True\s*$'
    $matches = @([regex]::Matches((Get-Content -LiteralPath $LogPath -Raw), $pattern,
            [Text.RegularExpressions.RegexOptions]::Multiline))
    if ($matches.Count -ne 1) {
        throw "$Label raw producer marker must occur exactly once; actual=$($matches.Count)"
    }
    [long]$canonical = [long]$matches[0].Groups['canonical'].Value
    [long]$terminal = [long]$matches[0].Groups['terminal'].Value
    [long]$minimum = [long]$matches[0].Groups['minimum'].Value
    if ($canonical -ne [long]$ExpectedCanonicalFrames -or $minimum -ne $MinimumRawTailFrames) {
        throw "$Label raw producer marker contract mismatch: canonical=$canonical minimum=$minimum"
    }
    if ($terminal -lt $minimumRawFrames) {
        throw "$Label raw producer terminal is before the auditable tail: terminal=$terminal minimum=$minimumRawFrames"
    }
    [long]$writerShutdownFrames = [long]$raw.Header.TotalFrames - $terminal
    if ($writerShutdownFrames -lt 1 -or $writerShutdownFrames -gt 2) {
        throw "$Label raw producer/movie shutdown boundary is unbound: raw=$($raw.Header.TotalFrames) terminal=$terminal delta=$writerShutdownFrames"
    }
    return [pscustomobject]@{
        RawFrames = [uint32]$raw.Header.TotalFrames
        TerminalProcessFrame = [uint32]$terminal
        WriterShutdownFrames = [uint32]$writerShutdownFrames
    }
}

function New-Task6CanonicalAviPrefix {
    param(
        [Parameter(Mandatory)][string]$SourcePath,
        [Parameter(Mandatory)][string]$Destination,
        [Parameter(Mandatory)][uint32]$ExpectedFrames,
        [int]$MinimumRawTailFrames = 1,
        [string]$Label = 'AVI'
    )
    if (Test-Path -LiteralPath $Destination) {
        throw "$Label canonical AVI destination must be fresh: $Destination"
    }
    $container = Get-Task6AviContainerOffsets -Path $SourcePath
    $raw = $container.Index
    [long]$minimumRawFrames = [long]$ExpectedFrames + [long]$MinimumRawTailFrames
    if ([long]$raw.Header.TotalFrames -lt $minimumRawFrames) {
        throw "$Label raw AVI lacks a real trim tail: expected>=$minimumRawFrames actual=$($raw.Header.TotalFrames)"
    }
    $retainedLastFrame = $raw.Frames[[int]$ExpectedFrames - 1]
    $retainedFrameStart = [long]$retainedLastFrame.Offset - 8L
    $retainedFrameEnd = $retainedFrameStart + 8L + [long]$retainedLastFrame.Size +
        ([long]$retainedLastFrame.Size % 2L)
    if ($retainedFrameEnd -gt $raw.Layout.MoviEnd) {
        throw "$Label canonical prefix frame exceeds the raw movi boundary."
    }

    $input = [IO.FileStream]::new($raw.Layout.Path, [IO.FileMode]::Open,
        [IO.FileAccess]::Read, [IO.FileShare]::Read)
    $output = [IO.FileStream]::new($Destination, [IO.FileMode]::CreateNew,
        [IO.FileAccess]::ReadWrite, [IO.FileShare]::None)
    try {
        $retainedEntries = [Collections.Generic.List[byte[]]]::new()
        $input.Position = $raw.Layout.IndexStart
        $indexEnd = [long]$raw.Layout.IndexStart + [long]$raw.Layout.IndexSize
        $retainedVideoEntries = 0
        while ($input.Position -lt $indexEnd) {
            $entry = Read-Task6StreamBytes -Stream $input -Count 16 -Label 'AVI idx1 canonical-prefix entry'
            $kind = [Text.Encoding]::ASCII.GetString($entry, 0, 4)
            [long]$chunkStart = [long][BitConverter]::ToUInt32($entry, 8) +
                ([long]$raw.Layout.MoviStart - 4L)
            if ($chunkStart -lt $retainedFrameEnd) {
                $retainedEntries.Add($entry)
                if ($kind -ceq '00dc' -or $kind -ceq '00db') { $retainedVideoEntries++ }
            }
        }
        if ($retainedVideoEntries -ne [int]$ExpectedFrames) {
            throw "$Label canonical prefix retained $retainedVideoEntries video index entries; expected=$ExpectedFrames"
        }

        Copy-Task6AviStreamRange -InputStream $input -OutputStream $output -Start 0 -Length $retainedFrameEnd
        $idxId = [Text.Encoding]::ASCII.GetBytes('idx1')
        $output.Write($idxId, 0, $idxId.Length)
        [uint32]$newIndexSize = [uint32]($retainedEntries.Count * 16)
        $indexSizeBytes = [BitConverter]::GetBytes($newIndexSize)
        $output.Write($indexSizeBytes, 0, $indexSizeBytes.Length)
        foreach ($entry in $retainedEntries) { $output.Write($entry, 0, $entry.Length) }

        Write-Task6AviUInt32At -Stream $output -Offset 4 -Value ([uint32]($output.Length - 8L))
        Write-Task6AviUInt32At -Stream $output -Offset ($container.AvihPayloadStart + 16L) -Value $ExpectedFrames
        Write-Task6AviUInt32At -Stream $output -Offset ($container.VideoStreamHeaderPayloadStart + 32L) -Value $ExpectedFrames
        [uint32]$newMoviSize = [uint32]($retainedFrameEnd - ($container.MoviListChunkStart + 8L))
        Write-Task6AviUInt32At -Stream $output -Offset ($container.MoviListChunkStart + 4L) -Value $newMoviSize
    }
    finally {
        $output.Dispose()
        $input.Dispose()
    }
    return Assert-Task6CanonicalAviPrefix -RawPath $SourcePath -CanonicalPath $Destination `
        -ExpectedFrames $ExpectedFrames -MinimumRawTailFrames $MinimumRawTailFrames -Label $Label
}

function Copy-Task6AviFrameToFile {
    param(
        [Parameter(Mandatory)][string]$AviPath,
        [Parameter(Mandatory)][long]$Offset,
        [Parameter(Mandatory)][long]$Size,
        [Parameter(Mandatory)][string]$Destination
    )

    if ($Size -lt 4 -or $Size -gt $script:Task6AviMaximumFrameBytes) {
        throw "Refusing unsafe MJPEG frame copy of $Size bytes."
    }
    $input = [System.IO.FileStream]::new((Resolve-Path -LiteralPath $AviPath).Path,
        [System.IO.FileMode]::Open, [System.IO.FileAccess]::Read, [System.IO.FileShare]::Read)
    $output = [System.IO.FileStream]::new($Destination, [System.IO.FileMode]::CreateNew,
        [System.IO.FileAccess]::Write, [System.IO.FileShare]::None)
    try {
        $input.Position = $Offset
        $remaining = $Size
        $buffer = New-Object byte[] $script:Task6AviBufferBytes
        while ($remaining -gt 0) {
            $requested = [int][Math]::Min([long]$buffer.Length, $remaining)
            $read = $input.Read($buffer, 0, $requested)
            if ($read -le 0) { throw 'Unexpected EOF while streaming an MJPEG frame.' }
            $output.Write($buffer, 0, $read)
            $remaining -= $read
        }
    }
    finally {
        $output.Dispose()
        $input.Dispose()
    }
}

function Get-Task6BytesSha256 {
    param([Parameter(Mandatory)][byte[]]$Bytes)
    $algorithm = [Security.Cryptography.SHA256]::Create()
    try {
        return ([BitConverter]::ToString($algorithm.ComputeHash($Bytes)) -replace '-', '')
    }
    finally { $algorithm.Dispose() }
}

function Get-Task6ImageSample {
    param([Parameter(Mandatory)][string]$Path)

    # A decoded 1280x720 PNG is bounded evidence (unlike the multi-hundred-MB AVI).  Sample a
    # distributed 20px grid so motion checks compare image content, rather than MJPEG bytes.
    Add-Type -AssemblyName System.Drawing
    $bitmap = [Drawing.Bitmap]::new((Resolve-Path -LiteralPath $Path).Path)
    try {
        if ($bitmap.Width -ne 1280 -or $bitmap.Height -ne 720) {
            throw "Task6 image sample must be 1280x720: $Path actual=$($bitmap.Width)x$($bitmap.Height)"
        }
        $bytes = [Collections.Generic.List[byte]]::new()
        $points = 0
        for ($y = 10; $y -lt $bitmap.Height; $y += 20) {
            for ($x = 10; $x -lt $bitmap.Width; $x += 20) {
                $color = $bitmap.GetPixel($x, $y)
                $bytes.Add($color.R)
                $bytes.Add($color.G)
                $bytes.Add($color.B)
                $points++
            }
        }
        $sample = $bytes.ToArray()
        return [pscustomobject]@{
            Bytes = $sample; Points = $points; Sha256 = (Get-Task6BytesSha256 -Bytes $sample)
        }
    }
    finally { $bitmap.Dispose() }
}

function Get-Task6ImageSampleDelta {
    param(
        [Parameter(Mandatory)][byte[]]$Left,
        [Parameter(Mandatory)][byte[]]$Right
    )
    if ($Left.Length -ne $Right.Length -or ($Left.Length % 3) -ne 0) {
        throw "Task6 image sample dimensions differ: left=$($Left.Length) right=$($Right.Length)"
    }
    [long]$changedPoints = 0
    [long]$absoluteDelta = 0
    for ($index = 0; $index -lt $Left.Length; $index += 3) {
        [int]$red = [Math]::Abs([int]$Left[$index] - [int]$Right[$index])
        [int]$green = [Math]::Abs([int]$Left[$index + 1] - [int]$Right[$index + 1])
        [int]$blue = [Math]::Abs([int]$Left[$index + 2] - [int]$Right[$index + 2])
        [int]$delta = $red + $green + $blue
        if ($delta -ne 0) { $changedPoints++ }
        $absoluteDelta += $delta
    }
    return [pscustomobject]@{
        Points = [long]($Left.Length / 3); ChangedPoints = $changedPoints; AbsoluteDelta = $absoluteDelta
    }
}

function Assert-Task6CompletionCardVisual {
    param([Parameter(Mandatory)][string]$Path)

    # Completion is a specifically required visible proof, not merely a terminal log line.
    # Decode the manifest-bound 1280x720 PNG and require the centered formal-card field plus
    # its gold top border.  The two independent samples reject the prior Boss-HUD frame while
    # leaving ordinary scene lighting and anti-aliasing outside the proof window irrelevant.
    Add-Type -AssemblyName System.Drawing
    $bitmap = [Drawing.Bitmap]::new((Resolve-Path -LiteralPath $Path).Path)
    try {
        if ($bitmap.Width -ne 1280 -or $bitmap.Height -ne 720) {
            throw "Task6 completion proof must be 1280x720: $Path actual=$($bitmap.Width)x$($bitmap.Height)"
        }
        [int]$panelSamples = 0
        [int]$goldBorderSamples = 0
        for ($y = 105; $y -le 615; $y += 10) {
            for ($x = 395; $x -le 885; $x += 10) {
                $color = $bitmap.GetPixel($x, $y)
                if ($color.R -le 22 -and $color.G -ge 20 -and $color.G -le 35 -and
                    $color.B -ge 28 -and $color.B -le 45) {
                    $panelSamples++
                }
            }
        }
        for ($x = 400; $x -le 880; $x += 8) {
            $color = $bitmap.GetPixel($x, 100)
            if ($color.R -gt 110 -and $color.G -gt 80 -and $color.G -lt 180 -and $color.B -lt 90) {
                $goldBorderSamples++
            }
        }
        if ($panelSamples -lt 2000 -or $goldBorderSamples -lt 48) {
            throw "Task6 decoded completion keyframe does not visibly contain the formal completion card: " +
                "panel_samples=$panelSamples gold_border_samples=$goldBorderSamples path=$Path"
        }
        return [pscustomobject]@{ PanelSamples = $panelSamples; GoldBorderSamples = $goldBorderSamples }
    }
    finally { $bitmap.Dispose() }
}

function Assert-Task6ModeHudVisual {
    param([Parameter(Mandatory)][string]$Path)

    # The named mode HUD frame is only meaningful once the formal HUD has actually reached the
    # movie writer.  A log marker alone used to select its all-gray transition predecessor.
    # Require two independently sampled elements of the rendered formal HUD: its warm text / UI
    # field and the dark ink / panel field in the upper-left HUD region.  The bounds intentionally
    # tolerate anti-aliasing but reject an empty, black, or uniform gray transition surface.
    Add-Type -AssemblyName System.Drawing
    $bitmap = [Drawing.Bitmap]::new((Resolve-Path -LiteralPath $Path).Path)
    try {
        if ($bitmap.Width -ne 1280 -or $bitmap.Height -ne 720) {
            throw "Task6 mode HUD proof must be 1280x720: $Path actual=$($bitmap.Width)x$($bitmap.Height)"
        }
        [int]$creamSamples = 0
        [int]$inkSamples = 0
        for ($y = 0; $y -le 185; $y++) {
            for ($x = 0; $x -le 500; $x++) {
                $color = $bitmap.GetPixel($x, $y)
                if ($color.R -ge 185 -and $color.G -ge 155 -and $color.B -ge 100) {
                    $creamSamples++
                }
                if ($color.R -le 35 -and $color.G -le 45 -and $color.B -le 60) {
                    $inkSamples++
                }
            }
        }
        if ($creamSamples -lt 500 -or $inkSamples -lt 5000) {
            throw "Task6 decoded mode-HUD keyframe does not visibly contain the formal HUD: " +
                "cream_samples=$creamSamples ink_samples=$inkSamples path=$Path"
        }
        return [pscustomobject]@{ CreamSamples = $creamSamples; InkSamples = $inkSamples }
    }
    finally { $bitmap.Dispose() }
}

function Assert-Task6SecondBoostRejectedVisual {
    param([Parameter(Mandatory)][string]$Path)

    # The second attached Space proof must show the released rope together with the purple
    # rejection ring.  This blocks a marker that names the pre-rejection rope frame while keeping
    # the test independent of JPEG byte identity and ordinary background movement.
    Add-Type -AssemblyName System.Drawing
    $bitmap = [Drawing.Bitmap]::new((Resolve-Path -LiteralPath $Path).Path)
    try {
        if ($bitmap.Width -ne 1280 -or $bitmap.Height -ne 720) {
            throw "Task6 second-boost-rejection proof must be 1280x720: $Path actual=$($bitmap.Width)x$($bitmap.Height)"
        }
        [int]$ropeCyanSamples = 0
        [int]$purpleRingSamples = 0
        for ($y = 120; $y -le 420; $y++) {
            for ($x = 540; $x -le 850; $x++) {
                $color = $bitmap.GetPixel($x, $y)
                if ($color.R -le 150 -and $color.G -ge 170 -and $color.B -ge 160) {
                    $ropeCyanSamples++
                }
            }
        }
        for ($y = 140; $y -le 250; $y++) {
            for ($x = 740; $x -le 850; $x++) {
                $color = $bitmap.GetPixel($x, $y)
                if ($color.R -ge 130 -and $color.R -le 220 -and
                    $color.G -ge 80 -and $color.G -le 180 -and
                    $color.B -ge 170 -and $color.B -le 255) {
                    $purpleRingSamples++
                }
            }
        }
        if ($ropeCyanSamples -gt 150 -or $purpleRingSamples -lt 100) {
            throw "Task6 decoded second-boost-rejection keyframe does not visibly show released rope and rejection ring: " +
                "rope_cyan_samples=$ropeCyanSamples purple_ring_samples=$purpleRingSamples path=$Path"
        }
        return [pscustomobject]@{ RopeCyanSamples = $ropeCyanSamples; PurpleRingSamples = $purpleRingSamples }
    }
    finally { $bitmap.Dispose() }
}

function Assert-Task6AviMovieContract {
    param([Parameter(Mandatory)][string]$Path, [int]$MinimumFrames, [string]$Label = 'AVI')
    # Compatibility helper for bounded historical/probe checks only.  Formal Task 6 evidence
    # must use Assert-Task6FormalAviMovieContract below, whose declared count is exact.
    $index = Get-Task6AviMjpegIndex -Path $Path
    $header = $index.Header
    $fps = [Math]::Round(1000000.0 / [double]$header.MicrosecondsPerFrame)
    if ($header.Width -ne 1280 -or $header.Height -ne 720 -or $fps -ne 60 -or
        $header.TotalFrames -lt $MinimumFrames) {
        throw "$Label AVI fails the 1280x720/60 contract: $($header.Width)x$($header.Height) $($header.TotalFrames)@$fps"
    }
    return $index
}

function Get-Task6FormalCaptureFrameContract {
    # The fixed deterministic presentation runners write these exact MJPEG frame counts at
    # 1280x720/60.  This is a capture contract, not a minimum-duration heuristic: the streaming
    # AVI parser independently reconciles the main header, idx1 count, and movi frame count.
    return [pscustomobject]@{
        FullFlow = [uint32]4053
        BossRetry = [uint32]2719
    }
}

function Assert-Task6FormalAviMovieContract {
    param(
        [Parameter(Mandatory)][string]$Path,
        [Parameter(Mandatory)][uint32]$ExpectedFrames,
        [string]$Label = 'AVI'
    )

    $index = Assert-Task6AviMovieContract -Path $Path -MinimumFrames 1 -Label $Label
    if ($index.Header.TotalFrames -ne $ExpectedFrames) {
        throw "formal_frame_count_mismatch label=$Label expected=$ExpectedFrames actual=$($index.Header.TotalFrames)"
    }
    return $index
}

function Get-Task6ExactStageFrameMap {
    param(
        [Parameter(Mandatory)][string]$LogPath,
        [Parameter(Mandatory)][string]$StagePrefix,
        [Parameter(Mandatory)][string[]]$ExpectedStages,
        [string]$Label = 'presentation'
    )

    # Engine logs are intentionally small and may be read as text. Large AVI containers are
    # parsed above through bounded FileStream reads only.
    $lines = @(Get-Content -LiteralPath $LogPath)
    $allStageLines = @($lines | Where-Object {
        $_ -match ('^' + [regex]::Escape($StagePrefix) + '=')
    })
    if ($allStageLines.Count -ne $ExpectedStages.Count) {
        throw "$Label stage count must be $($ExpectedStages.Count); actual=$($allStageLines.Count)"
    }

    $frames = [ordered]@{}
    $previous = -1L
    foreach ($stage in $ExpectedStages) {
        $pattern = '^' + [regex]::Escape($StagePrefix + '=' + $stage) +
            ' frame=(?<frame>\d+)\s*$'
        $matches = [Collections.Generic.List[System.Text.RegularExpressions.Match]]::new()
        foreach ($line in $lines) {
            $match = [regex]::Match($line, $pattern)
            if ($match.Success) { $matches.Add($match) }
        }
        if ($matches.Count -ne 1) {
            throw "$Label stage '$stage' must occur exactly once; actual=$($matches.Count)"
        }
        [long]$frame = [long]$matches[0].Groups['frame'].Value
        if ($frame -le $previous) {
            throw "$Label stage '$stage' is not strictly ordered; frame=$frame previous=$previous"
        }
        $frames[$stage] = $frame
        $previous = $frame
    }
    return [pscustomobject]@{ Lines = $lines; Frames = $frames }
}

function Assert-Task6FullFlowTimelineContract {
    param(
        [Parameter(Mandatory)][string]$LogPath,
        [Parameter(Mandatory)][long]$TotalFrames,
        [Parameter(Mandatory)][uint32]$ExpectedFrames,
        [Parameter(Mandatory)][string[]]$ExpectedStages,
        [Parameter(Mandatory)][string[]]$ExpectedReticles,
        [string]$Label = 'full-flow'
    )

    # The visible timeline has explicit dwell/motion windows below and an exact deterministic
    # movie count.  Do not weaken this to >=4000: 4052/4054 can satisfy every local dwell while
    # still proving a different recording than the fixed presentation contract.
    if ($TotalFrames -ne [long]$ExpectedFrames) {
        throw "formal_frame_count_mismatch label=$Label expected=$ExpectedFrames actual=$TotalFrames"
    }
    $stages = Get-Task6ExactStageFrameMap -LogPath $LogPath -StagePrefix 'PROLOGUE_PRESENTATION_STAGE' `
        -ExpectedStages $ExpectedStages -Label $Label
    $frames = $stages.Frames
    $windows = @(
        [pscustomobject]@{ From = 'title_start'; To = 'mode_hud'; Minimum = 36L; Label = 'title dwell' },
        [pscustomobject]@{ From = 'recovery'; To = 'canyon_descent'; Minimum = 180L; Label = 'recovery dwell' },
        [pscustomobject]@{ From = 'canyon_descent'; To = 'canyon_basin'; Minimum = 300L; Label = 'descent traversal' },
        [pscustomobject]@{ From = 'canyon_basin'; To = 'checkpoint_basin'; Minimum = 90L; Label = 'basin checkpoint approach' },
        [pscustomobject]@{ From = 'checkpoint_basin'; To = 'shield_overheat'; Minimum = 180L; Label = 'basin checkpoint dwell' },
        [pscustomobject]@{ From = 'ascent_one'; To = 'ascent_two'; Minimum = 120L; Label = 'first ascent dwell' },
        [pscustomobject]@{ From = 'ascent_two'; To = 'ascent_three'; Minimum = 120L; Label = 'second ascent dwell' },
        [pscustomobject]@{ From = 'optional_alcove'; To = 'lost_temple'; Minimum = 18L; Label = 'optional alcove dwell' },
        [pscustomobject]@{ From = 'lost_temple'; To = 'checkpoint_temple'; Minimum = 180L; Label = 'temple approach' },
        [pscustomobject]@{ From = 'checkpoint_temple'; To = 'boss_intro'; Minimum = 60L; Label = 'temple checkpoint dwell' },
        [pscustomobject]@{ From = 'death_retry'; To = 'completion'; Minimum = 120L; Label = 'Boss death/retry/completion dwell' },
        # Boost state changes are instantaneous live InputMap/physics events.  They must be
        # separated by real movie frames, but this contract deliberately does not pad them with
        # paused or repeated static frames.  The extractor verifies each critical event against
        # both adjacent AVI frames and requires actual sampled-pixel movement.
        # The latch must hand off to the first Space boost on the immediately following
        # simulation tick; requiring two frames there would alter the live mechanic.
        [pscustomobject]@{ From = 'boost_attached'; To = 'boost_start'; Minimum = 1L; Label = 'attached boost handoff' },
        [pscustomobject]@{ From = 'boost_start'; To = 'attached_boost_consumed'; Minimum = 1L; Label = 'boost consume observation' },
        [pscustomobject]@{ From = 'attached_boost_consumed'; To = 'boost_arc'; Minimum = 1L; Label = 'boost arc observation' },
        [pscustomobject]@{ From = 'boost_start'; To = 'boost_arc'; Minimum = 2L; Label = 'boost start-to-arc motion' },
        [pscustomobject]@{ From = 'boost_arc'; To = 'boost_second_rejected'; Minimum = 2L; Label = 'boost arc transition' },
        [pscustomobject]@{ From = 'boost_second_rejected'; To = 'boost_land'; Minimum = 2L; Label = 'second boost rejection transition' },
        [pscustomobject]@{ From = 'boss_sweep'; To = 'boss_leap_slam'; Minimum = 18L; Label = 'Sweep proof dwell' },
        [pscustomobject]@{ From = 'boss_leap_slam'; To = 'boss_ground_wave'; Minimum = 18L; Label = 'Leap Slam proof dwell' },
        [pscustomobject]@{ From = 'boss_ground_wave'; To = 'boss_hammer_throw'; Minimum = 18L; Label = 'Ground Wave proof dwell' },
        [pscustomobject]@{ From = 'boss_hammer_throw'; To = 'boss_attack_contract'; Minimum = 18L; Label = 'Hammer Throw proof dwell' }
    )
    foreach ($window in $windows) {
        [long]$actual = [long]$frames[$window.To] - [long]$frames[$window.From]
        if ($actual -lt $window.Minimum) {
            throw "$Label $($window.Label) is too short: expected=$($window.Minimum) actual=$actual from=$($window.From) to=$($window.To)"
        }
    }

    $reticleFrames = [ordered]@{}
    foreach ($reticle in $ExpectedReticles) {
        $pattern = '^PROLOGUE_RETICLE_DWELL=' + [regex]::Escape($reticle) + ' frame=(?<frame>\d+)\s*$'
        $matches = [Collections.Generic.List[System.Text.RegularExpressions.Match]]::new()
        foreach ($line in $stages.Lines) {
            $match = [regex]::Match($line, $pattern)
            if ($match.Success) { $matches.Add($match) }
        }
        if ($matches.Count -ne 1) {
            throw "$Label reticle '$reticle' must occur exactly once; actual=$($matches.Count)"
        }
        $reticleFrames[$reticle] = [long]$matches[0].Groups['frame'].Value
    }
    for ($index = 0; $index -lt $ExpectedReticles.Count - 1; $index++) {
        [long]$actual = [long]$reticleFrames[$ExpectedReticles[$index + 1]] -
            [long]$reticleFrames[$ExpectedReticles[$index]]
        if ($actual -lt 18L) {
            throw "$Label reticle dwell is too short: expected=18 actual=$actual from=$($ExpectedReticles[$index]) to=$($ExpectedReticles[$index + 1])"
        }
    }
    [long]$lastReticleToBoss = [long]$frames['boss_sweep'] -
        [long]$reticleFrames[$ExpectedReticles[$ExpectedReticles.Count - 1]]
    if ($lastReticleToBoss -lt 18L) {
        throw "$Label final reticle dwell is too short: expected=18 actual=$lastReticleToBoss"
    }
    [long]$titleTail = $TotalFrames - [long]$frames['completion_title_return']
    if ($titleTail -lt 36L) {
        throw "$Label completion-title dwell is too short: expected=36 actual=$titleTail"
    }
    $completePattern = '^PROLOGUE_PRESENTATION_COMPLETE frame=(?<frame>\d+) route=generated_main inputmap=True presentation_reposition=post_proof title_return=True\s*$'
    $completeMatches = [Collections.Generic.List[System.Text.RegularExpressions.Match]]::new()
    foreach ($line in $stages.Lines) {
        $match = [regex]::Match($line, $completePattern)
        if ($match.Success) { $completeMatches.Add($match) }
    }
    if ($completeMatches.Count -ne 1) {
        throw "$Label terminal presentation marker must occur exactly once; actual=$($completeMatches.Count)"
    }
    [long]$completeFrame = [long]$completeMatches[0].Groups['frame'].Value
    if ($completeFrame -gt $TotalFrames -or $completeFrame -lt [long]$frames['completion_title_return']) {
        throw "$Label terminal presentation frame is outside the bound AVI: terminal=$completeFrame avi=$TotalFrames"
    }
    return [pscustomobject]@{
        StageFrames = $frames; ReticleFrames = $reticleFrames; TotalFrames = $TotalFrames;
        CompletionFrame = $completeFrame; TitleTail = $titleTail
    }
}
