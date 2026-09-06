# VRMod Roadmap

Unofficial OpenXR rebuild — bugs, improvements, and feature goals.
Current version: **2.19.17**

---

## Known issues / bugs

- [ ] **Y button lobby remap — awaiting confirmation:** 2.19.5 skips elements 15/24 in the input loop and polls Y directly for Load (element 32). Needs in-game verification that pause menu flickering is fully resolved.
- [ ] **GitHub publish — force-push needed:** The amended 2.19.5 commit requires a one-time force-push via the updated `Publish-ToGitHub.ps1` script. Awaiting user run.
- [ ] **Comfort vignette tuning:** `ConfortVignette.cs` (sic — upstream typo) has not been reviewed for the OpenXR rebuild; verify it works with Quest 3 via Virtual Desktop.
- [ ] **Recenter behavior during menus:** Recenter re-parks open menus (added in 2.18.2), but edge cases with nested menus or settings tabs may still exist.

## Polish / quality-of-life

- [ ] **Haptics DLL loading feedback:** 2.19.3 fixed the TypeLoadException hang, but there's no user-facing indication of whether haptics loaded successfully. Consider a log line or config-panel note.
- [ ] **Input glyph accuracy for WMR/Vive:** Glyphs were updated for element 32 (Load) across all three controller arrays, but full glyph coverage for all remapped actions hasn't been audited.
- [ ] **Auto-sprint edge cases:** 2.19.1 fixed the main auto-sprint issue (fresh press edges instead of holding), but behavior during teleporting or climbing hasn't been verified.
- [ ] **Settings panel discoverability:** The mod has many config options in `ModConfig.cs` — could benefit from better in-VR organization or tooltips.

## Features

- [ ] **Survivor-specific motion control profiles:** `MotionControls.cs` and `MotionControlledAbilities.cs` handle per-survivor weapon animations. New survivors from DLC2 (Seekers of the Storm) may need custom profiles.
- [ ] **Snap turn / smooth turn options:** Comfort settings for players who prefer snap turning over smooth rotation.
- [ ] **Seated mode improvements:** Adjustable camera height offset and default recenter position for seated play.
- [ ] **Dominant hand switching:** Allow left-handed players to swap which hand aims and which hand holds menus/items.
- [ ] **Mixed-reality capture (LIV) verification:** The LIV SDK folder is unchanged from DrBibop's original. Verify it works with current LIV versions and the OpenXR stack.
- [ ] **Two-handed weapon aiming:** For survivors with large weapons (e.g. MUL-T, Railgunner), use both controllers to aim.

## Upstream tracking

- [ ] **Monitor DrBibop/RoR2VRMod for updates:** The upstream repo may receive patches for new game updates or DLC content. Periodically check and merge relevant changes.
- [ ] **Unity / OpenXR version bumps:** The game is on Unity 2021.3.33f1 with OpenXR 1.9.1. If Hopoo/Gearbox updates the engine, VR bindings and camera hooks may need adjustment.

## Completed (recent)

- [x] **2.19.17** — Diagnostic build: trace the actual selection (Lobby select: lines) to find why the highlight disappears on the rules panel
- [x] **2.19.15** — Reverted the 2.19.14 canvas-rect filter (it did nothing); shrink the character-select canvas to ~53° so the rules panel fits in view
- [x] **2.19.14** — Character-select off-screen fix (root-caused from diagnostics): build navigation only from controls inside the canvas's own local rect; survivor-grid anchor kept independent
- [x] **2.19.13** — Diagnostic build: log the lobby canvas/camera/control layout to find the real off-screen cause
- [x] **2.19.12** — Character-select off-screen fix v4: bound navigation to the UI camera's view frustum (anchor computed independently so it can't collapse)
- [x] **2.19.11** — Character-select off-screen fix v3 (keep the survivor-grid anchor; bound navigation to the canvas's world-space rect); removed right-stick-click recenter so pinging works
- [x] **2.19.10** — Character-select off-screen fix v2: keep only controls that project inside the visible screen (UI camera)
- [x] **2.19.9** — Fix stick walking the highlight off the right edge of the character select screen; renamed to "Resurrected VRMod"
- [x] **2.19.8** — Confine the left stick to one side of the character select screen
- [x] **2.19.5** — Fix Y button flickering (skip elements in loop, don't clear after)
- [x] **2.19.4** — ProperSave compatibility (Y triggers Load in lobby)
- [x] **2.19.3** — Fix TypeLoadException hang when haptics DLLs are missing
- [x] **2.19.2** — Cross-headset OpenXR support (Rift CV1, Index, Bigscreen Beyond)
- [x] **2.19.1** — Auto Sprint fix (fresh press edges instead of holding)
- [x] **2.19.0** — Settings tabs via grip poll
