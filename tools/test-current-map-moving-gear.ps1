param([Parameter(Mandatory)][string]$SourceJson,[Parameter(Mandatory)][string]$FixtureDirectory)
Set-StrictMode -Version Latest
$ErrorActionPreference='Stop'
$allowed='G:\knight\验证证据\october13-stable-claw\resident-map-preview-20261108-084843-157-bafa34f1ba2345569c15908d5d24e9ee\storage\'
$fixture=[IO.Path]::GetFullPath($FixtureDirectory)
if(!$fixture.StartsWith($allowed,[StringComparison]::OrdinalIgnoreCase)-or(Test-Path -LiteralPath $fixture)){
    throw 'Fresh validation-only directory inside the explicitly scoped storage is required'
}
[void](New-Item -ItemType Directory -Path $fixture)
$renderer=Join-Path $PSScriptRoot 'render-current-prologue-map.ps1'
$runtime='C:/Users/22499/.cache/codex-runtimes/codex-primary-runtime/dependencies/native/powershell/pwsh.exe'
$base=Get-Content -LiteralPath $SourceJson -Raw | ConvertFrom-Json
if($null-ne$base.PSObject.Properties['configuredMovingGears']){throw 'This fixture builder requires the retained pre-rail source, not a native-qualification claim'}
$gearId='CanyonBasin/Gameplay/Mechanisms/UpperGalleryMovingGear'
$base.objects+= [pscustomobject]@{id=$gearId;type='CanyonMovingGear';sector='CanyonBasin';path='Gameplay/Mechanisms/UpperGalleryMovingGear';
    point=@(3400,3550);status='saved-anchor-not-dynamic-danger-envelope'}
$base | Add-Member -NotePropertyName configuredMovingGears -NotePropertyValue @([pscustomobject]@{
    id=$gearId;objectId=$gearId;type='CanyonMovingGear';sector='CanyonBasin';path='Gameplay/Mechanisms/UpperGalleryMovingGear';
    start=@(3400,3550);end=@(3400,3630);radius=24;periodSeconds=3.2;damage=2;contactIntervalSeconds=0.5;
    envelope=@(3376,3526,48,128);envelopePolicy='conservative-aabb-not-damage-shape';status='configured-rail-not-runtime-route-proof'
})
foreach($relative in @('scripts/mechanisms/CanyonMovingGear.cs','scripts/mechanisms/MovingGearMotionModel.cs')){
    $base.sources+=[pscustomobject]@{path='res://'+$relative;sha256=(Get-FileHash -LiteralPath (Join-Path (Split-Path $PSScriptRoot) $relative) -Algorithm SHA256).Hash}
}
# Every negative exercises the real -SelfTest boundary. A renderer which ignores rails
# accepts the bad input and makes this test fail. Nothing here renders or invents native provenance.
$cases=@(
    @{name='positive-validation-only';change={param($x)};accepted=$true},
    @{name='wrong-rail-end';change={param($x) $x.configuredMovingGears[0].end[1]=3629};accepted=$false},
    @{name='wrong-linkage';change={param($x) $x.configuredMovingGears[0].objectId=$x.objects[0].id};accepted=$false},
    @{name='missing-rail';change={param($x) $x.PSObject.Properties.Remove('configuredMovingGears')};accepted=$false},
    @{name='empty-rail';change={param($x) $x.configuredMovingGears=@()};accepted=$false},
    @{name='duplicate-rail';change={param($x) $x.configuredMovingGears+=$x.configuredMovingGears[0]};accepted=$false},
    @{name='wrong-id';change={param($x) $x.configuredMovingGears[0].id='wrong-id'};accepted=$false},
    @{name='wrong-type';change={param($x) $x.configuredMovingGears[0].type='CanyonFixedGear'};accepted=$false},
    @{name='wrong-sector';change={param($x) $x.configuredMovingGears[0].sector='CanyonAscent'};accepted=$false},
    @{name='wrong-path';change={param($x) $x.configuredMovingGears[0].path='Gameplay/Mechanisms/Other'};accepted=$false},
    @{name='wrong-start';change={param($x) $x.configuredMovingGears[0].start[0]=3399};accepted=$false},
    @{name='outside-end';change={param($x) $x.configuredMovingGears[0].end[1]=4400};accepted=$false},
    @{name='nonfinite-start';change={param($x) $x.configuredMovingGears[0].start[0]='NaN'};accepted=$false},
    @{name='non-numeric-end';change={param($x) $x.configuredMovingGears[0].end[0]='3400'};accepted=$false},
    @{name='short-start';change={param($x) $x.configuredMovingGears[0].start=@(3400)};accepted=$false},
    @{name='wrong-envelope';change={param($x) $x.configuredMovingGears[0].envelope[3]=127};accepted=$false},
    @{name='outside-envelope';change={param($x) $x.configuredMovingGears[0].envelope=@(3376,4300,48,128)};accepted=$false},
    @{name='non-numeric-envelope';change={param($x) $x.configuredMovingGears[0].envelope[0]='3376'};accepted=$false},
    @{name='wrong-radius';change={param($x) $x.configuredMovingGears[0].radius=25};accepted=$false},
    @{name='string-radius';change={param($x) $x.configuredMovingGears[0].radius='24'};accepted=$false},
    @{name='wrong-period';change={param($x) $x.configuredMovingGears[0].periodSeconds=0};accepted=$false},
    @{name='nonfinite-period';change={param($x) $x.configuredMovingGears[0].periodSeconds='NaN'};accepted=$false},
    @{name='wrong-damage';change={param($x) $x.configuredMovingGears[0].damage=3};accepted=$false},
    @{name='string-damage';change={param($x) $x.configuredMovingGears[0].damage='2'};accepted=$false},
    @{name='wrong-contact-interval';change={param($x) $x.configuredMovingGears[0].contactIntervalSeconds=-0.5};accepted=$false},
    @{name='string-contact-interval';change={param($x) $x.configuredMovingGears[0].contactIntervalSeconds='0.5'};accepted=$false},
    @{name='false-envelope-damage-shape';change={param($x) $x.configuredMovingGears[0].envelopePolicy='exact-live-damage-shape'};accepted=$false},
    @{name='false-runtime-route-proof';change={param($x) $x.configuredMovingGears[0].status='runtime-route-accepted'};accepted=$false},
    @{name='missing-object';change={param($x) $x.objects=@($x.objects | Where-Object type -CNE 'CanyonMovingGear')};accepted=$false},
    @{name='duplicate-object';change={param($x) $x.objects+=($x.objects | Where-Object type -CEQ 'CanyonMovingGear')};accepted=$false},
    @{name='wrong-object-point';change={param($x) ($x.objects | Where-Object type -CEQ 'CanyonMovingGear').point[1]=3551};accepted=$false},
    @{name='wrong-object-type';change={param($x) ($x.objects | Where-Object type -CEQ 'CanyonMovingGear').type='CanyonFixedGear'};accepted=$false},
    @{name='wrong-object-sector';change={param($x) ($x.objects | Where-Object type -CEQ 'CanyonMovingGear').sector='CanyonAscent'};accepted=$false},
    @{name='wrong-object-path';change={param($x) ($x.objects | Where-Object type -CEQ 'CanyonMovingGear').path='Gameplay/Mechanisms/Other'};accepted=$false},
    @{name='missing-gear-source';change={param($x) $x.sources=@($x.sources | Where-Object path -CNE 'res://scripts/mechanisms/CanyonMovingGear.cs')};accepted=$false},
    @{name='duplicate-gear-source';change={param($x) $x.sources+=($x.sources | Where-Object path -CEQ 'res://scripts/mechanisms/CanyonMovingGear.cs')};accepted=$false},
    @{name='wrong-gear-source-sha';change={param($x) ($x.sources | Where-Object path -CEQ 'res://scripts/mechanisms/CanyonMovingGear.cs').sha256=('A'*64)};accepted=$false},
    @{name='missing-model-source';change={param($x) $x.sources=@($x.sources | Where-Object path -CNE 'res://scripts/mechanisms/MovingGearMotionModel.cs')};accepted=$false},
    @{name='duplicate-model-source';change={param($x) $x.sources+=($x.sources | Where-Object path -CEQ 'res://scripts/mechanisms/MovingGearMotionModel.cs')};accepted=$false},
    @{name='wrong-model-source-sha';change={param($x) ($x.sources | Where-Object path -CEQ 'res://scripts/mechanisms/MovingGearMotionModel.cs').sha256=('A'*64)};accepted=$false}
)
$rows=[Collections.Generic.List[object]]::new()
foreach($case in $cases){
    $map=$base | ConvertTo-Json -Depth 100 | ConvertFrom-Json
    & $case.change $map
    $path=Join-Path $fixture ($case.name+'.json')
    [IO.File]::WriteAllText($path,($map | ConvertTo-Json -Depth 100),[Text.UTF8Encoding]::new($false))
    $receipt=Join-Path $fixture ($case.name+'-selftest.json')
    $start=[Diagnostics.ProcessStartInfo]::new($runtime)
    $start.UseShellExecute=$false;$start.CreateNoWindow=$true;$start.RedirectStandardOutput=$true;$start.RedirectStandardError=$true
    foreach($argument in @('-NoProfile','-File',$renderer,'-InputJson',$path,'-SelfTest','-SelfTestReceipt',$receipt)){[void]$start.ArgumentList.Add($argument)}
    $process=[Diagnostics.Process]::Start($start)
    $stdout=$process.StandardOutput.ReadToEndAsync();$stderr=$process.StandardError.ReadToEndAsync()
    if(!$process.WaitForExit(60000)){$process.Kill();$process.WaitForExit();throw 'Owned validation-only renderer timed out'}
    $accepted=$process.ExitCode-eq0-and(Test-Path -LiteralPath $receipt)
    $rows.Add([pscustomobject]@{name=$case.name;wantAccepted=$case.accepted;accepted=$accepted;
        passed=($accepted-eq$case.accepted)-and($case.accepted-or!(Test-Path -LiteralPath $receipt));
        exit=$process.ExitCode;stdout=$stdout.GetAwaiter().GetResult();stderr=$stderr.GetAwaiter().GetResult();path=$path})
    $process.Dispose()
}
$result=[ordered]@{kind='moving-gear-validation-only-fixtures-not-native';version='v314';sourceNativeQualificationClaimed=$false;
    sourceJson=$SourceJson;sourceJsonSha256=(Get-FileHash -LiteralPath $SourceJson -Algorithm SHA256).Hash;
    rendererSha256=(Get-FileHash -LiteralPath $renderer -Algorithm SHA256).Hash;controls=$rows.ToArray();
    passed=(@($rows | Where-Object {!$_.passed}).Count-eq0);nativeBehavior=$false;mediaPublished=$false}
$resultPath=Join-Path $fixture 'moving-gear-validation-only-results.json'
[IO.File]::WriteAllText($resultPath,($result | ConvertTo-Json -Depth 12),[Text.UTF8Encoding]::new($false))
foreach($row in $rows){Write-Output ('MOVING_GEAR_VALIDATION_ONLY name='+$row.name+' passed='+$row.passed+' accepted='+$row.accepted)}
Write-Output ('MOVING_GEAR_VALIDATION_RECEIPT='+$resultPath)
if(!$result.passed){throw 'Validation-only moving gear controls failed'}
