# Dual Results Apps Script Design

## Intent

Create a new Google Apps Script endpoint for the Micro-Project spreadsheet `1GLTRyTaMMfnfRoNknXkFWjkIuikdSJeVK7jJvoXXrXU`. The endpoint stores completed Single Player rounds in the `Singleplayer` sheet and completed local Versus matches in the `Multiplayer` sheet without mixing their row contracts.

The current Single Player Unity uploader must continue to work while Multiplayer is developed. Multiplayer stores one player per row so each player's statistics are readable without scanning across a very wide match row. Each match is always written as two adjacent rows: P1 followed by P2.

## Goals

- Preserve the existing Single Player payload and its 29 data columns.
- Route legacy payloads with no `mode`, and explicit `mode: "singleplayer"`, to `Singleplayer`.
- Accept explicit `mode: "multiplayer"` payloads containing exactly two players.
- Write both Multiplayer rows atomically while holding one script lock.
- Make adjacent player rows visually read as one match.
- Reject malformed, duplicate, partial, or ambiguous requests without adding rows.
- Provide setup and contract self-test functions that can be run from Apps Script.

## Non-goals

- No Unity uploader implementation in this change.
- No STM32/ESP32 serial work.
- No dashboard, formulas, charts, ranking aggregation, authentication, or clinical interpretation.
- No migration of records from the previous CPS spreadsheet.
- No deletion or clearing of existing sheet data.

The current Unity build continues using its existing deployment until a later integration step explicitly replaces its endpoint URL. This new script is developed and verified independently first. Its backward-compatible Single Player route makes the later cutover possible without changing the existing Single Player payload at the same time.

## Approaches considered

### Selected: one endpoint with mode routing

Use one web-app deployment. Missing `mode` remains backward-compatible with the existing Single Player uploader. Explicit `mode: "multiplayer"` selects the new contract.

This avoids changing the existing Unity uploader immediately and keeps one deployment URL. Two deployments would duplicate validation and deployment maintenance. Requiring `mode` on every request would break the current Single Player client until Unity changed at the same time.

### Multiplayer row shape

The selected layout writes two rows per match rather than one very wide row. Both rows repeat the match metadata and carry the same `matchNumber` and `matchId`; `playerSlot` identifies `P1` or `P2`. This keeps sorting and filtering safe without merged cells.

## Request contracts

### Single Player

The request may omit `mode` or set `mode` to `singleplayer`. `schemaVersion` remains `1`. All existing required and optional fields retain their current validation and mapping.

Duplicate detection uses `sessionId` within `Singleplayer`.

### Multiplayer

The request has this shape:

```json
{
  "mode": "multiplayer",
  "schemaVersion": 1,
  "matchId": "match-uuid",
  "winnerSlot": "P1",
  "endReason": "PlayerDefeated",
  "endedAtUtc": "2026-10-27T12:00:00.000Z",
  "playedSeconds": 83.25,
  "players": [
    {
      "playerSlot": "P1",
      "playerId": "profile-1",
      "displayName": "Arc",
      "score": 240,
      "remainingHp": 2,
      "foodCaught": 18,
      "foodMissed": 7,
      "trashEaten": 1,
      "maxCombo": 9,
      "highestRank": "A",
      "inputSource": "Keyboard",
      "leftMaxStrength01": 1,
      "leftMeanStrength01": 0.4,
      "leftPressedSeconds": 20,
      "leftMaxForceN": null,
      "leftMeanForceN": null,
      "rightMaxStrength01": 1,
      "rightMeanStrength01": 0.45,
      "rightPressedSeconds": 22,
      "rightMaxForceN": null,
      "rightMeanForceN": null,
      "bothPressedSeconds": 2,
      "leftBalance01": null,
      "hatColorHex": "#F28C28",
      "shoeColorHex": "#F28C28"
    },
    {
      "playerSlot": "P2",
      "playerId": "profile-2",
      "displayName": "Tet",
      "score": 210,
      "remainingHp": 0,
      "foodCaught": 16,
      "foodMissed": 9,
      "trashEaten": 3,
      "maxCombo": 6,
      "highestRank": "B",
      "inputSource": "Keyboard",
      "leftMaxStrength01": 1,
      "leftMeanStrength01": 0.35,
      "leftPressedSeconds": 18,
      "leftMaxForceN": null,
      "leftMeanForceN": null,
      "rightMaxStrength01": 1,
      "rightMeanStrength01": 0.38,
      "rightPressedSeconds": 19,
      "rightMaxForceN": null,
      "rightMeanForceN": null,
      "bothPressedSeconds": 1,
      "leftBalance01": null,
      "hatColorHex": "#3C78D8",
      "shoeColorHex": "#3C78D8"
    }
  ]
}
```

Validation requires exactly two player objects, one `P1` and one `P2`, in either incoming order. The script writes P1 first and P2 second. Profile IDs must be non-empty and distinct. Display names may match. `winnerSlot` is `P1`, `P2`, or `Draw`. A named winner must have positive HP and the loser must have zero HP. A draw requires both players at zero HP. Requests where both players still have positive HP, or where the HP state contradicts `winnerSlot`, are invalid because this endless match ends only on first death or simultaneous death. Colors are optional CSS hex colors in `#RRGGBB` form and remain data only.

## Multiplayer columns

The sheet uses these groups:

1. Match: `receivedAtUtc`, `schemaVersion`, `matchNumber`, `matchId`, `winnerSlot`, `outcome`, `endReason`, `endedAtUtc`, `playedSeconds`.
2. Player identity: `playerSlot`, `playerId`, `displayName`, `hatColorHex`, `shoeColorHex`.
3. Player result: `score`, `remainingHp`, `foodCaught`, `foodMissed`, `trashEaten`, `maxCombo`, `highestRank`.
4. Input: the same source, strength, duration, force, both-pressed, and balance fields used by Single Player.

`outcome` is derived per row as `Win`, `Loss`, or `Draw`; Unity does not send it.

## Sheet behavior

`setupSheets()` creates missing `Singleplayer` and `Multiplayer` tabs. If a tab is blank, it writes the expected header. If row 1 contains a different non-empty schema, setup throws instead of overwriting user data. It freezes row 1, replaces the filter with the exact table width, applies date and numeric formats, and assigns subtle colors to header groups.

Single Player data keeps the existing alternating row treatment. Multiplayer formatting alternates by complete match block:

- Match 1: both P1 and P2 rows use light gray.
- Match 2: both rows use white.
- Match 3: both rows return to light gray.

Each pair receives a top border on P1 and a bottom border on P2. Cells are never merged. `setupSheets()` reapplies pair formatting to existing complete pairs, and `doPost()` formats only the newly appended pair.

## Write and duplicate behavior

`doPost()` parses and validates before taking a lock. After acquiring the script lock it opens the selected sheet, checks the exact header contract, and checks the relevant identifier.

- Single Player duplicate: return `{ "ok": true, "status": "duplicate" }` without writing.
- Multiplayer duplicate: if either existing row has the same `matchId`, return duplicate without writing.
- New Multiplayer result: derive the next `matchNumber` from the highest valid number already present, build both rows in memory, and write them with one `setValues()` call.

The response additionally includes `mode`; a created Multiplayer response may include `matchNumber`. Internal errors return the generic `Request failed`. Contract errors retain an `Invalid payload: ...` message.

## Testing

`runContractSelfTests()` remains non-writing and covers:

- legacy Single Player routing and 29-column mapping;
- explicit Single Player routing;
- valid Multiplayer P1/P2 ordering and two-row mapping;
- derived Win/Loss/Draw outcomes;
- duplicate profile IDs;
- missing or duplicate slots;
- wrong player count;
- invalid winner/HP relationship;
- malformed dates, non-finite numbers, invalid optional force values, and invalid color strings;
- malformed JSON and unsupported mode/schema.

Sheet-bound behavior is verified separately with an empty classroom sheet by running `setupSheets()`, submitting one Single Player fixture and at least three Multiplayer fixtures, then confirming gray/white/gray pair blocks, filters, formats, and duplicate responses.

## Security and operational limits

The deployed classroom endpoint remains unauthenticated if access is set to `Anyone`. Anyone with the URL can submit rows. Validation, locking, and duplicate detection reduce malformed or repeated data but do not provide identity or abuse protection. Deployment must execute as the spreadsheet owner, and a new deployment version is required after code changes.

