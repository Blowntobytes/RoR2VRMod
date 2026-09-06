# Offline build tooling (no .NET SDK / NuGet required)

These scripts reproduce the 2.10.0 build. They need only PowerShell 7 (its bundled Roslyn is used
as the C# compiler), Mono.Cecil (from a MonoMod release zip) and the game's `Managed` folder.

1. `gen.ps1`        - runs MonoMod HookGen on `RoR2.dll` to produce `MMHOOK_RoR2.dll` exactly like
                      HookGenPatcher does at runtime (private members included).
2. `publicize.ps1`  - Cecil-based publicizer used to create reference copies of `RoR2.dll` and the
                      XR packages so the mod can compile against private members (the mod skips IL
                      verification at runtime, as before).
3. `ubuild.py`      - gathers sources (skipping Editor/Tests folders) and drives `csc.ps1`.
4. `csc.ps1`        - csc-compatible front end over Roslyn (`Microsoft.CodeAnalysis.CSharp.dll`
                      shipped inside PowerShell 7).

Package sources compiled from the needle-mirror GitHub mirrors:
`com.unity.inputsystem@1.6.3`, `com.unity.xr.management@4.4.0`, `com.unity.xr.core-utils@2.1.1`,
`com.unity.xr.openxr@1.9.1` (matches the native `UnityOpenXR.dll` 1.9.1 in `VREnabler/Plugins`).

Build order: InputSystem -> XR.Management -> XR.CoreUtils -> XR.OpenXR -> VRPatcher -> VRMod -> VRAPI.
Adjust the absolute paths in the `*_refs.txt` files to your machine.

## Version control routine (one build = one commit = one tag)

- `upstream` is DrBibop's repository (read-only reference), `origin` is
  https://github.com/Blowntobytes/RoR2VRMod. Work happens on `main`.
- Before a build: bump the version in `RoR2VRMod/VRMod.cs` (BepInPlugin), `manifest.json`,
  `manifestV2.json`, `INSTALL.txt` and add a `CHANGELOG.md` entry. Build, package, test.
- After the build is packaged: `git add -A && git commit -m "<version>: <what changed>"` and
  `git tag -a v<version> -m "Unofficial build <version>"`. `git log --oneline upstream/main..main`
  is then the complete build history, one line per release.
- Commit identity is `Blowntobytes <147319775+Blowntobytes@users.noreply.github.com>` (repo-local
  `git config`), so no personal e-mail address ever lands in the public history.
- `.gitignore` keeps game assemblies, reference/publicized DLLs, logs, local configs, packaged
  zips and any token or secret out of the repository. Check with `git status` before committing;
  `git ls-files -i -c --exclude-standard` must stay empty (no tracked file may match an ignore rule).
- Deliverables per build: the Thunderstore zip, a web-upload bundle containing exactly
  `git diff --name-only upstream/main main` (so ignored files can never be included), and
  `git bundle create RoR2VRMod-history-<version>.bundle upstream/main..main $(git tag -l 'v2.*')`,
  a small file that restores every commit and tag on top of a fresh clone of DrBibop's repository:
  `git fetch <bundle> main:refs/heads/main 'refs/tags/*:refs/tags/*'`.
