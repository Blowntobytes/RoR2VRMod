param([string]$asmPath,[string]$typeName,[string]$methodName,[string]$outFile)
Add-Type -Path '/root/tools/monomod/Mono.Cecil.dll' | Out-Null
$r = New-Object Mono.Cecil.DefaultAssemblyResolver; $r.AddSearchDirectory('/root/refs/game'); $r.AddSearchDirectory('/root/build/libs')
$rp = New-Object Mono.Cecil.ReaderParameters; $rp.AssemblyResolver=$r
$a=[Mono.Cecil.AssemblyDefinition]::ReadAssembly($asmPath,$rp)
$t=$a.MainModule.GetTypes() | ? { $_.FullName -eq $typeName }
if(-not $t){ "MISSING TYPE $typeName" | Out-File $outFile; exit }
$sb = New-Object System.Text.StringBuilder
foreach($m in ($t.Methods | ? { $_.Name -eq $methodName })){
  [void]$sb.AppendLine("== $($m.FullName)")
  if(-not $m.HasBody){ [void]$sb.AppendLine("  (no body)"); continue }
  foreach($v in $m.Body.Variables){ [void]$sb.AppendLine("  local V_$($v.Index): $($v.VariableType.FullName)") }
  foreach($i in $m.Body.Instructions){ [void]$sb.AppendLine(("  IL_{0:x4}: {1} {2}" -f $i.Offset, $i.OpCode.Name, $i.Operand)) }
}
$sb.ToString() | Out-File $outFile
