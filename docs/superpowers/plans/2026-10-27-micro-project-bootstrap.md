# Micro Project Bootstrap Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Create an independent Unity project at `C:\Users\Arc\Downloads\Micro` from the complete tracked CPS Project Version 2.1.0 snapshot and verify it with Unity 6000.3.25f1.

**Architecture:** Export the immutable `v2.1.0` Git tag rather than copying a working directory so the new project contains tracked source and assets but no Git identity, cache, build output, or unrelated untracked files. Import and test the exported snapshot from its new path, then compare tracked-file hashes to detect unintended Unity rewrites.

**Tech Stack:** Git archive, PowerShell, Unity 6000.3.25f1, Unity Test Framework

**Spec:** User-approved full-project duplication request; source context is documented in `README.md`, `CHANGELOG.md`, and `VERSION`.

## Global Constraints

- Source must be `C:\Users\Arc\Downloads\CPS\Unity_Project` at tagged revision `v2.1.0` (`cec5bc81586888c3b98d24dc1ccd1904ea7d4707`).
- Destination must be exactly `C:\Users\Arc\Downloads\Micro` and empty before extraction.
- Do not copy `.git`, Unity caches, build outputs, IDE files, or untracked working files.
- Do not change gameplay, project identity, or STM32/ESP32 responsibilities during bootstrap.
- Unity verification must use `C:\Program Files\Unity\Hub\Editor\6000.3.25f1\Editor\Unity.exe`.

## Review Focus

- A dirty source checkout must not leak untracked or modified files into the snapshot.
- A non-empty destination must stop the operation rather than overwrite files.
- The destination must not retain the source repository identity or remote.
- Unity import must not silently change tracked source/assets.
- Test results must distinguish newly introduced failures from the documented audio-volume failure.

---

### Task 1: Export the independent Version 2.1.0 snapshot

**Files:**
- Create: `C:\Users\Arc\Downloads\Micro\**` from the tracked `main` tree
- Create: `C:\Users\Arc\Downloads\Micro\docs\superpowers\plans\2026-10-27-micro-project-bootstrap.md`

**Interfaces:**
- Consumes: Git tag `v2.1.0` and root `VERSION` from the CPS repository
- Produces: an independent, non-Git Unity project directory

- [x] **Step 1: Verify source revision and destination preconditions**

Run read-only Git and filesystem checks. Expected: tag `v2.1.0`, `origin/main`, and the release checkout resolve to `cec5bc81586888c3b98d24dc1ccd1904ea7d4707`; tagged `VERSION` is `2.1.0`; and destination contains zero children.

- [x] **Step 2: Export and extract the tracked tree**

Use `git archive --format=zip v2.1.0` and expand it into the destination. Expected: tracked file count matches `git ls-tree -r v2.1.0`, and `.git` is absent.

- [x] **Step 3: Record the pre-import manifest**

Hash every tracked file from `git ls-tree -r --name-only v2.1.0`. Expected: every tracked path exists and has a SHA-256 value.

### Task 2: Import and verify the Unity project

**Files:**
- Generate: `C:\Users\Arc\Downloads\Micro\Library\**`
- Generate: `C:\Users\Arc\Downloads\Micro\Logs\**`
- Create: Unity test result and log files under a bootstrap verification directory

**Interfaces:**
- Consumes: the independent Unity project from Task 1
- Produces: an imported project and verification evidence

- [x] **Step 1: Import with Unity 6000.3.25f1**

Run Unity in batch mode against the destination. Expected: process exits successfully without compile errors.

- [x] **Step 2: Run EditMode tests**

Run the Unity Test Framework EditMode suite. Expected baseline: 189/189 pass.

- [x] **Step 3: Run PlayMode tests**

Run the Unity Test Framework PlayMode suite. Expected baseline: 106/107 pass, with only `LocalDemoAudioTests.InteractionUnlocksSceneMusicWithLoopAndApprovedVolumes` failing because expected volume `0.3` differs from actual `0.99`.

- [x] **Step 4: Compare the post-import tracked-file manifest**

Re-hash every exported tracked path. Expected: no tracked source or asset differs from the pre-import snapshot; generated Unity directories are allowed.

- [x] **Step 5: Confirm project separation**

Expected: destination contains no `.git`, and source checkout status is unchanged from the pre-export state.

## Execution Ruling

This task duplicates and configures an existing snapshot without adding production behavior, so no new failing unit test can precede the copy. Preconditions, manifests, Unity import, and the existing Unity suites are the executable acceptance tests for this operational task.

The local `main` branch is stale at Version 1.0.0 while tag `v2.1.0` and `origin/main` both resolve to `cec5bc8`. The immutable release tag is therefore the export authority; using local `main` would duplicate the wrong version.

## Verification Result

- Exported 7,988 tracked files from tag `v2.1.0`; destination contains no `.git` directory.
- The first Windows archive inherited native CRLF conversion and exposed a line-ending-sensitive EditMode assertion. Re-exporting with `core.autocrlf=false` and `core.eol=lf` preserved the release content and restored the expected result.
- Unity 6000.3.25f1 imported and compiled the project successfully.
- EditMode: 189 passed, 0 failed.
- PlayMode: 106 passed, 1 failed. The only failure is the documented `LocalDemoAudioTests.InteractionUnlocksSceneMusicWithLoopAndApprovedVolumes` mismatch: expected `0.30`, actual `0.99`.
- Unity tests reserialized `GameplayCanvas.prefab` and `MainMenuCanvas.prefab`; both were restored from the release archive. Final manifest comparison: 0 missing and 0 changed across all 7,988 tracked files.
