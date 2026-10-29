## Task 7: Production visuals, animation และ ambient kitchen

### Execution clarifications

Read `constraints.md`, `D/docs/unity-ui-direction.md` and `D/docs/unity-asset-provenance.md`. Controller produced five native-imagegen source PNGs in `D/art/production`; use these as production sources, not the approved concept boards. Inspect all sources and reference images before importing. Slice through Unity importer APIs; align foot pivots and per-pose hat anchors by real alpha bounds/visual checks, not the centre of padded cells. Leave exact IoTE lettering nonmirrored, including every facing and dead pose. Correct actual consistency failures before claiming visual acceptance; ask controller if raster regeneration is needed. Manual code edits via apply_patch; image pixel edits only via imagegen, not Python or System.Drawing painting.

User-selected Font/Audio are pending. Wire optional clip/font references, first-interaction audio unlocking and silent/no-clip-safe hooks; do not fetch/synthesise substitutes. Report actual audio levels and Thai font acceptance as pending unless supplied licensed files arrive. No blur or extra shader framework. Preserve gameplay object IDs/catch zones/result authority when swapping sprites. Provenance already contains exact prompts; append Unity import/verification evidence without replacing generation history.

All five production source PNGs now contain exact generation prompts as PNG metadata, verified by Impeccable scan/read on9Oct. Preserve metadata when copying into Unity; use the original PNG with Sprite importer rectangles rather than unnecessarily re-encoding it. Root performs final shipping-raster provenance scan and fresh visual finish review after integration. No image pixels were changed by provenance embedding.

Sixth source added9Oct: `D/art/production/ui-panel-v1.png` (1774×887) is a generated empty cream panel with wood rim from the approved kitchen UI reference. Inspect it and use a cropped Sprite rect with nine-slice borders for selected main UI/HUD panels; keep live labels/buttons semantic, avoid framing every nested stat. This supplies the approved warm panel material while the user's display font is pending. Exact prompt and alpha notes are in provenance; all6 source PNGs now have embedded prompts.

Task4 geometry correction rejects horizontal catch-zone offsets because its protected path starts centrally. Keep gameplay catchZoneOffset.x zero; align body/hat visuals through child renderer pivots/anchors, not by shifting the physical catcher. Update mascotHalfWidth to actual production body footprint and verify authored lanes remain inside PlayerController's actual reachable catcher-center range before claiming bounds acceptance.

Task6 earlier placeholder captures showed the old player overlapping the bottom-center Simulated Input label; final delivery captures now have a gap. When integrating the kitchen, align the running feet with the real counter surface and keep the hand indicators/source label below that plane as the approved reference specifies. Adjust authored vertical player/catcher geometry coherently with the production footprint; verify falling-object start/catch/miss behavior after it. Do not preserve the placeholder's vertical placement merely because its horizontal range tests pass.

Task6 delivery captures (`D/verification/task6-captures-delivery`) show menu/gameplay text visibly softer than pause/Results at the same1920×1080 resolution. An extra dynamic-font frame did not establish a cause or resolve it. Check actual rendering during production integration and preserve the observation in final QA if unresolved; do not silently claim it fixed or alter user URP settings speculatively. User-selected font is still pending.

Task5's GameplayRig prefab is shared by MainMenu validation and Gameplay. Apply production visual/geometry/reference edits to that shared prefab, with no relevant Gameplay scene-only overrides. Replace Task5 temporary sprites/cloned definitions with production originals and remove owned obsolete prototype assets when unreferenced; preserve exact kind/points/config values. Read task-5-report.md for actual ownership and paths.

Task5 recorded an intermittent first URP capture showing the wrong prototype player texture, followed by a correct capture and a passing live pixel assertion without changing the material. The shader explanation was not proved. Retain this as a named render-risk check when integrating separate body/hat/items textures; do not silently claim it fixed or change user URP settings. Adapt prototype-specific render assertions to meaningful production-asset checks, retaining actual capture/component coverage instead of dropping it. Inspect all submitted captures for validity before visual review.

**Files:** Create `Assets/CPS/Presentation/{MascotPresentation,KitchenAmbient}.cs`, `Art/{Mascot,Kitchen,Items,UI}/` PNG assets, `Animation/Mascot/` clips/controller, `Prefabs/{Mascot,KitchenAmbient,FoodBurst}.prefab`, `Audio/` licensed/original clips; Modify gameplay/UI prefabs/scenes; Test `Tests/PlayMode/PresentationTests.cs`; docs `D/docs/unity-asset-provenance.md`

**Interfaces:** `MascotPresentation.SetMovement(int direction)` / `PlayEat()` / `PlayHurt()` / `PlayTerminal(EndReason reason)` / `ResetPresentation()`; KitchenAmbient only presentation, no game report references; GameSessionController owns end timing, animation cannot change result

- [ ] Tests terminal cannot affect score/HP, burst4 decorative objects with no gameplay colliders, reset clears red tint/XX/dissolve, IoTE label doesn't mirror, ambient has no gameplay colliders

```csharp
Assert.That(foodBurstCount, Is.EqualTo(4));
Assert.That(scoreAfterTerminalVfx, Is.EqualTo(frozenScore));
Assert.That(labelTransform.lossyScale.x, Is.GreaterThan(0f));
```

- [ ] Run P fail; inspect the four approved references in spec/references before asset work; use imagegen skill for raster creation/editing, never import a concept board as runtime sprite sheet; verify generated frames visually and slice/pivots/alpha before use
- [ ] Produce separated usable mascot idle/run/eat/hurt/death/triumph frames, hat/nameplate layer with exact IoTE; original cream mascot/orange engineer hat/feet; kitchen back/mid/front layers,3food/2trash readable silhouettes; motion10–12FPS, ambient8–12FPS with low amplitude/contrast; original/legal audio unlocked by first user interaction in browser
- [ ] Implement Triumph1.2s; Death redflash0–.12,fall.12–.40,XX.40–.72,4food arcs.52–.95,pixel/voxel dissolve.72–1.40; VFX time independent from simulation pause/timeScale; UnityEngine.Pool burst; steam/plant/utensils modest; no blur unless measured frame budget allows
- [ ] Run P green; capture game/terminal screenshots against reference, inspect both facings every pose, distinct food/trash not color-only, audio levels; record image/audio/font provenance/license in D; generated sprite consistency failure must be corrected before claiming art complete
- [ ] Commit verified art/presentation/prefabs/scenes/tests/metas: `feat: add mascot animation and animated kitchen`

