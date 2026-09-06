# Minimal csc-compatible driver on top of the Roslyn bundled with PowerShell 7.
# Usage: pwsh -File csc.ps1 @response.rsp   (or plain csc-style args)
$ErrorActionPreference = 'Stop'
$pwshDir = Split-Path -Parent (Get-Process -Id $PID).Path
Add-Type -Path (Join-Path $pwshDir 'Microsoft.CodeAnalysis.dll') | Out-Null
Add-Type -Path (Join-Path $pwshDir 'Microsoft.CodeAnalysis.CSharp.dll') | Out-Null

# Expand @response files
$argv = New-Object System.Collections.Generic.List[string]
foreach ($a in $args) {
    if ($a.StartsWith('@')) {
        $rsp = $a.Substring(1)
        foreach ($line in Get-Content $rsp) {
            $line = $line.Trim()
            if ($line -eq '' -or $line.StartsWith('#')) { continue }
            # tokenize on whitespace outside quotes; keep quoted segments intact
            $sb = New-Object System.Text.StringBuilder
            $inQuote = $false
            foreach ($ch in $line.ToCharArray()) {
                if ($ch -eq '"') {
                    $inQuote = -not $inQuote
                    [void]$sb.Append($ch)
                } elseif ($ch -eq ' ' -and -not $inQuote) {
                    if ($sb.Length -gt 0) { $argv.Add($sb.ToString()); [void]$sb.Clear() }
                } else {
                    [void]$sb.Append($ch)
                }
            }
            if ($sb.Length -gt 0) { $argv.Add($sb.ToString()) }
        }
    } else { $argv.Add($a) }
}

$baseDir = (Get-Location).Path
$parser = [Microsoft.CodeAnalysis.CSharp.CSharpCommandLineParser]::Default
$parseMethod = [Microsoft.CodeAnalysis.CSharp.CSharpCommandLineParser].GetMethods() | Where-Object { $_.Name -eq 'Parse' -and $_.ReturnType.Name -eq 'CSharpCommandLineArguments' } | Select-Object -First 1
$cmd = $parseMethod.Invoke($parser, @([System.Collections.Generic.IEnumerable[string]]([string[]]$argv), [string]$baseDir, $null, $null))

$hadError = $false
foreach ($d in $cmd.Errors) {
    Write-Host $d.ToString()
    if ($d.Severity -eq [Microsoft.CodeAnalysis.DiagnosticSeverity]::Error) { $hadError = $true }
}

# Sources
$trees = New-Object 'System.Collections.Generic.List[Microsoft.CodeAnalysis.SyntaxTree]'
foreach ($src in $cmd.SourceFiles) {
    $text = [System.IO.File]::ReadAllText($src.Path)
    $tree = [Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree]::ParseText($text, $cmd.ParseOptions, $src.Path, [System.Text.Encoding]::UTF8)
    $trees.Add($tree)
}

# References
$refs = New-Object 'System.Collections.Generic.List[Microsoft.CodeAnalysis.MetadataReference]'
foreach ($r in $cmd.MetadataReferences) {
    $p = $r.Reference
    if (-not [System.IO.Path]::IsPathRooted($p)) { $p = Join-Path $baseDir $p }
    if (-not (Test-Path $p)) { Write-Host "error: reference not found: $p"; $hadError = $true; continue }
    $refs.Add([Microsoft.CodeAnalysis.MetadataReference]::CreateFromFile($p, $r.Properties))
}
if ($hadError) { exit 1 }

$opts = $cmd.CompilationOptions
$asmName = $cmd.CompilationName
if (-not $asmName) { $asmName = [System.IO.Path]::GetFileNameWithoutExtension($cmd.OutputFileName) }
$comp = [Microsoft.CodeAnalysis.CSharp.CSharpCompilation]::Create($asmName, $trees, $refs, $opts)

$outPath = Join-Path $cmd.OutputDirectory $cmd.OutputFileName
$emitOpts = $cmd.EmitOptions
$resources = $cmd.ManifestResources

$pe = [System.IO.File]::Create($outPath)
try {
    $result = $comp.Emit($pe, $null, $null, $null, $resources, $emitOpts)
} finally { $pe.Dispose() }

$errors = 0; $warnings = 0
foreach ($d in $result.Diagnostics) {
    if ($d.Severity -eq [Microsoft.CodeAnalysis.DiagnosticSeverity]::Error) { $errors++; Write-Host $d.ToString() }
    elseif ($d.Severity -eq [Microsoft.CodeAnalysis.DiagnosticSeverity]::Warning -and $env:CSC_SHOW_WARNINGS) { $warnings++; Write-Host $d.ToString() }
}
if (-not $result.Success) {
    Remove-Item $outPath -ErrorAction SilentlyContinue
    Write-Host "FAILED: $errors error(s)"
    exit 1
}
Write-Host "OK: $outPath ($([System.IO.FileInfo]::new($outPath).Length) bytes)"
exit 0
