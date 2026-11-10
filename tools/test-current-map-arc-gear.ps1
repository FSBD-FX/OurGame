param([Parameter(Mandatory)][string]$SourceJson,[Parameter(Mandatory)][string]$FixtureDirectory)
Set-StrictMode -Version Latest
$ErrorActionPreference='Stop'
$source=[IO.Path]::GetFullPath($SourceJson)
$fixture=[IO.Path]::GetFullPath($FixtureDirectory)
$storage=[IO.Path]::GetDirectoryName($source)
if($source -notmatch '^G:\\knight\\验证证据\\october13-stable-claw\\resident-map-preview-[^\\]+\\storage\\current-prologue-map\.json$' -or
    !$fixture.StartsWith($storage+'\arc-renderer-controls-',[StringComparison]::OrdinalIgnoreCase) -or (Test-Path -LiteralPath $fixture)){
    throw 'Use the real retained map JSON and one fresh validation-only directory in its storage'
}
foreach($path in @($source,$fixture)){
    $entry=$path
    while($entry){
        if(Test-Path -LiteralPath $entry){if((Get-Item -LiteralPath $entry -Force).Attributes-band[IO.FileAttributes]::ReparsePoint){throw 'No reparse validation input/output'}}
        $entry=[IO.Path]::GetDirectoryName($entry)
    }
}
$base=Get-Content -Raw -LiteralPath $source|ConvertFrom-Json
if($null-eq$base.PSObject.Properties['configuredArcGears'] -or @($base.configuredArcGears).Count-ne1){throw 'Real exported Arc data required, not synthetic native provenance'}
[void](New-Item -ItemType Directory -Path $fixture)
$runtime='C:/Users/22499/.cache/codex-runtimes/codex-primary-runtime/dependencies/native/powershell/pwsh.exe'
$renderer=Join-Path $PSScriptRoot 'render-current-prologue-map.ps1'
$cases=[Collections.Generic.List[object]]::new()
$cases.Add(@{name='positive-real-export';change={param($x)};accepted=$true})
foreach($row in @(
    @{name='missing-arc';change={param($x)$x.PSObject.Properties.Remove('configuredArcGears')}},
    @{name='empty-arc';change={param($x)$x.configuredArcGears=@()}},
    @{name='duplicate-arc';change={param($x)$x.configuredArcGears+=$x.configuredArcGears[0]}},
    @{name='wrong-id';change={param($x)$x.configuredArcGears[0].id='wrong-id'}},
    @{name='rail-linkage';change={param($x)$x.configuredArcGears[0].objectId=$x.configuredMovingGears[0].id}},
    @{name='wrong-type';change={param($x)$x.configuredArcGears[0].type='CanyonMovingGear'}},
    @{name='wrong-sector';change={param($x)$x.configuredArcGears[0].sector='CanyonAscent'}},
    @{name='wrong-path';change={param($x)$x.configuredArcGears[0].path='Gameplay/Mechanisms/Other'}},
    @{name='wrong-start';change={param($x)$x.configuredArcGears[0].start[0]+=1}},
    @{name='wrong-pivot';change={param($x)$x.configuredArcGears[0].pivot[1]+=1}},
    @{name='wrong-end';change={param($x)$x.configuredArcGears[0].end[0]+=1}},
    @{name='endpoints-swapped';change={param($x)$v=$x.configuredArcGears[0].start;$x.configuredArcGears[0].start=$x.configuredArcGears[0].end;$x.configuredArcGears[0].end=$v}},
    @{name='wrong-angle-sign';change={param($x)$x.configuredArcGears[0].swingRadians*=-1}},
    @{name='degrees-not-radians';change={param($x)$x.configuredArcGears[0].swingRadians=-36.87}},
    @{name='endpoint-only-envelope';change={param($x)$x.configuredArcGears[0].envelope=@(3296,3932,208,48)}},
    @{name='envelope-small';change={param($x)$x.configuredArcGears[0].envelope[3]-=1}},
    @{name='envelope-large';change={param($x)$x.configuredArcGears[0].envelope[3]+=1}},
    @{name='false-damage-aabb';change={param($x)$x.configuredArcGears[0].envelopePolicy='exact-live-damage-shape'}},
    @{name='false-route-proof';change={param($x)$x.configuredArcGears[0].status='runtime-route-accepted'}},
    @{name='missing-object';change={param($x)$x.objects=@($x.objects|Where-Object type -CNE 'CanyonArcGear')}},
    @{name='duplicate-object';change={param($x)$x.objects+=($x.objects|Where-Object type -CEQ 'CanyonArcGear')}},
    @{name='object-pivot-not-start';change={param($x)($x.objects|Where-Object type -CEQ 'CanyonArcGear').point=@(3400,3716)}}
)){$row.accepted=$false;$cases.Add($row)}
foreach($key in @('radius','periodSeconds','damage','contactIntervalSeconds','swingRadians')){
    foreach($bad in @('string','nonfinite','zero','wrong-numeric')){
        $k=$key;$b=$bad
        $change={param($x)
            $value=switch($b){'string'{[string]$x.configuredArcGears[0].$k} 'nonfinite'{'NaN'} 'zero'{0} default{[double]$x.configuredArcGears[0].$k+0.1}}
            $x.configuredArcGears[0].$k=$value
        }.GetNewClosure()
        $cases.Add(@{name=$key+'-'+$bad;change=$change;accepted=$false})
    }
}
foreach($relative in @('CanyonArcGear','ArcGearMotionModel','CircularArcSweep')){
    foreach($bad in @('missing','duplicate','wrong-real-sha')){
        $binding='res://scripts/mechanisms/'+$relative+'.cs';$mode=$bad
        $change={param($x)
            switch($mode){
                'missing'{$x.sources=@($x.sources|Where-Object path -CNE $binding)}
                'duplicate'{$x.sources+=($x.sources|Where-Object path -CEQ $binding)}
                default{($x.sources|Where-Object path -CEQ $binding).sha256=($x.sources|Where-Object path -CEQ 'res://scripts/mechanisms/CanyonMovingGear.cs').sha256}
            }
        }.GetNewClosure()
        $cases.Add(@{name=$relative+'-'+$bad;change=$change;accepted=$false})
    }
}
$rows=[Collections.Generic.List[object]]::new()
foreach($case in $cases){
    $map=$base|ConvertTo-Json -Depth 100|ConvertFrom-Json
    & $case.change $map
    $path=Join-Path $fixture ($case.name+'.json');$receipt=Join-Path $fixture ($case.name+'-selftest.json')
    [IO.File]::WriteAllText($path,($map|ConvertTo-Json -Depth 100),[Text.UTF8Encoding]::new($false))
    $start=[Diagnostics.ProcessStartInfo]::new($runtime)
    $start.UseShellExecute=$false;$start.CreateNoWindow=$true;$start.RedirectStandardOutput=$true;$start.RedirectStandardError=$true
    foreach($arg in @('-NoProfile','-File',$renderer,'-InputJson',$path,'-SelfTest','-SelfTestReceipt',$receipt)){[void]$start.ArgumentList.Add($arg)}
    $process=[Diagnostics.Process]::Start($start)
    try{
        $out=$process.StandardOutput.ReadToEndAsync();$err=$process.StandardError.ReadToEndAsync()
        if(!$process.WaitForExit(60000)){$process.Kill();$process.WaitForExit();throw 'Owned validation renderer timed out'}
        $accepted=$process.ExitCode-eq0-and(Test-Path -LiteralPath $receipt)
        $rows.Add([pscustomobject]@{name=$case.name;wantAccepted=$case.accepted;accepted=$accepted;
            passed=($accepted-eq$case.accepted)-and($case.accepted-or!(Test-Path -LiteralPath $receipt));
            exit=$process.ExitCode;stdout=$out.GetAwaiter().GetResult();stderr=$err.GetAwaiter().GetResult()})
    }finally{$process.Dispose()}
}
$failed=@($rows|Where-Object {!$_.passed})
$result=[ordered]@{nativeBehavior=$false;humanAccepted=$false;validationOnly=$true;sourceJson=$source;
    sourceSha256=(Get-FileHash -LiteralPath $source).Hash;rendererSha256=(Get-FileHash -LiteralPath $renderer).Hash;
    cases=$rows.Count;passed=$rows.Count-$failed.Count;failed=$failed.Count;rows=$rows}
[IO.File]::WriteAllText((Join-Path $fixture 'arc-validation-results.json'),($result|ConvertTo-Json -Depth 12),[Text.UTF8Encoding]::new($false))
if($failed.Count){throw "Arc renderer rejected a valid map or accepted invalid geometry: $($failed.name-join', ')"}
"ARC_MAP_RENDERER_CONTROLS_OK cases=$($rows.Count) native_behavior=false validation_only=true human=false"
