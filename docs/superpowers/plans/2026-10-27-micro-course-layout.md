# Micro Course Layout Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Reorganize `C:\Users\Arc\Downloads\Micro` into an independent Microcontroller-course monorepo named Micro-Project Version 0.1.0 without breaking the duplicated Unity game.

**Architecture:** Keep the Unity application isolated under `Unity/`, group the existing Arduino-compatible firmware under `Firmware/ESP32/`, reserve a documented `Firmware/STM32/` boundary, and keep shared documentation/protocol/evidence at the repository root. Rename only project identity and documentation; retain `Assets/CPS`, `CPS.*` namespaces and assemblies so Unity serialized references remain stable.

**Tech Stack:** Unity 6000.3.25f1, PowerShell, Unity Test Framework, Arduino/ESP32 sketches, Markdown

**Spec:** `docs/superpowers/specs/2026-10-27-micro-course-layout-design.md`

## Global Constraints

- Modify only `C:\Users\Arc\Downloads\Micro`; do not modify either CPS source checkout.
- New project identity is `Micro-Project` Version `0.1.0`.
- Keep `Assets/CPS`, namespace `CPS.KitchenCatch`, `CPS.*` assembly names, scene names, protocol version, profile keys and result payload contracts.
- Do not rewrite historical reports or old CPS release descriptions as if they originally belonged to Micro.
- Do not add STM32 behavior, initialize Git, publish, push, or change gameplay.
- Stop rather than overwrite if any move destination already contains a conflicting file.

## Review Focus

- Unity Hub must open `C:\Users\Arc\Downloads\Micro\Unity`, not the monorepo root.
- Moving `docs/` must preserve the approved design file already present at the root.
- Firmware-relative documentation links must follow the new `Firmware/ESP32/` paths.
- Git ignore and attributes must work when Git is initialized at the Micro root, including Unity generated files and LF C# files.
- Unity test authoring may reserialize tracked scenes/prefabs; compare manifests and restore unrelated serialization changes before completion.

---

### Task 1: Establish a safe structural baseline

**Files:**
- Read: `C:\Users\Arc\Downloads\Micro\**`
- Create: verification manifests outside the target tree

**Interfaces:**
- Consumes: approved current Micro tree and the design spec
- Produces: exact pre-change file inventory and hash manifest for later comparison

- [x] **Step 1: Confirm no Unity process has `C:\Users\Arc\Downloads\Micro\Unity` open**

Expected: zero matching Unity processes before moves or settings edits.

- [x] **Step 2: Validate all source and destination paths**

Expected: `Unity/{Assets,Packages,ProjectSettings}` exist; the three ESP32 source directories exist at root; target firmware directories do not yet exist; root `docs/` contains only the approved Micro layout design while `Unity/docs/` contains the inherited documentation.

- [x] **Step 3: Record a SHA-256 manifest for Unity source assets and project settings**

Expected: every file under `Unity/Assets`, `Unity/Packages`, and `Unity/ProjectSettings` is recorded before changes.

### Task 2: Reorganize documentation and firmware

**Files:**
- Move: `Unity/docs/**` → `docs/**`
- Move: `Sensor_Controller/**` → `Firmware/ESP32/Sensor_Controller/**`
- Move: `FSR_Reading/**` → `Firmware/ESP32/FSR_Reading/**`
- Move: `HX711_Reading/**` → `Firmware/ESP32/HX711_Reading/**`
- Create: `Firmware/STM32/README.md`
- Move: `Unity/.gitattributes` → `.gitattributes`
- Create: `.gitignore`
- Preserve: `Unity/.gitignore`
- Remove generated: `.vsconfig`, `Micro.slnx`

**Interfaces:**
- Consumes: the collision-free source paths verified in Task 1
- Produces: the approved course-monorepo directory boundaries

- [x] **Step 1: Move inherited documentation into root `docs/` one child at a time**

Expected: no destination conflict; the approved Micro design remains; `Unity/docs` is empty and then removed.

- [x] **Step 2: Move the three existing firmware directories under `Firmware/ESP32/`**

Expected: each source root path disappears and its target contains the same file count and hashes.

- [x] **Step 3: Create the STM32 boundary document**

The README states that STM32 source and responsibilities are intentionally not implemented yet and must be designed against the shared protocol before code is added.

- [x] **Step 4: Place repository attributes and ignore rules**

Move `.gitattributes` to the root, set `*.cs text eol=lf`, retain `Unity/.gitignore`, and add root rules for IDE output and MCU build artifacts.

- [x] **Step 5: Remove stale generated root IDE files**

Delete only root `.vsconfig` and `Micro.slnx`. Expected: no root solution remains pointing at missing root `.csproj` files.

### Task 3: Apply Micro-Project identity and repair live documentation

**Files:**
- Modify: `VERSION`
- Modify: `Unity/ProjectSettings/ProjectSettings.asset`
- Modify: `README.md`
- Modify: `CHANGELOG.md`
- Modify: `docs/unity-setup-guide.md`
- Modify: `docs/unity-usb-controller-guide.md`
- Modify: `Firmware/ESP32/Sensor_Controller/README.md` only where paths/current-project identity require it

**Interfaces:**
- Consumes: final paths from Task 2
- Produces: coherent visible identity and working current documentation

- [x] **Step 1: Change machine-readable project identity**

Set root `VERSION` and Unity `bundleVersion` to `0.1.0`; set Unity `productName`, `projectName`, `metroPackageName`, and `metroApplicationDescription` to `Micro-Project`; set Android and Standalone application identifiers to `com.DefaultCompany.microproject`.

- [x] **Step 2: Rewrite the root README for the Micro course fork**

Explain that Micro-Project 0.1.0 derives from CPS Project 2.1.0, show the new directory map, use `Unity/Assets/CPS/Scenes/Boot.unity`, and update firmware links to `Firmware/ESP32/...`. Preserve gameplay, hardware values, limitations, and the distinction between FSR percentages and Load Cell newtons.

- [x] **Step 3: Add the Micro 0.1.0 changelog entry**

Record the fork baseline, structural moves, project-identity rename and retained CPS implementation identifiers. Keep all CPS 2.1.0 and earlier entries as historical source history.

- [x] **Step 4: Repair current setup and controller guides**

Replace instructions that tell a current Micro user to open the repository root or old CPS checkout with `C:\Users\Arc\Downloads\Micro\Unity`; update the combined firmware path. Do not rewrite historical execution plans/reports.

- [x] **Step 5: Check live Markdown links**

Run a local relative-link audit over root README, current setup/controller guides, and firmware README. Expected: every referenced local path exists after the move.

### Task 4: Import and verify the reorganized project

**Files:**
- Generate: `Unity/Library/**`, `Unity/Logs/**`, Unity IDE files as applicable
- Create: `verification/micro-layout/*.xml` and `*.log`

**Interfaces:**
- Consumes: the reorganized Unity project and project identity from Tasks 2–3
- Produces: compile and test evidence from the final path

- [x] **Step 1: Import `C:\Users\Arc\Downloads\Micro\Unity` with Unity 6000.3.25f1**

Expected: successful batch import/compile and no missing-script or unresolved-asset errors.

- [x] **Step 2: Run EditMode tests**

Expected baseline: 189 passed and 0 failed.

- [x] **Step 3: Run PlayMode tests**

Expected baseline: 106 passed and only `CPS.KitchenCatch.Tests.LocalDemoAudioTests.InteractionUnlocksSceneMusicWithLoopAndApprovedVolumes` fails with expected 0.30 versus actual 0.99.

- [x] **Step 4: Validate asset/meta and build-scene integrity**

Expected: every Unity asset has its required `.meta`, Boot/MainMenu/Gameplay remain in build settings, and no moved documentation/firmware path is referenced as a Unity asset.

- [x] **Step 5: Compare the final Unity manifest**

Expected changes are limited to `ProjectSettings/ProjectSettings.asset` plus any generated files. Restore unrelated scene/prefab serialization changes caused by test authoring.

- [x] **Step 6: Verify source isolation and final structure**

## Verification Result

- Unity 6000.3.25f1 batch import/compile: exit 0.
- EditMode: 189 passed, 0 failed.
- PlayMode: 106 passed, 1 known failure (`LocalDemoAudioTests.InteractionUnlocksSceneMusicWithLoopAndApprovedVolumes`, expected 0.30 versus actual 0.99).
- Unity source manifest: only `ProjectSettings/ProjectSettings.asset` differs from the pre-change baseline.
- Firmware source: all sketches and support code remain byte-identical; only `Sensor_Controller/README.md` changed as planned.
- Asset metadata: 0 missing; Boot, MainMenu and Gameplay remain enabled in build settings.
- Current-document links: 0 broken across 11 local references.
- Both CPS checkout status snapshots are unchanged.
- Firmware host verification after the move: FSR 8/8, HX711 9/9 and combined controller 10/10 passed.
- WebGL verification helpers now derive the Micro root rather than writing to the CPS checkout; Node syntax 10/10 and Python syntax 2/2 passed.
- Executable path audit: no stale CPS checkout paths or pre-move firmware includes remain.

Expected: both CPS checkout statuses match their pre-change state; Micro has no `.git`; stale root solution files are absent; `VERSION` and Unity `bundleVersion` are `0.1.0`; Unity product identity is `Micro-Project`.
