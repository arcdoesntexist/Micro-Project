# Micro-Project Results Apps Script

This container-bound Google Apps Script receives completed Unity rounds for spreadsheet `1GLTRyTaMMfnfRoNknXkFWjkIuikdSJeVK7jJvoXXrXU`.

It uses two tabs:

- `Singleplayer`: one completed Single Player round per row using the existing 29-column contract.
- `Multiplayer`: two adjacent rows per match, always P1 followed by P2.

The Multiplayer tab repeats `matchNumber` and `matchId` on both rows. Odd matches use a light-gray pair block and even matches use a white pair block. Cells are not merged, so filtering and sorting remain usable.

## Install and verify

1. Open the target spreadsheet and choose Extensions → Apps Script.
2. Replace `Code.gs` with the version in this folder and use `appsscript.json` as the manifest.
3. Run `runContractSelfTests()`. It must finish without an exception and does not write to the spreadsheet.
4. Run `setupSheets()` once and authorize access.
5. Verify that `Singleplayer` and `Multiplayer` have frozen headers, filters, number formats, and subtle header-group colors.

`setupResultsSheet()` remains as an alias for `setupSheets()` for operator compatibility. Setup creates missing tabs and initializes blank tabs. If an existing non-empty header differs from the expected contract, setup stops instead of overwriting data.

## Request routing

Legacy payloads with no `mode`, and payloads with `"mode":"singleplayer"`, write to `Singleplayer`. This allows the current Single Player Unity payload to continue working after a later endpoint cutover.

Multiplayer payloads use `"mode":"multiplayer"` and contain exactly two distinct profiles. The complete example is in the approved design document. The script normalizes incoming player order and always writes P1 before P2. A named winner must have positive HP while the loser has zero HP. A draw requires both players to have zero HP.

Optional player fields `hatColorHex` and `shoeColorHex` accept `#RRGGBB` values. Existing strength and force fields retain the Single Player meaning; unavailable values remain `null` in the request and blank in the sheet.

## Responses

Successful requests return one of:

```json
{"ok":true,"status":"created","mode":"singleplayer"}
```

```json
{"ok":true,"status":"created","mode":"multiplayer","matchNumber":1}
```

```json
{"ok":true,"status":"duplicate","mode":"multiplayer"}
```

Contract errors return an `Invalid payload: ...` message without adding rows. Other failures return `Request failed`.

## Deploy and cut over

Create a Web app deployment that executes as the spreadsheet owner. For the classroom demo, allowing access to `Anyone` makes the endpoint public and unauthenticated; anyone who knows the URL can submit junk data.

After changing `Code.gs`, create a new deployment version or update the existing deployment to use the new version. Test this deployment before changing Unity. The current Unity build continues using its previous endpoint until a separate integration task replaces and verifies the URL.

Deployment URL: `https://script.google.com/macros/s/AKfycbxmUwlzaqkbO04UAfhy2CxkpmMR5fEXioOeCoTGxHrwuX4V6VCA9zC3RJ_YQlq-gq8h/exec`

Deployment version 1 was created on 2026-10-28 with **Execute as: Me** and **Who has access: Anyone**. Unity has not been repointed to this URL yet.
