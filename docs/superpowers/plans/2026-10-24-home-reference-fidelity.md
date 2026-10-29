# Home reference fidelity implementation plan

> **For agentic workers:** Use superpowers:executing-plans inline, as requested by the user. Preserve the existing checkout and unrelated edits.

**Goal:** Match the approved Playground composition with illustrated UI components derived from the supplied team sheet.
**Architecture:** Keep MainMenuUI and its profile/settings services. Update the prefab through an idempotent editor authoring pass; keep raster assets separate from live text and runtime behavior.
**Tech Stack:** Unity 6000.3.25f1, uGUI, Input System, C#, transparent PNG sprites.
**Spec:** ../specs/2026-10-24-home-reference-fidelity-design.md (approved by user).

## Global constraints

16:9; source layout 1600x900; FF Spoken Trial; existing kitchen and mascot; no scoring/profile-storage changes; source documents remain under CPS/Unity_Project.

## Review focus

- Empty profiles: Create remains usable and Start disabled.
- Text entry, settings, rules and profile drawer block practice movement.
- Input System-only configuration must not call legacy input APIs.
- Reapplying setup must not duplicate Settings or event listeners.
- Buttons, fields and overlays stay readable at 1280x720 and letterboxed sizes.

### Task 1: Reference assets and layout

- [x] Inspect browser-saved layout if accessible; otherwise record source defaults.
- [x] Generate a transparent UI atlas with distinct plaque, cream panel, button, field, leaf and icon regions; record provenance and inspect alpha and silhouettes.
- [x] Import atlas with explicit sprite rectangles and nine-slice borders where appropriate.

### Task 2: Home assembly and behavior

Files: Assets/CPS/Editor/ApprovedMenuDesignSetup.cs, Assets/CPS/UI/MainMenuUI.cs, Assets/CPS/UI/MenuPracticeUI.cs, MainMenuCanvas.prefab; add a focused editor setup file if it keeps the existing authoring pass readable.

- [x] Reuse serialized MainMenuUI references and rebuild positions from Playground coordinates; decorate with asset sprites, live labels, stars, pencil and gear.
- [x] Move nickname creation into a modal with open, cancel, confirm and validation feedback.
- [x] Leave practice scenery open and hide it behind the profile drawer. Use Input System and block input for all open overlays, text focus and lost application focus.
- [x] Remove redundant Settings instance at its actual source, retaining the bound working control. Inspection found one bound button and its label; replace that label with gear artwork, and assert one root Settings control.
- [x] Run Unity compilation and relevant profile/menu tests, including modal and practice regression checks.

### Task 3: Evidence and handoff

- [x] Capture the browser reference and Unity Home at 16:9 plus profile/rules/create states. Browser screenshot includes its editor chrome; stage composition compared proportionally.
- [x] Inspect together, apply one correction batch, then confirm. Fresh code review completed; implementer inspected final screenshots.
- [x] Update provenance and verification report with exact outcomes and remaining limitations.

## Execution ledger

- Started: user approved implementation. Existing Unity checkout contains prior user and assistant changes; no unrelated changes will be staged or reverted.
- Detailed execution evidence: `verification/home-reference/progress.md`. Live browser layout matched source defaults. Native atlas is 1254×1254 with transparent alpha; slice coordinates account for preview scaling.
