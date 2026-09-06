$ErrorActionPreference='Stop'
$mm='/root/tools/monomod'
Add-Type -Path "$mm/Mono.Cecil.dll" | Out-Null
Add-Type -Path "$mm/MonoMod.Utils.dll" | Out-Null
Add-Type -Path "$mm/MonoMod.RuntimeDetour.dll" | Out-Null
[System.Reflection.Assembly]::LoadFrom("$mm/MonoMod.exe") | Out-Null
$asm=[System.Reflection.Assembly]::LoadFrom("$mm/MonoMod.RuntimeDetour.HookGen.exe")
$env:MONOMOD_DEPDIRS='/root/refs/game:/root/tools/monomod'
$prog=$asm.GetType('MonoMod.RuntimeDetour.HookGen.Program')
$main=$prog.GetMethod('Main',[System.Reflection.BindingFlags]'Static,Public,NonPublic')
$args1=[string[]]@('--private','/root/refs/game/RoR2.dll','/root/build/mmhook/MMHOOK_RoR2.dll')
$main.Invoke($null,@(,$args1))
