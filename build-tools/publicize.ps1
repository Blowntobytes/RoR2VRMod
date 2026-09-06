# publicize.ps1 <in.dll> <out.dll> [searchDir...]
# Makes every type/method/field/property/event public so mod code can be compiled
# against private game members (the mod skips IL verification at runtime).
param([string]$inPath, [string]$outPath, [string[]]$searchDirs)
$ErrorActionPreference = 'Stop'
Add-Type -Path '/root/tools/monomod/Mono.Cecil.dll' | Out-Null

$resolver = New-Object Mono.Cecil.DefaultAssemblyResolver
foreach ($d in $searchDirs) { $resolver.AddSearchDirectory($d) }
$rp = New-Object Mono.Cecil.ReaderParameters
$rp.AssemblyResolver = $resolver
$rp.ReadWrite = $false
$asm = [Mono.Cecil.AssemblyDefinition]::ReadAssembly($inPath, $rp)

function Publicize-Type([Mono.Cecil.TypeDefinition]$t) {
    if ($t.IsNested) {
        $t.Attributes = ($t.Attributes -band (-bnot [Mono.Cecil.TypeAttributes]::VisibilityMask)) -bor [Mono.Cecil.TypeAttributes]::NestedPublic
    } else {
        $t.Attributes = ($t.Attributes -band (-bnot [Mono.Cecil.TypeAttributes]::VisibilityMask)) -bor [Mono.Cecil.TypeAttributes]::Public
    }
    foreach ($m in $t.Methods) {
        if ($m.Overrides.Count -gt 0) { continue } # explicit interface impls stay as-is
        $m.Attributes = ($m.Attributes -band (-bnot [Mono.Cecil.MethodAttributes]::MemberAccessMask)) -bor [Mono.Cecil.MethodAttributes]::Public
    }
    $eventNames = @{}
    foreach ($e in $t.Events) { $eventNames[$e.Name] = $true }
    foreach ($f in $t.Fields) {
        if ($eventNames.ContainsKey($f.Name)) { continue } # event backing field would clash with the event
        $f.Attributes = ($f.Attributes -band (-bnot [Mono.Cecil.FieldAttributes]::FieldAccessMask)) -bor [Mono.Cecil.FieldAttributes]::Public
    }
    foreach ($n in $t.NestedTypes) { Publicize-Type $n }
}

$count = 0
foreach ($module in $asm.Modules) {
    foreach ($t in $module.Types) { Publicize-Type $t; $count++ }
}
$asm.Write($outPath)
Write-Host "publicized $count top-level types -> $outPath"
