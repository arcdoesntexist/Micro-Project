# Versus Hybrid Input Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [x]`) syntax for tracking.

**Goal:** Use keyboard and healthy FSR controls together for each Versus player, with independent Windows USB connections and explicit keyboard continuation after a sensor failure.

**Architecture:** Keep the existing Single Player serial singleton and protocol unchanged. Add a persistent Versus controller hub with two serial channels, then merge their fresh FSR states into the existing VersusKeyboardInput seam. Use the same seam in practice and gameplay.

**Tech Stack:** Unity 6000.3.25f1, C#, existing Windows NativeSerialTransport and Unity UI; no new package or firmware flash.

**Spec:** C:/Users/Arc/Downloads/Micro/docs/superpowers/specs/2026-10-29-versus-hybrid-input-design.md (approved by the user).

## Global Constraints

- Actual target is C:/Users/Arc/Downloads/Micro/Unity. Back up changed files before installation; stage edits in the current writable worktree.
- Do not change original CPS projects, initialize Git, publish, or submit test matches to the live Sheet.
- Windows Editor/player serial only for this delivery. WebGL continues to offer keyboard, with an explicit unsupported dual-USB message.
- P1: A/D or ESP32 FSR. P2: Left Arrow; Right Arrow or STM32 right FSR. Ignore STM32 left until its sensor is replaced.
- Preserve Single Player behavior and existing uploaded payload shape. Never report FSR or keyboard as measured newtons.
- Native execution in this session; use no implementation subagents.

## Review Focus

- Old transport callbacks after reconnect must not refresh or fault a new connection.
- COM-port collisions across players or the Single Player connection must not open a device twice.
- Damaged STM32 left values and malformed packets must never move a player or ready them.
- USB loss must release sensor state, pause once, and allow explicit keyboard continuation without an endless pause loop.
- Focus loss, profile text entry and scene changes must not leave inputs latched or leak background serial workers.

### Task 1: Independent sensor state and configuration

**Files (relative to actual Unity root):**
- Create: Assets/CPS/Input/VersusSensorChannel.cs
- Create: Assets/CPS/Input/VersusControllerSettings.cs and Resources/VersusControllerSettings.asset
- Test: Assets/CPS/Tests/EditMode/VersusSensorChannelTests.cs

**Interfaces:** A channel owns a SerialLineBuffer, its ESP32 SensorPacketReader or strict STM32 raw-line parser, freshness timestamp, enabled-hand masks and hysteresis. `Receive(string chunk, double now)`, `Read(double now)` and `Reset()` are pure state operations; no Unity scene is required. Configuration exposes STM32 released/reference endpoints (4095/100 provisional), per-player enabled hands, 20%/15% thresholds and a maximum .75-second stale timeout. Reuse existing input structs and protocol reader.

- [x] Write tests for valid ESP packets, split raw lines, ignored STM32 left, raw range/format rejection, stale data and invalid endpoint/threshold settings.
- [x] Run the focused EditMode fixture, observing missing behavior fail before implementation.
- [x] Implement channel state and configuration; reuse existing JSON validation and SerialLineBuffer limits.
- [x] Run focused tests and verify hysteresis releases after stale state and invalid lines do not extend freshness.

### Task 2: Two-port lifetime and combined input

**Files:**
- Create: Assets/CPS/Input/VersusControllerHub.cs
- Modify: Assets/CPS/Input/VersusKeyboardInput.cs
- Modify: Assets/CPS/Input/SerialConnection.cs only if a read-only connected-port property is needed for port ownership
- Test: Assets/CPS/Tests/PlayMode/VersusHybridInputTests.cs

**Interfaces:** `VersusControllerHub.EnsureExists()`, `Connect(int player, string port)`, `Disconnect(int player)`, `Read(int player)`, `RefreshPorts()`, status/error accessors, and a consumable stream-loss notification. The hub owns two NativeSerialTransport instances and bounded generation-checked main-thread queues. It survives Setup to Gameplay, releases Single Player ownership before opening its selected port, rejects duplicate ports, and closes Versus transports when leaving Versus. Combine each enabled sensor hand with the configured keyboard hand using OR; preserve actual strength metadata rather than summing keyboard and sensor values.

- [x] Write tests for independent P1/P2 merge, keyboard-only use, mixed Ready, source/force metadata, invalid player IDs and duplicate ports.
- [x] Run focused PlayMode tests and observe the absent combined behavior fail.
- [x] Implement hub and merge through the existing VersusKeyboardInput component so authored practice scenes keep their references.
- [x] Add/verify reconnect-generation and bounded-queue tests; ensure disposal clears input and stale queued callbacks cannot affect a new session.

### Task 3: Setup controls and shared match recovery

**Files:**
- Create: Assets/CPS/UI/VersusControllerPanel.cs
- Modify: Assets/CPS/UI/VersusSetupUI.cs
- Modify: Assets/CPS/UI/VersusPracticePlayer.cs only if required for status/input hooks
- Modify: Assets/CPS/Versus/VersusMatchController.cs
- Modify: Assets/CPS/UI/VersusGameplayUI.cs
- Test: Assets/CPS/Tests/PlayMode/VersusHybridFlowTests.cs

**Interfaces:** Add a compact Controllers button opening a themed overlay with per-player COM selector, Refresh, Connect/Disconnect, status and keyboard/FSR mapping. Gameplay Pause offers controller recovery and explicit keyboard continuation. Keep authored artwork/layout; do not rebuild menus. Start/restart captures the connected controller requirement, stale/lost required streams pause both once, recovery/resume runs the existing three-second countdown. Disabling a failed controller for this match is an explicit action and must not repeatedly pause keyboard continuation.

- [x] Write flow tests for shared pause on loss, stale pressed release, keyboard continuation, reconnect countdown, text entry not triggering Ready, and connection preservation across Setup/Gameplay.
- [x] Run focused tests and observe missing pause/recovery behavior fail.
- [x] Implement Setup overlay and Pause hooks using existing frame/button/font assets; show the Windows-only USB limitation in unsupported platforms.
- [x] Verify ports release when returning to Mode Select or Single Player; run existing Versus setup/match and Single Player serial fixtures.

### Task 4: Verification and handoff

**Files:**
- Update: Micro/docs/unity-usb-controller-guide.md and Micro/README.md
- Create: Micro/verification/versus-hybrid-input/RESULTS.md, logs, test XML and screenshots

- [x] Check whether Unity Editor is open; coordinate saved/closed Editor before batch tests. Do not overwrite unsaved Editor changes.
- [x] Run complete EditMode and PlayMode suites once after focused tests pass. Resolve failures caused by this change.
- [x] Exercise physical STM32 raw receive on its manually selected COM port without another serial terminal holding it; verify the right hand response if the user is present to press it. Report physical limitations honestly.
- [x] Inspect Setup controller overlay and Pause at 720p/1080p. Capture images and document provisional calibration, broken-left masking and the Windows-only dual-USB scope.
- [x] Deliver instructions for COM selection, keyboard/FSR mapping, explicit recovery, and the settings asset location. Do not claim unperformed WebGL, ESP32 physical, force-calibration or live-upload checks.

No commits: the Micro destination is not a Git repository.


Completed 2026-10-29. Evidence and verification limits: Micro/verification/versus-hybrid-input/RESULTS.md.

