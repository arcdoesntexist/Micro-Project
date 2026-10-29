const SPREADSHEET_ID = '1GLTRyTaMMfnfRoNknXkFWjkIuikdSJeVK7jJvoXXrXU';
const SINGLEPLAYER_SHEET = 'Singleplayer';
const MULTIPLAYER_SHEET = 'Multiplayer';
const RESULTS_SHEET = SINGLEPLAYER_SHEET;
const HEADERS = [
  'receivedAtUtc', 'schemaVersion', 'sessionId', 'playerId', 'displayName',
  'difficulty', 'endReason', 'endedAtUtc', 'playedSeconds', 'score',
  'remainingHp', 'foodCaught', 'foodMissed', 'trashEaten', 'maxCombo',
  'highestRank', 'inputSource', 'leftMaxStrength01', 'leftMeanStrength01',
  'leftPressedSeconds', 'leftMaxForceN', 'leftMeanForceN',
  'rightMaxStrength01', 'rightMeanStrength01', 'rightPressedSeconds',
  'rightMaxForceN', 'rightMeanForceN', 'bothPressedSeconds', 'leftBalance01'
];
const MULTIPLAYER_HEADERS = [
  'receivedAtUtc', 'schemaVersion', 'matchNumber', 'matchId', 'winnerSlot',
  'outcome', 'endReason', 'endedAtUtc', 'playedSeconds', 'playerSlot',
  'playerId', 'displayName', 'hatColorHex', 'shoeColorHex', 'score',
  'remainingHp', 'foodCaught', 'foodMissed', 'trashEaten', 'maxCombo',
  'highestRank', 'inputSource', 'leftMaxStrength01', 'leftMeanStrength01',
  'leftPressedSeconds', 'leftMaxForceN', 'leftMeanForceN',
  'rightMaxStrength01', 'rightMeanStrength01', 'rightPressedSeconds',
  'rightMaxForceN', 'rightMeanForceN', 'bothPressedSeconds', 'leftBalance01'
];

function modeFromPayload_(payload) {
  const mode = payload && payload.mode;
  if (typeof mode === 'undefined' || mode === null || mode === '' || mode === 'singleplayer')
    return 'singleplayer';
  if (mode === 'multiplayer') return 'multiplayer';
  throw new Error('Invalid payload: unsupported mode');
}

function validateSingleplayerPayload_(payload) {
  if (modeFromPayload_(payload) !== 'singleplayer')
    throw new Error('Invalid payload: mode is not singleplayer');
  return validatePayload_(payload);
}

function singleplayerRowFromPayload_(payload, receivedAtUtc) {
  validateSingleplayerPayload_(payload);
  const row = rowFromPayload_(payload, receivedAtUtc);
  [2, 3, 4, 5, 6, 7, 15, 16].forEach(function (index) {
    row[index] = sheetText_(row[index]);
  });
  return row;
}

function sheetText_(value) {
  if (typeof value !== 'string') return value;
  return /^[=+\-@]/.test(value) ? "'" + value : value;
}

function validateMultiplayerPayload_(payload) {
  if (!payload || modeFromPayload_(payload) !== 'multiplayer')
    throw new Error('Invalid payload: mode is not multiplayer');
  if (payload.schemaVersion !== 1)
    throw new Error('Invalid payload: unsupported schemaVersion');
  ['matchId', 'winnerSlot', 'endReason', 'endedAtUtc'].forEach(function (field) {
    if (typeof payload[field] !== 'string' || !payload[field].trim())
      throw new Error('Invalid payload: missing ' + field);
  });
  if (['P1', 'P2', 'Draw'].indexOf(payload.winnerSlot) < 0)
    throw new Error('Invalid payload: winnerSlot');
  requireFiniteNumber_(payload, 'playedSeconds', false);
  if (isNaN(new Date(payload.endedAtUtc).getTime()))
    throw new Error('Invalid payload: endedAtUtc');
  if (!Array.isArray(payload.players) || payload.players.length !== 2)
    throw new Error('Invalid payload: exactly two players required');

  const playersBySlot = {};
  payload.players.forEach(function (player) {
    if (!player || ['P1', 'P2'].indexOf(player.playerSlot) < 0)
      throw new Error('Invalid payload: one P1 and one P2 required');
    if (playersBySlot[player.playerSlot])
      throw new Error('Invalid payload: one P1 and one P2 required');
    ['playerId', 'displayName', 'highestRank', 'inputSource'].forEach(function (field) {
      if (typeof player[field] !== 'string' || !player[field].trim())
        throw new Error('Invalid payload: missing ' + field);
    });
    ['score', 'remainingHp', 'foodCaught', 'foodMissed', 'trashEaten', 'maxCombo']
      .forEach(function (field) { requireFiniteNumber_(player, field, false); });
    ['leftMaxStrength01', 'leftMeanStrength01', 'leftPressedSeconds', 'leftMaxForceN',
      'leftMeanForceN', 'rightMaxStrength01', 'rightMeanStrength01', 'rightPressedSeconds',
      'rightMaxForceN', 'rightMeanForceN', 'bothPressedSeconds', 'leftBalance01']
      .forEach(function (field) { requireFiniteNumber_(player, field, true); });
    ['hatColorHex', 'shoeColorHex'].forEach(function (field) {
      const value = player[field];
      if (value !== null && typeof value !== 'undefined' && value !== '' &&
          (typeof value !== 'string' || !/^#[0-9a-fA-F]{6}$/.test(value)))
        throw new Error('Invalid payload: ' + field);
    });
    playersBySlot[player.playerSlot] = player;
  });

  if (!playersBySlot.P1 || !playersBySlot.P2)
    throw new Error('Invalid payload: one P1 and one P2 required');
  if (playersBySlot.P1.playerId === playersBySlot.P2.playerId)
    throw new Error('Invalid payload: distinct playerId required');

  if (payload.winnerSlot === 'Draw') {
    if (playersBySlot.P1.remainingHp !== 0 || playersBySlot.P2.remainingHp !== 0)
      throw new Error('Invalid payload: draw remainingHp');
  } else {
    const winner = playersBySlot[payload.winnerSlot];
    const loser = playersBySlot[payload.winnerSlot === 'P1' ? 'P2' : 'P1'];
    if (winner.remainingHp <= 0)
      throw new Error('Invalid payload: winner remainingHp');
    if (loser.remainingHp !== 0)
      throw new Error('Invalid payload: loser remainingHp');
  }
  return true;
}

function outcomeForSlot_(winnerSlot, playerSlot) {
  if (winnerSlot === 'Draw') return 'Draw';
  return winnerSlot === playerSlot ? 'Win' : 'Loss';
}

function multiplayerRowsFromPayload_(payload, receivedAtUtc, matchNumber) {
  validateMultiplayerPayload_(payload);
  if (!Number.isInteger(matchNumber) || matchNumber < 1)
    throw new Error('Invalid payload: matchNumber');
  const playersBySlot = {};
  payload.players.forEach(function (player) { playersBySlot[player.playerSlot] = player; });
  return ['P1', 'P2'].map(function (slot) {
    const player = playersBySlot[slot];
    return [
      new Date(receivedAtUtc), payload.schemaVersion, matchNumber, sheetText_(payload.matchId),
      payload.winnerSlot, outcomeForSlot_(payload.winnerSlot, slot), sheetText_(payload.endReason),
      new Date(payload.endedAtUtc), payload.playedSeconds, slot, sheetText_(player.playerId),
      sheetText_(player.displayName), player.hatColorHex || '', player.shoeColorHex || '',
      player.score, player.remainingHp, player.foodCaught, player.foodMissed,
      player.trashEaten, player.maxCombo, sheetText_(player.highestRank),
      sheetText_(player.inputSource), optionalNumber_(player.leftMaxStrength01),
      optionalNumber_(player.leftMeanStrength01), optionalNumber_(player.leftPressedSeconds),
      optionalNumber_(player.leftMaxForceN), optionalNumber_(player.leftMeanForceN),
      optionalNumber_(player.rightMaxStrength01), optionalNumber_(player.rightMeanStrength01),
      optionalNumber_(player.rightPressedSeconds), optionalNumber_(player.rightMaxForceN),
      optionalNumber_(player.rightMeanForceN), optionalNumber_(player.bothPressedSeconds),
      optionalNumber_(player.leftBalance01)
    ];
  });
}

function setupSheets() {
  const spreadsheet = SpreadsheetApp.openById(SPREADSHEET_ID);
  setupSheet_(spreadsheet, SINGLEPLAYER_SHEET, HEADERS, 'singleplayer');
  setupSheet_(spreadsheet, MULTIPLAYER_SHEET, MULTIPLAYER_HEADERS, 'multiplayer');
}

function setupResultsSheet() {
  return setupSheets();
}

function setupSheet_(spreadsheet, name, headers, kind) {
  const sheet = spreadsheet.getSheetByName(name) || spreadsheet.insertSheet(name);
  ensureColumnCapacity_(sheet, headers.length);
  if (sheet.getLastRow() === 0) {
    sheet.getRange(1, 1, 1, headers.length).setValues([headers]);
  } else if (!headersMatchFor_(sheet, headers)) {
    throw new Error(name + ' header mismatch');
  }
  if (kind === 'multiplayer') validateMultiplayerPairStructure_(sheet);
  sheet.setFrozenRows(1);
  const filter = sheet.getFilter();
  if (filter) filter.remove();
  sheet.getRange(1, 1, sheet.getMaxRows(), headers.length).createFilter();
  const dataCapacity = Math.max(sheet.getMaxRows() - 1, 1);
  if (kind === 'multiplayer') {
    colorHeaderGroup_(sheet, 1, 9, '#d9e8f5');
    colorHeaderGroup_(sheet, 10, 5, '#e8def3');
    colorHeaderGroup_(sheet, 15, 7, '#dceee2');
    colorHeaderGroup_(sheet, 22, 13, '#f3e3d1');
    formatMultiplayerPairs_(sheet, 2, Math.max((sheet.getLastRow() - 1) / 2, 0));
    sheet.getRange(2, 1, dataCapacity, 1).setNumberFormat('yyyy-mm-dd hh:mm:ss');
    sheet.getRange(2, 8, dataCapacity, 1).setNumberFormat('yyyy-mm-dd hh:mm:ss');
    sheet.getRange(2, 9, dataCapacity, 1).setNumberFormat('0.000');
    sheet.getRange(2, 23, dataCapacity, 12).setNumberFormat('0.000');
  } else {
    colorHeaderGroup_(sheet, 1, 8, '#d9e8f5');
    colorHeaderGroup_(sheet, 9, 9, '#dceee2');
    colorHeaderGroup_(sheet, 18, 12, '#f3e3d1');
    formatSingleplayerRows_(sheet);
    sheet.getRange(2, 1, dataCapacity, 1).setNumberFormat('yyyy-mm-dd hh:mm:ss');
    sheet.getRange(2, 8, dataCapacity, 1).setNumberFormat('yyyy-mm-dd hh:mm:ss');
    sheet.getRange(2, 9, dataCapacity, 1).setNumberFormat('0.000');
    sheet.getRange(2, 18, dataCapacity, 12).setNumberFormat('0.000');
  }
  sheet.getRange(1, 1, 1, headers.length).setFontWeight('bold').setFontColor('#263238');
  sheet.setColumnWidths(1, headers.length, 120);
  sheet.setColumnWidth(1, 170);
  sheet.setColumnWidth(kind === 'multiplayer' ? 4 : 3, 245);
  sheet.setColumnWidth(kind === 'multiplayer' ? 11 : 4, 245);
  sheet.setColumnWidth(kind === 'multiplayer' ? 12 : 5, 180);
  sheet.setColumnWidth(8, 170);
  return sheet;
}

function headersMatchFor_(sheet, headers) {
  if (sheet.getLastColumn() !== headers.length) return false;
  const values = sheet.getRange(1, 1, 1, headers.length).getValues()[0];
  return headers.every(function (header, index) { return values[index] === header; });
}

function validateMultiplayerPairStructure_(sheet) {
  const dataRows = sheet.getLastRow() - 1;
  if (dataRows <= 0) return true;
  if (dataRows % 2 !== 0) throw new Error('Multiplayer pair structure: orphan row');
  for (let row = 2; row <= sheet.getLastRow(); row += 2) {
    const values = sheet.getRange(row, 3, 2, 8).getValues();
    if (values[0][7] !== 'P1' || values[1][7] !== 'P2')
      throw new Error('Multiplayer pair structure: slot order');
    if (!Number.isInteger(values[0][0]) || values[0][0] < 1 || values[0][0] !== values[1][0])
      throw new Error('Multiplayer pair structure: match number');
    if (typeof values[0][1] !== 'string' || !values[0][1] || values[0][1] !== values[1][1])
      throw new Error('Multiplayer pair structure: match id');
  }
  return true;
}

function colorHeaderGroup_(sheet, firstColumn, columnCount, color) {
  sheet.getRange(1, firstColumn, 1, columnCount).setBackground(color);
}

function formatSingleplayerRows_(sheet) {
  const rowCount = Math.max(sheet.getLastRow() - 1, 0);
  for (let offset = 0; offset < rowCount; offset++)
    formatDataRow_(sheet, offset + 2);
}

function formatMultiplayerPairs_(sheet, firstDataRow, pairCount) {
  for (let pair = 0; pair < pairCount; pair++) {
    const firstRow = firstDataRow + pair * 2;
    const matchNumber = sheet.getRange(firstRow, 3).getValues()[0][0];
    const color = matchNumber % 2 === 1 ? '#f2f2f2' : '#ffffff';
    sheet.getRange(firstRow, 1, 2, MULTIPLAYER_HEADERS.length).setBackground(color);
    sheet.getRange(firstRow, 1, 1, MULTIPLAYER_HEADERS.length)
      .setBorder(true, null, null, null, null, null, '#b7b7b7', SpreadsheetApp.BorderStyle.SOLID_MEDIUM);
    sheet.getRange(firstRow + 1, 1, 1, MULTIPLAYER_HEADERS.length)
      .setBorder(null, null, true, null, null, null, '#b7b7b7', SpreadsheetApp.BorderStyle.SOLID_MEDIUM);
  }
}

function doPost(e) {
  let lock;
  try {
    const payload = parsePayload_(e && e.postData && e.postData.contents);
    const mode = modeFromPayload_(payload);
    if (mode === 'multiplayer') validateMultiplayerPayload_(payload);
    else validateSingleplayerPayload_(payload);
    lock = LockService.getScriptLock();
    lock.waitLock(10000);
    const spreadsheet = SpreadsheetApp.openById(SPREADSHEET_ID);
    const response = mode === 'multiplayer'
      ? writeMultiplayerResult_(spreadsheet, payload)
      : writeSingleplayerResult_(spreadsheet, payload);
    SpreadsheetApp.flush();
    return response;
  } catch (error) {
    const message = error && /^Invalid payload/.test(error.message) ? error.message : 'Request failed';
    return jsonResponse_({ ok: false, status: 'error', message: message });
  } finally {
    if (lock && lock.hasLock()) lock.releaseLock();
  }
}

function writeSingleplayerResult_(spreadsheet, payload) {
  const sheet = spreadsheet.getSheetByName(SINGLEPLAYER_SHEET);
  if (!sheet || !headersMatchFor_(sheet, HEADERS))
    throw new Error('Singleplayer sheet is not configured');
  if (findIdentifierRow_(sheet, 3, payload.sessionId))
    return jsonResponse_({ ok: true, status: 'duplicate', mode: 'singleplayer' });
  const rowNumber = sheet.getLastRow() + 1;
  ensureRowCapacity_(sheet, rowNumber);
  sheet.getRange(rowNumber, 1, 1, HEADERS.length)
    .setValues([singleplayerRowFromPayload_(payload, new Date())]);
  formatDataRow_(sheet, rowNumber);
  return jsonResponse_({ ok: true, status: 'created', mode: 'singleplayer' });
}

function writeMultiplayerResult_(spreadsheet, payload) {
  const sheet = spreadsheet.getSheetByName(MULTIPLAYER_SHEET);
  if (!sheet || !headersMatchFor_(sheet, MULTIPLAYER_HEADERS))
    throw new Error('Multiplayer sheet is not configured');
  validateMultiplayerPairStructure_(sheet);
  if (findIdentifierRow_(sheet, 4, payload.matchId))
    return jsonResponse_({ ok: true, status: 'duplicate', mode: 'multiplayer' });
  const matchNumber = nextMatchNumber_(sheet);
  const rows = multiplayerRowsFromPayload_(payload, new Date(), matchNumber);
  const firstRow = sheet.getLastRow() + 1;
  ensureRowCapacity_(sheet, firstRow + 1);
  sheet.getRange(firstRow, 1, 2, MULTIPLAYER_HEADERS.length).setValues(rows);
  formatMultiplayerPairs_(sheet, firstRow, 1);
  return jsonResponse_({ ok: true, status: 'created', mode: 'multiplayer', matchNumber: matchNumber });
}

function findIdentifierRow_(sheet, column, value) {
  if (sheet.getLastRow() < 2) return 0;
  const target = value;
  const values = sheet.getRange(2, column, sheet.getLastRow() - 1, 1).getValues();
  for (let index = 0; index < values.length; index++)
    if (values[index][0] === target) return index + 2;
  return 0;
}

function nextMatchNumber_(sheet) {
  if (sheet.getLastRow() < 2) return 1;
  const values = sheet.getRange(2, 3, sheet.getLastRow() - 1, 1).getValues();
  let maximum = 0;
  values.forEach(function (row) {
    if (Number.isInteger(row[0]) && row[0] > maximum) maximum = row[0];
  });
  return maximum + 1;
}

function ensureRowCapacity_(sheet, lastRequiredRow) {
  if (sheet.getMaxRows() < lastRequiredRow)
    sheet.insertRowsAfter(sheet.getMaxRows(), lastRequiredRow - sheet.getMaxRows());
}

function ensureColumnCapacity_(sheet, lastRequiredColumn) {
  if (sheet.getMaxColumns() < lastRequiredColumn)
    sheet.insertColumnsAfter(sheet.getMaxColumns(), lastRequiredColumn - sheet.getMaxColumns());
}

function parsePayload_(raw) {
  if (typeof raw !== 'string' || !raw.trim()) throw new Error('Invalid payload: empty body');
  try {
    return JSON.parse(raw);
  } catch (error) {
    throw new Error('Invalid payload: malformed JSON');
  }
}

function validatePayload_(payload) {
  if (!payload || payload.schemaVersion !== 1)
    throw new Error('Invalid payload: unsupported schemaVersion');
  ['sessionId', 'playerId', 'displayName', 'difficulty', 'endReason', 'endedAtUtc', 'highestRank', 'inputSource']
    .forEach(function (field) {
      if (typeof payload[field] !== 'string' || !payload[field].trim())
        throw new Error('Invalid payload: missing ' + field);
    });
  ['playedSeconds', 'score', 'remainingHp', 'foodCaught', 'foodMissed', 'trashEaten', 'maxCombo']
    .forEach(function (field) { requireFiniteNumber_(payload, field, false); });
  ['leftMaxStrength01', 'leftMeanStrength01', 'leftPressedSeconds', 'leftMaxForceN',
    'leftMeanForceN', 'rightMaxStrength01', 'rightMeanStrength01', 'rightPressedSeconds',
    'rightMaxForceN', 'rightMeanForceN', 'bothPressedSeconds', 'leftBalance01']
    .forEach(function (field) { requireFiniteNumber_(payload, field, true); });
  if (isNaN(new Date(payload.endedAtUtc).getTime()))
    throw new Error('Invalid payload: endedAtUtc');
  return true;
}

function rowFromPayload_(payload, receivedAtUtc) {
  validatePayload_(payload);
  return [
    new Date(receivedAtUtc), payload.schemaVersion, payload.sessionId, payload.playerId,
    payload.displayName, payload.difficulty, payload.endReason, new Date(payload.endedAtUtc),
    payload.playedSeconds, payload.score, payload.remainingHp, payload.foodCaught,
    payload.foodMissed, payload.trashEaten, payload.maxCombo, payload.highestRank,
    payload.inputSource, optionalNumber_(payload.leftMaxStrength01),
    optionalNumber_(payload.leftMeanStrength01), optionalNumber_(payload.leftPressedSeconds),
    optionalNumber_(payload.leftMaxForceN), optionalNumber_(payload.leftMeanForceN),
    optionalNumber_(payload.rightMaxStrength01), optionalNumber_(payload.rightMeanStrength01),
    optionalNumber_(payload.rightPressedSeconds), optionalNumber_(payload.rightMaxForceN),
    optionalNumber_(payload.rightMeanForceN), optionalNumber_(payload.bothPressedSeconds),
    optionalNumber_(payload.leftBalance01)
  ];
}

function findSessionRow_(sheet, sessionId) {
  if (sheet.getLastRow() < 2) return 0;
  const match = sheet.getRange(2, 3, sheet.getLastRow() - 1, 1)
    .createTextFinder(sessionId).matchEntireCell(true).findNext();
  return match ? match.getRow() : 0;
}

function jsonResponse_(body) {
  return ContentService.createTextOutput(JSON.stringify(body)).setMimeType(ContentService.MimeType.JSON);
}

function runContractSelfTests() {
  const payload = contractFixture_();
  assert_(modeFromPayload_(payload) === 'singleplayer', 'legacy payload routing');
  assert_(validateSingleplayerPayload_(payload) === true, 'valid singleplayer payload should pass');
  const row = singleplayerRowFromPayload_(payload, '2026-10-20T01:00:00.000Z');
  assert_(row.length === 29, 'row must contain 29 columns');
  assert_(row[2] === 'contract-session' && row[4] === 'ผู้เล่นทดสอบ', 'identity mapping');
  assert_(row[5] === 'Normal' && row[6] === 'Triumph' && row[15] === 'B', 'enum mapping');
  assert_(row[20] === '' && row[21] === '' && row[25] === '' && row[26] === '', 'unavailable force cells');
  assert_(row[28] === '', 'unavailable balance cell');
  expectInvalid_(Object.assign({}, payload, { sessionId: '' }), 'missing sessionId');
  expectInvalid_(Object.assign({}, payload, { schemaVersion: 2 }), 'unsupported schema');
  expectInvalid_(Object.assign({}, payload, { score: 'NaN' }), 'non-finite number');
  expectInvalid_(Object.assign({}, payload, { rightMeanStrength01: 'Infinity' }), 'non-finite optional number');
  let malformed = false;
  try { parsePayload_('{'); } catch (error) { malformed = true; }
  assert_(malformed, 'malformed JSON should fail');

  const versus = multiplayerContractFixture_();
  assert_(validateMultiplayerPayload_(versus) === true, 'valid multiplayer payload should pass');
  const versusRows = multiplayerRowsFromPayload_(versus, '2026-10-27T13:00:00.000Z', 3);
  assert_(versusRows.length === 2 && versusRows[0].length === 34, 'multiplayer row dimensions');
  assert_(versusRows[0][9] === 'P1' && versusRows[1][9] === 'P2', 'player slot ordering');
  assert_(versusRows[0][5] === 'Win' && versusRows[1][5] === 'Loss', 'outcome mapping');
  const duplicate = multiplayerContractFixture_();
  duplicate.players[1].playerId = duplicate.players[0].playerId;
  expectMultiplayerInvalid_(duplicate, 'duplicate player ids');
}

function contractFixture_() {
  return {
    schemaVersion: 1,
    sessionId: 'contract-session',
    playerId: 'player-contract',
    displayName: 'ผู้เล่นทดสอบ',
    difficulty: 'Normal',
    endReason: 'Triumph',
    endedAtUtc: '2026-10-20T00:00:00.000Z',
    playedSeconds: 60,
    score: 45,
    remainingHp: 2,
    foodCaught: 3,
    foodMissed: 1,
    trashEaten: 0,
    maxCombo: 3,
    highestRank: 'B',
    inputSource: 'Keyboard',
    leftMaxStrength01: 1,
    leftMeanStrength01: 0.5,
    leftPressedSeconds: 30,
    leftMaxForceN: null,
    leftMeanForceN: null,
    rightMaxStrength01: 1,
    rightMeanStrength01: 0.4,
    rightPressedSeconds: 24,
    rightMaxForceN: null,
    rightMeanForceN: null,
    bothPressedSeconds: 5,
    leftBalance01: null
  };
}

function multiplayerContractFixture_() {
  function player(slot, id, hp) {
    return {
      playerSlot: slot,
      playerId: id,
      displayName: slot === 'P1' ? 'Arc' : 'Tet',
      score: slot === 'P1' ? 240 : 210,
      remainingHp: hp,
      foodCaught: 18,
      foodMissed: 7,
      trashEaten: hp === 0 ? 3 : 1,
      maxCombo: 9,
      highestRank: 'A',
      inputSource: 'Keyboard',
      leftMaxStrength01: 1,
      leftMeanStrength01: 0.4,
      leftPressedSeconds: 20,
      leftMaxForceN: null,
      leftMeanForceN: null,
      rightMaxStrength01: 1,
      rightMeanStrength01: 0.45,
      rightPressedSeconds: 22,
      rightMaxForceN: null,
      rightMeanForceN: null,
      bothPressedSeconds: 2,
      leftBalance01: null,
      hatColorHex: slot === 'P1' ? '#F28C28' : '#3C78D8',
      shoeColorHex: slot === 'P1' ? '#F28C28' : '#3C78D8'
    };
  }
  return {
    mode: 'multiplayer',
    schemaVersion: 1,
    matchId: 'contract-match',
    winnerSlot: 'P1',
    endReason: 'PlayerDefeated',
    endedAtUtc: '2026-10-27T12:00:00.000Z',
    playedSeconds: 83.25,
    players: [player('P2', 'player-2', 0), player('P1', 'player-1', 2)]
  };
}

function requireFiniteNumber_(payload, field, optional) {
  const value = payload[field];
  if (optional && (value === null || typeof value === 'undefined')) return;
  if (typeof value !== 'number' || !isFinite(value))
    throw new Error('Invalid payload: ' + field);
}

function optionalNumber_(value) {
  return value === null || typeof value === 'undefined' ? '' : value;
}

function headersMatch_(sheet) {
  const values = sheet.getRange(1, 1, 1, HEADERS.length).getValues()[0];
  return HEADERS.every(function (header, index) { return values[index] === header; });
}

function formatGroup_(sheet, firstColumn, columnCount, headerColor, oddColor, evenColor) {
  sheet.getRange(1, firstColumn, 1, columnCount).setBackground(headerColor);
  const rowCount = Math.max(sheet.getMaxRows() - 1, 1);
  const colors = [];
  for (let row = 0; row < rowCount; row++)
    colors.push(Array(columnCount).fill((row + 2) % 2 === 0 ? evenColor : oddColor));
  sheet.getRange(2, firstColumn, rowCount, columnCount).setBackgrounds(colors);
}

function formatDataRow_(sheet, row) {
  const even = row % 2 === 0;
  sheet.getRange(row, 1, 1, 8).setBackground(even ? '#eaf2f9' : '#f4f8fc');
  sheet.getRange(row, 9, 1, 9).setBackground(even ? '#eaf5ed' : '#f4faf6');
  sheet.getRange(row, 18, 1, 12).setBackground(even ? '#f8f0e6' : '#fcf8f2');
}

function expectInvalid_(payload, label) {
  let failed = false;
  try { validatePayload_(payload); } catch (error) { failed = true; }
  assert_(failed, label + ' should fail');
}

function expectMultiplayerInvalid_(payload, label) {
  let failed = false;
  try { validateMultiplayerPayload_(payload); } catch (error) { failed = true; }
  assert_(failed, label + ' should fail');
}

function assert_(condition, message) {
  if (!condition) throw new Error(message);
}
