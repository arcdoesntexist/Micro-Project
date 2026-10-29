# Unity Kitchen Catch Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** สร้างเกม Unity ที่เลือก Profile/ระดับ เล่นรับอาหาร 60 วินาที จบด้วย Triumph หรือ Death และ Restart ได้บน desktop WebGL

**Architecture:** Lean Modular สาม Scene: Boot, MainMenu, Gameplay ใช้ references ใน Inspector และ events เฉพาะ component ไม่ใช้ global event bus หรือ DI framework แยกกติกาและสถิติเป็น C# ที่ทดสอบได้ และใช้ MonoBehaviour สำหรับ input, scene, movement และ presentation

**Tech Stack:** Unity 6000.3.25f1, Universal 2D/URP 17.3.0, Input System 1.20.0, Unity Test Framework 1.6.0, uGUI 2.0.0, UnityEngine.Pool; ใช้แพ็กเกจเดิม

**Spec:** [Approved design](../specs/2026-10-08-unity-kitchen-catch-design.md) — ผู้ใช้ให้ไปขั้นถัดไปวันที่ 8 ตุลาคม 2026

## Global Constraints

- Unity `6000.3.25f1` (6.3 LTS), Universal 2D, URP และ Input System ที่ติดตั้งอยู่
- WebGL สำหรับ Desktop Browser แนวนอน อ้างอิง 1920×1080
- Camera แบบ orthographic พื้นที่เกมอัตราส่วน 16:9 คงที่ ใช้ letterbox/pillarbox เมื่อหน้าจอสัดส่วนต่างออกไป
- เป้าหมาย gameplay/rendering 60 FPS บนเครื่องที่ใช้สาธิต; ตรวจ frame time เป้าหมายประมาณ 16.7 ms ใน build จริง
- Sprite animation ตัวละครประมาณ 10–12 FPS และบางฉากหลัง 8–12 FPS
- เริ่ม 3 HP รับ Apple = 10, Bread = 15, Cake = 20 คะแนน ไม่มีตัวคูณ Combo
- Keyboard: `A` หรือ Left Arrow สำหรับซ้าย, `D` หรือ Right Arrow สำหรับขวา; ทั้งสองข้าง/ไม่มีข้างใดกด = หยุด
- Playing: เวลาเล่นจริง 60 วินาที; HP = 0 มี priority เหนือ timeout หากเหตุการณ์เกิดใน simulation step เดียวกัน
- ป้าย `IoTE` คงตัวพิมพ์ตามนี้เสมอและต้องไม่กลับด้าน
- Keyboard input เป็น simulation; แรง N แสดง “— / Sensor not connected” ไม่สร้าง N จำลอง
- HiveMQ, MQTT client, Node-RED, Google Sheets, ESP32, FSR และการสอบเทียบจริงเป็นเฟสถัดไป
- ทุกเอกสาร `.md` ใหม่อยู่ใต้ `C:\Users\Arc\Downloads\CPS\Unity_Project`; โค้ด/asset อยู่ใน Unity project

## Review Focus

1. Delta time ใหญ่หรือ HP หมดตรง 60s: ไม่สะสมเวลา/สถิติเกินรอบ และจบ Death ครั้งเดียว — Task 1/5
2. Alias key ค้าง, focus หายระหว่าง countdown, browser กลับมา: ไม่เคลื่อนเองและไม่เสียเวลาเล่นตอนพัก — Task 2/5
3. JSON เสีย, storage save ล้มเหลว, ชื่อเป็น markup/ไทย/ชื่อซ้ำ: ไม่ทำข้อมูลดิบหาย ไม่ตีความ markup และยังเล่นต่อได้ — Task 3/6
4. วัตถุคืน pool แล้ว callback เก่ามาถึง, Restart/ปุ่มกดซ้ำ: ไม่มีคะแนนข้ามรอบหรือ AppSession ซ้ำ — Task 4/5/6
5. Frame ต่ำและหน้าจอสูง/แคบ: วัตถุไม่ทะลุ catch zone, HUD อยู่ใน viewport และป้ายหมวกอ่านได้ — Task 4/7/8

## Workspace, file map และวิธีตรวจ

Unity root `U = C:\Users\Arc\Unity\CPS-Project`; docs root `D = C:\Users\Arc\Downloads\CPS\Unity_Project` ทุก path `Assets/...` ด้านล่างอ้างจาก U

ตรวจแล้ว: มีเฉพาะ SampleScene และ template settings ไม่มี game scripts, production sprite/font/audio; Unity.exe มีอยู่ แต่ไม่มี `Editor\Data\PlaybackEngines\WebGLSupport` ณ วันที่จัดทำแผน

จัดไฟล์ตามหน้าที่:

| Folder | Owner / responsibility |
|---|---|
| `Assets/CPS/Game/` | health, score/combo, round rules, result data |
| `Assets/CPS/Input/` | input snapshot, Keyboard, force statistics, player movement |
| `Assets/CPS/Profiles/` | local profiles, persistence และ AppSession |
| `Assets/CPS/Objects/` | object/difficulty definitions, spawn policy, pooled falling objects |
| `Assets/CPS/Flow/` | session orchestration และ scene navigation |
| `Assets/CPS/UI/` | menu, HUD, results และ 16:9 viewport |
| `Assets/CPS/Presentation/` | mascot และ ambient presentation |
| `Assets/CPS/Editor/` | asset/scene authoring และ WebGL build entry point |
| `Assets/CPS/Tests/EditMode/`, `PlayMode/` | กติกาและ interaction tests |
| `Assets/CPS/Scenes/`, `Config/`, `Prefabs/`, `Art/`, `Animation/`, `Audio/`, `Fonts/` | serialized production assets |

สร้าง `Assets/CPS/CPS.Runtime.asmdef`, `Tests/EditMode/CPS.EditModeTests.asmdef` ใน Task 1; สร้าง `Tests/PlayMode/CPS.PlayModeTests.asmdef` ใน Task2 และ `Editor/CPS.Editor.asmdef` ใน Task4 เมื่อมี source จริงเพื่อไม่สร้าง empty assembly warnings; namespace `CPS.KitchenCatch` ให้ test assemblies ใช้ Unity test references และ Runtime references Input System/uGUI ตาม package assembly ที่ติดตั้งจริง ไม่เพิ่ม packages

ก่อน execution ใช้ using-git-worktrees ตรวจ isolation และ working tree; รักษา edits ของผู้ใช้ใน Settings และการลบ HubForceResolve ห้าม reset/restore ห้ามย้าย scene settings ของผู้ใช้โดยไม่ตรวจ diff เมื่อจบแต่ละ task commit เฉพาะไฟล์ของ task รวม `.meta` ที่ Unity สร้าง ไม่ใช้ `git add .`; เอกสารนอก Git project ไม่รวมใน game commits และไม่สร้าง repository ใหม่เพื่อเอกสาร

ตั้งค่าการรัน PowerShell เมื่อเริ่ม execution (ไม่ใช่การรันในขั้นเขียนแผน):

```powershell
$unityExe = 'C:\Program Files\Unity\Hub\Editor\6000.3.25f1\Editor\Unity.exe'
$gameRoot = 'C:\Users\Arc\Unity\CPS-Project'
$verificationRoot = 'C:\Users\Arc\Downloads\CPS\Unity_Project\verification'
New-Item -ItemType Directory -Path $verificationRoot -Force | Out-Null
```

หากใช้ isolated checkout ให้ `$gameRoot` เป็น path ที่คืนมาจริง ก่อน batch tests ตรวจว่าไม่มี Unity Editor เปิด checkout เดียวกัน; ไม่ kill process/ปิดงานที่ยังไม่ save เอง หากยังเปิดอยู่ให้ใช้ Test Runner ใน Editor หรือขอผู้ใช้ปิดอย่างปลอดภัย

ทุก task ใช้ TDD: เขียน test → run ให้เห็น fail จาก behavior/API ที่ยังไม่มี → implement → run ให้ pass ห้ามนับไม่มี tests/compile error ไม่เกี่ยวข้องว่า green ตัวอย่างคำสั่ง E (EditMode) และ P (PlayMode):

```powershell
& $unityExe -batchmode -projectPath $gameRoot -runTests -testPlatform EditMode -testFilter CPS.KitchenCatch.Tests -testResults "$verificationRoot\editmode.xml" -logFile "$verificationRoot\editmode.log"
& $unityExe -batchmode -projectPath $gameRoot -runTests -testPlatform PlayMode -testFilter CPS.KitchenCatch.Tests -testResults "$verificationRoot\playmode.xml" -logFile "$verificationRoot\playmode.log"
```

ใช้ filter ระบุ test class ของ task ได้ แต่หลัง green รันทั้ง assembly ที่ได้รับผลกระทบ ตรวจ XML ว่ามี tests จริงและ failed=0 พร้อมอ่าน log; ไม่ใช้ `-quit` คู่ `-runTests` เพราะอาจจบก่อน tests เสร็จ ตาม [Unity command-line reference](https://docs.unity3d.com/6000.3/Documentation/Manual/EditorCommandLineArguments.html)

## Task 1: กติกาคะแนน Combo และเวลารอบ

**Files:** Create `Assets/CPS/Game/{GameTypes,HealthSystem,ScoreComboSystem,GameSessionModel,SessionResult}.cs`, asmdefs ข้างต้น; Test `Assets/CPS/Tests/EditMode/GameRulesTests.cs`

**Interfaces:** ผลิต enums `DifficultyId { Easy, Normal, Hard }`, `SessionState { Ready, Playing, Triumph, Dying, Results }`, `ObjectKind { Food, Trash }`, `ObjectOutcome { Caught, Missed }`, `ComboRank { D, C, B, A, S, SS, SSS }`, `EndReason { Triumph, HpDepleted }`
- `HealthSystem.Reset(int initialHp)` / `Damage(int amount)` / `int CurrentHp { get; }`
- `ScoreComboSystem.Reset()` / `Resolve(ObjectKind kind, ObjectOutcome outcome, int points)`; read-only `int Score`, `int Streak`, `int MaxCombo`, `ComboRank HighestRank`, `int FoodCaught`, `int FoodMissed`, `int TrashEaten`
- `GameSessionModel.Start()` / `Resolve(long objectId, ObjectKind kind, ObjectOutcome outcome, int points)` / `CompleteStep(float dt)`; State, PlayedSeconds, RemainingSeconds, Health, Score; `event Action<EndReason> Ended`
- `CompleteStep` เป็น terminal decision หลัง resolve outcomes ของ step; เล่นได้ไม่เกิน remaining time, objectId ไม่ reuse ภายในรอบ, terminal latch ปฏิเสธ outcomes ใหม่
- `SessionResult` plain serializable DTO ตาม spec §11; ใส่ schemaVersion=1 และ stats types ที่ Task 2 เพิ่ม ผลิต `SessionResult Copy()` สำหรับส่งสำเนา ไม่ expose backing frozen result ให้ UI แก้

- [x] เขียน failing tests ใน `GameRulesTests`: `FoodPointsAre10_15_20`, `TrashMissIsHarmless`, `DamageClampsAtZero`, `DuplicateObjectIdIsIgnored`, `ComboThresholdsAndReset`, `DeathWinsAtDeadline`, `LargeDtStopsAt60`, `TerminalEventOnce` ใช้ assertions:

```csharp
Assert.That(scoreAfterAppleBreadCake, Is.EqualTo(45));
Assert.That(ranksAtStreaks2_3_4_5_8_12, Is.EqualTo(new[] { ComboRank.C, ComboRank.B, ComboRank.A, ComboRank.S, ComboRank.SS, ComboRank.SSS }));
Assert.That(model.PlayedSeconds, Is.EqualTo(60).Within(0.001));
Assert.That(model.State, Is.EqualTo(SessionState.Dying));
Assert.That(endedCount, Is.EqualTo(1));
```

- [x] Run E filter `CPS.KitchenCatch.Tests.GameRulesTests`; เก็บ fail ที่ตรงกับ feature
- [x] Implement APIs ข้างต้น: initial HP3, duration60, damage1, ไม่มี invulnerability/multiplier/decay; MaxCombo/HighestRank ไม่ reset ตอน miss; Ready→Playing และ terminal มี guard
- [x] Run E green: ทุก boundary rank รวม streak0/1/7/11/12, trash miss, outcomes หลัง terminal, result copy แก้แล้วต้นฉบับไม่เปลี่ยน
- [x] Commit เฉพาะ Game, asmdefs/tests และ metas: `feat: add tested kitchen catch rules`

## Task 2: Keyboard, การวิ่ง และสถิติ input

**Files:** Create `Assets/CPS/Input/{InputSnapshot,IPlayerInput,KeyboardInput,PlayerController,ForceStatsCollector}.cs`; Test `Tests/EditMode/InputStatsTests.cs`, `Tests/PlayMode/KeyboardPlayerTests.cs`

**Interfaces:** `HandInput(bool pressed, float strength01, bool valid, float? forceN)`; `InputSnapshot(HandInput left, HandInput right, string source)`; `IPlayerInput.Read(): InputSnapshot`; `KeyboardInput` MonoBehaviour implements interface; `PlayerController.Step(InputSnapshot input, float dt)` / `ResetPosition()`; `ForceStatsCollector.Accumulate(InputSnapshot input, float dt)` / `Reset()` / `InputStatistics Snapshot()`
`InputStatistics` serializable DTO has `HandStatistics Left/Right`, `float BothPressedDurationSeconds`, `bool HasBalance`, `float LeftBalance01`; `HandStatistics` has public serializable fields `bool HasStrength/HasForce`, `float MaxStrength01/MeanStrength01/MaxForceN/MeanForceN/ValidDurationSeconds/ForceValidDurationSeconds/PressedDurationSeconds` ไม่พึ่ง nullable fields ที่ JsonUtility ไม่ serialize; null force จาก snapshot กลายเป็น HasForce=false

- [x] เขียน tests input 4 states, A+Left ปล่อย A ยังวิ่ง, missing keyboard/invalid sample/NaN/negative dt ไม่ทำสถิติพัง, clamp bounds, movement เท่ากันที่30/60/120steps ใช้ `InputTestFixture` จาก package TestFramework reference

```csharp
Assert.That(directionLeftRightBothNeither, Is.EqualTo(new[] { -1, 1, 0, 0 }));
Assert.That(stats.Left.MeanStrength01, Is.EqualTo(0.25).Within(0.001)); // valid 1s pressed + 3s released
Assert.That(stats.Left.HasForce, Is.False);
Assert.That(empty.Left.HasStrength, Is.False);
```

- [x] Run E/P class filters ให้เห็น fail
- [x] Implement Read aliases OR ต่อข้าง; source=`Keyboard`; pressed strength1 else0; force absent; movement7units/s, field16units, configurable mascot half-width; แยก invalid duration ของแต่ละมือ, force weighted mean ใช้ force-valid duration เท่านั้น, rejected nonfinite sample ไม่ปนค่าเฉลี่ย
- [x] Run E/P green รวม mean max, release, both duration, force-valid sample ผสม unavailable, zero balance และ bounds
- [x] Commit Input/tests/metas: `feat: add keyboard control and input statistics`

## Task 3: Profiles และตัวเลือกข้าม Scene

**Files:** Create `Assets/CPS/Profiles/{PlayerProfile,ProfileStore,AppSession}.cs`; Test `Tests/EditMode/ProfileStoreTests.cs`, `Tests/PlayMode/AppSessionTests.cs`

**Interfaces:** `PlayerProfile` serializable data with `string PlayerId`, `string DisplayName`, `int PlayCount`; `ProfileStore.Load()` / `Create(string displayName): PlayerProfile` / `RecordResult(string playerId, string sessionId): bool` / `Save(): bool`; `Profiles` read-only และ `Warning` string; constructor receives `Func<string,string> read`, `Action<string,string> write`, `Action flush` สำหรับ PlayerPrefs default หรือ fake tests
`AppSession.Select(string playerId, DifficultyId difficulty)` / SelectedProfile / SelectedDifficulty; singleton persists scenes; Boot loads profiles then navigates MainMenu; RecordResult increments once, remembers completed session IDs in runtime, persists playCount ไม่สร้างประวัติ raw input

- [x] เขียน tests trim/empty/25characters/ชื่อไทย/duplicate names unique IDs, reload playCount, malformed JSON raw backup, save exception preserves in-memory profile และ repeated sessionId no extra count

```csharp
Assert.That(store.Create("  อาร์ค  ").DisplayName, Is.EqualTo("อาร์ค"));
Assert.That(first.PlayerId, Is.Not.EqualTo(second.PlayerId));
Assert.That(store.RecordResult(first.PlayerId, "session-1"), Is.False); // second call
Assert.That(backupRaw, Is.EqualTo(malformedRaw));
Assert.That(store.Warning, Is.Not.Empty);
```

- [x] Run E/P fail
- [x] Implement GUID IDs, PlayerPrefs JSON wrapper version1/list profiles; names1–24 Unicode text elements; validate loaded IDs/names/counts, unknown schema warns and preserves raw; backup under separate key before replacing corrupt data, หาก backup/save fails ไม่ overwrite original stored raw; memory remains usable พร้อม warning
- [x] Run E/P green รวม duplicate AppSession ถูกกำจัดโดยไม่ลบ original selection, malformed empty/null payload และ save failure
- [x] Commit Profiles/tests/metas: `feat: add local profiles and app session`

## Task 4: วัตถุตก, pooling และ spawn ที่เล่นได้

**Files:** Create `Assets/CPS/Objects/{DifficultyConfig,FallingObjectDefinition,SpawnPolicy,FallingObjectSpawner,FallingObject}.cs`; assets `Config/{Easy,Normal,Hard,Apple,Bread,Cake,Can,BananaPeel}.asset`, `Prefabs/FallingObject.prefab`; Test `Tests/EditMode/SpawnPolicyTests.cs`, `Tests/PlayMode/FallingObjectTests.cs`

**Interfaces:** `DifficultyConfig` SO speed/interval/trashProbability/lanes; `FallingObjectDefinition` SO kind/points/sprite; `SpawnPolicy.TryChoose(float now, float playerSpeed, float catchHalfWidth, out SpawnChoice choice): bool`, constructor config+seed; `Spawner.Begin(DifficultyConfig config, Action<long,FallingObjectDefinition,ObjectOutcome> report)` / `Step(float dt)` / `StopAndClear()`; `FallingObject.Spawn(long id, FallingObjectDefinition definition, Vector2 position, float speed)` / `Step(float dt, Bounds catchZone)` / `Despawn()`

- [x] Tests Easy no trash, seed reproducibility, food same selection weight, feasible adjacent food arrivals, trash separated from required food catch window; pooled object old spawn cannot resolve new spawn; coarse step crossing catcher counts catch once not miss

```csharp
Assert.That(easySpawnKinds.All(k => k == ObjectKind.Food), Is.True);
Assert.That(requiredTravel, Is.LessThanOrEqualTo(7f * arrivalGap));
Assert.That(reportCountForSpawn, Is.EqualTo(1));
Assert.That(activeCountAfterClear, Is.EqualTo(0));
```

- [x] Run E/P fail
- [x] Implement speed/interval/trash: Easy2/1.40/0, Normal2.8/1.10/.15, Hard3.5/.85/.25; spawn-to-catch7units; Easy lanes[-6,0,6] cyclic; Normal[-6,-3,0,3,6], Hard[-6,-4,-2,0,2,4,6]; food types uniform, trash variants uniform
- [x] Implement simple candidate rejection using previous food arrival reachable distance and expanded catch windows against active food/trash; if no safe candidate defer spawn, ไม่ฝืน spawn; bounded attempts, max16; validate config speed/interval/lanes; UnityEngine.Pool resets all state/callbacks with fresh monotonically increasing spawnId; swept segment catch-zone check before miss (ไม่อาศัย trigger ที่อาจทะลุระหว่างเฟรม)
- [x] Run E/P green และ deterministic stress1000spawns checks bounds/travel/conflicts; 30FPS large-step catch, duplicate reporting และ Stop/reuse
- [x] Commit Objects/config/prefab/tests/metas: `feat: add safe pooled falling objects`

## Task 5: เชื่อม playable session และ terminal flow

**Files:** Create `Assets/CPS/Flow/{GameSessionController,SceneNavigator}.cs`, `Assets/CPS/Editor/GameSceneSetup.cs`, `Assets/CPS/Scenes/{Boot,MainMenu,Gameplay}.unity`; Modify `ProjectSettings/EditorBuildSettings.asset` เฉพาะ scene list โดยรักษา m_configObjects; Test `Tests/PlayMode/GameSessionFlowTests.cs`

**Interfaces:** `GameSessionController.State`, `IsPaused`, `LastResult` returns copy; `event Action<SessionState> StateChanged`, `event Action<SessionResult> Finished`; `BeginRound()` / `Pause()` / `Resume()` / `Restart()` / `ReturnToMenu()`; `SceneNavigator.GoTo(string sceneName)` returns guarded transition; `GameSceneSetup.CreateOrUpdate()` explicit Editor menu/executeMethod ไม่ auto-run เมื่อ import
Consumes models Task1, snapshots/stats Task2, AppSession Task3, spawner Task4; terminal result metadata sessionId GUID/endedAtUtc ISO8601; stats DTO source and units included

- [x] Tests countdown3→Playing, timeout→Triumph→Results1.2s, HP0→Dying→Results1.4s, simultaneous last damage wins, result frozen once, playCount1, 10restarts no state/object/event leak, pause during Ready/Playing/terminal and repeated scene request

```csharp
Assert.That(result.PlayedSeconds, Is.EqualTo(60f).Within(0.001));
Assert.That(result.EndReason, Is.EqualTo(EndReason.HpDepleted));
Assert.That(resultsEmitted, Is.EqualTo(1));
Assert.That(restarted.Health.CurrentHp, Is.EqualTo(3));
Assert.That(loadedAppSessions, Is.EqualTo(1));
```

- [x] Run P fail
- [x] Implement one explicit Update order: cap gameplay dt by remainingSeconds → input/player → objects/spawner resolution → stats → CompleteStep → terminal freeze/clear → presentation delay → Results; terminal transition rejects further queued reports; Ready3s, resume3s freezes all simulation/stats; focus loss pauses and resets input, return requires Resume button (ไม่ auto-resume)
- [x] Author minimal functional scenes and references via Unity Editor APIs; Boot first, MainMenu second, Gameplay third; MainMenu มี temporary demo-profile/Start buttons เรียก AppSession.Select และ SceneNavigator โดยตรงก่อนแทนด้วย Task6; missing configs/references disable Start with named error; editor setup aborts if authored target scenes already exist rather than overwrite manual edits silently; preserve SampleScene
- [x] Run E/P green and automated real-scene core exercise with temporary sprites: 60second survival, early death, restart same selection. Human manual Play unavailable in this harness and retained for Task8; no production visual or manual acceptance claim.
- [x] Commit exact new Flow/scenes/setup/tests/metas และ reviewed scene-list diff: `feat: connect playable round and terminal flow`

## Task 6: Main menu, HUD และ Results

**Files:** Create `Assets/CPS/UI/{MainMenuUI,GameplayUI,ResultPanel,GameplayViewport}.cs`, `Prefabs/{MainMenuCanvas,GameplayCanvas,ResultPanel}.prefab`; Modify three scenes/setup references; Test `Tests/PlayMode/GameUITests.cs`; docs `D/docs/unity-playtest-checklist.md`

**Interfaces:** `MainMenuUI.RefreshProfiles()` / `StartSelectedGame()`; `GameplayUI.Bind(GameSessionController controller)`; `ResultPanel.Show(SessionResult result)`; `GameplayViewport.Apply(int width, int height)` adjusts camera rect and matching canvas viewport; listeners subscribe/unsubscribe with lifetime; consume only copied frozen result

- [x] Tests input name markup stays literal, profile choice duplicate label ID, Thai text, Start requires valid profile/config, result all fields, force unavailable, keyboard navigation, double-click transition single call, viewport1280×720/1920×1080/1024×768/1080×1920

```csharp
Assert.That(nameText.richText, Is.False);
Assert.That(forceLabel.text, Does.Contain("Sensor not connected"));
Assert.That(startSceneCalls, Is.EqualTo(1));
Assert.That(uiRectIsInsideCameraViewport, Is.True);
```

- [x] Run P fail
- [x] Implement uGUI Canvas Scaler1920×1080, anchored viewport, EventSystem with InputSystemUIInputModule; Profile dropdown/create name, Easy/Normal/Hard unlocked; selected panel button focus and warnings; nickname plain text1–24; assignable font slots with temporary Unity font
- [ ] Supplied licensed font: verify actual Thai glyphs/combining marks; do not assume Windows font redistribution rights
- [x] Implement HUD score/HP/time/rank badge/left-right0–100 + Simulated Input; transient rank callout kitchen names and OK/Good/Great/Perfect2/3/4/5, no score multiplier; Pause/Resume; Results displays spec§10/11 every field including input mean/max/pressed/both/balance and physical force unavailable; buttons RESTART/MAIN MENU; dim overlay not blur yet
- [x] Run P green; automated actual UI flow, Thai/markup/duplicate names, missing/zero input stats; playtest checklist with actual Unity screenshots and behavior
- [ ] Manual keyboard-only flow and actual Thai font appearance remain hands-on acceptance in Task8/checklist
- [x] Commit exact UI/prefabs/scene changes/tests/metas: `feat: add menu hud and session results`; docs stay D; reviewed af2fa7c + eab1e00

## Task 7: Production visuals, animation และ ambient kitchen

**Files:** Create `Assets/CPS/Presentation/{MascotPresentation,KitchenAmbient}.cs`, `Art/{Mascot,Kitchen,Items,UI}/` PNG assets, `Animation/Mascot/` clips/controller, `Prefabs/{Mascot,KitchenAmbient,FoodBurst}.prefab`, `Audio/` licensed/original clips; Modify gameplay/UI prefabs/scenes; Test `Tests/PlayMode/PresentationTests.cs`; docs `D/docs/unity-asset-provenance.md`

**Interfaces:** `MascotPresentation.SetMovement(int direction)` / `PlayEat()` / `PlayHurt()` / `PlayTerminal(EndReason reason)` / `ResetPresentation()`; KitchenAmbient only presentation, no game report references; GameSessionController owns end timing, animation cannot change result

- [x] Tests terminal cannot affect score/HP, burst4 decorative objects with no gameplay colliders, reset clears red tint/XX/dissolve, IoTE label doesn't mirror, ambient has no gameplay colliders

```csharp
Assert.That(foodBurstCount, Is.EqualTo(4));
Assert.That(scoreAfterTerminalVfx, Is.EqualTo(frozenScore));
Assert.That(labelTransform.lossyScale.x, Is.GreaterThan(0f));
```

- [x] Run P fail; inspect the four approved references in spec/references before asset work; use imagegen skill for raster creation/editing, never import a concept board as runtime sprite sheet; verify generated frames visually and slice/pivots/alpha before use
- [x] Produce separated usable mascot idle/run/eat/hurt/death/triumph frames, hat/nameplate layer with exact IoTE; original cream mascot/orange engineer hat/feet; kitchen back/mid/front layers,3food/2trash readable silhouettes; motion10–12FPS, ambient8–12FPS with low amplitude/contrast
- [ ] Import original/licensed audio and verify browser first-interaction unlock after the user supplies the selected files
- [x] Implement Triumph1.2s; Death redflash0–.12,fall.12–.40,XX.40–.72,4food arcs.52–.95,pixel/voxel dissolve.72–1.40; VFX time independent from simulation pause/timeScale; UnityEngine.Pool burst; steam/plant/utensils modest; no blur unless measured frame budget allows
- [x] Run P green; capture game/terminal screenshots against reference, inspect both facings every pose, distinct food/trash not color-only; record image provenance in D; generated sprite consistency failure corrected before claiming art complete
- [ ] Verify audio levels and font provenance/Thai shaping after the user supplies production audio/font files
- [x] Commit verified art/presentation/prefabs/scenes/tests/metas: `feat: add mascot animation and animated kitchen`

## Task 8: WebGL delivery, tuning และ regression

**Files:** Create `Assets/CPS/Editor/WebGLBuild.cs`; Modify difficulty assets from measured playtest only and relevant build settings; docs `D/docs/{unity-setup-guide,unity-verification-report}.md`; output `U/Builds/WebGL` (not source commit)

**Interfaces:** `CPS.KitchenCatch.Editor.WebGLBuild.Build()` static entry point: validates three scenes/config, builds WebGL to Builds/WebGL, throws on BuildReport non-success; no auto module installation

- [x] Add editor validation tests `Tests/EditMode/BuildValidationTests.cs` requiring enabled Boot/MainMenu/Gameplay and valid definitions/config, invalid references block build; run E fail then implement validation/build entry point
- [x] Confirm WebGL module installed for exact6000.3.25f1; if missing ask user to add Web Build Support via Unity Hub, continue Editor tests without claiming WebGL verified; [Unity build documentation](https://docs.unity3d.com/6000.3/Documentation/Manual/webgl-building.html)
- [x] Run full E/P green then build (closed checkout Editor; no `-quit` for tests, `-quit` valid for build executeMethod):

```powershell
& $unityExe -batchmode -quit -projectPath $gameRoot -buildTarget WebGL -executeMethod CPS.KitchenCatch.Editor.WebGLBuild.Build -logFile "$verificationRoot\webgl-build.log"
```

- [x] Require BuildReport success/index.html/data/wasm artifacts; serve over HTTP not file URL using available runtime/static server, inspect console/network; play Easy/Normal/Hard to Triumph and forced early Death through test harness (debug harness excluded release), Restart10times, return menu, persistence reload, focus lost/resume, fullscreen and aspect sizes
- [x] Profile actual demo browser/machine for three complete sustained rounds with FPS/frame-time and GC evidence
- [ ] Validate foreground 60 FPS, genuine 30/60 gameplay-speed comparison, and hands-on difficulty feel/user playtest; no tuning was made because headless Chrome ignored the requested frame caps
- [x] Write setup guide with Unity version/modules, opening Boot/Play, keyboard simulation, valid stats/units, build/run steps, current limitations; verification report lists counts/logs/screenshots/browser/machine/test time and gaps explicitly
- [x] Review all requirement coverage, selective commit tested settings/editor/tests/metas: `build: verify and document webgl demo`; do not push/create PR or claim hardware/cloud works

## Self-review และส่งต่อ

Coverage: spec§1–3→Tasks5/6/8; §4→7; §5→1–6; §6→2/5; §7→1/5/7; §8→1/4; §9→1/6; §10→3/6; §11→1/2/5/6; §12→3–8; §13→testsทุก task/8; §14→ลำดับ task นี้ Interfaces ใช้ชนิดและชื่อเดียวกันทั้ง input→stats→result และ object→rules→flow

ข้อจำกัดก่อน execution: production artwork/font/audio ยังไม่มี; ต้องสร้างและตรวจใน Task7 (font สำหรับ Thai UI จัดใน Task6) WebGL module ยังไม่พบ; ไม่ทดสอบ cloud/sensor, ไม่สร้าง MQTT abstraction/queue ก่อนจำเป็น ไม่มีการแก้โค้ด/ตั้งค่าเกมในขั้นจัดทำแผนนี้

หลังผู้ใช้ตรวจแผน ให้เลือก Native (ลงมือใน session นี้) หรือ Subagent-driven (implementer/reviewer แยก task) แนะนำ Native เนื่องจาก task ของ playable core พึ่ง interfaces ต่อกันและมี automated checks ทุกช่วง จากนั้นใช้ execution skill ที่ตรงกับวิธีที่เลือก และตรวจ isolation ก่อน code changes
