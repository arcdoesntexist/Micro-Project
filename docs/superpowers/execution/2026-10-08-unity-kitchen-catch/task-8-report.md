# Task 8 report — WebGL delivery, tuning, and regression

## Outcome

Implemented a validated release WebGL entry point, a release-excluded development browser harness, deterministic scene-test waiting, and evidence-backed setup/verification documentation. Release and development builds remain generated and uncommitted. No user settings or pre-existing dirties are part of the Task 8 commit.

## Test-driven changes

1. Build validation began with four expected failures (`task8-build-validation-red.xml`, 0/4) and passed after `WebGLBuild` was implemented (`task8-build-validation-green.xml`, 4/4).
2. Development harness guards and build separation passed 7/7. Browser use exposed disabled legacy `Input`; the added source regression failed 1 of 7, then passed 7/7 after using `Keyboard.current`.
3. Final EditMode passed 122/122.
4. Two `-nographics` PlayMode attempts crashed in Unity's Null graphics/URP sprite path and produced no XML. With D3D, a real repeated Boot-transition failure showed the fixed 100-yield polling budget expired before a roughly 441 ms scene activation. A bounded five-second realtime wait retained the exact assertions: focused 1/1 and final PlayMode 71/71.

## Build and browser evidence

- Release: `task8-webgl-build.log`, BuildReport success, release `index.html`, Brotli data/framework/Wasm present.
- Development: the first detached build produced artifacts, but a guarded retry overwrote its terminal log; it is not claimed as successful from artifacts alone. Fresh tracked builds then exited 0 (`task8-webgl-dev-build-retry.log`, `task8-webgl-dev-input-fix-build.log`).
- Release HTTP/CDP: all four build resources HTTP 200, correct encoding/MIME, zero console/network errors.
- Release flow: profile persistence, all difficulties to natural Death, focus-loss pause/manual resume, Restart ×10, Main Menu return, 1024×768, 2560×1080, fullscreen.
- Development flow: Easy/Normal/Hard Triumph and forced Death through real model/controller outcomes. Release Wasm contains no harness/metrics symbol.
- Measurements: three Triumph rounds and three death paths were stable around 143.7 updates/s and 6.96 ms sampled average with low per-frame GC. Headless Chrome ignored both requested 30/60 caps, so no 60 FPS or 30-vs-60 acceptance claim is made and no tuning was justified.
- Two development-only CDP events contain only `ERROR: Shader ` from `_JS_Log_Dump`, with no shader identity or compilation detail. All result screenshots render expected materials; the clean release emits no such error. Recorded as unresolved development diagnostic noise.

## Files owned

- `Assets/CPS/Editor/WebGLBuild.cs` and meta
- `Assets/CPS/Tests/EditMode/BuildValidationTests.cs` and meta
- `Assets/CPS/Tests/EditMode/CPS.EditModeTests.asmdef`
- `Assets/CPS/Development/DevelopmentBrowserHarness.cs`, directory meta, and source meta
- `Assets/CPS/Tests/PlayMode/GameSceneFlowTests.cs`
- Documentation outside the Unity repository: `docs/unity-setup-guide.md`, `docs/unity-verification-report.md`, `docs/unity-playtest-checklist.md`, and this report

## Self-review

- Build validation names all required scenes and validates the shared rig's controller, player, keyboard input, spawner, three difficulty configs, and object definitions before invoking `BuildPipeline`.
- Release options reject `Development`; development output is separate.
- The test harness cannot compile into a release player and does not fabricate Results.
- Generated build folders are ignored and remain uncommitted.
- No editor version/module installation, URP guess, difficulty tuning, cloud integration, sensor abstraction, font, or audio was added.
- Remaining acceptance gaps are explicit in `docs/unity-verification-report.md`.

## Fix round 1 — review findings

- Executable validator RED: `task8-fix1-build-validation-red.xml`, 9 total, 5 passed, 4 failed. The failures covered production scene validation, disabled-scene selection, a null player override on an effective temporary Gameplay scene instance, and controller/spawner player mismatch.
- Temporary-scene fixture attempts first exposed unsaved and empty original Editor scene setups. The fixture now restores a saved setup or recreates the original untitled state and deletes only its controlled scene asset. Final focused validator: `task8-fix1-build-validation-green3.xml`, 9/9.
- `WebGLBuild.Build` now opens and validates the effective controller instance in `Gameplay.unity`; it no longer validates only the source prefab. Component validation rejects controller/spawner player identity mismatches. Tests select MainMenu and Normal by path/id rather than array position.
- Sustained profiling RED/GREEN: `task8-fix1-sustain-red.xml` 0/1, then `task8-fix1-sustain-green.xml` 1/1. F10 exists only in development/editor compilation and raises HP once while preserving the real 60-second controller timer and spawner workload. Metrics label sustained, terminal-shortcut, and forced-death modes separately.
- Fix-head full suites: EditMode125/125 at 2026-10-18 02:26:52 +07:00; PlayMode71/71 at 02:27:32 +07:00.
- Fresh release build succeeded at 02:39:06 +07:00 and fresh development build succeeded at 09:52:42 +07:00. The rebuilt release smoke at 09:58:01 +07:00 returned HTTP200 for all assets with zero console/network errors, and Wasm inspection again found no development harness in release.
- Natural browser rounds: Easy60.007s/8,625 frames, Normal60.007s/8,625, Hard60.001s/8,624. All used real timer/spawner work and ended Triumph. F8 at1.808s and F9 at1.809s remain terminal-only checks.
- Demonstration host evidence: HP Victus15-fb3xxx, Ryzen5 8645HS,16.36GB RAM, Radeon Graphics plus RTX4050 Laptop GPU, Windows11 build26200, Chrome155.0.8059.80.
- Headless Chrome still ignored requested60 and ran at about143.73 updates/s. Foreground60FPS, genuine30/60 comparison, and hands-on difficulty feel remain open; no tuning was made.

## Fix round 2 — safe scene fixture

- The focused regression first retained the old `NewScene(Single)` fixture and asserted that active scene handle/count remain unchanged. It failed 0/1 with the handle changing from `-1474` to `-1594` (`task8-fix2-scene-fixture-red.xml`), directly reproducing the user-state risk.
- The corrected fixture creates a GUID-named test-owned path under `Assets/CPS/Tests`, copies the authored Gameplay scene without modifying it, opens only the copy additively, applies the broken player override, saves it, and closes it before invoking `ValidateGameplayScene`. This exercises the validator's disk-load branch.
- Cleanup closes only the temporary scene if still loaded and deletes only the exact GUID-owned asset through `AssetDatabase`, including its meta. Assertions preserve the original active scene handle, loaded-scene count, and dirty state. No `TempBuildValidationScene-*` asset remained after focused or full execution.
- Focused GREEN: `task8-fix2-scene-fixture-green2.xml`, 1/1. Full EditMode: `task8-fix2-final-edit.xml`, 125/125. No production/runtime/PlayMode file changed, so PlayMode and WebGL builds were not rerun.

