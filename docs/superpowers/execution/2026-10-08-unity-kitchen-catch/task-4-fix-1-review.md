# Task 4 fix round 1 re-review

Scope: the original Important reachability finding and the four-file fix only. Base `78eaf9fc67d62baf43c1474fb44f618bc5aecb27`; head `455c6d4880541667df2755a60af22d7493b68a70`. Reviewed the supplied diff package once, the task brief, binding constraints, appended implementation report and named XML/log evidence. No Git commands, source changes, Unity invocation or subagents.

## Original finding

1. **Configured player bounds can make accepted food impossible to catch.** `Assets/CPS/Objects/DifficultyConfig.cs:23` accepts every lane within +/-7.5, and `Assets/CPS/Objects/FallingObjectSpawner.cs:33` checks catcher finiteness/height but does not check those lanes against the actual player's horizontal range. The concrete dependency is `Assets/CPS/Input/PlayerController.cs:28`: its movement limit is `8 - Clamp(mascotHalfWidth, 0, 8)`, with the width exposed in the Inspector (`PlayerController.cs:10`) and the catcher offset also configurable (`PlayerController.cs:11`, `PlayerController.cs:15`). Set mascotHalfWidth to 3 and retain the default catcher half-width .75: player centers stop at +/-5 and catcher coverage stops at +/-5.75. Begin still accepts the authored Easy lanes, and its first food at -6 is impossible to catch. This violates the brief's runtime rejection of lanes outside the reachable field and invalidates the centered-path guarantee for a configuration the shared validator labels valid. Expose the actual reachable catcher-center range from PlayerController and validate every lane against that range before starting; either adapt the initial path anchor to supported offsets or reject unsupported horizontal geometry with a named error. Add one real component test showing this configuration is rejected. Existing tests use the default player width/offset, so their passing results do not cover this case.

## Finding status

1. **ADDRESSED — configured player reachability.** `Assets/CPS/Input/PlayerController.cs:15` makes the existing clamp calculation a shared producer, and `:16` exposes actual reachable catcher-center endpoints including the Inspector offset. `Assets/CPS/Objects/FallingObjectSpawner.cs:35` consumes those endpoints in the shared validator, rejects nonfinite bounds, rejects unsupported horizontal offsets with a named error, and rejects every lane outside the center range at `:40`. This matches Ruling8's conservative centered-path contract; it does not claim catcher-edge overlap is sufficient.

   `Assets/CPS/Tests/PlayMode/FallingObjectTests.cs:139` reproduces width3 with the real PlayerController, checks actual position5 and catcher edge5.75, and asserts both a named lane validation error and Begin rejection with zero active objects. The adjacent case at `:153` rejects partially covered lane5.5 and accepts boundary lanes±5. Offset±1 and NaN bounds have separate named rejection assertions. `Assets/CPS/Tests/PlayMode/KeyboardPlayerTests.cs:135` verifies literal endpoints for four widths and then drives queued keyboard input to both actual catcher extrema. The assertions cover the concrete dependency rather than only mirroring the new range property.

## Evidence assessment

The appended report names both affected suites and records executable RED output before implementation: missing rejection (`Expected: not null / But was: null`) and the temporary zero getter failing literal endpoint expectations. Actual `verification/task4-fix1-red-play.xml` contains34 tests,25 passes and9 failures, with the five rejection cases and four producer cases listed as failed. Actual `verification/task4-fix1-green-play.xml` reports34/34 passed,0 failed,0 skipped and0 inconclusive: FallingObjectTests16/16 and KeyboardPlayerTests18/18. These names and counts match the report and changed assertions.

The GREEN log search for compiler errors/warnings, exceptions, warnings and Curl errors returned no matches. The search exit1 means no matches, not a failed Unity run. The report records RED Unity exit2 and GREEN exit0. No independent rerun was needed: there is no concrete unanswered code doubt requiring another Unity invocation. The evidence is scoped to these component suites; it is not represented as a new full-project or EditMode run.

## New Breakage

None. The movement clamp retains its previous arithmetic and both movement and reported range now share it. The added rejection is the approved behavior for unsupported geometry; default geometry and configured boundary lanes remain accepted in the covering tests. No new Critical or Important issue was found within the fix diff.

## Out-of-Scope observations

None. The previously deferred Minor food-count assertion is not a finding in this fix round.

## Compliance assessment

Copied approved plan Global Constraints:

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

The fix preserves installed Unity/Input System usage, runtime/test namespaces and existing keyboard movement semantics. Its four paths contain no package, settings, artwork, font/audio, sensor, score, timing or label changes and add no comments/emojis. The report documents serial escalated Unity runs, exact scoped staging and preservation of unrelated edits; this review did not repeat Git inspection. Runtime range validation and the named unsupported-offset rejection follow the brief and Ruling8. Build frame time and later artwork/integration acceptance remain outside this fix review. This review document is the only file written, using apply_patch under the required documentation folder.

## Fix round verdict

**All findings addressed; no new Critical/Important findings.** Accept fix round1.

Not checked: broader Task4 implementation, unrelated suites and build/playtest acceptance were not re-reviewed or rerun.
