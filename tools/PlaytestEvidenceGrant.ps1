function Start-HandsOnPlaytestProcess {
    param(
        [Parameter(Mandatory = $true)][string]$FilePath,
        [Parameter(Mandatory = $true)][string[]]$ArgumentList,
        [Parameter(Mandatory = $true)][string]$ProjectPath
    )

    $canonicalProject = (Resolve-Path -LiteralPath $ProjectPath).Path
    $grantDirectory = Join-Path ([System.IO.Path]::GetTempPath()) 'knight-playtest-grants'
    New-Item -ItemType Directory -Path $grantDirectory -Force | Out-Null
    $grantPath = Join-Path $grantDirectory (([Guid]::NewGuid().ToString('N')) + '.json')
    $nonceBytes = New-Object byte[] 32
    $random = [System.Security.Cryptography.RandomNumberGenerator]::Create()
    try { $random.GetBytes($nonceBytes) }
    finally { $random.Dispose() }
    $nonce = -join ($nonceBytes | ForEach-Object { $_.ToString('x2') })
    $issued = [DateTimeOffset]::UtcNow
    $grant = [ordered]@{
        version = 1
        source = 'HandsOnLauncher'
        projectPath = $canonicalProject
        issuedAtUtc = $issued.ToString('O')
        expiresAtUtc = $issued.AddSeconds(90).ToString('O')
        nonce = $nonce
    }
    $grantJson = $grant | ConvertTo-Json -Compress
    $utf8WithoutBom = New-Object System.Text.UTF8Encoding($false)
    [System.IO.File]::WriteAllText($grantPath, $grantJson, $utf8WithoutBom)

    $priorPlaytestSource = [Environment]::GetEnvironmentVariable('KNIGHT_PLAYTEST_SOURCE', 'Process')
    $priorGrantNonce = [Environment]::GetEnvironmentVariable('KNIGHT_PLAYTEST_GRANT_NONCE', 'Process')
    try {
        $env:KNIGHT_PLAYTEST_SOURCE = 'HandsOnLauncher'
        $env:KNIGHT_PLAYTEST_GRANT_NONCE = $nonce
        $launchArguments = @($ArgumentList) + @(
            '--', '--knight-playtest-grant-file', $grantPath,
            '--knight-playtest-grant-nonce', $nonce)
        try {
            Start-Process -FilePath $FilePath -ArgumentList $launchArguments -PassThru
        }
        catch {
            Remove-Item -LiteralPath $grantPath -Force -ErrorAction SilentlyContinue
            throw
        }
    }
    finally {
        if ($null -eq $priorPlaytestSource) { Remove-Item Env:\KNIGHT_PLAYTEST_SOURCE -ErrorAction SilentlyContinue }
        else { $env:KNIGHT_PLAYTEST_SOURCE = $priorPlaytestSource }
        if ($null -eq $priorGrantNonce) { Remove-Item Env:\KNIGHT_PLAYTEST_GRANT_NONCE -ErrorAction SilentlyContinue }
        else { $env:KNIGHT_PLAYTEST_GRANT_NONCE = $priorGrantNonce }
    }
}
