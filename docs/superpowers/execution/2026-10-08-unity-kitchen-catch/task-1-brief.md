## Task 1: กติกาคะแนน Combo และเวลารอบ

### Execution context / binding constraints

Work in `C:/Users/Arc/Unity/CPS-Project`, branch `codex/kitchen-catch`. Preserve all pre-existing edits outside new `Assets/CPS` files. All new Markdown belongs in `C:/Users/Arc/Downloads/CPS/Unity_Project` (not Unity root).

Unity `6000.3.25f1` (6.3 LTS), packages already installed: Input System1.20.0, uGUI2.0.0, URP17.3.0, Test Framework1.6.0; no new dependencies. Namespace `CPS.KitchenCatch`; test namespace `CPS.KitchenCatch.Tests`.

Create Runtime/EditMode asmdefs: `Assets/CPS/CPS.Runtime.asmdef`, `Assets/CPS/Tests/EditMode/CPS.EditModeTests.asmdef`. Ruling4 postpones `Assets/CPS/Editor/CPS.Editor.asmdef` until Task5 and `Assets/CPS/Tests/PlayMode/CPS.PlayModeTests.asmdef` until Task2 to avoid empty assembly warnings/dummy sources. Validate exact installed assembly references (`Unity.InputSystem`, `UnityEngine.UI`, not unverified `Unity.ugui`). Future Editor-only build tests must not create runtime→Editor references.

Ruling 1: Task1 defines `HandStatistics`/`InputStatistics` serializable DTOs beside SessionResult (new `Assets/CPS/Game/InputStatistics.cs` permitted); Task2 implements collector only. Public fields: HandStatistics `bool HasStrength/HasForce`, `float MaxStrength01/MeanStrength01/MaxForceN/MeanForceN/ValidDurationSeconds/ForceValidDurationSeconds/PressedDurationSeconds`; InputStatistics `HandStatistics Left/Right`, `float BothPressedDurationSeconds`, `bool HasBalance`, `float LeftBalance01`. Copy deep-copies these; no fabricated N.

SessionResult public serializable fields: `int SchemaVersion=1`, `string SessionId/PlayerId/DisplayName/EndedAtUtc/InputSource`, `DifficultyId Difficulty`, `EndReason EndReason`, `float PlayedSeconds`, `int Score/RemainingHp/FoodCaught/FoodMissed/TrashEaten/MaxCombo`, `ComboRank HighestRank`, `InputStatistics InputStatistics`. Camel-case JSON export can be handled when transport is scoped; current JsonUtility output must round-trip all data.

Expose `ComboRank CurrentRank` on ScoreComboSystem derived from streak; `HighestRank` is round maximum. Controller (Task5) owns Ready/Results delays while this model owns Playing/terminal decision; no UI/presentation/scene code in Task1.

Tests must observe failing behavior before implementation. Minimal API scaffolds throwing NotImplementedException are acceptable to make test assertions executable; a compiler error alone is not RED behavior evidence. After focused red→green, run full current test suite (only Task1 tests initially). Use hidden Start-Process with PassThru then `.WaitForExit()` rather than `-Wait` if child helper processes keep the shell waiting; never kill user processes.

Unity executable: `C:/Program Files/Unity/Hub/Editor/6000.3.25f1/Editor/Unity.exe`; logs/XML: `C:/Users/Arc/Downloads/CPS/Unity_Project/verification` with unique task1-red/task1-green filenames. Run `-batchmode -projectPath <checkout> -runTests -testPlatform EditMode -testFilter CPS.KitchenCatch.Tests -testResults <xml> -logFile <log>` (no `-quit`). Verify actual XML counts, log compile errors and existing unrelated warnings separately. Task0 baseline has no existing tests; don't claim zero tests pass.

Use apply_patch for edits. No unnecessary code comments/emojis. Before git stage/commit inspect exact diff and stage only Task1 files/metas. Git .git writes may require escalation; ask for tool escalation where needed, not repeated user confirmation. Do not push/PR, touch unrelated settings, or spawn any agents/reviewers.

**Files:** Create `Assets/CPS/Game/{GameTypes,HealthSystem,ScoreComboSystem,GameSessionModel,SessionResult}.cs`, asmdefs ข้างต้น; Test `Assets/CPS/Tests/EditMode/GameRulesTests.cs`

**Interfaces:** ผลิต enums `DifficultyId { Easy, Normal, Hard }`, `SessionState { Ready, Playing, Triumph, Dying, Results }`, `ObjectKind { Food, Trash }`, `ObjectOutcome { Caught, Missed }`, `ComboRank { D, C, B, A, S, SS, SSS }`, `EndReason { Triumph, HpDepleted }`
- `HealthSystem.Reset(int initialHp)` / `Damage(int amount)` / `int CurrentHp { get; }`
- `ScoreComboSystem.Reset()` / `Resolve(ObjectKind kind, ObjectOutcome outcome, int points)`; read-only `int Score`, `int Streak`, `int MaxCombo`, `ComboRank HighestRank`, `int FoodCaught`, `int FoodMissed`, `int TrashEaten`
- `GameSessionModel.Start()` / `Resolve(long objectId, ObjectKind kind, ObjectOutcome outcome, int points)` / `CompleteStep(float dt)`; State, PlayedSeconds, RemainingSeconds, Health, Score; `event Action<EndReason> Ended`
- `CompleteStep` เป็น terminal decision หลัง resolve outcomes ของ step; เล่นได้ไม่เกิน remaining time, objectId ไม่ reuse ภายในรอบ, terminal latch ปฏิเสธ outcomes ใหม่
- `SessionResult` plain serializable DTO ตาม spec §11; ใส่ schemaVersion=1 และ stats types ที่ Task 2 เพิ่ม ผลิต `SessionResult Copy()` สำหรับส่งสำเนา ไม่ expose backing frozen result ให้ UI แก้

- [ ] เขียน failing tests ใน `GameRulesTests`: `FoodPointsAre10_15_20`, `TrashMissIsHarmless`, `DamageClampsAtZero`, `DuplicateObjectIdIsIgnored`, `ComboThresholdsAndReset`, `DeathWinsAtDeadline`, `LargeDtStopsAt60`, `TerminalEventOnce` ใช้ assertions:

```csharp
Assert.That(scoreAfterAppleBreadCake, Is.EqualTo(45));
Assert.That(ranksAtStreaks2_3_4_5_8_12, Is.EqualTo(new[] { ComboRank.C, ComboRank.B, ComboRank.A, ComboRank.S, ComboRank.SS, ComboRank.SSS }));
Assert.That(model.PlayedSeconds, Is.EqualTo(60).Within(0.001));
Assert.That(model.State, Is.EqualTo(SessionState.Dying));
Assert.That(endedCount, Is.EqualTo(1));
```

- [ ] Run E filter `CPS.KitchenCatch.Tests.GameRulesTests`; เก็บ fail ที่ตรงกับ feature
- [ ] Implement APIs ข้างต้น: initial HP3, duration60, damage1, ไม่มี invulnerability/multiplier/decay; MaxCombo/HighestRank ไม่ reset ตอน miss; Ready→Playing และ terminal มี guard
- [ ] Run E green: ทุก boundary rank รวม streak0/1/7/11/12, trash miss, outcomes หลัง terminal, result copy แก้แล้วต้นฉบับไม่เปลี่ยน
- [ ] Commit เฉพาะ Game, asmdefs/tests และ metas: `feat: add tested kitchen catch rules`

