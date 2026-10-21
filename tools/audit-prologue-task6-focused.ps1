[CmdletBinding()]
param(
    [string]$AuditRoot,
    [switch]$SkipBuild
)

$ErrorActionPreference = 'Stop'
$project = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
. (Join-Path $PSScriptRoot 'EvidenceChecks.ps1')
$dotnetRoot = 'G:\godot\dotnet-sdk'
$godot = 'G:\godot\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64_console.exe'
$env:DOTNET_ROOT = $dotnetRoot
$env:DOTNET_ROOT_X64 = $dotnetRoot
$env:DOTNET_ROLL_FORWARD = 'LatestMajor'
$env:DOTNET_CLI_UI_LANGUAGE = 'en'

if ([string]::IsNullOrWhiteSpace($AuditRoot)) {
    $AuditRoot = Join-Path $project ('artifacts\task6-focused-audit-' +
        (Get-Date -Format 'yyyyMMddTHHmmssfffZ') + '-' + [guid]::NewGuid().ToString('N'))
}
if (Test-Path -LiteralPath $AuditRoot) {
    throw "Task 6 audit root must be fresh; refusing to reuse or overwrite: $AuditRoot"
}
New-Item -ItemType Directory -Path $AuditRoot -Force | Out-Null

function Assert-OneMarker {
    param([string]$Text, [string]$Marker, [string]$Label)
    $count = @($Text -split "`r?`n" | Where-Object { $_ -ceq $Marker }).Count
    if ($count -ne 1) { throw "$Label must occur exactly once; actual=$count marker=$Marker" }
}

if (-not $SkipBuild) {
    $build = & (Join-Path $dotnetRoot 'dotnet.exe') build (Join-Path $project 'Knight.csproj') 2>&1
    $build | ForEach-Object { $_ }
    if ($LASTEXITCODE -ne 0 -or ($build -join "`n") -notmatch '0 Warning\(s\)' -or
        ($build -join "`n") -notmatch '0 Error\(s\)') {
        throw 'Task 6 focused audit build must complete with 0 warnings and 0 errors.'
    }
}

$log = Join-Path $AuditRoot 'production-audit.log'
$console = Join-Path $AuditRoot 'production-audit.console.log'
$started = [DateTime]::UtcNow
$result = Invoke-MergedNativeCommand -FilePath $godot -ArgumentList @(
    '--path', $project, '--fixed-fps', '60', '--display-driver', 'windows',
    '--rendering-method', 'gl_compatibility', '--audio-driver', 'Dummy',
    '--script', 'res://tests/presentation/PrologueVisualAuditTask6.cs', '--log-file', $log
) -ConsolePath $console
if ($result.ExitCode -ne 0) { throw "Task 6 focused production audit failed: $($result.ExitCode)" }
Assert-FreshNonEmptyOutputs -Paths @($log, $console) -StartedUtc $started
Assert-NoEngineLogErrors -Paths @($log, $console)
$text = Get-Content -LiteralPath $log -Raw
Assert-OneMarker -Text $text -Marker 'PROLOGUE_AUDIT_TOPOLOGY_OK sectors=5 micro_scenes=15 claw_roles=4' `
    -Label 'topology marker'
Assert-OneMarker -Text $text -Marker 'PROLOGUE_PRODUCTION_AUDIT_OK' -Label 'audit completion marker'
$match = [regex]::Match($text,
    '(?m)^PROLOGUE_PERFORMANCE peak_ms=(?<peak>\d+(?:\.\d+)?) ceiling_ms=50 basis=callback_wall_clock headroom_ms=\d+(?:\.\d+)?\s*$')
if (-not $match.Success -or @([regex]::Matches($text, $match.Value)).Count -ne 1) {
    throw 'focused audit misses an exact callback-performance marker'
}
$peak = [double]$match.Groups['peak'].Value
if ($peak -ge 50d) { throw "focused audit callback budget fails: peak_ms=$peak ceiling_ms=50" }

$manifest = Join-Path $AuditRoot 'audit-manifest.txt'
[IO.File]::WriteAllText($manifest, (@(
    "audit_root=$AuditRoot",
    'driver=windows fixed_fps=60 resolution=1280x720',
    'sectors=5 micro_scenes=15 claw_roles=4',
    "callback_peak_ms=$peak",
    "log_sha256=$(Get-EvidenceSha256 -LiteralPath $log)",
    "console_sha256=$(Get-EvidenceSha256 -LiteralPath $console)"
) -join "`n") + "`n", [Text.UTF8Encoding]::new($false))
$manifestHash = Get-EvidenceSha256 -LiteralPath $manifest
"PROLOGUE_FOCUSED_AUDIT_OK driver=windows fixed_fps=60 sectors=5 micro_scenes=15 claw_roles=4 peak_ms=$peak manifest=$manifest sha256=$manifestHash"
