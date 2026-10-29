## Task 2: Keyboard, การวิ่ง และสถิติ input

### Execution additions from preflight

Read `constraints.md` beside this brief. Task1 already defines HandStatistics/InputStatistics in Game/InputStatistics.cs and deep copy on SessionResult; do not duplicate types. Read only relevant existing Game DTO/API files to consume exact names.

Ruling2: Add read-only `Bounds PlayerController.CatchZone` from Inspector-defined offset/half extents, stable through facing/animation. Later spawner reads serialized PlayerController reference. Preserve fixed speed7units/s and width16; player visible half-width separate from catch-zone extents. Expose movement direction read-only if presentation needs it, not a test-only helper.

Ruling4: Create `Assets/CPS/Tests/PlayMode/CPS.PlayModeTests.asmdef` now with first PlayMode source; references CPS.Runtime, Unity.InputSystem and installed Unity.InputSystem.TestFramework for InputTestFixture, optionalUnityReferences TestAssemblies. Do not add package dependencies. Verify actual package exports/constraint names before reference.

Keyboard focus loss must return non-pressed invalid input, not contribute time or fabricate force. Game-level pause/resume countdown is Task5, not added here. Missing keyboard returns invalid empty snapshot; valid release strength0 counts in mean. Reject nonfinite duration/strength/force values; no NaN contamination, force validity independent from normalized-strength availability where sensible. Ensure stats return snapshot copies, not mutable collector internals. Tests use real queued Keyboard events with InputTestFixture; no GetAxis legacy input.

**Files:** Create `Assets/CPS/Input/{InputSnapshot,IPlayerInput,KeyboardInput,PlayerController,ForceStatsCollector}.cs`; Test `Tests/EditMode/InputStatsTests.cs`, `Tests/PlayMode/KeyboardPlayerTests.cs`

**Interfaces:** `HandInput(bool pressed, float strength01, bool valid, float? forceN)`; `InputSnapshot(HandInput left, HandInput right, string source)`; `IPlayerInput.Read(): InputSnapshot`; `KeyboardInput` MonoBehaviour implements interface; `PlayerController.Step(InputSnapshot input, float dt)` / `ResetPosition()`; `ForceStatsCollector.Accumulate(InputSnapshot input, float dt)` / `Reset()` / `InputStatistics Snapshot()`
`InputStatistics` serializable DTO has `HandStatistics Left/Right`, `float BothPressedDurationSeconds`, `bool HasBalance`, `float LeftBalance01`; `HandStatistics` has public serializable fields `bool HasStrength/HasForce`, `float MaxStrength01/MeanStrength01/MaxForceN/MeanForceN/ValidDurationSeconds/ForceValidDurationSeconds/PressedDurationSeconds` ไม่พึ่ง nullable fields ที่ JsonUtility ไม่ serialize; null force จาก snapshot กลายเป็น HasForce=false

- [ ] เขียน tests input 4 states, A+Left ปล่อย A ยังวิ่ง, missing keyboard/invalid sample/NaN/negative dt ไม่ทำสถิติพัง, clamp bounds, movement เท่ากันที่30/60/120steps ใช้ `InputTestFixture` จาก package TestFramework reference

```csharp
Assert.That(directionLeftRightBothNeither, Is.EqualTo(new[] { -1, 1, 0, 0 }));
Assert.That(stats.Left.MeanStrength01, Is.EqualTo(0.25).Within(0.001)); // valid 1s pressed + 3s released
Assert.That(stats.Left.HasForce, Is.False);
Assert.That(empty.Left.HasStrength, Is.False);
```

- [ ] Run E/P class filters ให้เห็น fail
- [ ] Implement Read aliases OR ต่อข้าง; source=`Keyboard`; pressed strength1 else0; force absent; movement7units/s, field16units, configurable mascot half-width; แยก invalid duration ของแต่ละมือ, force weighted mean ใช้ force-valid duration เท่านั้น, rejected nonfinite sample ไม่ปนค่าเฉลี่ย
- [ ] Run E/P green รวม mean max, release, both duration, force-valid sample ผสม unavailable, zero balance และ bounds
- [ ] Commit Input/tests/metas: `feat: add keyboard control and input statistics`

