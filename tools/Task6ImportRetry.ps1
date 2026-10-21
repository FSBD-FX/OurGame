$ErrorActionPreference = 'Stop'

function Get-Task6ImportRetryAbsolutePath {
    param([Parameter(Mandatory)][string]$Path)
    return [IO.Path]::GetFullPath($Path).TrimEnd('\')
}

function Test-Task6ImportRetryPathWithinRoot {
    param(
        [Parameter(Mandatory)][string]$Path,
        [Parameter(Mandatory)][string]$Root
    )
    $fullPath = Get-Task6ImportRetryAbsolutePath -Path $Path
    $fullRoot = Get-Task6ImportRetryAbsolutePath -Path $Root
    return $fullPath.StartsWith(($fullRoot + '\'), [StringComparison]::OrdinalIgnoreCase)
}

function Get-Task6ImportRetryCachePath {
    param([Parameter(Mandatory)][string]$ProjectRoot)
    $root = Get-Task6ImportRetryAbsolutePath -Path $ProjectRoot
    $cache = Get-Task6ImportRetryAbsolutePath -Path (Join-Path $root '.godot')
    if (-not $cache.Equals(($root + '\.godot'), [StringComparison]::OrdinalIgnoreCase) -or
        -not (Test-Task6ImportRetryPathWithinRoot -Path $cache -Root $root)) {
        throw "BASE_IMPORT_NATIVE_RETRY_CACHE_PATH_UNSAFE path=$cache root=$root"
    }
    return $cache
}

function Get-Task6ImportRetryChildPath {
    param(
        [Parameter(Mandatory)][string]$ProjectRoot,
        [Parameter(Mandatory)][string]$RelativePath,
        [switch]$RequireLeaf
    )
    if ([string]::IsNullOrWhiteSpace($RelativePath)) {
        throw 'BASE_IMPORT_NATIVE_RETRY_PATH_EMPTY'
    }
    $root = Get-Task6ImportRetryAbsolutePath -Path $ProjectRoot
    $path = Get-Task6ImportRetryAbsolutePath -Path (Join-Path $root $RelativePath)
    if (-not (Test-Task6ImportRetryPathWithinRoot -Path $path -Root $root)) {
        throw "BASE_IMPORT_NATIVE_RETRY_PATH_ESCAPE relative=$RelativePath root=$root"
    }
    if ($RequireLeaf -and -not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "BASE_IMPORT_NATIVE_RETRY_PATH_NOT_FILE relative=$RelativePath"
    }
    return $path
}

function Get-Task6ImportRetryGitStatus {
    param([Parameter(Mandatory)][string]$ProjectRoot)
    $lines = @(& git -C $ProjectRoot status --porcelain=v1 --untracked-files=all)
    if ($LASTEXITCODE -ne 0) {
        throw 'BASE_IMPORT_NATIVE_RETRY_STATUS_QUERY_FAILED'
    }
    return @($lines)
}

function Get-Task6ImportRetryPreState {
    param(
        [Parameter(Mandatory)][string]$ProjectRoot,
        [Parameter(Mandatory)][string]$EvidenceRoot
    )
    $root = (Resolve-Path -LiteralPath $ProjectRoot).Path
    $evidence = (Resolve-Path -LiteralPath $EvidenceRoot).Path
    if (Test-Task6ImportRetryPathWithinRoot -Path $evidence -Root $root) {
        throw "BASE_IMPORT_NATIVE_RETRY_EVIDENCE_INSIDE_PROJECT root=$evidence project=$root"
    }
    $status = @(Get-Task6ImportRetryGitStatus -ProjectRoot $root)
    if ($status.Count -ne 0) {
        throw "BASE_IMPORT_NATIVE_RETRY_PRESTATE_DIRTY status=$($status -join '; ')"
    }
    $cachePath = Get-Task6ImportRetryCachePath -ProjectRoot $root
    if (Test-Path -LiteralPath $cachePath) {
        throw "BASE_IMPORT_NATIVE_RETRY_CACHE_PREEXISTING path=$cachePath"
    }
    $headLines = @(& git -C $root rev-parse HEAD)
    if ($LASTEXITCODE -ne 0 -or $headLines.Count -ne 1 -or
        $headLines[0] -notmatch '^[0-9a-fA-F]{40}$') {
        throw 'BASE_IMPORT_NATIVE_RETRY_HEAD_UNRESOLVED'
    }
    $tracked = @{}
    foreach ($line in @(& git -C $root ls-files -s)) {
        if ($line -notmatch '^\d+\s+([0-9a-fA-F]{40})\s+\d+\t(.+)$') {
            throw "BASE_IMPORT_NATIVE_RETRY_TRACKED_MANIFEST_MALFORMED line=$line"
        }
        $relative = $Matches[2].Replace('\', '/')
        if ($tracked.ContainsKey($relative)) {
            throw "BASE_IMPORT_NATIVE_RETRY_TRACKED_MANIFEST_DUPLICATE relative=$relative"
        }
        $tracked[$relative] = $Matches[1].ToUpperInvariant()
    }
    if ($LASTEXITCODE -ne 0 -or $tracked.Count -eq 0) {
        throw 'BASE_IMPORT_NATIVE_RETRY_TRACKED_MANIFEST_EMPTY'
    }
    $untracked = @(& git -C $root ls-files --others --exclude-standard)
    if ($LASTEXITCODE -ne 0) {
        throw 'BASE_IMPORT_NATIVE_RETRY_UNTRACKED_MANIFEST_QUERY_FAILED'
    }
    if ($untracked.Count -ne 0) {
        throw "BASE_IMPORT_NATIVE_RETRY_PRESTATE_UNTRACKED count=$($untracked.Count)"
    }
    $manifest = Join-Path $evidence 'verify-prologue-import-prestate.manifest.txt'
    $lines = [Collections.Generic.List[string]]::new()
    $lines.Add('schema=task6-import-native-retry-prestate-v1')
    $lines.Add("candidate_root=$root")
    $lines.Add("head=$($headLines[0].Trim().ToUpperInvariant())")
    $lines.Add("cache_path=$cachePath")
    $lines.Add('cache_preexisting=False')
    $lines.Add('git_status_porcelain=<empty>')
    $lines.Add("tracked_count=$($tracked.Count)")
    foreach ($relative in @($tracked.Keys | Sort-Object)) {
        $lines.Add("tracked=$relative git_blob_sha1=$($tracked[$relative])")
    }
    $lines.Add('untracked_count=0')
    [IO.File]::WriteAllLines($manifest, $lines.ToArray(), [Text.UTF8Encoding]::new($false))
    return [pscustomobject]@{
        ProjectRoot = $root
        EvidenceRoot = $evidence
        Head = $headLines[0].Trim().ToUpperInvariant()
        CachePath = $cachePath
        Tracked = $tracked
        Manifest = $manifest
    }
}

function Assert-Task6ImportRetryCacheSafe {
    param([Parameter(Mandatory)][string]$CachePath)
    if (-not (Test-Path -LiteralPath $CachePath)) { return @() }
    $rootItem = Get-Item -LiteralPath $CachePath -Force -ErrorAction Stop
    if (-not $rootItem.PSIsContainer -or ($rootItem.Attributes -band [IO.FileAttributes]::ReparsePoint)) {
        throw "BASE_IMPORT_NATIVE_RETRY_CACHE_UNSAFE_ROOT path=$CachePath"
    }
    $files = [Collections.Generic.List[string]]::new()
    $stack = [Collections.Generic.Stack[string]]::new()
    $stack.Push($CachePath)
    while ($stack.Count -gt 0) {
        $directory = $stack.Pop()
        foreach ($item in @(Get-ChildItem -LiteralPath $directory -Force -ErrorAction Stop)) {
            $fullPath = [IO.Path]::GetFullPath($item.FullName)
            if (-not $fullPath.StartsWith(($CachePath + '\'), [StringComparison]::OrdinalIgnoreCase)) {
                throw "BASE_IMPORT_NATIVE_RETRY_CACHE_PATH_ESCAPE path=$fullPath root=$CachePath"
            }
            if ($item.Attributes -band [IO.FileAttributes]::ReparsePoint) {
                throw "BASE_IMPORT_NATIVE_RETRY_CACHE_REPARSE_POINT path=$fullPath"
            }
            if ($item.PSIsContainer) {
                $stack.Push($fullPath)
            }
            else {
                $files.Add($fullPath)
            }
        }
    }
    return @($files | Sort-Object)
}

function Restore-Task6ImportRetryPreState {
    param(
        [Parameter(Mandatory)][object]$State,
        [Parameter(Mandatory)][int]$Attempt
    )
    $headLines = @(& git -C $State.ProjectRoot rev-parse HEAD)
    if ($LASTEXITCODE -ne 0 -or $headLines.Count -ne 1 -or
        $headLines[0].Trim().ToUpperInvariant() -cne $State.Head) {
        throw 'BASE_IMPORT_NATIVE_RETRY_CLEANUP_HEAD_MISMATCH'
    }
    $restored = [Collections.Generic.List[string]]::new()
    foreach ($line in @(Get-Task6ImportRetryGitStatus -ProjectRoot $State.ProjectRoot)) {
        if ($line.StartsWith('?? ')) { continue }
        if ($line.Length -lt 4) {
            throw "BASE_IMPORT_NATIVE_RETRY_CLEANUP_STATUS_MALFORMED line=$line"
        }
        $indexState = $line.Substring(0, 1)
        $worktreeState = $line.Substring(1, 1)
        $relative = $line.Substring(3).Replace('\', '/')
        if ($indexState -ne ' ') {
            throw "BASE_IMPORT_NATIVE_RETRY_CLEANUP_INDEXED_MUTATION line=$line"
        }
        if ($worktreeState -notin @('M', 'D', 'T')) {
            throw "BASE_IMPORT_NATIVE_RETRY_CLEANUP_TRACKED_STATUS_REFUSED line=$line"
        }
        if (-not $State.Tracked.ContainsKey($relative)) {
            throw "BASE_IMPORT_NATIVE_RETRY_CLEANUP_TRACKED_OUTSIDE_MANIFEST relative=$relative"
        }
        $path = Get-Task6ImportRetryChildPath -ProjectRoot $State.ProjectRoot -RelativePath $relative
        & git -C $State.ProjectRoot restore --source=HEAD --worktree -- $relative
        if ($LASTEXITCODE -ne 0) {
            throw "BASE_IMPORT_NATIVE_RETRY_CLEANUP_RESTORE_FAILED relative=$relative"
        }
        $hashLines = @(& git -C $State.ProjectRoot hash-object -- $relative)
        if ($LASTEXITCODE -ne 0 -or $hashLines.Count -ne 1 -or
            $hashLines[0].Trim().ToUpperInvariant() -cne $State.Tracked[$relative]) {
            throw "BASE_IMPORT_NATIVE_RETRY_CLEANUP_HASH_MISMATCH relative=$relative"
        }
        $restored.Add($relative)
    }
    $removedSidecars = [Collections.Generic.List[string]]::new()
    $untracked = @(& git -C $State.ProjectRoot ls-files --others --exclude-standard)
    if ($LASTEXITCODE -ne 0) {
        throw 'BASE_IMPORT_NATIVE_RETRY_CLEANUP_UNTRACKED_QUERY_FAILED'
    }
    foreach ($relativeInput in @($untracked | Sort-Object)) {
        $relative = $relativeInput.Replace('\', '/')
        if ($relative -eq '.godot' -or $relative.StartsWith('.godot/', [StringComparison]::OrdinalIgnoreCase)) {
            # The cache is a separately verified, owned output.  It is deleted only after
            # Assert-Task6ImportRetryCacheSafe records every file in its manifest.
            continue
        }
        if (-not ($relative.EndsWith('.uid', [StringComparison]::OrdinalIgnoreCase) -or
                  $relative.EndsWith('.import', [StringComparison]::OrdinalIgnoreCase))) {
            throw "BASE_IMPORT_NATIVE_RETRY_CLEANUP_UNTRACKED_REFUSED relative=$relative"
        }
        $path = Get-Task6ImportRetryChildPath -ProjectRoot $State.ProjectRoot -RelativePath $relative -RequireLeaf
        $item = Get-Item -LiteralPath $path -Force -ErrorAction Stop
        if ($item.Attributes -band [IO.FileAttributes]::ReparsePoint) {
            throw "BASE_IMPORT_NATIVE_RETRY_CLEANUP_SIDECAR_REPARSE_POINT relative=$relative"
        }
        $sha256 = Get-EvidenceSha256 -LiteralPath $path
        Remove-Item -LiteralPath $path -Force
        if (Test-Path -LiteralPath $path) {
            throw "BASE_IMPORT_NATIVE_RETRY_CLEANUP_SIDECAR_DELETE_FAILED relative=$relative"
        }
        $removedSidecars.Add($relative + '|sha256=' + $sha256)
    }
    $cacheFiles = @(Assert-Task6ImportRetryCacheSafe -CachePath $State.CachePath)
    $cacheDeleted = $false
    $cacheManifestHash = 'none'
    if (Test-Path -LiteralPath $State.CachePath) {
        $cacheManifest = Join-Path $State.EvidenceRoot ('verify-prologue-import-attempt' + $Attempt + '.cache-before-delete.manifest.txt')
        $cacheLines = [Collections.Generic.List[string]]::new()
        $cacheLines.Add("cache_root=$($State.CachePath)")
        $cacheLines.Add("file_count=$($cacheFiles.Count)")
        foreach ($path in $cacheFiles) {
            $relative = $path.Substring(($State.CachePath + '\').Length).Replace('\', '/')
            $cacheLines.Add('file=' + $relative + '|sha256=' + (Get-EvidenceSha256 -LiteralPath $path))
        }
        [IO.File]::WriteAllLines($cacheManifest, $cacheLines.ToArray(), [Text.UTF8Encoding]::new($false))
        $cacheManifestHash = Get-EvidenceSha256 -LiteralPath $cacheManifest
        Remove-Item -LiteralPath $State.CachePath -Recurse -Force
        if (Test-Path -LiteralPath $State.CachePath) {
            throw "BASE_IMPORT_NATIVE_RETRY_CLEANUP_CACHE_DELETE_FAILED path=$($State.CachePath)"
        }
        $cacheDeleted = $true
    }
    $finalStatus = @(Get-Task6ImportRetryGitStatus -ProjectRoot $State.ProjectRoot)
    if ($finalStatus.Count -ne 0) {
        throw "BASE_IMPORT_NATIVE_RETRY_CLEANUP_STATUS_MISMATCH status=$($finalStatus -join '; ')"
    }
    & git -C $State.ProjectRoot diff --quiet
    if ($LASTEXITCODE -ne 0) { throw 'BASE_IMPORT_NATIVE_RETRY_CLEANUP_DIFF_MISMATCH' }
    & git -C $State.ProjectRoot diff --cached --quiet
    if ($LASTEXITCODE -ne 0) { throw 'BASE_IMPORT_NATIVE_RETRY_CLEANUP_INDEX_MISMATCH' }
    if (Test-Path -LiteralPath $State.CachePath) {
        throw "BASE_IMPORT_NATIVE_RETRY_CLEANUP_CACHE_REMAINS path=$($State.CachePath)"
    }
    $restoreLog = Join-Path $State.EvidenceRoot ('verify-prologue-import-attempt' + $Attempt + '.restore.txt')
    $restoreLines = [Collections.Generic.List[string]]::new()
    $restoreLines.Add('schema=task6-import-native-retry-restore-v1')
    $restoreLines.Add("attempt=$Attempt")
    $restoreLines.Add("prestate_manifest=$($State.Manifest)")
    $restoreLines.Add("restored_tracked_count=$($restored.Count)")
    foreach ($relative in $restored) { $restoreLines.Add("restored=$relative") }
    $restoreLines.Add("removed_sidecar_count=$($removedSidecars.Count)")
    foreach ($sidecar in $removedSidecars) { $restoreLines.Add("removed_sidecar=$sidecar") }
    $restoreLines.Add("cache_deleted=$cacheDeleted")
    $restoreLines.Add("cache_manifest_sha256=$cacheManifestHash")
    $restoreLines.Add('pre_hashes_restored=True')
    $restoreLines.Add('git_status_porcelain=<empty>')
    $restoreLines.Add('cache_exists=False')
    [IO.File]::WriteAllLines($restoreLog, $restoreLines.ToArray(), [Text.UTF8Encoding]::new($false))
    return [pscustomobject]@{
        RestoredTrackedCount = $restored.Count
        RemovedSidecarCount = $removedSidecars.Count
        CacheDeleted = $cacheDeleted
        RestoreLog = $restoreLog
    }
}

function Test-Task6ImportNativeAccessViolation {
    param([Parameter(Mandatory)][int]$ExitCode)
    return $ExitCode -eq -1073741819
}

function Get-Task6ImportRetryDiagnostics {
    param(
        [Parameter(Mandatory)][datetime]$StartedUtc,
        [AllowEmptyString()][string]$GodotPath = ''
    )
    $eventIds = [Collections.Generic.List[string]]::new()
    try {
        foreach ($event in @(Get-WinEvent -LogName Application -MaxEvents 100 -ErrorAction Stop | Where-Object {
                    $_.TimeCreated.ToUniversalTime() -ge $StartedUtc.AddSeconds(-1) -and
                    $_.Id -in 1000, 1001, 1026 -and $_.Message -match 'Godot'
                })) {
            $eventIds.Add([string]$event.Id)
        }
    }
    catch { }
    $dumpPath = 'none'
    $dumpHash = 'none'
    try {
        $dumpRoot = Join-Path $env:LOCALAPPDATA 'CrashDumps'
        if (Test-Path -LiteralPath $dumpRoot -PathType Container) {
            $dump = @(Get-ChildItem -LiteralPath $dumpRoot -File -ErrorAction Stop | Where-Object {
                    $_.LastWriteTimeUtc -ge $StartedUtc -and $_.Name -match '^Godot.*\.dmp$'
                } | Sort-Object LastWriteTimeUtc -Descending | Select-Object -First 1)
            if ($dump.Count -eq 1) {
                $dumpPath = $dump[0].FullName
                $dumpHash = Get-EvidenceSha256 -LiteralPath $dump[0].FullName
            }
        }
    }
    catch { }
    $godotHash = 'unavailable'
    if (-not [string]::IsNullOrWhiteSpace($GodotPath) -and
        (Test-Path -LiteralPath $GodotPath -PathType Leaf)) {
        $godotHash = Get-EvidenceSha256 -LiteralPath $GodotPath
    }
    return [pscustomobject]@{
        EventIds = @($eventIds | Sort-Object -Unique)
        DumpPath = $dumpPath
        DumpSha256 = $dumpHash
        GodotSha256 = $godotHash
    }
}

function Invoke-Task6ImportRetryAttempt {
    param(
        [Parameter(Mandatory)][object]$State,
        [Parameter(Mandatory)][int]$Attempt,
        [Parameter(Mandatory)][scriptblock]$PrepareAttempt,
        [Parameter(Mandatory)][scriptblock]$InvokeAttempt,
        [AllowEmptyString()][string]$GodotPath = ''
    )
    $log = Join-Path $State.EvidenceRoot ('verify-prologue-import-attempt' + $Attempt + '.log')
    $console = Join-Path $State.EvidenceRoot ('verify-prologue-import-attempt' + $Attempt + '.console.log')
    if ((Test-Path -LiteralPath $log) -or (Test-Path -LiteralPath $console)) {
        throw "BASE_IMPORT_NATIVE_RETRY_ATTEMPT_EVIDENCE_EXISTS attempt=$Attempt"
    }
    $startedUtc = [DateTime]::UtcNow
    $prepareOutput = @(& $PrepareAttempt $Attempt)
    $nativeOutput = @(& $InvokeAttempt $Attempt $log $console)
    $endedUtc = [DateTime]::UtcNow
    if ($nativeOutput.Count -ne 1 -or $null -eq $nativeOutput[0] -or
        $null -eq $nativeOutput[0].PSObject.Properties['ExitCode']) {
        throw "BASE_IMPORT_NATIVE_RETRY_INVOKER_RESULT_INVALID attempt=$Attempt"
    }
    $result = $nativeOutput[0]
    $exitCode = 0
    if (-not [int]::TryParse([string]$result.ExitCode, [ref]$exitCode)) {
        throw "BASE_IMPORT_NATIVE_RETRY_EXIT_INVALID attempt=$Attempt value=$($result.ExitCode)"
    }
    foreach ($path in @($log, $console)) {
        if (-not (Test-Path -LiteralPath $path -PathType Leaf) -or (Get-Item -LiteralPath $path).Length -le 0) {
            throw "BASE_IMPORT_NATIVE_RETRY_ATTEMPT_EVIDENCE_MISSING attempt=$Attempt path=$path"
        }
    }
    $markerCount = @((Get-Content -LiteralPath $log) | Where-Object {
            $_ -ceq 'VERIFICATION_EDITOR_GC_DRAIN_OK'
        }).Count
    $diagnostics = Get-Task6ImportRetryDiagnostics -StartedUtc $startedUtc -GodotPath $GodotPath
    $metadata = Join-Path $State.EvidenceRoot ('verify-prologue-import-attempt' + $Attempt + '.metadata.txt')
    $metadataLines = @(
        'schema=task6-import-native-retry-attempt-v1',
        ('attempt=' + $Attempt),
        ('started_utc=' + $startedUtc.ToString('o')),
        ('ended_utc=' + $endedUtc.ToString('o')),
        ('exit_code=' + $exitCode),
        ('exit_hex=' + $(if (Test-Task6ImportNativeAccessViolation -ExitCode $exitCode) { '0xC0000005' } else { 'not-native-access-violation' })),
        ('gc_drain_marker_count=' + $markerCount),
        ('log_path=' + $log),
        ('log_sha256=' + (Get-EvidenceSha256 -LiteralPath $log)),
        ('console_path=' + $console),
        ('console_sha256=' + (Get-EvidenceSha256 -LiteralPath $console)),
        ('windows_event_count=' + $diagnostics.EventIds.Count),
        ('windows_event_ids=' + ($diagnostics.EventIds -join ',')),
        ('crash_dump_path=' + $diagnostics.DumpPath),
        ('crash_dump_sha256=' + $diagnostics.DumpSha256),
        ('godot_sha256=' + $diagnostics.GodotSha256)
    )
    [IO.File]::WriteAllLines($metadata, $metadataLines, [Text.UTF8Encoding]::new($false))
    return [pscustomobject]@{
        Attempt = $Attempt
        ExitCode = $exitCode
        MarkerCount = $markerCount
        Log = $log
        Console = $console
        Metadata = $metadata
        Lines = @($prepareOutput) + @($result.Lines)
    }
}

function Invoke-Task6ImportWithBoundedNativeRetry {
    param(
        [Parameter(Mandatory)][string]$ProjectRoot,
        [Parameter(Mandatory)][string]$EvidenceRoot,
        [Parameter(Mandatory)][scriptblock]$PrepareAttempt,
        [Parameter(Mandatory)][scriptblock]$InvokeAttempt,
        [AllowEmptyString()][string]$GodotPath = ''
    )
    if (-not (Test-Path -LiteralPath $EvidenceRoot -PathType Container)) {
        throw "BASE_IMPORT_NATIVE_RETRY_EVIDENCE_ROOT_MISSING root=$EvidenceRoot"
    }
    $state = Get-Task6ImportRetryPreState -ProjectRoot $ProjectRoot -EvidenceRoot $EvidenceRoot
    $first = Invoke-Task6ImportRetryAttempt -State $state -Attempt 1 -PrepareAttempt $PrepareAttempt `
        -InvokeAttempt $InvokeAttempt -GodotPath $GodotPath
    foreach ($line in $first.Lines) { Write-Output $line }
    $restore = $null
    $second = $null
    if ($first.ExitCode -eq 0) {
        if ($first.MarkerCount -ne 1) {
            throw "BASE_IMPORT_NATIVE_RETRY_MARKER_MISSING attempt=1 actual=$($first.MarkerCount)"
        }
        $terminal = 'PROLOGUE_IMPORT_NATIVE_RETRY_OK attempts=1 first_exit=0 restored=False'
    }
    else {
        if (-not (Test-Task6ImportNativeAccessViolation -ExitCode $first.ExitCode)) {
            throw "BASE_IMPORT_NATIVE_RETRY_UNEXPECTED_EXIT exit=$($first.ExitCode)"
        }
        if ($first.MarkerCount -ne 1) {
            throw "BASE_IMPORT_NATIVE_RETRY_MARKER_MISSING attempt=1 actual=$($first.MarkerCount)"
        }
        try {
            $restore = Restore-Task6ImportRetryPreState -State $state -Attempt 1
        }
        catch {
            throw "BASE_IMPORT_NATIVE_RETRY_CLEANUP_FAILED $($_.Exception.Message)"
        }
        $second = Invoke-Task6ImportRetryAttempt -State $state -Attempt 2 -PrepareAttempt $PrepareAttempt `
            -InvokeAttempt $InvokeAttempt -GodotPath $GodotPath
        foreach ($line in $second.Lines) { Write-Output $line }
        if ($second.ExitCode -ne 0) {
            throw "BASE_IMPORT_NATIVE_RETRY_ATTEMPT2_FAILED exit=$($second.ExitCode)"
        }
        if ($second.MarkerCount -ne 1) {
            throw "BASE_IMPORT_NATIVE_RETRY_MARKER_MISSING attempt=2 actual=$($second.MarkerCount)"
        }
        $terminal = "PROLOGUE_IMPORT_NATIVE_RETRY_OK attempts=2 first_exit=$($first.ExitCode) restored=True"
    }
    $summary = Join-Path $state.EvidenceRoot 'verify-prologue-import-retry-summary.txt'
    $summaryLines = [Collections.Generic.List[string]]::new()
    $summaryLines.Add($terminal)
    $summaryLines.Add("prestate_manifest=$($state.Manifest)")
    $summaryLines.Add("attempt1_metadata=$($first.Metadata)")
    if ($null -ne $restore) { $summaryLines.Add("attempt1_restore=$($restore.RestoreLog)") }
    if ($null -ne $second) { $summaryLines.Add("attempt2_metadata=$($second.Metadata)") }
    [IO.File]::WriteAllLines($summary, $summaryLines.ToArray(), [Text.UTF8Encoding]::new($false))
    Write-Output $terminal
    $paths = [Collections.Generic.List[string]]::new()
    foreach ($path in @($state.Manifest, $first.Log, $first.Console, $first.Metadata, $summary)) { $paths.Add($path) }
    if ($null -ne $restore) { $paths.Add($restore.RestoreLog) }
    if ($null -ne $second) {
        foreach ($path in @($second.Log, $second.Console, $second.Metadata)) { $paths.Add($path) }
    }
    return [pscustomobject]@{
        Attempts = if ($null -eq $second) { 1 } else { 2 }
        FirstExit = $first.ExitCode
        Restored = $null -ne $restore
        FinalLog = if ($null -eq $second) { $first.Log } else { $second.Log }
        FinalConsole = if ($null -eq $second) { $first.Console } else { $second.Console }
        Summary = $summary
        EvidencePaths = @($paths | Sort-Object -Unique)
    }
}
