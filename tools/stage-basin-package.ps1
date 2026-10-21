param(
    [Parameter(Mandatory=$true)][string]$Destination,
    [string]$SourceCommit = 'HEAD',
    [string]$GitPath = 'C:/Users/22499/AppData/Local/Microsoft/WinGet/Packages/Git.MinGit_Microsoft.Winget.Source_8wekyb3d8bbwe/cmd/git.exe'
)
$ErrorActionPreference = 'Stop'
Add-Type -TypeDefinition @'
using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

public static class BasinStageFinalPath
{
    private const uint FileFlagBackupSemantics = 0x02000000;
    private const uint VolumeNameDos = 0x0;
    private const uint VolumeNameNt = 0x2;

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern SafeFileHandle CreateFileW(string name, uint access, FileShare share,
        IntPtr security, FileMode creation, uint flags, IntPtr template);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern uint GetFinalPathNameByHandleW(SafeFileHandle file, StringBuilder path,
        uint length, uint flags);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool CreateDirectoryW(string path, IntPtr security);

    public static string[] ResolveExisting(string path)
    {
        using SafeFileHandle handle = CreateFileW(path, 0, FileShare.ReadWrite | FileShare.Delete,
            IntPtr.Zero, FileMode.Open, FileFlagBackupSemantics, IntPtr.Zero);
        if (handle.IsInvalid) throw new Win32Exception(Marshal.GetLastWin32Error());
        return new[] { ReadName(handle, VolumeNameNt), ReadName(handle, VolumeNameDos) };
    }

    public static void CreateNewDirectory(string path)
    {
        if (!CreateDirectoryW(path, IntPtr.Zero))
            throw new Win32Exception(Marshal.GetLastWin32Error());
    }

    private static string ReadName(SafeFileHandle handle, uint flags)
    {
        int capacity = 512;
        while (true)
        {
            var buffer = new StringBuilder(capacity);
            uint length = GetFinalPathNameByHandleW(handle, buffer, (uint)buffer.Capacity, flags);
            if (length == 0) throw new Win32Exception(Marshal.GetLastWin32Error());
            if (length < buffer.Capacity) return buffer.ToString();
            capacity = checked((int)length + 1);
        }
    }
}
'@

function ConvertTo-StageExtendedPath([string]$Path) {
    $stageFullPath = [IO.Path]::GetFullPath($Path)
    if ($stageFullPath.StartsWith('\\.\', [StringComparison]::OrdinalIgnoreCase)) {
        throw 'Device namespace destinations are not supported.'
    }
    if ($stageFullPath.StartsWith('\\?\', [StringComparison]::OrdinalIgnoreCase)) { return $stageFullPath }
    if ($stageFullPath.StartsWith('\\', [StringComparison]::OrdinalIgnoreCase)) {
        return '\\?\UNC\' + $stageFullPath.Substring(2)
    }
    return '\\?\' + $stageFullPath
}

function ConvertFrom-StageExtendedPath([string]$Path) {
    if ($Path.StartsWith('\\?\UNC\', [StringComparison]::OrdinalIgnoreCase)) {
        return '\\' + $Path.Substring(8)
    }
    if ($Path.StartsWith('\\?\', [StringComparison]::OrdinalIgnoreCase)) { return $Path.Substring(4) }
    return $Path
}

function Remove-StageTrailingSeparator([string]$Path) {
    $stageRoot = [IO.Path]::GetPathRoot($Path)
    if ($Path.Equals($stageRoot, [StringComparison]::OrdinalIgnoreCase)) { return $Path }
    return $Path.TrimEnd('\','/')
}

function Resolve-StageCandidate([string]$Path) {
    $stageFullPath = Remove-StageTrailingSeparator ([IO.Path]::GetFullPath($Path))
    $stageMissing = [Collections.Generic.List[string]]::new()
    $stageProbe = $stageFullPath
    while (-not (Test-Path -LiteralPath $stageProbe)) {
        $stageLeaf = [IO.Path]::GetFileName($stageProbe)
        $stageParent = [IO.Path]::GetDirectoryName($stageProbe)
        if ([string]::IsNullOrEmpty($stageLeaf) -or [string]::IsNullOrEmpty($stageParent) -or
            $stageParent.Equals($stageProbe, [StringComparison]::OrdinalIgnoreCase)) {
            throw "Cannot resolve an existing destination ancestor: $Path"
        }
        $stageMissing.Insert(0, $stageLeaf)
        $stageProbe = Remove-StageTrailingSeparator $stageParent
    }
    if (-not (Test-Path -LiteralPath $stageProbe -PathType Container)) {
        throw "Destination ancestor is not a directory: $stageProbe"
    }
    $stageFinal = [BasinStageFinalPath]::ResolveExisting((ConvertTo-StageExtendedPath $stageProbe))
    $stageIdentity = $stageFinal[0]
    $stageOperational = $stageFinal[1]
    foreach ($stagePart in $stageMissing) {
        $stageIdentity = $stageIdentity.TrimEnd('\') + '\' + $stagePart
        $stageOperational = [IO.Path]::Combine($stageOperational, $stagePart)
    }
    [pscustomobject]@{
        Exists = $stageMissing.Count -eq 0
        Identity = Remove-StageTrailingSeparator $stageIdentity
        Operational = Remove-StageTrailingSeparator $stageOperational
    }
}

function Test-StageSameOrChild([string]$Candidate, [string]$Container) {
    if ($Candidate.Equals($Container, [StringComparison]::OrdinalIgnoreCase)) { return $true }
    return $Candidate.StartsWith($Container.TrimEnd('\') + '\', [StringComparison]::OrdinalIgnoreCase)
}

$stageRepository = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
if (-not [IO.Path]::IsPathFullyQualified($Destination)) { throw 'Destination must be an absolute new directory.' }
$stageDestinationCandidate = Resolve-StageCandidate $Destination
$stageRepositoryFinal = Resolve-StageCandidate $stageRepository
if ($stageDestinationCandidate.Exists -or
    (Test-StageSameOrChild $stageDestinationCandidate.Identity $stageRepositoryFinal.Identity) -or
    (Test-StageSameOrChild $stageRepositoryFinal.Identity $stageDestinationCandidate.Identity)) {
    throw 'Destination must be new and outside the active checkout, not its ancestor.'
}
$stageCommitOutput = & $GitPath -C $stageRepository rev-parse --verify --end-of-options ($SourceCommit + '^{commit}')
if ($LASTEXITCODE -ne 0) { throw 'SourceCommit must resolve to an existing commit.' }
$stageCommit = ($stageCommitOutput | Select-Object -Last 1).Trim()
if ($stageCommit -notmatch '^[0-9a-f]{40}$') { throw 'Unexpected commit identifier.' }
$stageAllowed = @('project.godot','Knight.csproj','Knight.sln','Knight.slnx','export_presets.cfg',
    'default_bus_layout.tres','scripts','scenes','assets')
$stageRoots = @(& $GitPath -C $stageRepository ls-tree --name-only $stageCommit -- @stageAllowed)
if ($LASTEXITCODE -ne 0) { throw 'Cannot inspect committed runtime roots.' }
foreach ($stageRequired in @('project.godot','Knight.csproj','default_bus_layout.tres','scripts','scenes','assets')) {
    if ($stageRequired -notin $stageRoots) { throw "Source commit has no required runtime root: $stageRequired" }
}
$stageEntries = @(& $GitPath -C $stageRepository ls-tree -r $stageCommit -- @stageRoots)
if ($LASTEXITCODE -ne 0 -or @($stageEntries | Where-Object { $_ -match '^(120000|160000) ' }).Count -gt 0) {
    throw 'Runtime staging does not support symlinks or submodules; resolve their dependency explicitly.'
}
[BasinStageFinalPath]::CreateNewDirectory($stageDestinationCandidate.Operational)
$stageCreatedDestination = Resolve-StageCandidate $stageDestinationCandidate.Operational
if (-not $stageCreatedDestination.Exists -or
    -not $stageCreatedDestination.Identity.Equals($stageDestinationCandidate.Identity, [StringComparison]::OrdinalIgnoreCase) -or
    (Test-StageSameOrChild $stageCreatedDestination.Identity $stageRepositoryFinal.Identity) -or
    (Test-StageSameOrChild $stageRepositoryFinal.Identity $stageCreatedDestination.Identity)) {
    throw 'Created destination did not retain its validated filesystem identity.'
}
$stageDestination = ConvertFrom-StageExtendedPath $stageCreatedDestination.Operational
$stageArchive = Join-Path $stageDestination 'source-runtime.zip'
& $GitPath -C $stageRepository -c core.autocrlf=false -c core.longpaths=true archive --format=zip ('--output=' + $stageArchive) $stageCommit -- @stageRoots
if ($LASTEXITCODE -ne 0) { throw 'Could not archive the committed runtime allowlist.' }
$stageProject = Join-Path $stageDestination 'project'
[IO.Compression.ZipFile]::ExtractToDirectory($stageArchive, $stageProject)
# The release tree has no editor-only addons. This generated config transform is recorded below.
$stageConfigPath = Join-Path $stageProject 'project.godot'
$stageConfig = [IO.File]::ReadAllText($stageConfigPath)
$stageConfig = [regex]::Replace($stageConfig, '(?ms)^\[editor_plugins\]\s*\r?\n.*?(?=^\[|\z)',
    "[editor_plugins]`nenabled=PackedStringArray()`n`n")
[IO.File]::WriteAllText($stageConfigPath, $stageConfig, [Text.UTF8Encoding]::new($false))
$stageFiles = @(Get-ChildItem -LiteralPath $stageProject -File -Recurse | Sort-Object FullName | ForEach-Object {
    [ordered]@{ path=[IO.Path]::GetRelativePath($stageProject,$_.FullName).Replace('\','/'); bytes=$_.Length;
        sha256=(Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant() }
})
$stageManifest = [ordered]@{
    source_commit=$stageCommit
    build_tool_sha256=(Get-FileHash -LiteralPath $PSCommandPath -Algorithm SHA256).Hash.ToLowerInvariant()
    source_archive_sha256=(Get-FileHash -LiteralPath $stageArchive -Algorithm SHA256).Hash.ToLowerInvariant()
    transformations=@('project.godot: disable editor-only plugins in generated staging tree')
    roots=$stageRoots
    files=$stageFiles
}
[IO.File]::WriteAllText((Join-Path $stageDestination 'staging-manifest.json'),
    ($stageManifest | ConvertTo-Json -Depth 6), [Text.UTF8Encoding]::new($false))
Write-Output "BASIN_RUNTIME_STAGED commit=$stageCommit files=$($stageFiles.Count) path=$stageProject"
