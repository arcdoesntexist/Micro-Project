# Unity USB Controllers Implementation Plan

**Implementation status (2026-10-27):** Tasks 1–5 implemented and verified; see `verification/unity-usb/RESULTS.md` and `progress.md` for executed test names, final build/browser evidence, the existing audio-test failure, and physical/asset-preservation limits. Hardware upload and real-port/device testing remain with the user as planned.

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox syntax for tracking. Preserve the user's native execution preference; no repeated execution-method question.

**Goal:** Use one USB-connected ESP32 as selectable FSR or Load Cell input in Unity Editor/WebGL, retaining Keyboard and adding Spacebar Pause/Resume.

**Architecture:** One combined firmware emits versioned JSON lines. Native and browser adapters feed one validated packet/state path; a controller router serves both Home practice and Gameplay. Availability-aware statistics and the existing UI display only units genuinely present in the selected mode.

**Tech Stack:** Unity 6000.3.25f1, existing Input System/uGUI/Newtonsoft.Json, Arduino-ESP32 3.3.12, installed Rob Tillaart HX711 0.6.5, Web Serial and ES5-compatible .jslib. No MQTT or new dependency install.

**Spec:** docs/superpowers/specs/2026-10-26-unity-usb-controllers-design.md

## Global Constraints

- Actual Unity root: `C:/Users/Arc/Unity/CPS-Project`. In this plan `U/` means that root. Documentation/firmware/evidence root `D/` means `C:/Users/Arc/Downloads/CPS/Unity_Project`.
- Keep existing dirty edits and user-selected checkouts. Stage source edits under `D/verification/unity-usb/staging/` and copy only named changes to Unity with filesystem approval. No broad art/UI regeneration, settings reset, hardware upload, port open, commit, push or publish.
- FSR Left GPIO34: 551→135; Right GPIO35: 4095→156.5. 12-bit, alpha 0.2, 20 ms sampling. Relative % only.
- Load Left DT32/SCK18, 1 kg, -2449.84 counts/g; Right DT33/SCK4, 5 kg, -429.11 counts/g. Estimated N = grams * 0.00980665. Preserve existing bench sketches.
- Press/release defaults per hand: FSR 20/15%; Load Cell 0.50/0.30 N. `>= press` sets pressed, `<= release` clears, intermediate values retain state. Freeze mode/thresholds for each round.
- Keyboard: A/Left Arrow left; D/B/Right Arrow right. Spacebar and Escape toggle Pause/Resume subject to input/modal/sensor guards. Both/neither pressed stop. Constant movement/scoring unchanged.
- 115200 baud, ~20 telemetry frames/s, newline JSON, maximum line 1024 chars, stale at 750 ms. Reject bad data, never silently fall back to keyboard or force=0 for an unavailable sensor.
- Home/Pause connection panel only. No mode change in an active round. Loss pauses without losing HP/time; reconnect/tare does not auto-resume. All .md stay in D/.
- Keep the existing design assets/layout/font. No new art, re-theme, schema changes to Sheets columns or Node-RED/MQTT work.

## Review Focus

1. A live serial heartbeat carrying old HX711 samples must not keep a hand moving or accrue force statistics.
2. Old callbacks/queued frames from a closed port, or a new board boot, must not resurrect stale held/tared state.
3. Browser port permission requires a native user gesture; cancellation, unsupported APIs and closing a pending picker must leave the game usable.
4. Spacebar must not both resume and activate a focused button, fire while typing, or bypass a sensor-recovery/modal guard.
5. Switching profile/scene/restarting must not mix controller modes, force/percentage units, calibration metadata or old readings.

## Shared protocol contract

Canonical fixtures: `D/sensor-protocol/fixtures/`. Copy those exact fixture bytes into `U/Assets/CPS/Tests/Fixtures/Usb/` for Unity tests and assert hashes in verification, rather than maintaining divergent examples.

Protocol version 1 fields (all required):

- `version`: integer 1; `bootId`, `sequence`, `uptimeMs`, `tareId`: uint32. Firmware bootId uses the installed ESP32 random API; sequence and uptime support uint32 rollover. tareId increments for each new `t` command.
- `calibrationVersion`: exact string `cps-2026-10-26-v1`.
- `fsrLeftPercent`, `fsrRightPercent`: finite numbers 0..100.
- `loadLeftN`, `loadRightN`: finite signed number when valid, otherwise null.
- `loadLeftTared`, `loadRightTared`, `loadLeftValid`, `loadRightValid`: JSON booleans, not strings/numbers.
- `loadLeftAgeMs`, `loadRightAgeMs`: uint32 when valid, otherwise null. Per-hand age is time since the most recent completed HX711 sample used in its rolling average.

Example released/untared fixture: version=1, bootId=17, sequence=0, uptimeMs=100, tareId=0, calibrationVersion as above, both FSR=0, both load N/age=null, both tared/valid=false. Valid fixture after tare: same boot, sequence=12, uptimeMs=700, tareId=1, FSR Left=50/Right=0, Load Left=0.980665/Right=-0.01, both tared/valid=true, both age=20. Diagnostics are not telemetry. No NaN/Infinity emission or missing-field defaults.

`t\n` requests tare of BOTH unloaded load cells. Within a received command batch, repeated t/T/line endings cause one tare request. New connection/boot requires the UI to request a new tare before Load Cell readiness; only a newer tareId plus both tared/valid hands completes that request. FSR does not require tare.

## Verification runner contract

Create `D/verification/unity-usb/Invoke-UnityTests.ps1 -Platform EditMode|PlayMode [-Filter <fixture>] [-Label <run>]`: detect a running Editor before launch, use the installed Unity executable and explicit U project path, write XML/logs to this evidence folder, wait for the root Unity process (not Start-Process -Wait descendants), enforce a documented timeout, fail on compiler errors/missing XML/test failures, and print a compact result. Never kill a user Editor. If the Editor is open, stop only that run and request closure when execution reaches it; continue unaffected firmware/host work.

Use a `Test-WebSerial.ps1 -NodePath <bundled node executable>` runner for JS tests. Resolve Node via the app's read-only workspace dependency tool during setup; do not install Node/packages. Firmware host tests use existing g++/PowerShell conventions. Full EditMode/PlayMode runs are mandatory after scoped runs; report the two known pre-existing LocalDemoAudio expectation failures separately instead of changing user volume settings.

---

### Task 1: Shared packet/framing validation

**Files:** Create `D/sensor-protocol/fixtures/*.json`, `D/sensor-protocol/README.md`; `U/Assets/CPS/Input/SensorPacket.cs`, `SensorPacketReader.cs`, `SerialLineBuffer.cs`; `U/Assets/CPS/Tests/EditMode/SensorPacketTests.cs`; create the Unity test runner above.

**Interfaces:** `SensorPacket` is immutable typed telemetry with the exact protocol fields. `SensorPacketReader.TryAccept(string line, double receivedAtSeconds, out string error) -> bool`, `Latest -> SensorPacket`, `ReceivedAtSeconds -> double`, `Reset() -> void`. The reader owns accepted-sequence/boot history; input conversion owns selected-mode readiness. `SerialLineBuffer.Push(string chunk, Action<string> onLine)` and `Reset()` provide newline framing with a 1024-character bound; CRLF is supported, oversized lines are discarded until the next newline.

- [ ] Write `SensorPacketTests`: missing/duplicate fields, wrong JSON types/version/calibration, NaN/Infinity, FSR -1/101, oversized/truncated/multiple/CRLF chunks, null unavailable force, per-hand independence, duplicate/out-of-order sequence, uint32 rollover and retired boot rejection. A duplicate must not update ReceivedAtSeconds; a new boot must clear old sample/press/tare readiness via an explicit reset signal to its consumer.
- [ ] Run `Invoke-UnityTests.ps1 -Platform EditMode -Filter SensorPacketTests -Label packet-red`; expected failures from absent packet behavior, not unrelated compiler/dependency errors. Add the minimal compiling test target first when required.
- [ ] Implement framing/strict Newtonsoft.Json validation/state acceptance. Preserve a bounded recent/retired boot history; reset it only on explicit port-session reset. Reject JSON duplicate keys using JsonLoadSettings rather than silently accepting the last key. Diagnostics do not overwrite the latest packet or its freshness timestamp.
- [ ] Run the same fixture with `-Label packet-green`; expected all packet cases pass, then record results and fixture hashes. No physical serial connection.

### Task 2: Single-board combined firmware

**Files:** Create `D/Sensor_Controller/Sensor_Controller.ino`, `D/Sensor_Controller/README.md`, `D/verification/sensor-controller/` host stub/tests/runner. Consume Task 1 fixtures; do not edit the two bench sketches.

**Interfaces:** `setup()` and `loop()` emit the shared newline protocol at 115200; consume t/T on Serial. Keep offsets, rolling averages, last-sample time and tare progress separate for each HX711. Emit load validity false/null until five-sample tare succeeds AND three post-tare measurement samples form a fresh average. Publish every 50 ms regardless of HX711 readiness.

- [ ] Write host tests for FSR endpoints/midpoint/clamps, one HX711 absent, disconnect during tare, tare recovery, repeated t/T in one batch, signed N, independent sample ages, 3-sample force averaging, millis rollover and bounded serialization. Expect exact field types and byte length <=1024; parse actual emitted frames against the canonical fixtures where state matches.
- [ ] Run `verification/sensor-controller/Test-Controller.ps1`; confirm RED for absent combined behavior.
- [ ] Implement the nonblocking loop with native Arduino APIs and installed HX711 0.6.5: no-reset begin, deliberate power-cycle, ready-before-single-read, per-channel asynchronous five-sample tare with 1000 ms no-sample timeout, three-sample ring averages and the existing FSR filter. Each successful tare clears force averaging history. Store uint32 times and compute ages by unsigned subtraction; determine valid from tared state/complete average/age, not packet heartbeat.
- [ ] Run host tests plus existing FSR/HX711 runners; expected all green. Compile `arduino-cli compile --config-file verification/hx711-calibration/arduino-cli.yaml --fqbn esp32:esp32:esp32 --build-path verification/sensor-controller/build Sensor_Controller`; expected exit 0. No upload/port open.

### Task 3: Persistent serial connection in Editor and WebGL

**Files:** Create `U/Assets/CPS/Input/SerialConnection.cs`, `NativeSerialTransport.cs`, `WebSerialTransport.cs`; `U/Assets/Plugins/WebGL/CpsSerial.jslib`; `U/Assets/CPS/Tests/EditMode/SerialConnectionTests.cs`; `D/verification/unity-usb/web-serial.test.cjs`, `Test-WebSerial.ps1`. Keep native references in a platform-only adapter/assembly if necessary; no System.IO.Ports reference in a WebGL build.

**Interfaces:** `SerialConnection` is the single persistent MonoBehaviour owner, exposing `Packets -> SensorPacketReader`, `Status`, `LastError`, `IsConnected`, `ConnectionGeneration`; native `RefreshPorts() -> string[]`, `ConnectNative(string portName)`, `Disconnect()`, and `RequestTare() -> bool`. Web adapter exposes native DOM connection-target show/hide and receives status/line callbacks into the same owner. Every async callback carries a connection generation; outdated callbacks cannot mutate state. RequestTare clears load readiness and records the required next tareId for that boot, not merely whether a command was written.

- [ ] Verify SerialPort type availability against the installed Unity API/profile in an isolated compile-only check before importing dependencies or changing ProjectSettings. Installed Mono 4.5/4.8 System.dll contains SerialPort; this is evidence to check, not proof that the current Unity compilation profile exposes it. If it does not, report the exact error and seek approval for the narrowest reference/profile/dependency change; finish unaffected work without silently installing binaries or altering global API settings.
- [ ] Write `SerialConnectionTests` for bounded line buffering, one owner across scenes, generation cancellation, old-port callbacks, duplicate connect/close, tare gating/new boot, source sample age and cleanup. Write JS VM tests against the actual .jslib with browser/port substitutes: missing navigator.serial/insecure context, native-click requestPort, picker cancellation, permission/open/read/write errors, split/oversized lines, stream cancellation, disconnect/reconnect, and closing the panel while permission is pending. Pending operations must not attach a hidden connection later.
- [ ] Run the scoped Unity tests and `Test-WebSerial.ps1 -NodePath <resolved bundled Node path>`; confirm RED for missing adapter behavior.
- [ ] Implement native 115200 selected-port reads on a bounded worker loop; marshal data/status through bounded queues, never Unity APIs from the worker, and close/cancel with bounded joins. Implement ES5 .jslib using Promise callbacks and native browser streams, no JS framework/CDN. Use a genuine DOM button's handler for requestPort; expose a rect-aligned accessible transparent/native overlay over the existing visual Connect button. Mount only while the connection panel is visible, with no gameplay-wide key interception.
- [ ] Run scoped tests GREEN, compile native adapter and build a minimal WebGL smoke to prove native SerialPort types are excluded. Record browser unsupported/permission-error mock evidence separately from physical-device tests. Do not open real ports automatically.

### Task 4: Selected input, honest units and guarded pause flow

**Files:** Create `U/Assets/CPS/Input/ControllerMode.cs`, `ControllerSettings.cs`, `ControllerInput.cs`, `PauseHotkeys.cs`; modify `InputSnapshot.cs`, `KeyboardInput.cs`, `ForceStatsCollector.cs`, `Flow/GameSessionController.cs`, `Game/SessionResult.cs`; create `Tests/EditMode/ControllerInputTests.cs`; extend `Tests/EditMode/InputStatsTests.cs`, `Tests/PlayMode/GameSessionFlowTests.cs`, and add `Tests/PlayMode/PauseHotkeyTests.cs`.

**Interfaces:** enum `ControllerMode { Keyboard, FSR, LoadCell }`; `ControllerSettings` is an Inspector-editable ScriptableObject with separate per-hand press/release thresholds and 750 ms stale limit. `ControllerInput : MonoBehaviour, IPlayerInput` consumes SerialConnection and KeyboardInput; `SelectedMode`, `SelectMode(ControllerMode)`, `FreezeForRound()`, `ReleaseRound()`, `CanPlay(out string reason)`, and existing `Read() -> InputSnapshot`. Selection is global PlayerPrefs, not profile state. Snapshot Source is exactly Keyboard/FSR/LoadCell; add optional calibration metadata while preserving existing constructor call sites.

- [ ] Write input tests with literal expected results: FSR 19→20→17→15 gives false/true/true/false; N .49→.50→.40→.30 matches the same sequence; negative N maps to 0, not abs(); invalid/stale/missing selected hand releases both movement and readiness. Live heartbeat with aged load sample stays invalid. Keyboard never gains ForceN; FSR never gains ForceN; LoadCell never gains percentage availability.
- [ ] Write play-flow/hotkey tests: loss during Ready and Playing freezes countdown/HP/time/stats; reconnect/new boot/new tare alone never resumes; Spacebar/Escape respect readiness and existing 3-second resume countdown; mode locked during a round; A/D/B/arrows and both-held behavior remain valid; text inputs/settings/connection modals suppress hotkeys; focused-button submit cannot toggle twice; terminal/Results ignore pause keys; keyboard mode does not depend on serial. New session/profile/restart must use the selected mode consistently without old held/force state.
- [ ] Run focused EditMode/PlayMode cases RED before implementing. Retain GameSessionController.Initialize injection so existing fake-input tests remain meaningful.
- [ ] Implement router, validated Inspector configuration, global selection/frozen round state and explicit HasStrength availability semantics on HandInput (existing constructor calls default to available). Extend collector to skip absent percentage while still collecting valid press duration and force. Freeze inputSource/calibration version into SessionResult; preserve old result copying/deserialization and existing uploader mapping. Keep signed force in diagnostics but gameplay/statistics max(0,N).
- [ ] Integrate Pause/Resume safety into GameSessionController and hotkeys with UI focus/modal guards. Check selected input before advancing countdown/Playing, even while sensor-paused. Route every Resume path through the same readiness gate; do not let a stale snapshot advance one extra game step.
- [ ] Run the scoped tests GREEN, then the entire EditMode and PlayMode suites. Report known LocalDemoAudio volume expectation failures by name without changing approved/user volumes or weakening assertions. No Unity/WebGL hardware latency claim from unit tests.

### Task 5: Home/Pause UI, HUD/results and end-to-end handoff

**Files:** Create `U/Assets/CPS/UI/ControllerPanel.cs`, `Editor/ControllerUISetup.cs`; modify `UI/MainMenuUI.cs`, `MenuPracticeUI.cs`, `GameplayUI.cs`, `ResultPanel.cs`, `ResultReferenceView.cs`, `Editor/WebGLBuild.cs`; narrowly update `Prefabs/MainMenuCanvas.prefab`, `Prefabs/GameplayRig.prefab` and the existing Gameplay UI prefab identified by current scene binding. Create `Resources/ControllerSettings.asset` and `Tests/PlayMode/ControllerSceneTests.cs`, `Tests/EditMode/ControllerAuthoringTests.cs`; update `D/docs/unity-setup-guide.md`, add `D/docs/unity-usb-controller-guide.md` and evidence `D/verification/unity-usb/RESULTS.md`.

**Interfaces:** `ControllerPanel.Show(...)` opens the shared Home/Pause subpanel, restores focus on close, delegates connection/tare to SerialConnection and selection to ControllerInput; in Pause mode selector is locked. MenuPracticeUI consumes the same IPlayerInput conversion, retaining its modal/text-entry/focus guards. Native refresh/port selection appears only on native; Web mounts/unmounts the DOM Connect target from Task 3. One idempotent authoring menu `CPS/Apply USB Controllers` performs only the named additions/bindings.

- [ ] Read Impeccable craft-floor immediately before UI edits. Inspect current prefabs/screenshots first; preserve existing font/art and user scene prop/hat/HUD edits. Do not call broad ReferenceHomeSetup/ProductionArtSetup/CafeEnvironmentSetup as an implementation shortcut.
- [ ] Write UI tests for first-use Keyboard, remembered mode independent of profile, port-open vs telemetry-ready state, tare instruction/ready state, keyboard Start without hardware, selected-sensor Start disabled until valid, locked Pause selection and recovery/resume, Home practice direction parity with Gameplay, per-hand N/A, FSR no N, LoadCell no fabricated percentage/fill, and calibration metadata on completed results. Authoring twice must not duplicate owners/components or add unrelated shared-rig overrides.
- [ ] Run focused UI/authoring tests RED. Inspect existing UI Submit action bindings before adding Spacebar, so gameplay hotkeys do not double-activate focused controls.
- [ ] Implement the small Controller selector and existing-style connection subpanel, including plain-language states and native accessible Web Connect target. Add Controller entry to Pause without shifting or removing Keep Cooking/Main Menu/Settings. Hardware reflash instructions belong in the guide, not an automatic UI action. Maintain selected mode across scenes and lock it within the round.
- [ ] Update HUD/results/fallback result view to label actual source/units and per-hand availability, not hardcoded Simulated Input. Keep the approved result summary/drawer/food-tray layout, Easy trash hiding, score rules and existing Sheets columns. Apply narrow authoring and confirm bindings persist after reload.
- [ ] Run focused then full Unity suites. Compile combined firmware and rerun the two unchanged bench host suites. Run JS tests against the final bridge. Build `WebGLBuild.BuildDevelopment` to its existing output path; require a successful BuildReport, not merely an opened browser tab.
- [ ] Inspect Home connection panel and sensor-paused Gameplay at 1280x720 and 1920x1080 in one batched visual pass, fix findings once and confirm once. Browser smoke-test unsupported API/cancel/error and packet fixtures; check native DOM overlay does not cover the field when hidden. Do not claim a real successful picker/device/latency test without the ESP32.
- [ ] Record exact changed files, test/build results and remaining physical checks. Provide the one combined sketch, full pin map, actual-board selection reminder, 115200 baud/close Serial Monitor instructions, unload/Tare workflow, browser HTTPS/localhost requirements, Inspector threshold location and Spacebar usage. State unperformed hardware upload/physical checks clearly. No push/publish/commit unless requested later.

## Plan self-review

Spec coverage maps to Tasks 1–5: protocol/validity, firmware, platform transports/lifecycle, routing/stats/pause, UI/verification. Every Review Focus item has a named test group above. Controller-mode freeze and HasStrength availability are consumed consistently across routing/statistics/HUD. Connect/Tare recovery is explicitly available in Pause without mode switching. Native SerialPort availability remains an explicit dependency approval gate, not silent scope expansion. Keep implementation native as previously requested; this plan awaits the user's review before product edits.
