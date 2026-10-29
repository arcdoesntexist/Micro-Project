## Task 6: Main menu, HUD และ Results

### Execution clarifications

Read `constraints.md` and `D/PRODUCT.md`, `D/docs/unity-ui-direction.md` before UI work. The user already approved the warm kitchen world, gameplay composition, flow and original mascot; implement that precise direction rather than reopening identity selection. Main controller ran Impeccable context once; no PRODUCT/DESIGN in Unity checkout because user requires Markdown in D. Read Impeccable skill and required craft-floor immediately before UI edits; do not run another context launcher or create competing Markdown in U. Keep the main controller informed if a real design ambiguity remains.

Task5 early integration ownership: `UI/GameplayViewport.cs` already contains production camera/Canvas matching; extend it instead of creating a duplicate. MainMenu preflight and Gameplay instance share the authored GameplayRig prefab, which remains reference/geometry/config authority; apply relevant edits to that prefab, not only Gameplay scene overrides. Replace/remove Task5's tiny prototype menu/HUD components with this task's production UI, retaining Boot routing and shared validator behavior. See task-5-report.md for exact resulting interfaces/files.

Update existing Task5 scene-flow fixtures to drive the production UI paths when replacing prototype components, preserving their real scene/input/spawner/terminal/restart coverage rather than deleting tests or bypassing navigation. Keep the shared-template serialization check in Editor preview scenes, where prefab metadata exists; PlayMode checks actual behavior. Existing capture uses authored ScreenSpaceCamera and supported URP render request, not an altered screenshot-only layout.

User-selected font is pending: wire an assignable licensed-font slot and use temporary Unity font only, no unsolicited font download/copy from Windows. Tests can confirm literal Thai strings and markup setting, but Thai glyph/combining-mark visual acceptance must be reported pending. Use the approved actual reference for hierarchy; short rank callouts must not obscure falling-object play. Capture playable UI screenshots through Unity if browser build unavailable, and label environment and placeholder art honestly. Final production visual review belongs after Task7 artwork integration.

Nickname limit is Unicode text elements from ProfileStore, not UTF-16 code units. Do not set an InputField/TMP characterLimit of24 that would reject a legal24-grapheme Thai/combining-mark name early. Keep one validation authority and test the actual creation field path with combining-mark input. Disable rich text on every displayed nickname/dropdown/result label, not only one test label.

**Files:** Create `Assets/CPS/UI/{MainMenuUI,GameplayUI,ResultPanel,GameplayViewport}.cs`, `Prefabs/{MainMenuCanvas,GameplayCanvas,ResultPanel}.prefab`; Modify three scenes/setup references; Test `Tests/PlayMode/GameUITests.cs`; docs `D/docs/unity-playtest-checklist.md`

**Interfaces:** `MainMenuUI.RefreshProfiles()` / `StartSelectedGame()`; `GameplayUI.Bind(GameSessionController controller)`; `ResultPanel.Show(SessionResult result)`; `GameplayViewport.Apply(int width, int height)` adjusts camera rect and matching canvas viewport; listeners subscribe/unsubscribe with lifetime; consume only copied frozen result

- [ ] Tests input name markup stays literal, profile choice duplicate label ID, Thai text, Start requires valid profile/config, result all fields, force unavailable, keyboard navigation, double-click transition single call, viewport1280×720/1920×1080/1024×768/1080×1920

```csharp
Assert.That(nameText.richText, Is.False);
Assert.That(forceLabel.text, Does.Contain("Sensor not connected"));
Assert.That(startSceneCalls, Is.EqualTo(1));
Assert.That(uiRectIsInsideCameraViewport, Is.True);
```

- [ ] Run P fail
- [ ] Implement uGUI Canvas Scaler1920×1080, anchored viewport, EventSystem with InputSystemUIInputModule; Profile dropdown/create name, Easy/Normal/Hard unlocked; selected panel button focus and warnings; nickname plain text1–24; TMP/UI font includes actual Thai glyphs and combining marks, ตรวจ font license ก่อนนำเข้า ไม่ใช้ font จาก Windows โดยสมมติว่าแจกจ่ายได้
- [ ] Implement HUD score/HP/time/rank badge/left-right0–100 + Simulated Input; transient rank callout kitchen names and OK/Good/Great/Perfect2/3/4/5, no score multiplier; Pause/Resume; Results displays spec§10/11 every field including input mean/max/pressed/both/balance and physical force unavailable; buttons RESTART/MAIN MENU; dim overlay not blur yet
- [ ] Run P green และ manual keyboard-only flow, Thai/markup/duplicate names, zero input stats; เริ่ม playtest checklist ด้วย verified screenshots และ behavior ไม่ใช้ concept sheet แทน screenshot
- [ ] Commit exact UI/prefabs/scene changes/tests/metas: `feat: add menu hud and session results`; docs stay D

