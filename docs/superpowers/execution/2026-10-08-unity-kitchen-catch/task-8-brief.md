## Task 8: WebGL delivery, tuning และ regression

**Files:** Create `Assets/CPS/Editor/WebGLBuild.cs`; Modify difficulty assets from measured playtest only and relevant build settings; docs `D/docs/{unity-setup-guide,unity-verification-report}.md`; output `U/Builds/WebGL` (not source commit)

**Interfaces:** `CPS.KitchenCatch.Editor.WebGLBuild.Build()` static entry point: validates three scenes/config, builds WebGL to Builds/WebGL, throws on BuildReport non-success; no auto module installation

- [ ] Add editor validation tests `Tests/EditMode/BuildValidationTests.cs` requiring enabled Boot/MainMenu/Gameplay and valid definitions/config, invalid references block build; run E fail then implement validation/build entry point
- [ ] Confirm WebGL module installed for exact6000.3.25f1; if missing ask user to add Web Build Support via Unity Hub, continue Editor tests without claiming WebGL verified; [Unity build documentation](https://docs.unity3d.com/6000.3/Documentation/Manual/webgl-building.html)
- [ ] Run full E/P green then build (closed checkout Editor; no `-quit` for tests, `-quit` valid for build executeMethod):

```powershell
& $unityExe -batchmode -quit -projectPath $gameRoot -buildTarget WebGL -executeMethod CPS.KitchenCatch.Editor.WebGLBuild.Build -logFile "$verificationRoot\webgl-build.log"
```

- [ ] Require BuildReport success/index.html/data/wasm artifacts; serve over HTTP not file URL using available runtime/static server, inspect console/network; play Easy/Normal/Hard to Triumph and forced early Death through test harness (debug harness excluded release), Restart10times, return menu, persistence reload, focus lost/resume, fullscreen and aspect sizes
- [ ] Profile actual demo browser/machine 3 complete rounds with FPS/frame time and GC evidence; compare gameplay speed at30/60FPS; target60FPS and ~16.7ms, no guarantees for background tabs; tune difficulty and particles only with recorded results and rerun affected tests; user playtest validates safe spawn isn't boring or impossible
- [ ] Write setup guide with Unity version/modules, opening Boot/Play, keyboard simulation, valid stats/units, build/run steps, current limitations; verification report lists counts/logs/screenshots/browser/machine/test time and gaps explicitly
- [ ] Review all requirement coverage, selective commit tested settings/editor/tests/metas: `build: verify and document webgl demo`; do not push/create PR or claim hardware/cloud works

## Self-review และส่งต่อ

Coverage: spec§1–3→Tasks5/6/8; §4→7; §5→1–6; §6→2/5; §7→1/5/7; §8→1/4; §9→1/6; §10→3/6; §11→1/2/5/6; §12→3–8; §13→testsทุก task/8; §14→ลำดับ task นี้ Interfaces ใช้ชนิดและชื่อเดียวกันทั้ง input→stats→result และ object→rules→flow

ข้อจำกัดก่อน execution: production artwork/font/audio ยังไม่มี; ต้องสร้างและตรวจใน Task7 (font สำหรับ Thai UI จัดใน Task6) WebGL module ยังไม่พบ; ไม่ทดสอบ cloud/sensor, ไม่สร้าง MQTT abstraction/queue ก่อนจำเป็น ไม่มีการแก้โค้ด/ตั้งค่าเกมในขั้นจัดทำแผนนี้

หลังผู้ใช้ตรวจแผน ให้เลือก Native (ลงมือใน session นี้) หรือ Subagent-driven (implementer/reviewer แยก task) แนะนำ Native เนื่องจาก task ของ playable core พึ่ง interfaces ต่อกันและมี automated checks ทุกช่วง จากนั้นใช้ execution skill ที่ตรงกับวิธีที่เลือก และตรวจ isolation ก่อน code changes
