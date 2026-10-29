# Kitchen Catch Local Versus Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add an isolated local two-player Versus flow to the Micro Unity project while preserving the current Single Player game and its data.

**Architecture:** Boot enters a small mode-selection scene. Single Player routes to the unchanged `MainMenu`; Versus routes to a dedicated preparation scene and then a dedicated match scene. Pure Versus setup, spawn-stream, match, and payload models are tested independently; scene controllers bind those models to the existing profile store, mascot art, falling-object definitions, audio settings, and Google Sheets endpoint.

**Tech Stack:** Unity 6000.3.25f1, C#, Unity Input System, uGUI, URP 2D, NUnit EditMode/PlayMode tests.

**Spec:** `docs/superpowers/specs/2026-10-26-local-versus-design.md`

## Global Constraints

- Target project: `C:/Users/Arc/Downloads/Micro/Unity`; do not modify `C:/Users/Arc/Unity/CPS-Project`.
- The Micro workspace is not a Git repository; do not initialize Git, commit, publish, or push.
- Preserve Single Player controls, difficulty, 60-second timing, profile storage, result view, and upload behavior.
- P1 uses A/D and P2 uses Left/Right Arrow during keyboard development; physical-controller routing is a later phase.
- Versus uses two distinct profile IDs, one shared deterministic spawn stream, independent player outcomes, and first-death termination with simultaneous-death draw.
- Missed food resets combo but never removes HP in Versus; caught trash removes one HP.
- The match is endless, uses active-play time for progression, and pauses both sides together.
- Completed Versus matches upload once to the deployed Micro endpoint; abandoned matches upload nothing.
- Reuse current assets and dependencies; add no package for local multiplayer.

## Review Focus

- Simultaneous A/D and Arrow input must not leak between players; Task 2 pins all four keys and focus loss.
- Reversed or duplicate profile choices must never start a match; Task 1 pins missing, unknown, and duplicate IDs.
- Divergent catches must not change later spawn records; Task 3 compares both sides against the same immutable schedule.
- Both players dying during one simulation step must produce Draw regardless of processing order; Task 4 resolves both sides before terminal evaluation.
- Retry/rematch and duplicate finish callbacks must not append or upload twice; Tasks 5 and 6 pin match-ID idempotence.

---

### Task 1: Mode and Versus setup state

**Files:**
- Create: `Unity/Assets/CPS/Game/GameMode.cs`
- Create: `Unity/Assets/CPS/Game/VersusMatchSetup.cs`
- Modify: `Unity/Assets/CPS/Profiles/AppSession.cs`
- Create: `Unity/Assets/CPS/Tests/EditMode/VersusSetupTests.cs`

**Interfaces:**
- Consumes: `ProfileStore.Profiles`, existing nickname rules and stable profile IDs.
- Produces: `AppSession.ConfigureVersus(string p1Id, string p2Id, string p1HatColorHex, string p1ShoeColorHex, string p2HatColorHex, string p2ShoeColorHex)`, `AppSession.VersusSetup`, and `VersusMatchSetup.Copy()`.

- [x] **Step 1: Add failing setup tests** for distinct known IDs, duplicate/unknown/blank IDs, identical display names, `#RRGGBB` colors, and immutable name snapshots.
- [x] **Step 2: Run the focused EditMode fixture and observe RED** because the setup types and API do not exist.
- [x] **Step 3: Implement the smallest setup model and AppSession API** without changing the existing `Select` method or profile JSON schema.
- [x] **Step 4: Run the focused fixture and existing AppSession/ProfileStore fixtures GREEN.**

### Task 2: Independent keyboard channels

**Files:**
- Create: `Unity/Assets/CPS/Input/VersusKeyboardInput.cs`
- Create: `Unity/Assets/CPS/Tests/PlayMode/VersusKeyboardInputTests.cs`

**Interfaces:**
- Consumes: Unity Input System `Keyboard.current` and `IPlayerInput.Read()`.
- Produces: `VersusKeyboardInput.Configure(Key left, Key right, string source)` and independent P1/P2 `InputSnapshot` values.

- [x] **Step 1: Add failing Input Test Fixture cases** for A/D, Left/Right, simultaneous players, opposing keys on one player, missing keyboard, and focus loss.
- [x] **Step 2: Run the focused PlayMode fixture and observe RED.**
- [x] **Step 3: Implement one configurable input component**; do not modify `KeyboardInput`, preserving Single Player aliases.
- [x] **Step 4: Run focused and existing keyboard/controller fixtures GREEN.**

### Task 3: Shared endless spawn stream and progression

**Files:**
- Create: `Unity/Assets/CPS/Versus/VersusSpawnRecord.cs`
- Create: `Unity/Assets/CPS/Versus/VersusChallenge.cs`
- Create: `Unity/Assets/CPS/Versus/VersusSpawnStream.cs`
- Create: `Unity/Assets/CPS/Tests/EditMode/VersusSpawnStreamTests.cs`

**Interfaces:**
- Consumes: seed, active elapsed seconds, current tier, existing three food/two trash definition indices, and normalized horizontal lanes.
- Produces: immutable `VersusSpawnRecord` values with ID, kind, definition index, normalized X, spawn time, and fall speed; both fields consume the same record.

- [x] **Step 1: Add failing deterministic-stream tests** for same-seed equality, different-seed variation, bounded rates, tier caps, safe values after long play, and independence from field outcomes.
- [x] **Step 2: Run focused EditMode tests and observe RED.**
- [x] **Step 3: Implement bounded time-based progression** starting near 1.2-second interval/20% trash and capping near 60% trash, with Inspector-facing values held by `VersusChallenge`.
- [x] **Step 4: Run focused tests GREEN and retain the existing `SpawnPolicy` unchanged.**

### Task 4: Versus match model and results

**Files:**
- Create: `Unity/Assets/CPS/Versus/VersusPlayerModel.cs`
- Create: `Unity/Assets/CPS/Versus/VersusMatchModel.cs`
- Create: `Unity/Assets/CPS/Versus/VersusMatchResult.cs`
- Create: `Unity/Assets/CPS/Tests/EditMode/VersusMatchModelTests.cs`

**Interfaces:**
- Consumes: per-field object outcomes plus shared active-step duration.
- Produces: independent HP/score/combo/item counts, active duration/tier, `P1`/`P2`/`Draw`, and one immutable result snapshot.

- [x] **Step 1: Add failing model tests** for missed food, caught trash, independent scoring, no 60-second end, pause-excluded time, first death, simultaneous death, and duplicate object IDs per field.
- [x] **Step 2: Run focused EditMode tests and observe RED.**
- [x] **Step 3: Implement resolution in two phases** so both player outcomes apply before terminal state is evaluated.
- [x] **Step 4: Run focused tests plus existing game-rules fixtures GREEN.**

### Task 5: Mode Select and Versus preparation scenes

**Files:**
- Create: `Unity/Assets/CPS/UI/ModeSelectionUI.cs`
- Create: `Unity/Assets/CPS/UI/VersusSetupUI.cs`
- Create: `Unity/Assets/CPS/UI/VersusPracticePlayer.cs`
- Create: `Unity/Assets/CPS/Editor/VersusMenuSetup.cs`
- Create: `Unity/Assets/CPS/Scenes/ModeSelect.unity`
- Create: `Unity/Assets/CPS/Scenes/VersusSetup.unity`
- Modify: `Unity/Assets/CPS/Flow/BootRoute.cs`
- Modify: `Unity/Assets/CPS/Editor/WebGLBuild.cs`
- Modify: `Unity/ProjectSettings/EditorBuildSettings.asset`
- Create: `Unity/Assets/CPS/Tests/EditMode/VersusMenuAuthoringTests.cs`
- Create: `Unity/Assets/CPS/Tests/PlayMode/VersusSetupFlowTests.cs`

**Interfaces:**
- Consumes: Task 1 setup API, Task 2 keyboard channels, existing `ProfileStore`, current font/UI sprites, mascot body/helmet sprites, and `SceneNavigator`.
- Produces: Boot → Mode Select → unchanged Single Player Home or Versus Setup; two profile selectors/create actions, color palette controls, live left/right practice mascots, Ready indicators, Back, and Start.

- [x] **Step 1: Add failing authoring and flow tests** for scene routing, two selectors, profile creation targeting its initiating slot, distinct-ID gating, palette persistence, independent practice movement, and both-side ready gating.
- [x] **Step 2: Run focused tests and observe RED.**
- [x] **Step 3: Implement dedicated scenes rather than adding stateful overlays to `MainMenu`**, keeping the existing Home hierarchy and behavior untouched.
- [x] **Step 4: Run authoring/flow tests and the existing Home/AppSession suites GREEN.**
- [ ] **Step 5: Render 1280×720 and 1920×1080 captures** and inspect profile readability, mascot distinction, focus state, and practice bounds.

### Task 6: Versus gameplay scene and coordinator

**Files:**
- Create: `Unity/Assets/CPS/Versus/VersusFieldController.cs`
- Create: `Unity/Assets/CPS/Versus/VersusMatchController.cs`
- Create: `Unity/Assets/CPS/UI/VersusGameplayUI.cs`
- Create: `Unity/Assets/CPS/Presentation/VersusMascotStyle.cs`
- Create: `Unity/Assets/CPS/Editor/VersusGameSetup.cs`
- Create: `Unity/Assets/CPS/Scenes/VersusGameplay.unity`
- Modify: `Unity/Assets/CPS/Editor/WebGLBuild.cs`
- Modify: `Unity/ProjectSettings/EditorBuildSettings.asset`
- Create: `Unity/Assets/CPS/Tests/EditMode/VersusGameAuthoringTests.cs`
- Create: `Unity/Assets/CPS/Tests/PlayMode/VersusMatchFlowTests.cs`

**Interfaces:**
- Consumes: Tasks 1–4, existing mascot/falling-object definitions, global audio settings, kitchen background/decorations, and `SceneNavigator`.
- Produces: two isolated equal-scale fields fed by one shared record stream, shared countdown/pause/tier clock, terminal presentation, and immutable `VersusMatchResult`.

- [ ] **Step 1: Add failing scene/model integration tests** for mirrored records, divergent catches, independent HP/score, shared pause/resume, 60+ seconds, first/simultaneous death, and bounded active objects.
- [ ] **Step 2: Run focused tests and observe RED.**
- [ ] **Step 3: Implement the dedicated coordinator and authored scene** with one camera/canvas, two clipped fields, one music source, and optional Versus clip fallback.
- [ ] **Step 4: Run focused tests and all existing Single Player flow suites GREEN.**
- [ ] **Step 5: Capture both target resolutions** and inspect cross-field rendering, readable HUDs, reachable lanes, and terminal state.

### Task 7: Versus persistence, dual-row upload, and result actions

**Files:**
- Create: `Unity/Assets/CPS/Telemetry/VersusResultUploadPayload.cs`
- Create: `Unity/Assets/CPS/Telemetry/GoogleSheetsVersusUploader.cs`
- Create: `Unity/Assets/CPS/UI/VersusResultPanel.cs`
- Modify: `Unity/Assets/CPS/Profiles/ProfileStore.cs`
- Modify: `Unity/Assets/CPS/Versus/VersusMatchController.cs`
- Modify: `Unity/Assets/CPS/Editor/GameSceneSetup.cs`
- Create: `Unity/Assets/CPS/Tests/EditMode/VersusResultUploadPayloadTests.cs`
- Create: `Unity/Assets/CPS/Tests/PlayMode/VersusResultFlowTests.cs`

**Interfaces:**
- Consumes: `VersusMatchResult`, both setup color hex values, and endpoint `https://script.google.com/macros/s/AKfycbxmUwlzaqkbO04UAfhy2CxkpmMR5fEXioOeCoTGxHrwuX4V6VCA9zC3RJ_YQlq-gq8h/exec`.
- Produces: schema-version-1 `mode: "multiplayer"` JSON, one local match record, one idempotent HTTP submission, Restart with retained setup/new ID, and Main Menu without an extra record.

- [ ] **Step 1: Add failing payload and flow tests** for exact P1/P2 contract, nullable force fields, colors, duplicate finish callbacks, retry/rematch IDs, and abandonment.
- [ ] **Step 2: Run focused tests and observe RED.**
- [ ] **Step 3: Implement separate Versus storage and uploader paths**; do not widen or condition the current Single Player payload/uploader.
- [ ] **Step 4: Run focused tests and the existing result-upload fixtures GREEN.**
- [ ] **Step 5: Submit one disposable match fixture to the deployed endpoint** and verify one adjacent pair appears; keep the verification ID clearly prefixed and record it in verification notes.

### Task 8: Full verification and Micro documentation

**Files:**
- Modify: `README.md`
- Modify: `CHANGELOG.md`
- Modify: `docs/unity-playtest-checklist.md`
- Create: `verification/unity-versus/RESULTS.md`

**Interfaces:**
- Consumes: completed Tasks 1–7 and Unity test/build outputs.
- Produces: reproducible setup/playtest instructions and honest automated/manual acceptance evidence.

- [ ] **Step 1: Run all EditMode and PlayMode suites** and report every failure by name, including inherited audio expectations.
- [ ] **Step 2: Build WebGL from Boot** and verify all five scenes are included and loadable.
- [ ] **Step 3: Playtest Single Player regression and Versus** at 1280×720 and 1920×1080, including simultaneous keys, pause/focus loss, 60+ seconds, draw, rematch, menu abandonment, and upload.
- [ ] **Step 4: Update documentation and verification evidence** without claiming physical STM32/ESP32 multiplayer or measured FPS before those checks exist.

