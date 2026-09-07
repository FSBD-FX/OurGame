[CmdletBinding()]
param([string]$ProjectRoot)

$ErrorActionPreference = 'Stop'
if ([string]::IsNullOrWhiteSpace($ProjectRoot)) {
    $ProjectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
}

Describe 'Task 6 capture host compatibility' {
    It 'resolves every direct capture helper from its own or shared script closure' {
        $paths = @(
            (Join-Path $ProjectRoot 'tools\capture-prologue.ps1'),
            (Join-Path $ProjectRoot 'tools\EvidenceChecks.ps1'),
            (Join-Path $ProjectRoot 'tools\Task6AviStreaming.ps1'),
            # Capture dot-sources the bounded import/retry helper before formal materialization.
            # Keep the AST closure aligned with the actual runtime load graph.
            (Join-Path $ProjectRoot 'tools\Task6ImportRetry.ps1')
        )
        $definitions = @{}
        $asts = @()
        foreach ($path in $paths) {
            $tokens = $null
            $errors = $null
            $ast = [System.Management.Automation.Language.Parser]::ParseFile($path,
                [ref]$tokens, [ref]$errors)
            $errors.Count | Should Be 0
            $asts += $ast
            foreach ($function in $ast.FindAll({ param($node)
                        $node -is [System.Management.Automation.Language.FunctionDefinitionAst]
                    }, $true)) {
                $definitions[$function.Name] = $true
            }
        }

        $unresolved = @()
        foreach ($ast in $asts) {
            foreach ($commandAst in $ast.FindAll({ param($node)
                        $node -is [System.Management.Automation.Language.CommandAst]
                    }, $true)) {
                $name = $commandAst.GetCommandName()
                if ([string]::IsNullOrWhiteSpace($name) -or $definitions.ContainsKey($name)) { continue }
                if ($null -eq (Get-Command -Name $name -ErrorAction SilentlyContinue)) {
                    $unresolved += ('{0}:{1}' -f $name, $commandAst.Extent.StartLineNumber)
                }
            }
        }
        $unresolved.Count | Should Be 0
    }

    It 'runs the actual capture runtime fingerprint probe in Windows PowerShell and pwsh' {
        $capture = Join-Path $ProjectRoot 'tools\capture-prologue.ps1'
        $fingerprints = @{}
        foreach ($hostName in @('powershell', 'pwsh')) {
            $hostCommand = Get-Command $hostName -ErrorAction SilentlyContinue
            $hostCommand | Should Not BeNullOrEmpty

            $priorPreference = $ErrorActionPreference
            try {
                $ErrorActionPreference = 'Continue'
                $output = & $hostCommand.Source -NoProfile -ExecutionPolicy Bypass -File $capture `
                    -RuntimeFingerprintProbeOnly 2>&1
                $exitCode = $LASTEXITCODE
            }
            finally {
                $ErrorActionPreference = $priorPreference
            }

            $exitCode | Should Be 0
            $markers = @($output | Where-Object {
                $_.ToString() -match '^TASK6_CAPTURE_RUNTIME_FINGERPRINT_OK '
            })
            $markers.Count | Should Be 1
            $match = [regex]::Match($markers[0].ToString(),
                'candidate_runtime_sha256=(?<hash>[A-Fa-f0-9]{64})')
            $match.Success | Should Be $true
            $fingerprints[$hostName] = $match.Groups['hash'].Value
        }
        $fingerprints['powershell'] | Should Be $fingerprints['pwsh']
    }
}
