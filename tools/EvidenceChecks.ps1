function Remove-PrototypeCaptureOutputs {
    param(
        [Parameter(Mandatory)][string]$ProofPath,
        [switch]$FramesOnly
    )

    $resolvedProof = (Resolve-Path -LiteralPath $ProofPath).Path
    if ($FramesOnly) {
        Get-ChildItem -LiteralPath $resolvedProof -File -Filter 'frame*.png' | ForEach-Object {
            Remove-Item -LiteralPath $_.FullName -Force
        }
        $audio = Join-Path $resolvedProof 'frame.wav'
        if (Test-Path -LiteralPath $audio -PathType Leaf) { Remove-Item -LiteralPath $audio -Force }
        return
    }

    $avi = Join-Path $resolvedProof 'prototype.avi'
    if (Test-Path -LiteralPath $avi -PathType Leaf) { Remove-Item -LiteralPath $avi -Force }
}

function Assert-ExactLogMarker {
    param(
        [Parameter(Mandatory)][string]$Path,
        [Parameter(Mandatory)][string]$Marker
    )

    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) { throw "Required log is missing: $Path" }
    $markerLines = @(Get-Content -LiteralPath $Path | Where-Object { $_ -ceq $Marker })
    if ($markerLines.Count -ne 1) {
        throw "Expected exactly one '$Marker' line in $Path; found $($markerLines.Count)."
    }
}

function Get-EngineFailurePatterns {
    @(
        '(?i)ERROR:',
        '(?i)SCRIPT ERROR:',
        '(?i)Parse Error',
        '(?i)ObjectDB instances were leaked',
        '(?i)resources still in use',
        '(?i)\bRID allocations?\b',
        '(?i)\bfatal\b',
        '(?i)\b(?:unhandled (?:native )?exception|native exception|System\.AccessViolationException)\b',
        '(?i)\b(?:crash[_ -]?handler|crash signature|crash dump|Godot Engine has crashed)\b',
        '(?i)\baccess violation\b',
        '(?i)0xC0000005',
        '(?i)-1073741819',
        '(?i)\bprocess (?:was )?terminated unexpectedly\b',
        '(?i)\bsegmentation fault\b'
    )
}

function Assert-ExactEngineWarnings {
    param(
        [Parameter(Mandatory)][string]$Path,
        [string[]]$ExpectedWarnings = @()
    )

    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        throw "Required log is missing: $Path"
    }
    $actualWarnings = @(Get-Content -LiteralPath $Path | Where-Object { $_ -cmatch '^WARNING:' })
    $problems = [System.Collections.Generic.List[string]]::new()
    foreach ($expected in $ExpectedWarnings) {
        $declaredCount = @($ExpectedWarnings | Where-Object { $_ -ceq $expected }).Count
        if ($declaredCount -ne 1) {
            $problems.Add("expected warning contract itself declares '$expected' $declaredCount times")
            continue
        }
        $actualCount = @($actualWarnings | Where-Object { $_ -ceq $expected }).Count
        if ($actualCount -ne 1) {
            $problems.Add("expected warning '$expected' found $actualCount times")
        }
    }
    foreach ($actual in $actualWarnings) {
        if (@($ExpectedWarnings | Where-Object { $_ -ceq $actual }).Count -ne 1) {
            $problems.Add("unexpected warning '$actual'")
        }
    }
    if ($problems.Count) {
        throw "Engine warning contract failed for $Path`:`n" +
            ($problems -join [Environment]::NewLine)
    }
}

function Assert-EngineLogContract {
    param(
        [Parameter(Mandatory)][string]$Path,
        [string[]]$ExpectedWarnings = @()
    )

    $failurePatterns = @(Get-EngineFailurePatterns)
    Assert-ExactEngineWarnings -Path $Path -ExpectedWarnings $ExpectedWarnings
    $errors = @(Select-String -LiteralPath $Path -Pattern $failurePatterns)
    if (@($errors).Count) {
        $lines = $errors | ForEach-Object { "$($_.Path):$($_.LineNumber):$($_.Line)" }
        throw ($lines -join [Environment]::NewLine)
    }
}

function Assert-NoEngineLogErrors {
    param([Parameter(Mandatory)][string[]]$Paths)

    foreach ($path in $Paths) {
        Assert-EngineLogContract -Path $path
    }
}

function Invoke-MergedNativeCommand {
    param(
        [Parameter(Mandatory)][string]$FilePath,
        [Parameter(Mandatory)][string[]]$ArgumentList,
        [Parameter(Mandatory)][string]$ConsolePath
    )

    $priorErrorActionPreference = $ErrorActionPreference
    try {
        # Windows PowerShell 5 promotes native stderr to ErrorRecord objects. Keep
        # them non-terminating long enough to persist the complete merged stream.
        $ErrorActionPreference = 'Continue'
        $lines = @(& $FilePath @ArgumentList 2>&1 | ForEach-Object { $_.ToString() })
        $exitCode = $LASTEXITCODE
    }
    finally {
        $ErrorActionPreference = $priorErrorActionPreference
    }
    $lines | Set-Content -LiteralPath $ConsolePath -Encoding UTF8
    [pscustomobject]@{ ExitCode = $exitCode; Lines = $lines }
}

function Assert-FreshNonEmptyOutputs {
    param(
        [Parameter(Mandatory)][string[]]$Paths,
        [Parameter(Mandatory)][DateTime]$StartedUtc
    )

    if ($Paths.Count -eq 0) { throw 'Capture produced no output files.' }
    foreach ($path in $Paths) {
        if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { throw "Capture output is missing: $path" }
        $file = Get-Item -LiteralPath $path
        if ($file.Length -le 0) { throw "Capture output is empty: $path" }
        if ($file.LastWriteTimeUtc -lt $StartedUtc) {
            throw "Capture output predates this run: $path (output=$($file.LastWriteTimeUtc.ToString('o')), started=$($StartedUtc.ToString('o')))"
        }
    }
}

function ConvertTo-GodotSceneSemanticText {
    param([Parameter(Mandatory)][AllowEmptyString()][string]$Text)

    $normalized = (($Text -replace '\s+unique_id=\d+', '') -replace "`r`n", "`n")
    $extEntries = [System.Collections.Generic.List[object]]::new()
    $extById = [System.Collections.Generic.Dictionary[string,string]]::new(
        [StringComparer]::Ordinal)
    $extIdentities = [System.Collections.Generic.HashSet[string]]::new(
        [StringComparer]::Ordinal)
    foreach ($match in [regex]::Matches($normalized, '(?m)^\[ext_resource\b[^\]]*\]$')) {
        $line = $match.Value
        $type = [regex]::Match($line, '\btype="([^"]+)"').Groups[1].Value
        $path = [regex]::Match($line, '\bpath="([^"]+)"').Groups[1].Value
        $id = [regex]::Match($line, '\bid="([^"]+)"').Groups[1].Value
        if ([string]::IsNullOrEmpty($type) -or [string]::IsNullOrEmpty($path) -or
            [string]::IsNullOrEmpty($id)) {
            throw "Malformed ext_resource declaration: $line"
        }
        $identity = $type + "`0" + $path
        if ($extById.ContainsKey($id)) {
            throw "Duplicate ext_resource id '$id' is ambiguous."
        }
        $extById.Add($id, $identity)
        if (-not $extIdentities.Add($identity)) {
            throw "Duplicate ext_resource identity '$type|$path' is ambiguous."
        }
        $extEntries.Add([pscustomobject]@{
            Identity = $identity
            Id = $id
        })
    }

    $orderedExt = @($extEntries | Sort-Object -Property Identity)
    $extTokens = [System.Collections.Generic.Dictionary[string,string]]::new(
        [StringComparer]::Ordinal)
    for ($index = 0; $index -lt $orderedExt.Count; $index++) {
        $extTokens.Add($orderedExt[$index].Id,
            ("__GODOT_CANONICAL_EXT_RESOURCE_{0}__" -f $index))
    }

    $subTokens = [System.Collections.Generic.Dictionary[string,string]]::new(
        [StringComparer]::Ordinal)
    foreach ($match in [regex]::Matches($normalized,
        '(?m)^\[sub_resource\b[^\]]*\bid="([^"]+)"[^\]]*\]$')) {
        $id = $match.Groups[1].Value
        if ($subTokens.ContainsKey($id)) {
            throw "Duplicate sub_resource id '$id' is ambiguous."
        }
        $subTokens.Add($id,
            ("__GODOT_CANONICAL_SUB_RESOURCE_{0}__" -f $subTokens.Count))
    }

    $targetPattern = '(?m)^\[ext_resource\b[^\]]*\]$|' +
        '^\[sub_resource\b[^\]]*\]$|' +
        'ExtResource\("(?<extref>[^"]+)"\)|' +
        'SubResource\("(?<subref>[^"]+)"\)'
    $normalized = [regex]::Replace($normalized, $targetPattern,
        [System.Text.RegularExpressions.MatchEvaluator]{
            param($match)
            $value = $match.Value
            if ($value.StartsWith('[ext_resource', [StringComparison]::Ordinal)) {
                $id = [regex]::Match($value, '\bid="([^"]+)"').Groups[1].Value
                if (-not $extTokens.ContainsKey($id)) {
                    throw "Unknown ext_resource declaration id '$id'."
                }
                return [regex]::Replace($value, '\bid="[^"]+"',
                    ('id="' + $extTokens[$id] + '"'))
            }
            if ($value.StartsWith('[sub_resource', [StringComparison]::Ordinal)) {
                $id = [regex]::Match($value, '\bid="([^"]+)"').Groups[1].Value
                if (-not $subTokens.ContainsKey($id)) {
                    throw "Malformed sub_resource declaration: $value"
                }
                return [regex]::Replace($value, '\bid="[^"]+"',
                    ('id="' + $subTokens[$id] + '"'))
            }
            if ($match.Groups['extref'].Success) {
                $id = $match.Groups['extref'].Value
                if (-not $extTokens.ContainsKey($id)) {
                    throw "ExtResource reference uses undeclared id '$id'."
                }
                return 'ExtResource("' + $extTokens[$id] + '")'
            }
            $id = $match.Groups['subref'].Value
            if (-not $subTokens.ContainsKey($id)) {
                throw "SubResource reference uses undeclared id '$id'."
            }
            return 'SubResource("' + $subTokens[$id] + '")'
        })

    if ($extEntries.Count -gt 0) {
        $lines = @($normalized -split "`n")
        $canonicalExt = @($lines | Where-Object { $_ -cmatch '^\[ext_resource\b' } |
            Sort-Object)
        $body = [System.Collections.Generic.List[string]]::new()
        $insertAt = 0
        $seenFirstExt = $false
        foreach ($line in $lines) {
            if ($line -cmatch '^\[ext_resource\b') {
                if (-not $seenFirstExt) {
                    $insertAt = $body.Count
                    $seenFirstExt = $true
                }
                continue
            }
            $body.Add($line)
        }
        $canonicalLines = [System.Collections.Generic.List[string]]::new()
        for ($index = 0; $index -lt $body.Count; $index++) {
            if ($index -eq $insertAt) {
                foreach ($line in $canonicalExt) { $canonicalLines.Add($line) }
            }
            $canonicalLines.Add($body[$index])
        }
        if ($insertAt -eq $body.Count) {
            foreach ($line in $canonicalExt) { $canonicalLines.Add($line) }
        }
        $normalized = $canonicalLines -join "`n"
    }
    return $normalized.TrimEnd()
}

function Get-Task6GeneratedRuntimeSemantics {
    param(
        [Parameter(Mandatory)][string]$Root,
        [Parameter(Mandatory)][string[]]$RelativePaths
    )

    if ($RelativePaths.Count -eq 0) {
        throw 'Generated runtime semantic fingerprint requires at least one scene path.'
    }
    if (-not (Test-Path -LiteralPath $Root -PathType Container)) {
        throw "Generated runtime semantic root is missing: $Root"
    }

    $resolvedRoot = [IO.Path]::GetFullPath((Resolve-Path -LiteralPath $Root).Path).TrimEnd('\', '/')
    if ([string]::IsNullOrWhiteSpace($resolvedRoot)) {
        throw "Generated runtime semantic root is invalid: $Root"
    }
    $rootPrefix = $resolvedRoot + [IO.Path]::DirectorySeparatorChar
    $seen = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    $canonicalPaths = [Collections.Generic.List[string]]::new()
    $semanticHashes = [ordered]@{}
    $algorithm = [Security.Cryptography.SHA256]::Create()
    $utf8 = [Text.Encoding]::UTF8
    try {
        $header = $utf8.GetBytes("task6-generated-runtime-semantics-v1`n")
        [void]$algorithm.TransformBlock($header, 0, $header.Length, $header, 0)
        foreach ($relativePath in $RelativePaths) {
            if ([string]::IsNullOrWhiteSpace($relativePath)) {
                throw 'Generated runtime semantic path is empty.'
            }
            $nativeRelative = $relativePath.Replace('/', '\')
            if ([IO.Path]::IsPathRooted($nativeRelative)) {
                throw "Generated runtime semantic path must be relative: $relativePath"
            }
            $candidate = [IO.Path]::GetFullPath((Join-Path $resolvedRoot $nativeRelative))
            if (-not $candidate.StartsWith($rootPrefix, [StringComparison]::OrdinalIgnoreCase)) {
                throw "Generated runtime semantic path escapes root: $relativePath"
            }
            if (-not (Test-Path -LiteralPath $candidate -PathType Leaf)) {
                throw "Generated runtime semantic scene is missing: $relativePath"
            }
            $item = Get-Item -LiteralPath $candidate
            if (($item.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
                throw "Generated runtime semantic scene cannot be a reparse point: $relativePath"
            }
            if ($item.Length -gt 8388608) {
                throw "Generated runtime semantic scene exceeds the small-file limit: $relativePath"
            }
            $canonicalRelative = $nativeRelative.Replace('\', '/')
            if (-not $seen.Add($canonicalRelative)) {
                throw "Generated runtime semantic path is duplicated: $canonicalRelative"
            }

            $semantic = ConvertTo-GodotSceneSemanticText -Text ([IO.File]::ReadAllText($candidate))
            $pathBytes = $utf8.GetBytes($canonicalRelative)
            $semanticBytes = $utf8.GetBytes($semantic)
            foreach ($bytes in @(
                [BitConverter]::GetBytes([int]$pathBytes.Length), $pathBytes,
                [BitConverter]::GetBytes([int]$semanticBytes.Length), $semanticBytes
            )) {
                if ($bytes.Length -gt 0) {
                    [void]$algorithm.TransformBlock($bytes, 0, $bytes.Length, $bytes, 0)
                }
            }
            $canonicalPaths.Add($canonicalRelative)
            $sceneAlgorithm = [Security.Cryptography.SHA256]::Create()
            try {
                $semanticHashes[$canonicalRelative] =
                    ([BitConverter]::ToString($sceneAlgorithm.ComputeHash($semanticBytes)) -replace '-', '').ToLowerInvariant()
            }
            finally { $sceneAlgorithm.Dispose() }
        }
        [void]$algorithm.TransformFinalBlock([byte[]]::new(0), 0, 0)
        return [pscustomobject]@{
            Count = $canonicalPaths.Count
            Paths = @($canonicalPaths.ToArray())
            Sha256 = ([BitConverter]::ToString($algorithm.Hash) -replace '-', '').ToLowerInvariant()
            SceneSha256 = $semanticHashes
        }
    }
    finally { $algorithm.Dispose() }
}

function Get-ComparisonPngHash {
    param(
        [Parameter(Mandatory)][string]$Path,
        [Parameter(Mandatory)][System.Drawing.Rectangle[]]$IgnoredRegions,
        [Parameter(Mandatory)][string]$Label
    )

    if ($IgnoredRegions.Count -eq 0) {
        throw "$Label has no authorized volatile regions for comparison: $Path"
    }
    try { $source = [System.Drawing.Bitmap]::new((Resolve-Path -LiteralPath $Path).Path) }
    catch { throw "$Label PNG is invalid for comparison: $Path ($($_.Exception.Message))" }
    $bitmap = $null
    try {
        foreach ($region in $IgnoredRegions) {
            if ($region.Width -le 0 -or $region.Height -le 0 -or
                $region.X -lt 0 -or $region.Y -lt 0 -or
                $region.Right -gt $source.Width -or $region.Bottom -gt $source.Height) {
                throw "$Label volatile comparison region is outside ${Path}: $region"
            }
        }
        $bitmap = [System.Drawing.Bitmap]::new(
            $source.Width, $source.Height, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
        $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
        try {
            $graphics.DrawImageUnscaled($source, 0, 0)
            foreach ($region in $IgnoredRegions) {
                $graphics.FillRectangle([System.Drawing.Brushes]::Black, $region)
            }
        }
        finally { $graphics.Dispose() }
        $bounds = [System.Drawing.Rectangle]::new(0, 0, $bitmap.Width, $bitmap.Height)
        $locked = $bitmap.LockBits($bounds, [System.Drawing.Imaging.ImageLockMode]::ReadOnly,
            [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
        try {
            $bytes = [byte[]]::new([Math]::Abs($locked.Stride) * $locked.Height)
            [System.Runtime.InteropServices.Marshal]::Copy($locked.Scan0, $bytes, 0, $bytes.Length)
            $algorithm = [Security.Cryptography.SHA256]::Create()
            try {
                return ([BitConverter]::ToString($algorithm.ComputeHash($bytes)) -replace '-', '').ToLowerInvariant()
            }
            finally { $algorithm.Dispose() }
        }
        finally { $bitmap.UnlockBits($locked) }
    }
    finally {
        if ($null -ne $bitmap) { $bitmap.Dispose() }
        $source.Dispose()
    }
}

function Get-EvidenceSha256 {
    param([Parameter(Mandatory)][string]$LiteralPath)

    $resolvedPath = (Resolve-Path -LiteralPath $LiteralPath -ErrorAction Stop).Path
    $stream = [System.IO.File]::OpenRead($resolvedPath)
    $algorithm = [System.Security.Cryptography.SHA256]::Create()
    try {
        return ([BitConverter]::ToString($algorithm.ComputeHash($stream)) -replace '-', '').ToLowerInvariant()
    }
    finally {
        $algorithm.Dispose()
        $stream.Dispose()
    }
}

function Assert-PngCaptureContract {
    param(
        [Parameter(Mandatory)][string[]]$Paths,
        [Parameter(Mandatory)][int]$ExpectedCount,
        [Parameter(Mandatory)][int]$ExpectedWidth,
        [Parameter(Mandatory)][int]$ExpectedHeight,
        [Parameter(Mandatory)][string]$Label,
        [System.Collections.IDictionary]$ComparisonIgnoredRegionsByFileName
    )

    if ($Paths.Count -ne $ExpectedCount) {
        throw "$Label PNG count mismatch: expected $ExpectedCount, found $($Paths.Count)."
    }
    if ($Paths.Count -eq 0) { throw "$Label has no PNGs to hash." }
    Add-Type -AssemblyName System.Drawing
    $ordered = @($Paths | Sort-Object)
    $comparisonHashLines = [System.Collections.Generic.List[string]]::new()
    $hashLines = foreach ($path in $ordered) {
        try { $bitmap = [System.Drawing.Bitmap]::new((Resolve-Path -LiteralPath $path).Path) }
        catch { throw "$Label PNG is invalid: $path ($($_.Exception.Message))" }
        try {
            if ($bitmap.Width -ne $ExpectedWidth -or $bitmap.Height -ne $ExpectedHeight) {
                throw "$Label PNG dimensions mismatch: $path is $($bitmap.Width)x$($bitmap.Height), " +
                    "expected ${ExpectedWidth}x${ExpectedHeight}."
            }
        }
        finally { $bitmap.Dispose() }

        $hash = Get-EvidenceSha256 -LiteralPath $path
        if ([string]::IsNullOrWhiteSpace($hash)) { throw "$Label has an empty SHA-256: $path" }
        $fileName = Split-Path -Leaf $path
        $comparisonHash = $hash
        if ($null -ne $ComparisonIgnoredRegionsByFileName -and
            $ComparisonIgnoredRegionsByFileName.Contains($fileName)) {
            $comparisonHash = Get-ComparisonPngHash -Path $path `
                -IgnoredRegions @($ComparisonIgnoredRegionsByFileName[$fileName]) -Label $Label
        }
        $comparisonHashLines.Add("${fileName}=$comparisonHash")
        "${fileName}=$hash"
    }
    $joined = [Text.Encoding]::UTF8.GetBytes(($hashLines -join "`n"))
    $algorithm = [Security.Cryptography.SHA256]::Create()
    try { $manifestHash = ([BitConverter]::ToString($algorithm.ComputeHash($joined)) -replace '-', '').ToLowerInvariant() }
    finally { $algorithm.Dispose() }
    if ([string]::IsNullOrWhiteSpace($manifestHash)) { throw "$Label has an empty capture manifest hash." }
    $comparisonJoined = [Text.Encoding]::UTF8.GetBytes(($comparisonHashLines -join "`n"))
    $comparisonAlgorithm = [Security.Cryptography.SHA256]::Create()
    try {
        $comparisonManifestHash = ([BitConverter]::ToString(
            $comparisonAlgorithm.ComputeHash($comparisonJoined)) -replace '-', '').ToLowerInvariant()
    }
    finally { $comparisonAlgorithm.Dispose() }
    if ([string]::IsNullOrWhiteSpace($comparisonManifestHash)) {
        throw "$Label has an empty comparison capture manifest hash."
    }
    return [pscustomobject]@{
        Count = $ordered.Count
        Width = $ExpectedWidth
        Height = $ExpectedHeight
        ManifestSha256 = $manifestHash
        ComparisonManifestSha256 = $comparisonManifestHash
    }
}

function Assert-CaptureManifestMatch {
    param(
        [Parameter(Mandatory)][string[]]$ExpectedLines,
        [Parameter(Mandatory)][string[]]$ActualLines,
        [Parameter(Mandatory)][string]$Label
    )

    $expected = @($ExpectedLines | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | Sort-Object)
    $actual = @($ActualLines | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | Sort-Object)
    if ($expected.Count -ne $actual.Count) {
        throw "$Label capture manifest entry count mismatch: expected $($expected.Count), found $($actual.Count)."
    }
    for ($index = 0; $index -lt $expected.Count; $index++) {
        if (-not [string]::Equals($expected[$index], $actual[$index], [StringComparison]::Ordinal)) {
            throw "$Label capture manifest mismatch at entry ${index}: expected '$($expected[$index])', " +
                "found '$($actual[$index])'."
        }
    }
}

function Get-GodotPackedSceneRootProfileBounds {
    param([Parameter(Mandatory)][AllowEmptyString()][string]$Text)

    $normalized = $Text -replace "`r`n", "`n"
    $root = [regex]::Match($normalized, '(?m)^\[node\b[^\]]*\]$')
    if (-not $root.Success -or $root.Value -cmatch '\bparent="') { return $null }

    $bodyStart = $root.Index + $root.Length
    $nextSection = [regex]::Match($normalized.Substring($bodyStart), '(?m)^\[')
    $bodyLength = if ($nextSection.Success) { $nextSection.Index } else {
        $normalized.Length - $bodyStart
    }
    $body = $normalized.Substring($bodyStart, $bodyLength)
    $properties = @([regex]::Matches($body,
        '(?m)^metadata/profile_bounds[ \t]*=[ \t]*(?<value>[^\n]*?)[ \t]*$'))
    if ($properties.Count -ne 1) { return $null }

    [pscustomobject]@{
        Value = $properties[0].Groups['value'].Value.Trim()
    }
}

function Remove-RedundantActorPackedSceneProfileBoundsOverride {
    param(
        [Parameter(Mandatory)][AllowEmptyString()][string]$Text,
        [Parameter(Mandatory)][string]$Label,
        [System.Collections.IDictionary]$ResourceTexts
    )

    if ($null -eq $ResourceTexts) { return $Text }
    $actorLabels = @(
        'scenes\enemies\RustShellSquire.tscn',
        'scenes\enemies\GraveLampMoth.tscn',
        'scenes\enemies\BellBackLaborer.tscn',
        'scenes\boss\FallenBellkeeper.tscn'
    )
    $normalizedLabel = $Label.Replace('/', '\')
    if ($actorLabels -cnotcontains $normalizedLabel) { return $Text }

    $resourcesById = @{}
    # Text reaches this helper directly from production scene files, which use
    # CRLF on Windows.  Accept the optional carriage return before the
    # multiline end-of-line anchor so PackedScene declarations are discovered
    # before profile-bound inheritance is compared.
    foreach ($declaration in [regex]::Matches($Text, '(?m)^\[ext_resource\b[^\]]*\]\r?$')) {
        $line = $declaration.Value
        $type = [regex]::Match($line, '\btype="([^"]+)"').Groups[1].Value
        $path = [regex]::Match($line, '\bpath="([^"]+)"').Groups[1].Value
        $id = [regex]::Match($line, '\bid="([^"]+)"').Groups[1].Value
        if ([string]::IsNullOrEmpty($type) -or [string]::IsNullOrEmpty($path) -or
            [string]::IsNullOrEmpty($id) -or $resourcesById.ContainsKey($id)) {
            return $Text
        }
        $resourcesById[$id] = [pscustomobject]@{ Type = $type; Path = $path }
    }

    $nodePattern = '(?ms)^(?<header>\[node\b[^\r\n]*\])(?<body>\r?\n.*?)(?=^\[|\z)'
    return [regex]::Replace($Text, $nodePattern,
        [System.Text.RegularExpressions.MatchEvaluator]{
            param($node)
            $header = $node.Groups['header'].Value
            if ($header -cnotmatch '\bname="Formal"' -or
                $header -cnotmatch '\bparent="VisualRoot"') {
                return $node.Value
            }
            $instance = [regex]::Match($header,
                '\binstance=ExtResource\("(?<id>[^"]+)"\)')
            if (-not $instance.Success) { return $node.Value }
            $resourceId = $instance.Groups['id'].Value
            if (-not $resourcesById.ContainsKey($resourceId)) { return $node.Value }
            $resource = $resourcesById[$resourceId]
            if ($resource.Type -cne 'PackedScene' -or
                -not $ResourceTexts.Contains($resource.Path)) {
                return $node.Value
            }

            $overrides = @([regex]::Matches($node.Groups['body'].Value,
                '(?m)^metadata/profile_bounds[ \t]*=[ \t]*(?<value>[^\r\n]*?)[ \t]*(?:\r?\n|$)'))
            if ($overrides.Count -ne 1) { return $node.Value }

            $referencedRoot = Get-GodotPackedSceneRootProfileBounds `
                -Text ([string]$ResourceTexts[$resource.Path])
            if ($null -eq $referencedRoot -or
                $overrides[0].Groups['value'].Value.Trim() -cne $referencedRoot.Value) {
                return $node.Value
            }

            $body = $node.Groups['body'].Value
            $overrideOffset = $overrides[0].Index
            $bodyWithoutOverride = $body.Remove($overrideOffset, $overrides[0].Length)
            return $header + $bodyWithoutOverride
        })
}

function Assert-OnlyExpectedEngineError {
    param(
        [Parameter(Mandatory)][string]$Path,
        [Parameter(Mandatory)][string]$ExpectedPattern,
        # Some intentionally forced tests wrap their managed exception in a stable, named
        # runner marker.  Keep that wrapper opt-in and exact: callers that do not name one
        # retain the historical raw Godot error contract.
        [string]$ExpectedContext
    )

    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) { throw "Required log is missing: $Path" }
    Assert-ExactEngineWarnings -Path $Path
    $failurePatterns = @(Get-EngineFailurePatterns)
    $expectedPrefix = '^\s*ERROR:\s+'
    if (-not [string]::IsNullOrWhiteSpace($ExpectedContext)) {
        $expectedPrefix += [regex]::Escape($ExpectedContext) + ':\s+'
    }
    $expectedLinePattern = $expectedPrefix + 'System\.InvalidOperationException:\s+' +
        [regex]::Escape($ExpectedPattern) + '\s*$'
    $expected = @(Get-Content -LiteralPath $Path | Where-Object {
        $_ -cmatch $expectedLinePattern
    })
    $errors = @(Select-String -LiteralPath $Path -Pattern $failurePatterns)
    $unexpected = @($errors | Where-Object { $_.Line -cnotmatch $expectedLinePattern })
    if ($expected.Count -ne 1 -or $unexpected.Count -ne 0) {
        $lines = $errors | ForEach-Object { "$($_.Path):$($_.LineNumber):$($_.Line)" }
        throw "Expected exactly one anchored forced engine error '$ExpectedPattern' and no other " +
            "failure signature on the same or another line. Found:`n$($lines -join [Environment]::NewLine)"
    }
}

function Assert-GodotSceneSemanticallyEquivalent {
    param(
        [Parameter(Mandatory)][AllowEmptyString()][string]$ExpectedText,
        [Parameter(Mandatory)][AllowEmptyString()][string]$ActualText,
        [Parameter(Mandatory)][string]$Label,
        [System.Collections.IDictionary]$ExpectedResourceTexts,
        [System.Collections.IDictionary]$ActualResourceTexts
    )

    $ExpectedText = Remove-RedundantActorPackedSceneProfileBoundsOverride `
        -Text $ExpectedText -Label $Label -ResourceTexts $ExpectedResourceTexts
    $ActualText = Remove-RedundantActorPackedSceneProfileBoundsOverride `
        -Text $ActualText -Label $Label -ResourceTexts $ActualResourceTexts
    $expected = ConvertTo-GodotSceneSemanticText -Text $ExpectedText
    $actual = ConvertTo-GodotSceneSemanticText -Text $ActualText
    if ($expected -cne $actual) {
        throw "Generated Godot scene changed semantic content: $Label"
    }
}

function Assert-RequiredBuilderPlan {
    param([Parameter(Mandatory)][string[]]$BuilderNames)

    $required = @('BuildPlayer', 'BuildRustShellSquire', 'BuildPrototypeRoom')
    if ($BuilderNames.Count -ne $required.Count) {
        throw "Scene builder gate must contain exactly: $($required -join ', ')"
    }
    for ($index = 0; $index -lt $required.Count; $index++) {
        if ($BuilderNames[$index] -cne $required[$index]) {
            throw "Scene builder gate order must be leaf-to-parent: $($required -join ', ')"
        }
    }
}

function Get-AviMainHeader {
    param([Parameter(Mandatory)][string]$Path)

    # This compatibility entry point is used by earlier capture tests too.  Always delegate
    # container inspection to the bounded FileStream reader; whole-file AVI reads can exhaust
    # memory on a long 1280x720/60 capture.
    $streaming = Join-Path $PSScriptRoot 'Task6AviStreaming.ps1'
    if (-not (Get-Command Get-Task6AviMjpegIndex -ErrorAction SilentlyContinue)) {
        if (-not (Test-Path -LiteralPath $streaming -PathType Leaf)) {
            throw "Bounded AVI reader is missing: $streaming"
        }
        . $streaming
    }
    return (Get-Task6AviMjpegIndex -Path $Path).Header
}

function Assert-VisiblePngTail {
    param(
        [Parameter(Mandatory)][string[]]$Paths,
        [string]$ReferencePath,
        [Parameter(Mandatory)][string]$Label
    )

    if ($Paths.Count -lt 3) { throw "$Label requires at least three final PNG frames." }
    if ([string]::IsNullOrWhiteSpace($ReferencePath) -or
        -not (Test-Path -LiteralPath $ReferencePath -PathType Leaf)) {
        throw "$Label same-scene reference PNG is missing: $ReferencePath"
    }

    # Similarity is measured on an evenly spaced 32x18 grid. mean_rgb_mae is
    # the mean absolute per-channel RGB delta (0..255). changed_fraction is the
    # share of samples whose mean RGB delta exceeds 24. The paired 12 / 0.20
    # bounds tolerate small localized animation while rejecting broad scene
    # replacement and high-entropy output; this is not a visual-recognition test.
    $sampleColumns = 32
    $sampleRows = 18
    $changedSampleError = 24d
    $maximumMeanRgbMae = 12d
    $maximumChangedFraction = .20d
    Add-Type -AssemblyName System.Drawing
    try { $reference = [System.Drawing.Bitmap]::new((Resolve-Path -LiteralPath $ReferencePath).Path) }
    catch { throw "$Label same-scene reference PNG is invalid: $ReferencePath ($($_.Exception.Message))" }
    try {
        $referenceColors = [System.Collections.Generic.HashSet[int]]::new()
        $referenceMinimumLuminance = 255d
        $referenceMaximumLuminance = 0d
        for ($sampleX = 0; $sampleX -lt $sampleColumns; $sampleX++) {
            $x = [Math]::Floor($sampleX * ($reference.Width - 1) / ($sampleColumns - 1d))
            for ($sampleY = 0; $sampleY -lt $sampleRows; $sampleY++) {
                $y = [Math]::Floor($sampleY * ($reference.Height - 1) / ($sampleRows - 1d))
                $referenceColor = $reference.GetPixel($x, $y)
                [void]$referenceColors.Add($referenceColor.ToArgb())
                $referenceLuminance = .2126d * $referenceColor.R +
                    .7152d * $referenceColor.G + .0722d * $referenceColor.B
                $referenceMinimumLuminance = [Math]::Min(
                    $referenceMinimumLuminance, $referenceLuminance)
                $referenceMaximumLuminance = [Math]::Max(
                    $referenceMaximumLuminance, $referenceLuminance)
            }
        }
        if ($referenceColors.Count -lt 2 -or
            $referenceMaximumLuminance - $referenceMinimumLuminance -lt 8d) {
            throw "$Label same-scene reference PNG is visually uniform: $ReferencePath"
        }

        foreach ($path in $Paths) {
            if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
                throw "$Label final PNG frame is missing: $path"
            }
            try { $bitmap = [System.Drawing.Bitmap]::new((Resolve-Path -LiteralPath $path).Path) }
            catch { throw "$Label final PNG frame is invalid: $path ($($_.Exception.Message))" }
            try {
                if ($bitmap.Width -ne $reference.Width -or $bitmap.Height -ne $reference.Height) {
                    throw "$Label final PNG/reference dimensions differ: $path is " +
                        "$($bitmap.Width)x$($bitmap.Height), reference is " +
                        "$($reference.Width)x$($reference.Height)"
                }
                $colors = [System.Collections.Generic.HashSet[int]]::new()
                $minimumLuminance = 255d
                $maximumLuminance = 0d
                $absoluteError = 0d
                $changedSamples = 0
                for ($sampleX = 0; $sampleX -lt $sampleColumns; $sampleX++) {
                    $x = [Math]::Floor($sampleX * ($bitmap.Width - 1) / ($sampleColumns - 1d))
                    for ($sampleY = 0; $sampleY -lt $sampleRows; $sampleY++) {
                        $y = [Math]::Floor($sampleY * ($bitmap.Height - 1) / ($sampleRows - 1d))
                        $color = $bitmap.GetPixel($x, $y)
                        $referenceColor = $reference.GetPixel($x, $y)
                        [void]$colors.Add($color.ToArgb())
                        $luminance = .2126d * $color.R + .7152d * $color.G + .0722d * $color.B
                        $minimumLuminance = [Math]::Min($minimumLuminance, $luminance)
                        $maximumLuminance = [Math]::Max($maximumLuminance, $luminance)
                        $sampleError = ([Math]::Abs([int]$color.R - [int]$referenceColor.R) +
                            [Math]::Abs([int]$color.G - [int]$referenceColor.G) +
                            [Math]::Abs([int]$color.B - [int]$referenceColor.B)) / 3d
                        $absoluteError += $sampleError
                        if ($sampleError -gt $changedSampleError) { $changedSamples++ }
                    }
                }
                if ($colors.Count -lt 2 -or $maximumLuminance - $minimumLuminance -lt 8d) {
                    throw "$Label final PNG frame is visually uniform: $path " +
                        "(sample_colors=$($colors.Count), luminance_range=" +
                        "$([Math]::Round($maximumLuminance - $minimumLuminance, 3)))."
                }
                $sampleCount = $sampleColumns * $sampleRows
                $meanRgbMae = $absoluteError / $sampleCount
                $changedFraction = $changedSamples / [double]$sampleCount
                if ($meanRgbMae -gt $maximumMeanRgbMae -or
                    $changedFraction -gt $maximumChangedFraction) {
                    throw "$Label same-scene reference mismatch: $path " +
                        "(mean_rgb_mae=$([Math]::Round($meanRgbMae, 3)), " +
                        "changed_fraction=$([Math]::Round($changedFraction, 3)), " +
                        "limits=${maximumMeanRgbMae}/$maximumChangedFraction)."
                }
            }
            finally { $bitmap.Dispose() }
        }
    }
    finally { $reference.Dispose() }
}

function Assert-AviCaptureContract {
    param(
        [Parameter(Mandatory)][string]$Path,
        [Parameter(Mandatory)][int]$ExpectedFrameCount,
        [Parameter(Mandatory)][int]$ExpectedWidth,
        [Parameter(Mandatory)][int]$ExpectedHeight,
        [Parameter(Mandatory)][int]$ExpectedFps
    )

    $header = Get-AviMainHeader -Path $Path
    $actualFps = [Math]::Round(1000000.0 / $header.MicrosecondsPerFrame)
    if ($header.TotalFrames -ne $ExpectedFrameCount -or
        $header.Width -ne $ExpectedWidth -or
        $header.Height -ne $ExpectedHeight -or
        $actualFps -ne $ExpectedFps) {
        throw "AVI contract mismatch: expected ${ExpectedWidth}x${ExpectedHeight} ${ExpectedFrameCount}@${ExpectedFps}; got $($header.Width)x$($header.Height) $($header.TotalFrames)@$actualFps ($Path)"
    }
}

function Assert-StableGeneratedSceneSemantics {
    param(
        [Parameter(Mandatory)][AllowEmptyString()][string]$StartupText,
        [Parameter(Mandatory)][AllowEmptyString()][string]$CommittedText,
        [Parameter(Mandatory)][AllowEmptyString()][string]$ActualText,
        [Parameter(Mandatory)][bool]$StartedDirty,
        [Parameter(Mandatory)][string]$Label,
        [System.Collections.IDictionary]$ExpectedResourceTexts,
        [System.Collections.IDictionary]$ActualResourceTexts
    )

    $expectedText = if ($StartedDirty) { $CommittedText } else { $StartupText }
    Assert-GodotSceneSemanticallyEquivalent -ExpectedText $expectedText `
        -ActualText $ActualText -Label $Label `
        -ExpectedResourceTexts $ExpectedResourceTexts `
        -ActualResourceTexts $ActualResourceTexts
}

function Restore-ProtectedFileSnapshots {
    param(
        [Parameter(Mandatory)][string]$Root,
        [Parameter(Mandatory)][string[]]$RelativePaths,
        [Parameter(Mandatory)][System.Collections.IDictionary]$Snapshots,
        [scriptblock]$WriteBytes = {
            param([string]$Path, [byte[]]$Bytes)
            [System.IO.File]::WriteAllBytes($Path, $Bytes)
        }
    )

    $failures = [System.Collections.Generic.List[string]]::new()
    foreach ($relativePath in $RelativePaths) {
        $path = Join-Path $Root $relativePath
        try {
            if (-not $Snapshots.Contains($relativePath)) {
                throw 'snapshot bytes are missing'
            }
            [byte[]]$expected = $Snapshots[$relativePath]
            [byte[]]$current = if (Test-Path -LiteralPath $path -PathType Leaf) {
                [System.IO.File]::ReadAllBytes($path)
            }
            else { $null }
            $same = $null -ne $current -and $current.Length -eq $expected.Length
            if ($same) {
                for ($index = 0; $index -lt $expected.Length; $index++) {
                    if ($current[$index] -ne $expected[$index]) {
                        $same = $false
                        break
                    }
                }
            }
            if ($same) { continue }

            & $WriteBytes $path $expected
            [byte[]]$restored = [System.IO.File]::ReadAllBytes($path)
            $restoredExactly = $restored.Length -eq $expected.Length
            if ($restoredExactly) {
                for ($index = 0; $index -lt $expected.Length; $index++) {
                    if ($restored[$index] -ne $expected[$index]) {
                        $restoredExactly = $false
                        break
                    }
                }
            }
            if (-not $restoredExactly) { throw 'write completed without restoring exact bytes' }
        }
        catch {
            $failures.Add("$relativePath`: $($_.Exception.Message)")
        }
    }
    if ($failures.Count) {
        throw "Protected file restoration failed for $($failures.Count) path(s):`n" +
            ($failures -join [Environment]::NewLine)
    }
}

function Assert-RequiredFiles {
    param(
        [Parameter(Mandatory)][string[]]$Paths,
        [Parameter(Mandatory)][string]$Label
    )

    if ($Paths.Count -eq 0) { throw "Required $Label list is empty." }
    foreach ($path in $Paths) {
        if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
            throw "Required $Label is missing: $path"
        }
    }
}

function Assert-PrologueBuilderPlan {
    param([Parameter(Mandatory)][string[]]$BuilderNames)

    $required = @(
        'BuildPlayerFormalVisual',
        'BuildPlayer',
        'BuildEnemyFormalVisuals',
        'BuildRustShellSquire',
        'BuildGraveLampMoth',
        'BuildBellBackLaborer',
        'BuildFallenBellkeeperFormalVisual',
        'BuildFallenBellkeeper',
        'BuildPrologueMechanismVisuals',
        'BuildBellChainMechanism',
        'BuildPrologueEnvironmentKit',
        'BuildAwakeningCourtyard',
        'BuildBrokenToothCorridor',
        'BuildUpperPendulum',
        'BuildLowerBuriedMachinery',
        'BuildBellChainRoom',
        'BuildFallenBellkeeperArena',
        'BuildPrologueUi',
        'BuildPrologueVfx',
        'BuildCanyonEntrance',
        'BuildCanyonDescent',
        'BuildCanyonBasin',
        'BuildCanyonAscent',
        'BuildLostTemple',
        'BuildPrologueLevel'
    )
    if ($BuilderNames.Count -ne $required.Count) {
        throw "Prologue builder gate must contain exactly: $($required -join ', ')"
    }
    for ($index = 0; $index -lt $required.Count; $index++) {
        if ($BuilderNames[$index] -cne $required[$index]) {
            throw "Prologue builder gate order must be leaf-to-parent: $($required -join ', ')"
        }
    }
}
