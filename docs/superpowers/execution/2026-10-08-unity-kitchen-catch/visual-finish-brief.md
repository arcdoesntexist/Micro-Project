# Production visual finish handoff

Use after Task7 integration is committed and verified. The independent code review may run concurrently on the same frozen commit; both gates must be resolved before acceptance. This packet does not claim browser acceptance already exists.

## User request and confirmed direction

User: “ผมอยากใส่ Reference Combo จาก Devil may cry 5 เข้าไปด้วยนะครับ แต่ทำใน style cartoony แทน เราจะล็อค theme เป็น ตัวละครเสมือน Kirby ที่อยู่ในครัว วิ่งรับอาหารที่ตกจากฟ้า โดย design ให้เป็นกึ่ว pixel & cartoony ความ smooth เกมให้ 60 fps+ ส่วน animation ให้เป็นแบบ framerate ต่ำเพื่อความเจ๋ง แต่เกมเล่นด้วย fps 60”. The user supplied their own mascot sketch and approved the subsequent design; use the original approved mascot, not ripped game assets.

Further confirmed: tiny orange engineer helmet with exact nonmirrored `IoTE`; death falls with XX eyes, red flash, pixel dissolution and 3–5 food pieces (approved spec fixes four). Results show score/statistics and Restart/Main Menu. One-minute rounds; three HP; trash starts from Normal; equal food selection probabilities and no combo score multiplier. User requested slight background animation. User supplies chosen fonts/audio; they remain pending unless actual supplied files are documented.

## Authority and artifacts

- Unity checkout: `C:/Users/Arc/Unity/CPS-Project`; all documentation: `C:/Users/Arc/Downloads/CPS/Unity_Project` (D).
- Read D/PRODUCT.md, D/docs/unity-ui-direction.md and approved D/docs/superpowers/specs/2026-10-08-unity-kitchen-catch-design.md.
- Four approved concept references in that spec's references directory: character-concept-v2.png, gameplay-mockup-v2.png, combo-ranks-v2.png, death-storyboard-v1.png. Concepts are not runtime screenshots.
- D/docs/unity-asset-provenance.md records six original generated sources and exact prompts; Task7 report records actual imported sprites and runtime evidence.
- Read task-7-report.md for actual capture paths and scope. Check current capture freshness against the reviewed commit, not a filename alone. Task6 placeholder captures cannot establish final visual acceptance.

## Inspection scope

Actual Unity uGUI/URP, desktop WebGL target. Inspect 1920×1080,1280×720,1024×768,1080×1920 and wide2560×1080 layout evidence when available; retain fixed16:9 letterboxing. Inspect menu, gameplay, pause, Results, both mascot facings, eat/hurt/death/triumph frames, helmet lettering, counter/feet/catcher alignment and source/hand HUD separation. Rank callouts must not obscure falling items. Food/trash silhouettes must differ independently of color. Ambient motion is subdued, not competing with gameplay.

Named unresolved checks: Task5 intermittent incorrect first prototype texture; Task6 menu/gameplay text softer than pause/Results at same resolution. Judge current evidence and retain unverified concerns honestly. Static screenshots do not establish animation timing, Thai glyph shaping, licensed audio, browser interaction or 60 FPS.

Use the shipped Impeccable finish reviewer role (the fresh reviewer reads its own definition). Return ship/fix/rebuild/recapture with concrete evidence and bounded fixes; maximum two unattended fix rounds. No HTML/CSS detector findings apply to Unity uGUI. Do not download a substitute font/audio or change the approved visual world. Follow with fresh documenter only when the review outcome permits, storing DESIGN.md and .impeccable/design.json under D, never Markdown in Unity checkout.
