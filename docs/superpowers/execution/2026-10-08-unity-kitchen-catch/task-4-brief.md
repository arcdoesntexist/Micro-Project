## Task 4: วัตถุตก, pooling และ spawn ที่เล่นได้

### Execution clarifications

Read `constraints.md` beside brief. Use Task1 enums and Task2 PlayerController.CatchZone read-only Bounds; FallingObjectSpawner holds Inspector player/prefab/definition references (no DI/event bus). Own Step drives pooled objects; individual objects do not independently Update or use physical triggers to report duplicate outcomes. Fresh spawn ID guards stale callbacks across reuse.

Food reachability is mandatory, including Easy right→left wrap: keep cyclic lane order and defer unsafe wrap until enough arrival-time gap (Ruling3), never silently skip to a different lane merely to pass a test. Both kinds of candidates are checked against active future arrival plans; reject trash/food overlaps around required catch windows. A conservative protected travel corridor between consecutive required food catches is acceptable if it proves at least one safe movement path; bounded retries and delay instead of an impossible spawn, no AI planner framework. Difficulty trash probability is proposal probability before safety rejection; food definition choice remains uniform among3foods.

For authored configs/prefab, permitted Editor helper `Assets/CPS/Editor/GameObjectAssetSetup.cs` with explicit `CreateMissing()` entry point, creating only missing Task4 targets via Unity APIs; never silently overwrite existing edited assets. Create CPS.Editor.asmdef with this first actual Editor source, rather than waiting Task5 (Ruling4 purpose: no empty assemblies). Task5 GameSceneSetup may invoke CreateMissing to ensure required assets. No generated dummy MonoBehaviour, no manual unrelated project settings edits. Runtime validation rejects nonfinite/nonpositive speed/interval, invalid trash probability and lanes outside reachable field.

**Files:** Create `Assets/CPS/Objects/{DifficultyConfig,FallingObjectDefinition,SpawnPolicy,FallingObjectSpawner,FallingObject}.cs`; assets `Config/{Easy,Normal,Hard,Apple,Bread,Cake,Can,BananaPeel}.asset`, `Prefabs/FallingObject.prefab`; Test `Tests/EditMode/SpawnPolicyTests.cs`, `Tests/PlayMode/FallingObjectTests.cs`

**Interfaces:** `DifficultyConfig` SO speed/interval/trashProbability/lanes; `FallingObjectDefinition` SO kind/points/sprite; `SpawnPolicy.TryChoose(float now, float playerSpeed, float catchHalfWidth, out SpawnChoice choice): bool`, constructor config+seed; `Spawner.Begin(DifficultyConfig config, Action<long,FallingObjectDefinition,ObjectOutcome> report)` / `Step(float dt)` / `StopAndClear()`; `FallingObject.Spawn(long id, FallingObjectDefinition definition, Vector2 position, float speed)` / `Step(float dt, Bounds catchZone)` / `Despawn()`

- [ ] Tests Easy no trash, seed reproducibility, food same selection weight, feasible adjacent food arrivals, trash separated from required food catch window; pooled object old spawn cannot resolve new spawn; coarse step crossing catcher counts catch once not miss

```csharp
Assert.That(easySpawnKinds.All(k => k == ObjectKind.Food), Is.True);
Assert.That(requiredTravel, Is.LessThanOrEqualTo(7f * arrivalGap));
Assert.That(reportCountForSpawn, Is.EqualTo(1));
Assert.That(activeCountAfterClear, Is.EqualTo(0));
```

- [ ] Run E/P fail
- [ ] Implement speed/interval/trash: Easy2/1.40/0, Normal2.8/1.10/.15, Hard3.5/.85/.25; spawn-to-catch7units; Easy lanes[-6,0,6] cyclic; Normal[-6,-3,0,3,6], Hard[-6,-4,-2,0,2,4,6]; food types uniform, trash variants uniform
- [ ] Implement simple candidate rejection using previous food arrival reachable distance and expanded catch windows against active food/trash; if no safe candidate defer spawn, ไม่ฝืน spawn; bounded attempts, max16; validate config speed/interval/lanes; UnityEngine.Pool resets all state/callbacks with fresh monotonically increasing spawnId; swept segment catch-zone check before miss (ไม่อาศัย trigger ที่อาจทะลุระหว่างเฟรม)
- [ ] Run E/P green และ deterministic stress1000spawns checks bounds/travel/conflicts; 30FPS large-step catch, duplicate reporting และ Stop/reuse
- [ ] Commit Objects/config/prefab/tests/metas: `feat: add safe pooled falling objects`

