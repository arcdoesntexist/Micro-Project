# Binding project constraints for implementers and task reviewers

Copy of approved plan Global Constraints:

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

## User decisions and execution rules

- Approved Subagent-driven; original folder `C:/Users/Arc/Unity/CPS-Project` on new branch `codex/kitchen-catch`. No worktree, no main commits, no merge/push/PR.
- User supplies chosen Font and Audio; Codex designs production artwork. Font/audio not provided yet; leave hooks and truthful pending acceptance, do not download random replacements or synthesize audio without asking. Source artwork at `D/art/production`; production imports only when tasked.
- Read project/reference Markdown before work. Ask controller when requirements unclear. No unnecessary code comments/emojis. Use apply_patch for manual file edits; Unity Editor authoring allowed for generated scenes/config/metadata.
- Preserve all pre-existing edits outside task. Do not use `git add .`, reset/restore/clean. Git metadata writes need `require_escalated` if denied. Stage exact owned files/metas, verify diff, commit only with real test evidence.
- Runtime namespaces `CPS.KitchenCatch`, tests `CPS.KitchenCatch.Tests`. Installed UI assembly name is `UnityEngine.UI`, TMP is `Unity.TextMeshPro` (only reference when used), not `Unity.ugui`.
- Worker/reviewer must not spawn subagents. Only controller dispatches one implementer at a time and independent task reviews.
- TDD: failing executable behavior assertions before bodies. Missing compilation symbols alone don't prove RED; minimal throwing API skeleton okay. Full current suite before commit, focused checks during iteration. Read test-driven-development/writing-good-tests references.
- Run Unity exact installed path `C:/Program Files/Unity/Hub/Editor/6000.3.25f1/Editor/Unity.exe` using `require_escalated`; sandbox startup WMI/license fails, verified by first RED environment failure. Use hidden Start-Process PassThru + WaitForExit for GUI executable; check Unity process locks before run, don't kill user processes.
- Task5 environment observation: normal sandbox Get-Process can hide elevated Unity processes. Process inventory/lock checks and active-run diagnostics must execute inside the escalated launch/diagnostic context. A sandbox-only empty inventory is not evidence that no Unity process exists. Keep all Unity runs strictly serial and source/assets frozen through actual process exit.
- Test command: `-batchmode -projectPath <checkout> -runTests -testPlatform EditMode|PlayMode -testFilter CPS.KitchenCatch.Tests -testResults <unique XML in D/verification> -logFile <unique log in D/verification>`. No `-quit` with tests. Check XML actual totals/failures and compiler logs, distinguish pre-existing package/shutdown noise from new warnings.
- Baseline import/compile exit0; no existing project tests. Baseline shutdown `Curl error42: Callback aborted` recorded separately, not a game exception. Never call zero tests a green suite.
- Ruling1: stats DTOs in Game/InputStatistics.cs created Task1; Task2 adds collection, not duplicate types. Ruling2: Task2 PlayerController.CatchZone Bounds property consumed by spawner. Ruling3: unsafe Easy cyclic wrap delays spawn to preserve feasibility. Ruling4/5: PlayMode asmdef created Task2, Editor asmdef Task4 with its asset-authoring helper as first real source, no dummy empty-assembly silence files.
- Report files (Markdown) and reviews stay `D/docs/superpowers/execution/2026-10-08-unity-kitchen-catch`. Runtime data/results not raw every-frame logs. Full report includes RED/GREEN commands, actual XML counts, files changed, self-review, concerns; final short status only.
