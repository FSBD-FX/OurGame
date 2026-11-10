param([switch]$Show,[switch]$DisplayConfirmed,[switch]$FunctionsOnly,[switch]$IsolatedPreview)
Set-StrictMode -Version Latest
$ErrorActionPreference='Stop'
 $script:TitleProjectRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
 $script:TitlePwsh='C:/Users/22499/.cache/codex-runtimes/codex-primary-runtime/dependencies/native/powershell/pwsh.exe'
 $script:TitleGodot='G:/knight/Tools/Godot_v4.7.1-stable_mono_win64/Godot_v4.7.1-stable_mono_win64.exe'
function Get-TitleLaunchMode([bool]$Show,[bool]$DisplayConfirmed) {
    if($Show-ne$DisplayConfirmed){throw 'Show requires a separate current display confirmation; confirmation alone does not launch'}
    if($Show){return 'live'}
    return 'prepare'
}
function Assert-TitleReceipt($Receipt,[string]$ExpectedPhase='keyboard') {
    if($Receipt-isnot[Collections.IDictionary]){throw 'Title qualification must be an object'}
    foreach($field in @('phase','expectedFailure','failure','guardFailures','engine','sourceRows','verboseDiagnostic','refCountedIdentityDiagnostic','human','naturalRoute','wholeGoalAccepted')){
        if(!$Receipt.Contains($field)){throw 'Title qualification field missing'}
    }
    if($Receipt.phase-cne$ExpectedPhase -or $Receipt.expectedFailure-cne'' -or $null-ne$Receipt.failure -or
        $Receipt.guardFailures-isnot[Collections.IList] -or $Receipt.guardFailures.Count-ne0 -or
        $Receipt.sourceRows-isnot[int] -and $Receipt.sourceRows-isnot[long] -or $Receipt.sourceRows-le0 -or
        $Receipt.engine-isnot[Collections.IDictionary] -or !$Receipt.engine.Contains('ExitCode') -or !$Receipt.engine.Contains('TimedOut') -or
        $Receipt.engine.ExitCode-ne0 -or $Receipt.engine.TimedOut-isnot[bool] -or $Receipt.engine.TimedOut){throw 'Title qualification is not an ordinary successful keyboard contract'}
    foreach($field in @('verboseDiagnostic','refCountedIdentityDiagnostic','human','naturalRoute','wholeGoalAccepted')){
        if($Receipt[$field]-isnot[bool] -or $Receipt[$field]){throw 'Title qualification cannot claim diagnostics, route or human acceptance'}
    }
}
function Assert-TitlePreviewReceipt($Receipt) {
    Assert-TitleReceipt $Receipt 'title-resource-entry'
    foreach($field in @('titleResourceBoundaryDiagnostic','behaviorAccepted','resourceFixed','liveQualified')){
        if(!$Receipt.Contains($field) -or $Receipt[$field]-isnot[bool]){throw 'Preview diagnostic status is missing or invalid'}
    }
    if(!$Receipt.titleResourceBoundaryDiagnostic -or $Receipt.behaviorAccepted -or $Receipt.resourceFixed -or $Receipt.liveQualified){throw 'Preview cannot claim ordinary live qualification, resource fix or behavior acceptance'}
}
function Assert-TitleNoReparse([string]$Path) {
    if(Test-Path -LiteralPath $Path){if(((Get-Item -LiteralPath $Path -Force).Attributes-band[IO.FileAttributes]::ReparsePoint)-ne0){throw 'Title path is a reparse leaf'}}
    for($ancestor=[IO.DirectoryInfo]::new($Path);$null-ne$ancestor;$ancestor=$ancestor.Parent){
        if($ancestor.Exists -and ($ancestor.Attributes-band[IO.FileAttributes]::ReparsePoint)-ne0){throw 'Title path has a reparse ancestor'}
    }
}
function Import-TitleInventories {
    $donor='G:/knight/历史资料/E-OurGame-Knight/.worktrees/task6-certified-final-20260902T0555/.superpowers/sdd/2026-10-12-minimap/verify-minimap.ps1'
    if((Get-FileHash -LiteralPath $donor).Hash-cne'6143F916E19AEE7ACFB5133C7AD29D1C94F0AA306CD8B6E8C658E3567E172922'){throw 'Title inventory donor identity differs'}
    $tokens=$null;$errors=$null;$ast=[Management.Automation.Language.Parser]::ParseFile($donor,[ref]$tokens,[ref]$errors)
    if($errors.Count){throw 'Title inventory donor cannot parse'}
    foreach($name in @('Get-SourceInventory','Get-BinaryInventory')){
        $found=@($ast.FindAll({param($n)$n-is[Management.Automation.Language.FunctionDefinitionAst] -and $n.Name-ceq$name},$false))
        if($found.Count-ne1){throw 'Title inventory definition ambiguous'}
        $definition=[regex]::Replace($found[0].Extent.Text,'^function\s+'+[regex]::Escape($name)+'\b','function script:'+$name)
        . ([scriptblock]::Create($definition))
    }
}
function Read-TitleText([string]$Path) {
    Assert-TitleNoReparse $Path
    $value=Get-Content -LiteralPath $Path -Raw
    if($null-eq$value){return ''}
    return $value.TrimEnd([char[]]"`r`n")
}
function Get-TitleStartupState([string]$Stdout,[string]$Stderr,[string]$External,[bool]$ProcessExited) {
    $all=$Stdout+"`n"+$Stderr+"`n"+$External
    if($ProcessExited -or $all-match'(?im)^\s*(ERROR:|WARNING:|FATAL:)|Assertion failed:|Unhandled exception|ISOLATED_TITLE_END '){throw 'Title startup failed or ended; process-start is not title-ready'}
    $marker='ISOLATED_TITLE_READY user_visible=false human=false auto_start=false auto_exit=false input_driver=false'
    $count=[regex]::Matches($Stdout,'(?m)^'+[regex]::Escape($marker)+'\r?$').Count
    if($count-gt1){throw 'Title startup READY is duplicated'}
    if($count-eq1){return 'ready'}
    return 'pending'
}
function Wait-TitleStartup($Process,[string]$LiveRoot) {
    $watch=[Diagnostics.Stopwatch]::StartNew()
    try{
        do{
            $Process.Refresh()
            $texts=@()
            foreach($name in @('engine.stdout.log','engine.stderr.log','godot.external.log')){
                $path=Join-Path $LiveRoot $name
                $texts+=if(Test-Path -LiteralPath $path){Read-TitleText $path}else{''}
            }
            $state=Get-TitleStartupState $texts[0] $texts[1] $texts[2] $Process.HasExited
            if($state-ceq'ready'){
                @{state='ready';pid=$Process.Id;elapsedMs=$watch.ElapsedMilliseconds;userVisible=$false;human=$false}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $LiveRoot 'startup.json')
                return
            }
            Start-Sleep -Milliseconds 100
        }while($watch.ElapsedMilliseconds-lt55000)
        throw 'Title startup not confirmed within 55s; no automatic close or retry'
    }catch{
        @{state='unconfirmed';pid=$Process.Id;elapsedMs=$watch.ElapsedMilliseconds;failure=$_.Exception.Message;userVisible=$false;human=$false}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $LiveRoot 'startup.json')
        throw
    }
}
function Assert-TitleFrozenHelpers([string]$Case) {
    $rows=(Read-TitleText (Join-Path $Case 'helpers-after.txt'))-split"`n"
    foreach($name in @('verify-claw-knight-v001.ps1','show-isolated-title-v001.ps1','test-launch-plan.ps1')){
        $expected=@($rows|Where-Object{[IO.Path]::GetFileName(($_-split'\|')[0])-ceq$name})
        if($expected.Count-ne1){throw 'Title frozen helper is not uniquely inventoried'}
        foreach($side in @('before','after')){
            $entity=Join-Path $Case "$side/helpers/$name"
            Assert-TitleNoReparse $entity
            if(!(Test-Path -LiteralPath $entity -PathType Leaf) -or
                (Get-FileHash -LiteralPath $entity).Hash-cne($expected[0]-split'\|')[1]){throw 'Title frozen helper entity missing or changed'}
        }
    }
}
function Get-TitleQualification([string]$Case,[switch]$IsolatedPreview) {
    $casePath=[IO.Path]::GetFullPath($Case)
    $evidence=[IO.Path]::GetFullPath('G:/knight/验证证据/october13-stable-claw').TrimEnd('\')+'\'
    $prefix=if($IsolatedPreview){'title-resource-entry'}else{'keyboard'}
    if(!$casePath.StartsWith($evidence,[StringComparison]::OrdinalIgnoreCase) -or
        [IO.Path]::GetFileName($casePath)-cnotmatch('^'+$prefix+'-[0-9]{8}-[0-9]{6}-[0-9]{3}-[0-9a-f]{32}$')){throw 'Title requires the exact explicitly selected qualification mode'}
    Assert-TitleNoReparse $casePath
    $receipt=(Read-TitleText (Join-Path $casePath 'provenance.json'))|ConvertFrom-Json -AsHashtable
    if($IsolatedPreview){Assert-TitlePreviewReceipt $receipt}else{Assert-TitleReceipt $receipt}
    if([IO.Path]::GetFullPath($receipt.case)-cne$casePath){throw 'Title case identity mismatch'}
    Assert-TitleFrozenHelpers $casePath
    Import-TitleInventories
    $projectRoot=$script:TitleProjectRoot
    Push-Location $projectRoot
    try{
        $source=Get-SourceInventory
        if($LASTEXITCODE-ne0){throw 'Title source enumeration failed'}
        $bins=Get-BinaryInventory
        if(@($source-split"`n").Count-ne$receipt.sourceRows -or @($bins-split"`n").Count-ne6 -or
            (Read-TitleText (Join-Path $casePath 'source-before.txt'))-cne$source -or
            (Read-TitleText (Join-Path $casePath 'source-after.txt'))-cne$source -or
            (Read-TitleText (Join-Path $casePath 'fresh-binaries.txt'))-cne$bins -or
            (Read-TitleText (Join-Path $casePath 'binaries-after.txt'))-cne$bins){throw 'Title qualification does not match current source and six binaries'}
        foreach($row in $bins-split"`n"){
            $parts=$row-split'\|';$sealed=Join-Path $casePath ('binaries/'+$parts[0]);Assert-TitleNoReparse $sealed
            if((Get-FileHash -LiteralPath $sealed).Hash-cne$parts[1]){throw 'Title sealed binary mismatch'}
        }
        $helpers=Read-TitleText (Join-Path $casePath 'helpers-after.txt')
        if((Read-TitleText (Join-Path $casePath 'helpers-before.txt'))-cne$helpers){throw 'Title qualification helper drift'}
        foreach($file in @($script:TitlePwsh,$script:TitleGodot,(Join-Path $projectRoot 'tools/show-isolated-title-v001.ps1'),
            (Join-Path $projectRoot '.superpowers/sdd/2026-10-30-persistent-title/test-launch-plan.ps1'),
            (Join-Path $projectRoot '.superpowers/sdd/2026-10-13-stable-claw/verify-claw-knight-v001.ps1'))){
            $matches=@($helpers-split"`n"|Where-Object{[IO.Path]::GetFullPath(($_-split'\|')[0])-ieq[IO.Path]::GetFullPath($file)})
            if($matches.Count-ne1 -or ($matches[0]-split'\|')[1]-cne(Get-FileHash -LiteralPath $file).Hash){throw 'Title tool/helper identity is stale'}
        }
        $all=''
        $labels=if($IsolatedPreview){@('build','engine')}else{@('build','engine','title-entry')}
        foreach($label in $labels){
            $wait=(Read-TitleText (Join-Path $casePath "$label.wait.json"))|ConvertFrom-Json
            if($wait.ExitCode-ne0 -or $wait.TimedOut){throw 'Title bounded qualification did not finish successfully'}
            foreach($suffix in @('stdout.log','stderr.log')){$all+="`n"+(Read-TitleText (Join-Path $casePath "$label.$suffix"))}
        }
        $all+="`n"+(Read-TitleText (Join-Path $casePath 'godot.external.log'))
        if(!$IsolatedPreview){$all+="`n"+(Read-TitleText (Join-Path $casePath 'title-entry.external.log'))}
        if($all-match'(?im)^\s*(ERROR:|WARNING:|FATAL:)|Unhandled exception|Assertion failed:'){throw 'Title qualification has raw diagnostics'}
        $out=Read-TitleText (Join-Path $casePath 'engine.stdout.log')
        $marker='ISOLATED_TITLE_CONTRACT_OK mounts=2 hold_physics=180 channels=3 menus=true starts=0 closes=2 headless_contract=true user_visible=false human=false whole_goal=false'
        if([regex]::Matches($out,'(?m)^'+[regex]::Escape($marker)+'\r?$').Count-ne1 -or
            (!$IsolatedPreview -and [regex]::Matches($out,'(?m)^STABLE_CLAW_OK phase=keyboard isolated_fixture=true human=false whole_goal=false\r?$').Count-ne1)){throw 'Title/keyboard success receipts absent or duplicated'}
        $entry=if($IsolatedPreview){$out}else{Read-TitleText (Join-Path $casePath 'title-entry.stdout.log')}
        if((!$IsolatedPreview -and (!$receipt.Contains('titleEntry') -or $receipt.titleEntry.ExitCode-ne0 -or $receipt.titleEntry.TimedOut)) -or
            [regex]::Matches($entry,'(?m)^ISOLATED_TITLE_ENTRY_OK standalone=true starts=0 human=false whole_goal=false\r?$').Count-ne1 -or
            [regex]::Matches($entry,'(?m)^'+[regex]::Escape($marker)+'\r?$').Count-ne1 -or
            [regex]::Matches($entry,'(?m)^ISOLATED_TITLE_END result=0 human=false whole_goal=false\r?$').Count-ne1){throw 'Standalone title entry qualification absent or failed'}
        return [pscustomobject]@{Case=$casePath;Source=$source;Binaries=$bins;SourceRows=$receipt.sourceRows;IsolatedPreview=[bool]$IsolatedPreview}
    }finally{Pop-Location}
}
function Invoke-TitlePreparation([switch]$IsolatedPreview) {
$projectRoot=$script:TitleProjectRoot
Assert-TitleNoReparse $projectRoot
Import-TitleInventories
Push-Location $projectRoot
try{
    $source=Get-SourceInventory
    if($LASTEXITCODE-ne0){throw 'Title source enumeration failed'}
    $sourceRows=@($source-split"`n").Count
    $phase=if($IsolatedPreview){'title-resource-entry'}else{'keyboard'}
    $output=@(& $script:TitlePwsh -NoProfile -File (Join-Path $projectRoot '.superpowers/sdd/2026-10-13-stable-claw/verify-claw-knight-v001.ps1') -Phase $phase -SourceRows $sourceRows 2>&1)
    $exit=$LASTEXITCODE
    $output|ForEach-Object{Write-Host $_}
    $retained=@($output|ForEach-Object{"$_"}|Where-Object{$_-cmatch'^CLAW_RETAINED='})
    $finish=if($IsolatedPreview){'CLAW_DIAGNOSTIC_FINISHED phase=title-resource-entry title_lifetime_only=true behaviorAccepted=false resourceFixed=false liveQualified=false wholeGoalAccepted=false'}else{'CLAW_CHECK_FINISHED phase=keyboard namedRed=False wholeGoalAccepted=false'}
    if($exit-ne0 -or $retained.Count-ne1 -or @($output|Where-Object{"$_"-ceq$finish}).Count-ne1){throw 'Title preparation failed; no live process launched'}
    return $retained[0].Substring('CLAW_RETAINED='.Length)
}finally{Pop-Location}
}
function New-TitleWindowArguments([string]$LiveRoot) {
    return @('--path',$script:TitleProjectRoot,'--script','res://tests/RunIsolatedTitleHold.cs','--log-file',(Join-Path $LiveRoot 'godot.external.log'),
        '--display-driver','windows','--rendering-method','gl_compatibility','--rendering-driver','opengl3','--windowed','--resolution','1280x720','--position','40,40','--',('--case-storage-root='+(Join-Path $LiveRoot 'storage')))
}
function Start-TitleWindow($Qualification,[switch]$IsolatedPreview) {
    $projectRoot=$script:TitleProjectRoot
    Push-Location $projectRoot
    try{
    # Only this explicit branch starts a window. There is no foreground call,
    # input driver, capture, watchdog or automatic termination after startup.
    $live=Join-Path 'G:/knight/验证证据/title-hold' ('live-'+(Get-Date -Format 'yyyyMMdd-HHmmss-fff')+'-'+[guid]::NewGuid().ToString('N'))
    Assert-TitleNoReparse $live
    if(Test-Path -LiteralPath $live){throw 'Title live case already exists'}
    $null=New-Item -ItemType Directory -Path $live
    foreach($name in @('storage','tmp')){$null=New-Item -ItemType Directory -Path (Join-Path $live $name)}
    $childEnv=@{DOTNET_ROOT='G:/knight/Tools/dotnet-sdk';DOTNET_ROOT_X64='G:/knight/Tools/dotnet-sdk';DOTNET_MULTILEVEL_LOOKUP='0';
        DOTNET_ROLL_FORWARD='LatestMajor';DOTNET_PROCESSOR_COUNT='2';TEMP=(Join-Path $live 'tmp');TMP=(Join-Path $live 'tmp');KNIGHT_PLAYTEST_SOURCE=$null;KNIGHT_PLAYTEST_GRANT_NONCE=$null}
    foreach($name in @('DOTNET_DbgEnableMiniDump','DOTNET_DbgMiniDumpType','DOTNET_DbgMiniDumpName','DOTNET_DbgMiniDumpLogs','COMPlus_DbgEnableMiniDump','COMPlus_DbgMiniDumpType','COMPlus_DbgMiniDumpName','COMPlus_DbgMiniDumpLogs','DOTNET_EnableCrashReport','COMPlus_EnableCrashReport')){$childEnv[$name]=$null}
    $argv=New-TitleWindowArguments $live
    if($IsolatedPreview){$null=Get-TitleQualification $qualification.Case -IsolatedPreview}else{$null=Get-TitleQualification $qualification.Case}
    $qualification.Source|Set-Content -LiteralPath (Join-Path $live 'source-at-launch.txt')
    $qualification.Binaries|Set-Content -LiteralPath (Join-Path $live 'binaries-at-launch.txt')
    $process=$null;$admitted=$false
    try{
        $process=Start-Process -FilePath $script:TitleGodot -ArgumentList $argv -WorkingDirectory $projectRoot -WindowStyle Normal -Environment $childEnv -RedirectStandardOutput (Join-Path $live 'engine.stdout.log') -RedirectStandardError (Join-Path $live 'engine.stderr.log') -PassThru
        $identity=Get-CimInstance Win32_Process -Filter "ProcessId=$($process.Id)"
        if(!$identity -or $identity.ExecutablePath-ine$script:TitleGodot.Replace('/','\') -or $identity.CommandLine-notlike'*res://tests/RunIsolatedTitleHold.cs*'){throw 'Title owned process identity not confirmed'}
        [ordered]@{pid=$process.Id;created=$identity.CreationDate;executable=$identity.ExecutablePath;commandLine=$identity.CommandLine;qualification=$qualification.Case;isolatedPreview=[bool]$IsolatedPreview;ordinaryKeyboardQualified=(!$IsolatedPreview);fullSuiteQualified=$false;autoExit=$false;inputDriver=$false;foregroundActivation=$false;userVisible=$false;human=$false;wholeGoal=$false}|ConvertTo-Json -Depth 3|Set-Content -LiteralPath (Join-Path $live 'launch.json')
        $admitted=$true
        Write-Host ('TITLE_WINDOW_STARTED pid='+$process.Id+' case='+$live+' user_visible=unconfirmed auto_exit=false')
        Wait-TitleStartup $process $live
        Write-Host ('TITLE_WINDOW_READY pid='+$process.Id+' case='+$live+' user_visible=unconfirmed auto_exit=false')
        return [pscustomobject]@{Pid=$process.Id;Case=$live;Arguments=$argv}
    }finally{
        if($process){if(!$admitted -and !$process.HasExited){$process.Kill();$null=$process.WaitForExit(10000)};$process.Dispose()}
    }
}finally{Pop-Location}
}
function Invoke-TitleLauncher([bool]$Show,[bool]$DisplayConfirmed,[bool]$IsolatedPreview=$false) {
    $mode=Get-TitleLaunchMode $Show $DisplayConfirmed
    $case=if($IsolatedPreview){Invoke-TitlePreparation -IsolatedPreview}else{Invoke-TitlePreparation}
    $qualification=if($IsolatedPreview){Get-TitleQualification $case -IsolatedPreview}else{Get-TitleQualification $case}
    Write-Host ('TITLE_PREPARED case='+$qualification.Case+' rows='+$qualification.SourceRows+' user_visible=false')
    if($mode-eq'prepare'){return [pscustomobject]@{Mode=$mode;Qualification=$qualification;Window=$null}}
    if($IsolatedPreview){$null=Get-TitleQualification $qualification.Case -IsolatedPreview;$window=Start-TitleWindow $qualification -IsolatedPreview}
    else{$null=Get-TitleQualification $qualification.Case;$window=Start-TitleWindow $qualification}
    return [pscustomobject]@{Mode=$mode;Qualification=$qualification;Window=$window}
}
if($FunctionsOnly){return}
$null=Invoke-TitleLauncher ([bool]$Show) ([bool]$DisplayConfirmed) ([bool]$IsolatedPreview)
