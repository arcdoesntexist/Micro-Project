## Task 3: Profiles และตัวเลือกข้าม Scene

### Execution clarifications

Read `constraints.md` beside brief. Preserve existing user changes and never reset PlayerPrefs globally. Tests should use injected delegates/fake storage for data-loss boundaries; do not clear user's registry/browser storage to test.

AppSession owns loading/selecting/persisting profiles and duplicate singleton guard only; Boot's navigation to MainMenu is wired in Task5 by SceneNavigator. Do not create a Task3 dependency on nonexistent scenes or auto-load MainMenu when a test creates AppSession. Expose store/selection read-only for UI, instantiate one persistent AppSession in Boot later.

Nickname validation counts Unicode text elements1–24 after Trim; store literal markup-like names with no rich-text interpretation (actual UI text component setting Task6). Duplicate names allowed; duplicate PlayerId in loaded JSON invalid. JSON wrapper schema1/list; validate null entries, IDs, names/counts; no storage read exception may crash menu. Store unique player IDs, no auth/network/database.

Backup malformed/future-version raw JSON before replacing; if backup write/flush fails, leave primary raw untouched and use memory with warning. Save failures keep in-memory profiles and warnings; never claim persistence succeeded. Remember once-per-session counting in memory across scenes; actual Restart IDs produced by Task5. Test full malformed/empty/null/read-failure/write-failure/flush-failure paths and ensure raw remains recoverable. New records shouldn't silently overwrite recoverable corrupt input.

**Files:** Create `Assets/CPS/Profiles/{PlayerProfile,ProfileStore,AppSession}.cs`; Test `Tests/EditMode/ProfileStoreTests.cs`, `Tests/PlayMode/AppSessionTests.cs`

**Interfaces:** `PlayerProfile` serializable data with `string PlayerId`, `string DisplayName`, `int PlayCount`; `ProfileStore.Load()` / `Create(string displayName): PlayerProfile` / `RecordResult(string playerId, string sessionId): bool` / `Save(): bool`; `Profiles` read-only และ `Warning` string; constructor receives `Func<string,string> read`, `Action<string,string> write`, `Action flush` สำหรับ PlayerPrefs default หรือ fake tests
`AppSession.Select(string playerId, DifficultyId difficulty)` / SelectedProfile / SelectedDifficulty; singleton persists scenes; Boot loads profiles then navigates MainMenu; RecordResult increments once, remembers completed session IDs in runtime, persists playCount ไม่สร้างประวัติ raw input

- [ ] เขียน tests trim/empty/25characters/ชื่อไทย/duplicate names unique IDs, reload playCount, malformed JSON raw backup, save exception preserves in-memory profile และ repeated sessionId no extra count

```csharp
Assert.That(store.Create("  อาร์ค  ").DisplayName, Is.EqualTo("อาร์ค"));
Assert.That(first.PlayerId, Is.Not.EqualTo(second.PlayerId));
Assert.That(store.RecordResult(first.PlayerId, "session-1"), Is.False); // second call
Assert.That(backupRaw, Is.EqualTo(malformedRaw));
Assert.That(store.Warning, Is.Not.Empty);
```

- [ ] Run E/P fail
- [ ] Implement GUID IDs, PlayerPrefs JSON wrapper version1/list profiles; names1–24 Unicode text elements; validate loaded IDs/names/counts, unknown schema warns and preserves raw; backup under separate key before replacing corrupt data, หาก backup/save fails ไม่ overwrite original stored raw; memory remains usable พร้อม warning
- [ ] Run E/P green รวม duplicate AppSession ถูกกำจัดโดยไม่ลบ original selection, malformed empty/null payload และ save failure
- [ ] Commit Profiles/tests/metas: `feat: add local profiles and app session`

