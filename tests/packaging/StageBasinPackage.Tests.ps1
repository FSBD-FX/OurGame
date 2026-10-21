param(
    [string]$GitPath = 'C:/Users/22499/AppData/Local/Microsoft/WinGet/Packages/Git.MinGit_Microsoft.Winget.Source_8wekyb3d8bbwe/cmd/git.exe'
)
$ErrorActionPreference = 'Stop'
$taskRepository = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$taskStageTool = Join-Path $taskRepository 'tools/stage-basin-package.ps1'
$taskExpectedCommit = (& $GitPath -C $taskRepository rev-parse HEAD).Trim()
$taskDestination = Join-Path ([IO.Path]::GetTempPath()) ('knight-basin-stage-test-' + [Guid]::NewGuid().ToString('N'))
$taskAliasFixture = Join-Path ([IO.Path]::GetTempPath()) ('knight-basin-stage-alias-' + [Guid]::NewGuid().ToString('N'))
$taskShell = (Get-Process -Id $PID).Path
try {
    $taskOutput = & $taskShell -NoProfile -File $taskStageTool -Destination $taskDestination -SourceCommit $taskExpectedCommit -GitPath $GitPath 2>&1
    if ($LASTEXITCODE -ne 0) { throw "Stage command must succeed for the real committed runtime: $taskOutput" }
    $taskManifest = Get-Content -LiteralPath (Join-Path $taskDestination 'staging-manifest.json') -Raw | ConvertFrom-Json
    if ($taskManifest.source_commit -ne $taskExpectedCommit) { throw 'Staging must identify the exact frozen commit.' }
    $taskProject = Join-Path $taskDestination 'project'
    foreach ($taskRequired in @('project.godot','Knight.csproj','default_bus_layout.tres',
        'scenes/prologue/sample/BasinQualitySample.tscn','scripts/prologue/sample/BasinQualitySampleEntry.cs')) {
        if (-not (Test-Path -LiteralPath (Join-Path $taskProject $taskRequired) -PathType Leaf)) { throw "Missing real runtime dependency: $taskRequired" }
    }
    foreach ($taskForbidden in @('tests','tools','docs','addons','artifacts','.git','.godot','.agents','.superpowers')) {
        if (Test-Path -LiteralPath (Join-Path $taskProject $taskForbidden)) { throw "Unrelated or private tree entered staging: $taskForbidden" }
    }
    $taskAllowed = @('project.godot','Knight.csproj','Knight.sln','Knight.slnx','export_presets.cfg',
        'default_bus_layout.tres','scripts','scenes','assets')
    $taskExpectedPaths = @(& $GitPath -C $taskRepository ls-tree -r --name-only $taskExpectedCommit -- @taskAllowed)
    $taskActualPaths = @(Get-ChildItem -LiteralPath $taskProject -Recurse -File | ForEach-Object {
        [IO.Path]::GetRelativePath($taskProject, $_.FullName).Replace('\','/')
    })
    if (@(Compare-Object ($taskExpectedPaths | Sort-Object) ($taskActualPaths | Sort-Object)).Count -ne 0) {
        throw 'Staging must include exactly committed allowlisted files, including no untracked imports or missing assets.'
    }
    $taskExpectedBlobs = @{}
    foreach ($taskEntry in @(& $GitPath -C $taskRepository ls-tree -r $taskExpectedCommit -- @taskAllowed)) {
        if ($taskEntry -match '^\d+ blob ([0-9a-f]{40})\t(.+)$') { $taskExpectedBlobs[$Matches[2]] = $Matches[1] }
    }
    $taskUntransformedFiles = @(Get-ChildItem -LiteralPath $taskProject -Recurse -File | Where-Object {
        [IO.Path]::GetRelativePath($taskProject, $_.FullName).Replace('\','/') -ne 'project.godot'
    })
    $taskActualBlobs = @($taskUntransformedFiles.FullName | & $GitPath -C $taskRepository hash-object --no-filters --stdin-paths)
    if ($LASTEXITCODE -ne 0 -or $taskActualBlobs.Count -ne $taskUntransformedFiles.Count) {
        throw 'Cannot independently hash every untransformed staged runtime file.'
    }
    for ($taskIndex=0; $taskIndex -lt $taskUntransformedFiles.Count; $taskIndex++) {
        $taskFile = $taskUntransformedFiles[$taskIndex]
        $taskRelative = [IO.Path]::GetRelativePath($taskProject, $taskFile.FullName).Replace('\','/')
        $taskExpectedBlob = $taskExpectedBlobs[$taskRelative]
        $taskActualBlob = $taskActualBlobs[$taskIndex]
        if (-not $taskExpectedBlob -or $taskActualBlob -ne $taskExpectedBlob) {
            throw "Untransformed runtime file not from frozen commit: $taskRelative"
        }
    }
    $taskConfig = [IO.File]::ReadAllText((Join-Path $taskProject 'project.godot'))
    $taskPluginSections = [regex]::Matches($taskConfig, '(?ms)^\[editor_plugins\]\s*\r?\n.*?(?=^\[|\z)')
    if ($taskPluginSections.Count -ne 1 -or
        $taskPluginSections[0].Value.Trim() -ne "[editor_plugins]`nenabled=PackedStringArray()") {
        throw 'Generated project.godot must contain exactly one disabled editor-plugin section.'
    }
    if (@($taskManifest.transformations).Count -ne 1 -or
        $taskManifest.transformations[0] -ne 'project.godot: disable editor-only plugins in generated staging tree') {
        throw 'Manifest must disclose the exact generated project.godot transform.'
    }

    # An alias outside the checkout must not permit staging back inside it.
    $taskFixtureRepository = Join-Path $taskAliasFixture 'repository'
    $taskFixtureTools = Join-Path $taskFixtureRepository 'tools'
    New-Item -ItemType Directory -Path $taskFixtureTools | Out-Null
    Copy-Item -LiteralPath $taskStageTool -Destination (Join-Path $taskFixtureTools 'stage-basin-package.ps1')
    foreach ($taskFixtureDirectory in @('scripts','scenes','assets')) {
        New-Item -ItemType Directory -Path (Join-Path $taskFixtureRepository $taskFixtureDirectory) | Out-Null
        [IO.File]::WriteAllText((Join-Path $taskFixtureRepository "$taskFixtureDirectory/runtime.txt"), $taskFixtureDirectory)
    }
    [IO.File]::WriteAllText((Join-Path $taskFixtureRepository 'project.godot'),
        "[application]`nconfig/name=`"Fixture`"`n`n[editor_plugins]`nenabled=PackedStringArray(`"res://addons/private/plugin.cfg`")`n")
    [IO.File]::WriteAllText((Join-Path $taskFixtureRepository 'Knight.csproj'), '<Project />')
    [IO.File]::WriteAllText((Join-Path $taskFixtureRepository 'default_bus_layout.tres'), '[gd_resource format=3]')
    & $GitPath -C $taskFixtureRepository init -q
    & $GitPath -C $taskFixtureRepository config user.name 'Packaging Test'
    & $GitPath -C $taskFixtureRepository config user.email 'packaging-test.invalid'
    & $GitPath -C $taskFixtureRepository config core.autocrlf false
    & $GitPath -C $taskFixtureRepository add project.godot Knight.csproj default_bus_layout.tres scripts scenes assets
    & $GitPath -C $taskFixtureRepository commit -q -m 'fixture'
    if ($LASTEXITCODE -ne 0) { throw 'Cannot create disposable committed staging fixture.' }
    $taskAlias = Join-Path $taskAliasFixture 'checkout-alias'
    New-Item -ItemType Junction -Path $taskAlias -Target $taskFixtureRepository | Out-Null
    $taskAliasedDestination = Join-Path $taskAlias 'must-not-be-created'
    $taskAliasOutput = & $taskShell -NoProfile -File (Join-Path $taskFixtureTools 'stage-basin-package.ps1') `
        -Destination $taskAliasedDestination -SourceCommit HEAD -GitPath $GitPath 2>&1
    if ($LASTEXITCODE -eq 0 -or (Test-Path -LiteralPath (Join-Path $taskFixtureRepository 'must-not-be-created'))) {
        throw "A junction alias must not permit staging inside its own active checkout: $taskAliasOutput"
    }
    # Repeating a destination must fail and preserve its existing manifest.
    $taskManifestHash = (Get-FileHash -LiteralPath (Join-Path $taskDestination 'staging-manifest.json')).Hash
    $null = & $taskShell -NoProfile -File $taskStageTool -Destination $taskDestination -GitPath $GitPath 2>&1
    if ($LASTEXITCODE -eq 0 -or (Get-FileHash -LiteralPath (Join-Path $taskDestination 'staging-manifest.json')).Hash -ne $taskManifestHash) {
        throw 'A second invocation must refuse to overwrite an existing staging directory.'
    }
    Write-Output "BASIN_STAGE_TESTS_OK commit=$taskExpectedCommit runtime_files=$($taskActualPaths.Count) path=$taskDestination"
    exit 0
} finally {
    $taskTempRoot = [IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd('\','/') + [IO.Path]::DirectorySeparatorChar
    $taskAliasFixtureFull = [IO.Path]::GetFullPath($taskAliasFixture)
    if ($taskAliasFixtureFull.StartsWith($taskTempRoot, [StringComparison]::OrdinalIgnoreCase) -and
        (Split-Path $taskAliasFixtureFull -Leaf).StartsWith('knight-basin-stage-alias-', [StringComparison]::Ordinal)) {
        $taskAliasPath = Join-Path $taskAliasFixtureFull 'checkout-alias'
        if (Test-Path -LiteralPath $taskAliasPath) { Remove-Item -LiteralPath $taskAliasPath -Force }
        if (Test-Path -LiteralPath $taskAliasFixtureFull) { Remove-Item -LiteralPath $taskAliasFixtureFull -Recurse -Force }
    }
    # Keep the narrow generated fixture for inspection; never remove the user's checkout.
    Write-Output "STAGING_TEST_ARTIFACT=$taskDestination"
}
