# Unity WebGL verification report

## Delivery under test

- Project: `C:\Users\Arc\Unity\CPS-Project`
- Branch base for Task 8: `9f72081bdff5a1990596274e9f87e1b390713264`
- Unity: `6000.3.25f1` on Windows 11 Home Single Language `10.0.26200` build `26200`
- Web module: `C:\Program Files\Unity\Hub\Editor\6000.3.25f1\Editor\Data\PlaybackEngines\WebGLSupport`; the installation contains `UnityEditor.WebGL.Extensions.dll` and Emscripten.
- Demonstration machine: HP Victus 15-fb3xxx; AMD Ryzen 5 8645HS, 12 logical processors; 16,357,158,912 bytes RAM; AMD Radeon Graphics driver `32.0.22042.8003`; NVIDIA GeForce RTX 4050 Laptop GPU driver `32.0.15.9282`.
- Browser evidence: Google Chrome `155.0.8059.80`, local HTTP on `127.0.0.1`, 1920×1080 headless capture unless a different viewport is named.

## Automated tests

Task 8 used test-first changes and retained the executable failures:

| Evidence | Result | Purpose |
|---|---:|---|
| `verification/task8-build-validation-red.xml` | 0/4 passed | Build validation was not implemented yet. |
| `verification/task8-build-validation-green.xml` | 4/4 passed | Initial scene/config/definition validation. |
| `verification/task8-dev-input-red.xml` | 6/7 passed | Reproduced the development harness using the disabled legacy Input API. |
| `verification/task8-dev-input-green.xml` | 7/7 passed | Harness uses `Keyboard.current`; release guard remains enforced. |
| `verification/task8-scene-wait-green.xml` | 1/1 passed | Scene transition regression after replacing the frame-count poll with a five-second realtime condition. |
| `verification/task8-final-edit-after-harness.xml` | 122/122 passed | Final full EditMode suite. |
| `verification/task8-final-play-green.xml` | 71/71 passed | Final full PlayMode suite. |
| `verification/task8-fix1-build-validation-red.xml` | 5/9 passed | Review regressions reproduced for scene-effective and player-identity validation. |
| `verification/task8-fix1-build-validation-green3.xml` | 9/9 passed | Effective scene instance, mismatched player, and order-independent selection checks pass. |
| `verification/task8-fix1-sustain-red.xml` | 0/1 passed | Naturally timed development profiling mode was absent. |
| `verification/task8-fix1-sustain-green.xml` | 1/1 passed | Development-only sustain mode retains the natural controller timer. |
| `verification/task8-fix1-final-edit.xml` | 125/125 passed | Fix-head full EditMode suite at 2026-10-18 02:26:52 +07:00. |
| `verification/task8-fix1-final-play.xml` | 71/71 passed | Fix-head full PlayMode suite at 2026-10-18 02:27:32 +07:00. |

Two attempted PlayMode runs with `-nographics` exited in Unity's `NullGfxDevice`/URP sprite rendering before producing XML. They are diagnostic failures, not test failures. The verified PlayMode command used the normal D3D device. A repeated Boot transition failure then showed the old 100-yield poll asserting at about 441 ms before the asynchronous scene activation completed; the bounded realtime wait preserves the same scene assertion and makes all seven scene waits deterministic.

## Builds

The release command is documented in [unity-setup-guide.md](unity-setup-guide.md). `verification/task8-webgl-build.log` records `Build Successful` and `Build Finished, Result: Success` after 921.902 seconds. The generated release contains:

- `Builds/WebGL/index.html` — 5,594 bytes
- `Builds/WebGL/Build/WebGL.data.br` — 13,148,926 bytes after fix-round rebuild
- `Builds/WebGL/Build/WebGL.framework.js.br` — 78,654 bytes after fix-round rebuild
- `Builds/WebGL/Build/WebGL.wasm.br` — 10,346,217 bytes after fix-round rebuild

The separate development build was rerun with tracked process exit evidence after an earlier detached run's log was overwritten by a guarded retry. `verification/task8-webgl-dev-build-retry.log` and the post-input-fix `verification/task8-webgl-dev-input-fix-build.log` both ended successfully with exit code 0. The development build was never used to replace the release folder.

Fix round 1 rebuilt both outputs after scene-effective validation and sustained profiling were added. `verification/task8-fix1-webgl-release-build.log` ended successfully at 2026-10-18 02:39:06 +07:00, and `verification/task8-fix1-webgl-dev-build.log` ended successfully at 2026-10-18 09:52:42 +07:00. The rebuilt release HTTP smoke test completed at 2026-10-18 09:58:01 +07:00.

Decompressing the release Wasm found neither `DevelopmentBrowserHarness` nor `CPS_DEV_METRICS`; the development Wasm contains the harness. Source tests also require the whole harness to be compilation-guarded and its runtime bootstrap to exist only under `DEVELOPMENT_BUILD`.

## Release-browser evidence

The release loaded through HTTP with Brotli and correct MIME headers. `verification/task8-release-guard.json` records HTTP 200 for loader, framework, data, and Wasm with no console or network error. `verification/task8-release-guard.png` shows the rendered Main Menu.

Actual release-browser checks covered profile creation and reload persistence, Easy/Normal/Hard gameplay to natural HP-depleted Results, focus-loss pause and manual resume, Restart ten times, return to Main Menu, 1024×768 and 2560×1080 layouts, and fullscreen. Evidence is under `verification/task8-browser-*.png` and `verification/task8-browser-*.json`. Wide and 4:3 views retain the intended 16:9 playfield through pillarboxing or letterboxing.

Release gameplay cannot deterministically reach Triumph in unattended testing. A separate development-only harness drives the real session model/controller paths and does not construct a Results value directly. The original F8 images are terminal-path checks only: F8 advances the model timer and is not performance evidence. F9 remains a forced early-death terminal check.

## Browser measurements

`verification/task8-fix1-browser-sustained.json` records three rounds that allowed the real controller timer to elapse naturally while the spawner and scene workload stayed active. The development-only F10 sustain mode raises HP once to prevent an early death; it does not advance the timer. Captures show actual caught, missed, and trash outcomes:

| Difficulty | UTC interval | Model elapsed | Frames | Browser update rate | Avg/max frame | Avg/peak GC per frame |
|---|---|---:|---:|---:|---:|---:|
| Easy | 02:53:43–02:54:48 | 60.007 s | 8,625 | 143.73/s | 6.957/13 ms | 324/4,904 B |
| Normal | 02:54:50–02:55:54 | 60.007 s | 8,625 | 143.73/s | 6.957/10 ms | 315/4,846 B |
| Hard | 02:55:56–02:57:01 | 60.001 s | 8,624 | 143.73/s | 6.957/8 ms | 314/4,926 B |

All three ended in Triumph from the normal timeout path. `task8-fix1-browser-{easy,normal,hard}-sustained.png` shows the corresponding Results. The same run records F8 separately as `mode=terminal-shortcut` at 1.808 s and F9 as `mode=forced-death` at 1.809 s; neither is performance evidence.

The requested 30 and 60 runs both executed at about 143.7 updates/s because headless Chrome ignored Unity's requested target frame rate. These numbers demonstrate stable development-harness execution on this browser, but they do not validate 30-vs-60 logical speed or the user-visible 60 FPS target. A foreground, hands-on demonstration-machine measurement remains required. No gameplay, particle, or difficulty tuning was made without valid comparative evidence.

The sustained development run again emitted exactly two raw events whose only argument was `ERROR: Shader `, both from `_JS_Log_Dump` in `WebGL-Dev.framework.js`; CDP supplied no shader name or compiler detail. The captures for all three 60-second rounds and terminal paths show the kitchen, mascot, falling objects, panels, and text rendered without pink or missing materials. The rebuilt release load had zero console errors. This remains unresolved development-build diagnostic noise rather than being dismissed or used to justify speculative URP changes.

## Open acceptance items

- A human still needs to judge gameplay feel, spawn fairness, animation quality, and foreground 60 FPS on the actual demonstration machine.
- Mouse-only full-round and visible keyboard-focus traversal were not completed.
- The user-supplied licensed font and audio are absent, so Thai glyph/combining-mark and audio timing/mix/mute/autoplay acceptance remain open.
- Physical FSR/load-cell input, calibrated Newton values, MQTT, HiveMQ, Node-RED, Google Sheets, and cloud behavior are outside this Unity-only verification.

# Approved Main Menu design — 24 October 2026

- Applied the approved Design Playground layout to `MainMenuCanvas.prefab`.
- Added an interactive menu practice mascot using A/D or left/right arrows, stepped 10 FPS run sprites, bounded movement, and direction-specific readable IoTE helmet sprites.
- Added the Game Rules overlay with food scores, trash effects, 60-second/3-HP rules, difficulty disclosure, and D/C/B/A/S/SS/SSS streak thresholds.
- Preserved the existing profile create/select/edit/delete flow, global Settings, gameplay navigation, production font, kitchen background, and panel artwork.
- Unity EditMode verification: 1 passed, 0 failed (`verification/approved-menu-final-editmode.xml`).
- Unity PlayMode render verification: 1 passed, 0 failed (`verification/approved-menu-final-playmode.xml`).
- Rendered references: `verification/approved-menu-captures/approved-menu-1920x1080.png` and `approved-rules-1920x1080.png`.

