$ErrorActionPreference = 'Stop'
$project = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$script = Join-Path $project 'tools\play-prologue.ps1'
$grantHelper = Join-Path $project 'tools\PlaytestEvidenceGrant.ps1'
$fakeRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("knight-play-launcher-{0}" -f [Guid]::NewGuid().ToString('N'))
$fakeGodotDirectory = Join-Path $fakeRoot 'godot'
$fakeDotnetRoot = Join-Path $fakeRoot 'dotnet'
$fakeGodot = Join-Path $fakeGodotDirectory 'Godot.exe'
$fakeDotnet = Join-Path $fakeDotnetRoot 'dotnet.exe'

try {
    New-Item -ItemType Directory -Path $fakeGodotDirectory,$fakeDotnetRoot -Force | Out-Null
    Set-Content -LiteralPath $fakeGodot -Value 'fixture' -NoNewline
    Set-Content -LiteralPath $fakeDotnet -Value 'fixture' -NoNewline
    $priorPreference = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    $result = & powershell -NoProfile -ExecutionPolicy Bypass -Command `
        "& '$script' -ValidateOnly -GodotPath '$fakeGodot' -DotnetRoot '$fakeDotnetRoot' -DotnetVersionOverride '8.0.999'" 2>&1
    $badVersionExit = $LASTEXITCODE
    $ErrorActionPreference = $priorPreference
    if ($badVersionExit -eq 0 -or ($result -join "`n") -notmatch 'requires .NET SDK 9') {
        throw 'Launcher accepted a non-.NET-9 runtime.'
    }

    $result = & powershell -NoProfile -ExecutionPolicy Bypass -File $script -ValidateOnly `
        -GodotPath $fakeGodot -DotnetRoot $fakeDotnetRoot -DotnetVersionOverride '9.0.104' 2>&1
    if ($LASTEXITCODE -ne 0 -or ($result -join "`n") -notmatch 'PLAY_PROLOGUE_RUNTIME_OK') {
        throw 'Launcher rejected the pinned .NET 9 runtime fixture.'
    }
    $scriptText = Get-Content -LiteralPath $script -Raw
    if ($scriptText -notmatch "DOTNET_ROLL_FORWARD\s*=\s*'LatestMajor'") {
        throw 'Launcher cannot roll the Godot .NET 8 host forward to the required .NET 9 runtime.'
    }
    if ($scriptText -notmatch "'_console\.exe'") {
        throw 'Launcher does not separate headless import verification from the visible game executable.'
    }
    . $grantHelper
    $captureScript = Join-Path $fakeRoot 'capture-grant.ps1'
    Set-Content -LiteralPath $captureScript -Value @'
$payload = [ordered]@{
    source = [Environment]::GetEnvironmentVariable('KNIGHT_PLAYTEST_SOURCE', 'Process')
    nonce = [Environment]::GetEnvironmentVariable('KNIGHT_PLAYTEST_GRANT_NONCE', 'Process')
    arguments = @($args)
}
$payload | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath $env:KNIGHT_TEST_GRANT_CAPTURE
'@
    $priorSource = [Environment]::GetEnvironmentVariable('KNIGHT_PLAYTEST_SOURCE', 'Process')
    $priorNonce = [Environment]::GetEnvironmentVariable('KNIGHT_PLAYTEST_GRANT_NONCE', 'Process')
    $priorCapture = [Environment]::GetEnvironmentVariable('KNIGHT_TEST_GRANT_CAPTURE', 'Process')
    $grants = @()
    try {
        $env:KNIGHT_PLAYTEST_SOURCE = 'PriorFixtureValue'
        $env:KNIGHT_PLAYTEST_GRANT_NONCE = 'PriorNonceValue'
        for ($run = 0; $run -lt 2; $run++) {
            $capture = Join-Path $fakeRoot ("grant-capture-{0}.json" -f $run)
            $env:KNIGHT_TEST_GRANT_CAPTURE = $capture
            $child = Start-HandsOnPlaytestProcess -FilePath 'powershell.exe' -ArgumentList @(
                '-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', $captureScript) `
                -ProjectPath $project
            $child.WaitForExit()
            if ($child.ExitCode -ne 0 -or -not (Test-Path -LiteralPath $capture -PathType Leaf)) {
                throw "Spawned grant fixture failed for run $run."
            }
            $observed = Get-Content -LiteralPath $capture -Raw | ConvertFrom-Json
            if ($observed.source -ne 'HandsOnLauncher' -or
                [string]::IsNullOrWhiteSpace([string]$observed.nonce)) {
                throw 'Spawned game process did not inherit the bounded hands-on grant nonce.'
            }
            $arguments = @($observed.arguments)
            $fileIndex = [Array]::IndexOf($arguments, '--knight-playtest-grant-file')
            $nonceIndex = [Array]::IndexOf($arguments, '--knight-playtest-grant-nonce')
            if ($fileIndex -lt 0 -or $nonceIndex -lt 0 -or
                $fileIndex + 1 -ge $arguments.Count -or $nonceIndex + 1 -ge $arguments.Count) {
                throw 'Spawned game process did not receive the launcher argument/file handshake.'
            }
            $grantPath = [string]$arguments[$fileIndex + 1]
            $argumentNonce = [string]$arguments[$nonceIndex + 1]
            if (-not (Test-Path -LiteralPath $grantPath -PathType Leaf)) {
                throw 'Launcher grant file was not created for the spawned game process.'
            }
            $grantBytes = [System.IO.File]::ReadAllBytes($grantPath)
            if ($grantBytes.Length -ge 3 -and $grantBytes[0] -eq 0xEF -and
                $grantBytes[1] -eq 0xBB -and $grantBytes[2] -eq 0xBF) {
                throw 'Launcher grant uses a PowerShell 5.1 UTF-8 BOM that can break runtime JSON validation.'
            }
            $grant = Get-Content -LiteralPath $grantPath -Raw | ConvertFrom-Json
            $issued = [DateTimeOffset]::Parse([string]$grant.issuedAtUtc)
            $expires = [DateTimeOffset]::Parse([string]$grant.expiresAtUtc)
            if ($grant.version -ne 1 -or $grant.source -ne 'HandsOnLauncher' -or
                $grant.nonce -ne $observed.nonce -or $argumentNonce -ne $observed.nonce -or
                $grant.projectPath -ne $project -or $expires -le $issued -or
                ($expires - $issued).TotalSeconds -gt 120) {
                throw 'Launcher grant file, nonce, project, or bounded lifetime did not match.'
            }
            $grants += [pscustomobject]@{ path = $grantPath; nonce = [string]$grant.nonce }
        }
        if ($grants.Count -ne 2 -or $grants[0].path -eq $grants[1].path -or
            $grants[0].nonce -eq $grants[1].nonce) {
            throw 'Launcher reused a hands-on grant path or nonce across runs.'
        }
        if ($env:KNIGHT_PLAYTEST_SOURCE -ne 'PriorFixtureValue' -or
            $env:KNIGHT_PLAYTEST_GRANT_NONCE -ne 'PriorNonceValue') {
            throw 'Launcher leaked its hands-on evidence grant into the calling PowerShell session.'
        }
    }
    finally {
        foreach ($grant in $grants) {
            if (Test-Path -LiteralPath $grant.path) {
                Remove-Item -LiteralPath $grant.path -Force
            }
        }
        if ($null -eq $priorSource) { Remove-Item Env:\KNIGHT_PLAYTEST_SOURCE -ErrorAction SilentlyContinue }
        else { $env:KNIGHT_PLAYTEST_SOURCE = $priorSource }
        if ($null -eq $priorNonce) { Remove-Item Env:\KNIGHT_PLAYTEST_GRANT_NONCE -ErrorAction SilentlyContinue }
        else { $env:KNIGHT_PLAYTEST_GRANT_NONCE = $priorNonce }
        if ($null -eq $priorCapture) { Remove-Item Env:\KNIGHT_TEST_GRANT_CAPTURE -ErrorAction SilentlyContinue }
        else { $env:KNIGHT_TEST_GRANT_CAPTURE = $priorCapture }
    }
    'PLAY_PROLOGUE_TESTS_OK'
}
finally {
    if (Test-Path -LiteralPath $fakeRoot) { Remove-Item -LiteralPath $fakeRoot -Recurse -Force }
}
