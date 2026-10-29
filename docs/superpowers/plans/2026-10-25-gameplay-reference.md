# Gameplay reference implementation plan

> **For agentic workers:** Use superpowers:executing-plans inline. Preserve the existing codex/kitchen-catch checkout and unrelated edits.

**Goal:** Apply the user-approved Gameplay and Pause Playground to the actual Unity game.

**Architecture:** Reuse GameplayUI and the existing pause/settings/session lifecycle. Add an idempotent editor authoring pass for GameplayCanvas; use optional presentation bindings to retain compatibility with existing test fixtures.

**Tech Stack:** Unity 6000.3.25f1, uGUI, C#, existing Home atlas and FF Spoken Trial.

**Spec:** Approved `playground/gameplay.html`, `gameplay.css`, `gameplay.js`, screenshots under `verification/gameplay-playground/`, and the user's approval to integrate into Unity. Browser-only controls are not game UI.

## Global constraints

- Source composition 1600 × 900; convert to existing Unity canvas coordinates (1920 × 1080) without changing scene framing.
- Profile and three hearts top-left; timer top-center; score/difficulty and Pause top-right; Combo beneath score; left/right strength 0–100 at bottom.
- Reuse existing kitchen, mascot, hat, fonts and imported UI sprites. No new artwork generation or packages.
- Pause: Keep cooking, Settings, Back to Main Menu, and explicit score-discard notice. Existing global settings and score-discard behavior remain authoritative.
- Keep actual Unity rank names and scoring. Playground flavor text is not authoritative. Hide simulated-input footer, not sensor logic.
- Do not modify approved Home, results, profile storage, audio levels, or Sheets transport.

## Review focus

- Reapplying layout cannot duplicate controls, labels, or event listeners.
- Zero HP, zero time, long profile names, six-digit scores and SSS must fit.
- Pause and Settings must keep keyboard focus inside the active overlay and restore play correctly.
- Back to Main Menu must not emit/save a completed result.
- New presentation bindings must tolerate null references in older test fixtures.

### Task 1: Runtime presentation and authoring

**Files:** `Assets/CPS/UI/GameplayUI.cs`, new `Assets/CPS/Editor/ReferenceGameplaySetup.cs`, `Assets/CPS/Prefabs/GameplayCanvas.prefab`, new `Assets/CPS/Tests/EditMode/GameplayReferenceTests.cs`, relevant `Assets/CPS/Tests/PlayMode/GameUITests.cs` cases.

**Interfaces:** Consume existing GameSessionController.Model, LastInput, Pause/Resume/ReturnToMenu and AudioSettingsPanel. Produce `ReferenceGameplaySetup.Apply()` and serialized heart/strength/streak presentation bindings.

- [ ] Add and run failing tests: approved sprite bindings and positions; three heart slots; strength fills; hidden input-source footer; new Pause labels; safe repeated authoring.
- [ ] Add failing runtime tests for heart/strength/streak refresh and compact clock formatting, retaining legacy null-binding fixtures.
- [ ] Implement optional presentation bindings in GameplayUI; update them in Refresh without changing scoring or lifecycle. Preserve validation/countdown/status feedback.
- [ ] Implement targeted prefab authoring using existing sprite names Input, CreamPanel, OrangeButton and CocoaButton. Reuse existing serialized buttons/settings/result references. Use live text and actual game values, not screenshot/sample data.
- [ ] Run authoring twice and focused tests. Expected: no duplicate objects; all new tests pass.

### Task 2: Regression and visual evidence

- [ ] Run full EditMode and PlayMode suites using the installed Unity editor. Report exact outcomes and named failures; preserve user audio settings if existing volume tests still fail.
- [ ] Capture actual Unity gameplay and Pause at 1920 × 1080 and 1280 × 720; inspect hearts, timer, high score, SSS, force bars, settings and return-to-menu flow.
- [ ] Review scoped changes, fix material regressions, and save evidence and a concise report under `verification/gameplay-reference/`.
- [ ] Leave changes uncommitted for user inspection; no merge, publish, or WebGL rebuild in this scope.

## Inspection record

Implementation and verification completed; exact outcomes and remaining pre-existing audio-test failures: `verification/gameplay-reference/RESULTS.md`. Checklist history below describes the original plan; the execution ledger is the completion record.

- Existing branch is `codex/kitchen-catch`, with substantial prior user/assistant changes. No resets or broad staging.
- GameplayUI already binds pause, resume, settings and return-to-menu. GlobalSettingsSetup already authors working settings controls.
- ReferenceHomeSetup imports the reusable sprite atlas. Do not rerun Home authoring as a side effect.
- Prior baseline: EditMode 141/141; PlayMode 95/97, with two documented existing audio-volume expectation mismatches. Re-run rather than assuming the baseline is unchanged.
- No Unity source or prefab has been changed for this task yet.
