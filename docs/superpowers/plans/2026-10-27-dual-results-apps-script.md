# Dual Results Apps Script Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build one Google Apps Script web endpoint that preserves the existing Single Player upload contract and writes each Multiplayer match as an adjacent P1/P2 row pair in a separate sheet.

**Architecture:** `Code.gs` keeps the public `doPost`, `setupSheets`, and `runContractSelfTests` entry points while splitting validation, mapping, duplicate lookup, and formatting into private functions. Multiplayer requests contain two players; the script normalizes slot order, derives per-row outcomes and the next match number, then writes both rows in one `setValues()` operation under one script lock. A Node host harness supplies narrow Apps Script service mocks so contract and sheet behavior can be tested without writing to the live spreadsheet.

**Tech Stack:** Google Apps Script V8 JavaScript, Google Sheets `SpreadsheetApp`, Apps Script `LockService`/`ContentService`, Node.js host verification.

**Spec:** `docs/superpowers/specs/2026-10-27-dual-results-apps-script-design.md`

## Global Constraints

- Target spreadsheet ID: `1GLTRyTaMMfnfRoNknXkFWjkIuikdSJeVK7jJvoXXrXU`.
- Required tabs: `Singleplayer` and `Multiplayer`; never clear, rename, merge, or delete user data.
- Missing `mode` and `mode: "singleplayer"` use the existing schema version 1 Single Player contract.
- `mode: "multiplayer"` requires exactly one P1 and one P2 with distinct profile IDs.
- A Multiplayer match is two adjacent rows written by one `setValues()` call while holding one script lock.
- Pair coloring alternates by `matchNumber`: odd matches light gray, even matches white.
- Apps Script remains dependency-free and compatible with the V8 runtime.
- Unity uploader changes, deployment cutover, firmware, dashboards, and Git initialization are outside this plan.
- The Micro workspace is not a Git repository; do not initialize Git, commit, publish, or push.

## Review Focus

- A manually damaged Multiplayer sheet with an orphan row or mismatched pair must reject new writes instead of assigning a misleading match number; Task 2 tests the integrity guard.
- A duplicate retry whose `players` arrive in reversed order must still be detected by `matchId`; Task 3 tests duplicate lookup before normalization/write.
- Display names beginning with `=`, `+`, `-`, or `@` must be stored as literal text rather than executable sheet formulas; Task 1 tests text sanitization for user-controlled strings.
- Concurrent requests must not derive the same `matchNumber`; Task 3 verifies number derivation occurs after lock acquisition.
- A sheet at its initial row capacity must still accept both rows; Task 3 tests that the implementation extends rows before the two-row range write when required.

---

### Task 1: Payload contracts and deterministic row mapping

**Files:**
- Modify: `google-apps-script/Code.gs`
- Create: `verification/google-apps-script-dual-results/test-contract.cjs`
- Create: `verification/google-apps-script-dual-results/apps-script-mocks.cjs`

**Interfaces:**
- Consumes: the approved Single Player schema and Multiplayer request contract.
- Produces: `modeFromPayload_(payload)`, `validateSingleplayerPayload_(payload)`, `validateMultiplayerPayload_(payload)`, `singleplayerRowFromPayload_(payload, receivedAtUtc)`, `multiplayerRowsFromPayload_(payload, receivedAtUtc, matchNumber)`, `outcomeForSlot_(winnerSlot, playerSlot)`, and literal-text sanitization used by later tasks.

- [ ] **Step 1: Add failing host tests for routing and Single Player compatibility**

Assert that a missing mode and explicit `singleplayer` both map the existing fixture to the same 29 cells, while unknown modes and schema versions fail with `Invalid payload`.

- [ ] **Step 2: Run the focused contract test and observe RED**

Run: `node verification/google-apps-script-dual-results/test-contract.cjs`

Expected: FAIL because mode routing and the renamed Single Player helpers do not exist.

- [ ] **Step 3: Implement the Single Player constants, routing, validation, and mapping helpers**

Keep the existing 29 headers and optional-number semantics. Sanitize user-controlled text before row mapping so leading spreadsheet formula markers are prefixed with an apostrophe without changing ordinary Thai or English names.

- [ ] **Step 4: Add failing Multiplayer validation and mapping cases**

Cover reversed incoming order, exactly two distinct profile IDs, one P1 and one P2, winner/HP consistency, draw consistency, dates, finite required/optional numbers, `#RRGGBB` optional colors, P1-first output, 34 columns per row, shared match metadata, and derived Win/Loss/Draw.

- [ ] **Step 5: Implement Multiplayer validation and two-row mapping**

Define `MULTIPLAYER_HEADERS` in the exact order from the spec. Normalize players by slot without mutating the request. Reject both-positive HP, contradictory winner state, duplicate/missing slots, duplicate IDs, malformed colors, and non-finite values.

- [ ] **Step 6: Expand `runContractSelfTests()` and verify GREEN**

Run: `node verification/google-apps-script-dual-results/test-contract.cjs`

Expected: all contract cases pass, including formula-like display names stored as literal text.

---

### Task 2: Safe sheet setup and pair-block formatting

**Files:**
- Modify: `google-apps-script/Code.gs`
- Modify: `verification/google-apps-script-dual-results/apps-script-mocks.cjs`
- Create: `verification/google-apps-script-dual-results/test-sheets.cjs`

**Interfaces:**
- Consumes: `SINGLEPLAYER_HEADERS`, `MULTIPLAYER_HEADERS`, and normalized Multiplayer rows from Task 1.
- Produces: `setupSheets()`, `setupSheet_(spreadsheet, name, headers, kind)`, `validateMultiplayerPairStructure_(sheet)`, `formatSingleplayerRows_(sheet)`, and `formatMultiplayerPairs_(sheet, firstDataRow, pairCount)`.

- [ ] **Step 1: Add failing setup tests**

Assert that missing tabs are created, blank tabs receive headers, non-empty mismatched headers throw without overwriting cells, row 1 freezes, filters cover the exact width, and date/numeric formats target the correct columns.

- [ ] **Step 2: Run sheet tests and observe RED**

Run: `node verification/google-apps-script-dual-results/test-sheets.cjs`

Expected: FAIL because dual-sheet setup does not exist.

- [ ] **Step 3: Implement non-destructive dual-sheet setup**

Replace `setupResultsSheet()` with `setupSheets()` and private setup helpers. Preserve a temporary `setupResultsSheet()` alias that calls `setupSheets()` so an old operator instruction does not silently fail.

- [ ] **Step 4: Add failing pair-format and integrity tests**

Use three complete matches to assert gray/white/gray fills across both rows of each match, P1 top/P2 bottom borders, no merged cells, and correct reformatting on repeated setup. Add orphan-row, wrong-slot-order, mismatched-number, and mismatched-ID cases that must throw before formatting.

- [ ] **Step 5: Implement pair validation and formatting**

Validate existing rows in two-row blocks before applying colors. Use subtle neutral fills and group-specific header colors; do not rely on built-in alternating colors because it alternates individual rows.

- [ ] **Step 6: Run setup and formatting tests GREEN**

Run: `node verification/google-apps-script-dual-results/test-sheets.cjs`

Expected: all setup, preservation, pair integrity, and gray/white/gray cases pass.

---

### Task 3: Locked routing, duplicate handling, and atomic pair append

**Files:**
- Modify: `google-apps-script/Code.gs`
- Modify: `verification/google-apps-script-dual-results/apps-script-mocks.cjs`
- Create: `verification/google-apps-script-dual-results/test-endpoint.cjs`

**Interfaces:**
- Consumes: Task 1 validation/mapping and Task 2 sheet setup/integrity/formatting.
- Produces: final `doPost(e)`, `findIdentifierRow_(sheet, column, value)`, `nextMatchNumber_(sheet)`, `ensureRowCapacity_(sheet, lastRequiredRow)`, and JSON responses for created, duplicate, and error states.

- [ ] **Step 1: Add failing Single Player endpoint tests**

Assert legacy and explicit modes route to `Singleplayer`, duplicate `sessionId` returns duplicate, valid data appends one row, validation occurs before lock acquisition, and the lock releases on success and failure.

- [ ] **Step 2: Add failing Multiplayer endpoint tests**

Assert lookup uses `matchId`, duplicate requests add no rows, a new request acquires the lock before reading the next match number, reverses incoming players to P1/P2 order, extends capacity when one row remains, performs exactly one two-row `setValues()`, formats the pair, and returns `mode` plus `matchNumber`.

- [ ] **Step 3: Run endpoint tests and observe RED**

Run: `node verification/google-apps-script-dual-results/test-endpoint.cjs`

Expected: FAIL because `doPost()` only targets the old Results sheet.

- [ ] **Step 4: Implement locked mode routing and append behavior**

Parse and validate before locking. After locking, reopen the selected sheet, verify headers, validate existing Multiplayer pair structure, check duplicates, derive the match number, ensure capacity, and append. Keep public errors generic except `Invalid payload: ...` contract errors.

- [ ] **Step 5: Run endpoint tests GREEN**

Run: `node verification/google-apps-script-dual-results/test-endpoint.cjs`

Expected: all routing, duplicate, lock-order, capacity, atomic write, response, and failure-path cases pass.

- [ ] **Step 6: Run the complete host suite**

Run: `node verification/google-apps-script-dual-results/test-contract.cjs; node verification/google-apps-script-dual-results/test-sheets.cjs; node verification/google-apps-script-dual-results/test-endpoint.cjs`

Expected: all three commands exit 0.

---

### Task 4: Operator documentation and classroom-sheet verification

**Files:**
- Modify: `google-apps-script/README.md`
- Verify: `google-apps-script/appsscript.json`
- Create: `verification/google-apps-script-dual-results/RESULTS.md`

**Interfaces:**
- Consumes: final functions and response contract from Tasks 1–3.
- Produces: exact install, setup, self-test, deployment, cutover, rollback, and verification instructions.

- [ ] **Step 1: Update the README for the new spreadsheet and two-tab workflow**

Document spreadsheet ID, tab names, `setupSheets()`, `runContractSelfTests()`, deployment permissions, legacy Single Player behavior, Multiplayer payload example, duplicate responses, endpoint cutover, and the warning that the endpoint is public when deployed to `Anyone`.

- [ ] **Step 2: Run all host tests after documentation changes**

Run: `node verification/google-apps-script-dual-results/test-contract.cjs; node verification/google-apps-script-dual-results/test-sheets.cjs; node verification/google-apps-script-dual-results/test-endpoint.cjs`

Expected: all commands exit 0.

- [ ] **Step 3: Install and verify in the bound Apps Script project**

Copy `Code.gs` and `appsscript.json` into the Apps Script project bound to spreadsheet `1GLTRyTaMMfnfRoNknXkFWjkIuikdSJeVK7jJvoXXrXU`. Run `runContractSelfTests()` and then `setupSheets()`.

Expected: both functions complete without exception; `Singleplayer` and `Multiplayer` retain or receive the exact headers, formats, and filters.

- [ ] **Step 4: Exercise the deployed test contract without Unity cutover**

Submit one legacy Single Player fixture and three Multiplayer fixtures plus one duplicate retry. Confirm one Single Player row, six Multiplayer rows, P1/P2 adjacency, Match 1 gray/Match 2 white/Match 3 gray, and a duplicate response with no additional rows.

- [ ] **Step 5: Record evidence and unresolved external state**

Write `RESULTS.md` with the host-test totals, Apps Script self-test/setup results, sheet observations, deployment URL if created, and whether Unity still points at the previous endpoint. Do not claim Unity upload integration until a later Unity task changes and verifies that URL.

