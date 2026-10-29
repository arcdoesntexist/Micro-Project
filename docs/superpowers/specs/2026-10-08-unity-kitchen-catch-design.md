# Unity Kitchen Catch — Design Specification

วันที่: 8 ตุลาคม 2026

สถานะ: ผู้ใช้อนุมัติให้ไปขั้น implementation plan วันที่ 8 ตุลาคม 2026

โปรเจกต์: `C:\Users\Arc\Unity\CPS-Project`

## 1. เป้าหมายและขอบเขต

สร้างเกม 2D สำหรับวิชา Cyber-Physical System and Sensor ให้นักศึกษาและอาจารย์ทดลองควบคุมมาสคอตด้วยมือซ้าย–ขวา รับอาหารและหลบขยะ ภายในหนึ่งรอบ 60 วินาที แสดงคะแนนและสถิติหลังเล่น

เฟสนี้โฟกัส Unity ที่เล่นจบรอบได้จริง โดยทดสอบด้วย Keyboard และ build เป็น WebGL มีขอบเขตดังนี้:

- สร้าง/เลือก Profile ชื่อเล่น ไม่ใช้รหัสผ่าน และเลือก Easy / Normal / Hard ได้ทันที
- เคลื่อนที่ซ้าย–ขวา รับอาหาร 3 ชนิด หลบขยะ คะแนน HP และ Combo Rank
- Triumph เมื่ออยู่รอดครบเวลา และ Death Animation เมื่อ HP หมด
- Result Panel พร้อม Restart / Main Menu
- ใช้ภาพกึ่ง pixel และ cartoony ตาม mockup ที่อนุมัติ พร้อม ambient animation ในครัว
- เก็บข้อมูลรอบและข้อมูล input เป็นรูปแบบที่ต่อเซนเซอร์ภายหลังได้

HiveMQ, MQTT client, Node-RED, Google Sheets, ESP32, FSR และการสอบเทียบจริงเป็นเฟสถัดไป เฟสนี้สร้างเพียงจุดรับ input และข้อมูลผลรอบที่มีขอบเขตเล็ก ไม่สร้าง transport หรือ backend จำลอง

เกณฑ์สำเร็จ: เข้าเกมจากเมนู เล่นทั้ง 3 ระดับ จบได้ทั้ง Triumph/Death ดูผล Restart และกลับเมนูได้โดยไม่มี state จากรอบก่อนหลงเหลือ และเล่นบน WebGL ได้

## 2. ข้อตกลงและค่าเริ่มต้น

ผู้ใช้อนุมัติธีม ตัวละคร การควบคุม กติกา HP/เวลา คะแนน Profile Result Panel และโครงสร้าง Lean Modular พร้อม Triumph แล้ว

ตัวเลขความเร็ว อัตราขยะ ระยะเวลา Triumph เกณฑ์ Rank ระดับสูง และรายละเอียดการบันทึก Profile ด้านล่างเป็นค่าเริ่มต้นที่เสนอในเอกสารฉบับนี้ ปรับตาม playtest ได้ โดยรักษากติกาหลัก

## 3. Unity และพื้นที่แสดงผล

- Unity `6000.3.25f1` (6.3 LTS), Universal 2D, URP และ Input System ที่ติดตั้งอยู่
- WebGL สำหรับ Desktop Browser แนวนอน อ้างอิง 1920×1080
- Camera แบบ orthographic พื้นที่เกมอัตราส่วน 16:9 คงที่ ใช้ letterbox/pillarbox เมื่อหน้าจอสัดส่วนต่างออกไป
- HUD ใช้ Canvas Scaler และ anchors ภายในพื้นที่ 16:9 ไม่วางปุ่มนอก camera viewport
- เป้าหมาย gameplay/rendering 60 FPS บนเครื่องที่ใช้สาธิต; ตรวจ frame time เป้าหมายประมาณ 16.7 ms ใน build จริง
- Sprite animation ตัวละครประมาณ 10–12 FPS และบางฉากหลัง 8–12 FPS ขณะที่การเคลื่อนที่และอนุภาคคำนวณตามเวลา ไม่ผูกความเร็วกับ frame rate
- ไม่รับประกัน 60 FPS ทุกเครื่องหรือเมื่อ browser tab อยู่เบื้องหลัง

## 4. Visual และ animation

ตัวละครเป็นมาสคอตต้นฉบับทรงกลมสีครีม แขนเล็ก เท้าส้ม ปากใหญ่ หมวกนิรภัยวิศวกรจิ๋วสีส้ม ป้าย `IoTE` คงตัวพิมพ์ตามนี้เสมอ

Reference ที่บันทึกไว้คู่เอกสารในโฟลเดอร์ CPS:

- [Character v2](references/character-concept-v2.png)
- [Gameplay v2](references/gameplay-mockup-v2.png)
- [Combo v2](references/combo-ranks-v2.png)
- [Death storyboard](references/death-storyboard-v1.png)

ภาพเหล่านี้เป็น concept/reference ไม่ใช่ sprite sheets ที่นำเข้าเกมได้ทันที ต้องเตรียม sprite แยก ฉากแยก layer และภาพ animation ที่ใช้จริงในช่วงพัฒนา

Animation หลัก: Idle, Run Left/Right, Eat, Hurt, Death และ Triumph การหันซ้ายใช้การ flip ตัวละครได้ แต่ป้าย `IoTE` ต้องไม่กลับด้าน จึงแยกป้าย/หมวกหรือทำ facing sprites ให้เหมาะสม

### Ambient kitchen

ใช้ฉากครัวเดิมและ animation จำนวนเล็กน้อย: ไอน้ำจากหม้อ ใบไม้ไหว และเครื่องครัวแกว่งเบา ๆ แสง/ฝุ่นเพิ่มได้เมื่อยังผ่านงบ performance วัตถุเหล่านี้ไม่มี Collider และไม่เข้าสู่การคิดคะแนน

ลด contrast และ saturation ของ background เทียบกับอาหาร/ขยะ ไม่ใช้เอฟเฟกต์ที่คล้ายวัตถุตกในพื้นที่เล่น โดยคง motion amplitude ต่ำ ใช้ Animation Clips แบบ loop หรือ Particle System ของ Unity

เริ่มด้วยฉากครัวหนึ่งแบบตาม reference เกณฑ์ต้นทางอนุญาตฉาก 1–3 แบบ จึงไม่จำเป็นต้องสร้าง 3 แบบในเฟสนี้

## 5. Scenes และหน้าที่ component

| Scene / Component | หน้าที่ | ข้อมูลที่ใช้ / ส่งออก |
|---|---|---|
| Boot / AppSession | โหลด Profile และเก็บตัวเลือกข้าม Scene | selected playerId, displayName, difficulty |
| MainMenu | สร้าง/เลือก Profile เลือกระดับ และเริ่มเกม | ส่งตัวเลือกให้ AppSession |
| Gameplay / GameSessionController | ควบคุม state เวลา เริ่ม/จบรอบ และผลสุดท้าย | sessionId, endReason, immutable result |
| PlayerController | แปลงซ้าย/ขวาเป็นการเคลื่อนที่และจำกัดขอบสนาม | input snapshot และ move speed |
| KeyboardInput | อ่าน A/D และลูกศรเป็น input ซ้าย/ขวา | pressed และ normalized strength แบบจำลอง |
| FallingObjectSpawner | ปล่อยวัตถุตาม Difficulty และจัดการ pool | DifficultyConfig, object definitions |
| FallingObject | เคลื่อนที่และรายงาน catch/miss เพียงครั้งเดียว | definition และ outcome |
| HealthSystem | ลด HP และแจ้ง HP หมด | 3 HP เริ่มต้น, changed/depleted |
| ScoreComboSystem | คะแนน streak max combo และ rank | food points และ catch/miss/trash events |
| ForceStatsCollector | สรุป input ที่ valid เฉพาะช่วง Playing | count, duration, max, mean, source/units |
| GameplayUI / ResultPanel | แสดง HUD และผล พร้อมปุ่ม navigation | อ่าน state และ frozen result |
| Mascot presentation | เล่น sprite animation เสียงและ VFX | movement, catch, hurt, terminal state |

ใช้ component references ที่กำหนดใน Inspector และ event ภายในขอบเขตที่จำเป็น ไม่ใช้ global event bus หรือ DI framework ใช้ ScriptableObject สำหรับ `DifficultyConfig` และ `FallingObjectDefinition` เท่านั้น

AppSession มี instance เดียวข้าม Scene การเปลี่ยน Scene ต้องไม่สร้าง duplicate ตอน Restart สร้าง gameplay session ใหม่และคืน/reset pooled objects ทั้งหมด

## 6. Input และการเคลื่อนที่

| ซ้าย | ขวา | การกระทำ |
|---|---|---|
| กด | ปล่อย | วิ่งซ้าย |
| ปล่อย | กด | วิ่งขวา |
| ปล่อย | ปล่อย | หยุด |
| กด | กด | หยุด |

Keyboard: `A` หรือ Left Arrow สำหรับซ้าย, `D` หรือ Right Arrow สำหรับขวา กด key ใดในกลุ่มนั้นถือว่าข้างนั้นกดอยู่ ปล่อยหนึ่ง key ขณะที่อีก key ในกลุ่มยังค้างไม่ทำให้หยุดข้างนั้น

ความเร็วตัวละครคงที่ทุก Difficulty เพื่อให้เรียนรู้การควบคุมได้สม่ำเสมอ ค่าเริ่มต้น 7 world units/s สนามกว้าง 16 units จำกัดตำแหน่งโดยเผื่อความกว้างของตัวละคร รับวัตถุเมื่อชน catch zone บนตัวละครโดยไม่ต้องกดกินเพิ่ม

จุดรับ input ให้ snapshot ซ้าย/ขวาที่มี pressed, strength01, optional forceN, validity และ source ค่า `forceN` ไม่มีเมื่อเป็น Keyboard ภายหลัง MQTT adapter ส่ง snapshot ผ่านจุดเดิมได้ ไม่เพิ่ม MqttInput ในเฟสนี้

เมื่อหน้าต่าง/แท็บเสีย focus ให้หยุด input และพักเวลา gameplay กับ spawner เมื่อกลับมาให้มี Resume และ countdown สั้นก่อนเล่นต่อ ไม่นับช่วงพักเข้าเวลาเล่นหรือสถิติ

## 7. รอบเกมและลำดับ state

`Ready → Playing → Triumph หรือ Dying → Results`

- Ready: reset HP/score/combo/timer/pools/stats, วางตัวละครกลางสนาม, countdown 3–2–1
- Playing: เวลาเล่นจริง 60 วินาที รับ input ปล่อยวัตถุ รับอาหาร/ขยะ และสะสมสถิติ
- Triumph: เวลา 60 วินาทีครบและ HP > 0; หยุด gameplay ตรึง result แล้วฉลอง
- Dying: HP = 0; หยุด gameplay ตรึง result แล้วเล่น Death Animation
- Results: เปิด Result Panel บนฉากเดิมที่มืดลง พร้อม Restart และ Main Menu

HP = 0 มี priority เหนือ timeout หากเหตุการณ์เกิดใน simulation step เดียวกัน Terminal transition เกิดได้เพียงหนึ่งครั้ง จำนวนครั้งเล่นเพิ่มหนึ่งครั้งต่อ result ของ sessionId นั้น

วัตถุในอากาศที่ค้างเมื่อเวลาหมดไม่ถือเป็น Food Missed เอาผลเฉพาะเหตุการณ์ที่ resolve ระหว่าง Playing ผลที่ตรึงแล้วไม่เปลี่ยนตาม animation และ food burst

Restart ใช้ Profile/Difficulty เดิมและผ่าน Ready/countdown ใหม่ Main Menu กลับไปแก้ Profile และ Difficulty ได้

### Triumph presentation

ค่าเริ่มต้นประมาณ 1.2 วินาที: มาสคอตยิ้ม กระโดดสั้นหนึ่งครั้ง พร้อมประกาย/เศษกระดาษเล็กน้อย แล้วเปิด Result Panel ไม่มีคะแนนหรือ HP เพิ่มจากการฉลอง

### Death presentation

| เวลาโดยประมาณ | ภาพ / effect |
|---|---|
| 0.00–0.12 s | hit-stop และ body red flash; input/gameplay ถูกหยุดแล้ว |
| 0.12–0.40 s | เสียสมดุลและล้มแบบ squash-and-stretch |
| 0.40–0.72 s | นอนตา X X ให้เห็นสถานะชัด |
| 0.52–0.95 s | อาหารตกแต่ง 4 ชิ้นกระเด็นเป็น arc |
| 0.72–1.40 s | ตัวและหมวกสลายเป็นอนุภาคก้อนพิกเซล แล้วจาง |
| 1.40 s | เปิด Result Panel |

อาหาร burst เป็นของตกแต่ง ใช้รูปอาหารในเกม จำนวน 4 อยู่ในช่วง 3–5 ที่ตกลงไว้ ไม่มีคะแนนหรือ Collider ที่ทำความเสียหาย ใช้การสลายสี่เหลี่ยมต้นฉบับและโทนตลก ไม่มีเลือด

## 8. อาหาร ขยะ HP และ Difficulty

เริ่ม 3 HP รับ Apple = 10, Bread = 15, Cake = 20 คะแนน ไม่มีตัวคูณ Combo ผู้เล่นควรรับอาหารทุกชนิด

- รับอาหาร: เพิ่มคะแนน เพิ่ม streak และเล่น Eat
- อาหารผ่านเส้น miss ด้านล่าง: เสีย 1 HP และ reset streak
- รับขยะ: เสีย 1 HP และ reset streak
- ขยะผ่านเส้น miss: ไม่เสีย HP/คะแนน และไม่ตัด Combo
- วัตถุหนึ่งชิ้นคิด outcome ได้ครั้งเดียว จากนั้นคืน pool
- เมื่อได้รับความเสียหายยังแสดง Hurt/red flash สั้น โดยไม่เพิ่ม invulnerability ที่ยกเว้นโทษของอาหารชิ้นอื่น

ค่า tuning เริ่มต้น (ต้อง playtest): spawn-to-catch distance ประมาณ 7 units ทุกวัตถุตกตรงในเฟสแรก

| Difficulty | ความเร็วตก | ระยะระหว่าง spawn | โอกาสขยะ | Pattern |
|---|---:|---:|---:|---|
| Easy | 2 units/s | 1.40 s | 0% | วนตำแหน่งซ้าย–กลาง–ขวาแบบคาดเดาได้ |
| Normal | 2.8 units/s | 1.10 s | 15% | สุ่มตำแหน่งจาก lane ที่กำหนด |
| Hard | 3.5 units/s | 0.85 s | 25% | สุ่มถี่ขึ้นและใช้ตำแหน่งหลากหลายกว่า |

อาหาร 3 ชนิดมีโอกาสเท่ากันเมื่อ spawn เป็นอาหาร ขยะเริ่ม 2 รูป: กระป๋องยับและเปลือกกล้วย มีรูปทรง/สีแยกจากอาหารได้โดยไม่อาศัยสีอย่างเดียว

Spawner ต้องป้องกันลำดับอาหารที่ต้องวิ่งเกินระยะที่ทำได้ก่อน catch ครั้งถัดไป และหลีกเลี่ยงอาหาร/ขยะที่ถึง catch zone พร้อมกันจนต้องรับทั้งคู่ ใช้ระยะ catch zone, move speed และเวลาถึงพื้นประกอบการเลือกตำแหน่ง ไม่จำเป็นต้องใช้ระบบ AI pattern generator

## 9. Combo Rank

Combo ใช้จำนวนอาหารที่รับต่อเนื่อง ไม่เพิ่มคะแนนตัวคูณ เกณฑ์เริ่มต้น:

| Streak | Rank | Callout |
|---:|---|---|
| 0–1 | D | Dull Dish |
| 2 | C | Cookin' |
| 3 | B | Bold Bite |
| 4 | A | Appetizing! |
| 5–7 | S | Sizzling! |
| 8–11 | SS | Super Savory! |
| 12+ | SSS | Supreme Chef! |

แสดง HUD rank badge ขนาดเล็กต่อเนื่อง และ callout/เสียงสั้นเมื่อขึ้น Rank จำกัดอายุและพื้นที่ callout เพื่อไม่บังวัตถุตก ใช้คำชมตาม rubric ที่ streak 2/3/4/5 เช่น OK/Good/Great/Perfect ร่วมกับ Rank ผ่านข้อความรอง

พลาดอาหารหรือรับขยะ reset streak เป็น 0 แต่เก็บ maxCombo และ highestRank ของรอบไว้ ไม่เพิ่ม rank decay ตามเวลาหรือการเปลี่ยนชนิดอาหาร เพราะผู้เล่นมีหน้าที่รับวัตถุที่ตกมา

## 10. Profile และ Result Panel

Profile มี playerId ที่ระบบสร้าง และ displayName ซึ่งผู้เล่นกรอก ตัดช่องว่างหัวท้าย รับ 1–24 ตัวอักษร แสดงเป็น plain text และปิด rich-text interpretation ในชื่อ รองรับชื่อไทยด้วย font ที่มี glyph จริง

ชื่อซ้ำได้ แต่ในตัวเลือก Profile แสดง ID สั้นประกอบเพื่อแยกกัน เก็บ Profile กับ play count ในเครื่อง/browser เดิมผ่าน PlayerPrefs JSON การล้าง browser storage ทำให้ข้อมูลนี้หายและ Profile ไม่ sync ข้ามเครื่องในเฟสนี้ อ่าน JSON ไม่สำเร็จให้สำรองค่าดิบไว้และแจ้งว่ารายชื่อโหลดไม่ได้ก่อนใช้รายชื่อว่าง

Result Panel ประกอบด้วยชื่อ Profile, endReason (Triumph/HP depleted), Score, Difficulty, เวลาเล่นจริง, HP ที่เหลือ, Food Caught, Food Missed, Trash Eaten, Max Combo และ Highest Rank พร้อมกรอบสถิติซ้าย–ขวา

ปุ่ม RESTART และ MAIN MENU ใช้ Mouse/Keyboard navigation ได้ ล็อกการกดซ้ำระหว่าง scene transition และ reset focus เมื่อเปิด Panel ฉากหลังมืดลง; background blur เป็น visual polish ที่เพิ่มเมื่อผ่าน performance check

## 11. สถิติและความถูกต้องของหน่วย

ช่วง Playing เท่านั้นที่สะสมสถิติ ใช้เวลาเฉลี่ยถ่วงน้ำหนักด้วย delta time ของ valid input ไม่ใช้จำนวน render frames เป็นน้ำหนัก ไม่รวม Ready/Pause/Death/Triumph/Results

- maxStrength01 และ meanStrength01 แยกซ้าย–ขวา ค่าเฉลี่ยรวมช่วงปล่อยด้วย
- pressedDurationSeconds แยกแต่ละข้าง และเวลาที่บีบสองข้างพร้อมกัน
- เมื่อมี forceN ที่สอบเทียบแล้ว: maxForceN / meanForceN แยกข้าง ค่าเฉลี่ยรวมทุก valid sample ของ Playing
- หากไม่มี valid duration ใช้ unavailable ไม่หารด้วยศูนย์
- Keyboard input เป็น simulation (กด = 100%, ปล่อย = 0%) หน้าจอระบุ Simulated Input และแรง N แสดง “— / Sensor not connected” ไม่สร้าง N จำลอง
- phase hardware จะใช้ชื่อ Estimated Force (N) ตามที่ตกลง และเก็บ calibration version กับ input source ประกอบ
- Left–Right Balance แสดงสัดส่วน pressed duration ของซ้ายและขวา หากทั้งคู่เป็นศูนย์แสดง “—”; เป็นสัดส่วนการใช้งานในเกม ไม่ใช่การประเมินกำลังมือทางคลินิก

SessionResult เป็น plain serializable data มี schemaVersion, sessionId, playerId, displayName, difficulty, endReason, endedAtUtc, playedSeconds, score, remainingHp, caught/missed/trash counts, maxCombo, highestRank, inputSource และสถิติทั้งสองข้าง

ผลแต่ละรอบถูกส่งเข้า Result Panel ทันทีและพร้อม serialize เป็น JSON เก็บ last result สำหรับรอบปัจจุบัน ไม่ต้องเก็บ raw input ทุก frame หรือสร้างระบบ upload queue ในเฟส Unity นี้ Future transport ต้องรับผลตรึงนี้และไม่ block Result/Restart

## 12. Performance และการจัดการกรณีผิดปกติ

- ใช้ UnityEngine.Pool สำหรับอาหาร/ขยะและของตกแต่ง burst; reset callback, position, velocity, collision และ resolved flag ทุกครั้งที่ reuse
- จำกัดจำนวน live falling objects (เริ่มต้นสูงสุด 16) และ particle count; คืนวัตถุเมื่อรอบจบ
- กำหนด gameplay bounds/catch zone ให้ตรงกับภาพ แม้ flip/animation เปลี่ยน silhouette
- frame-rate independent movement และ timer; rendering ช้าชั่วคราวไม่เปลี่ยนความเร็วเกม
- missing reference/config ให้แจ้งชื่อปัญหาและ disable Start แทนการเริ่ม session ที่พังระหว่างเล่น
- save Profile ไม่สำเร็จให้แสดงคำเตือนและใช้ Profile ในหน่วยความจำต่อได้
- terminal transition, result creation และ play count ต้องเกิดครั้งเดียว
- ไม่ติดตั้ง dependency เพิ่มสำหรับงานที่แพ็กเกจเดิม/Unity API ทำได้

## 13. Verification ที่ต้องผ่าน

ใช้ Unity Test Framework สำหรับกติกาที่เสี่ยงผิด และ Play Mode/WebGL ตรวจ interaction จริง:

1. ตาราง input ทั้ง 4 สถานะ รวม alias keys และ focus loss
2. คะแนน 10/15/20; miss/trash ลด HP; object outcome ไม่ซ้ำ
3. Combo threshold, reset, highestRank และ maxCombo
4. เวลา 60s → Triumph, HP0 → Dying, เหตุการณ์พร้อมกัน → Dying, ผล frozen ระหว่าง animation
5. mean แบบ time-weighted, invalid sample, ไม่มีข้อมูล, N unavailable ใน Keyboard
6. Profile ชื่อไทย/ซ้ำ/ว่าง/rich-text-like string, reload และ malformed stored JSON
7. Restart ซ้ำหลายรอบไม่ค้าง object, callback, HP, score, stats หรือ AppSession duplicate
8. Easy ไม่มีขยะ; Normal/Hard ใช้ config; pattern ไม่บังคับรับขยะเพื่อรับอาหาร
9. letterbox/pillarbox และ UI ที่หลายขนาด browser รวม fullscreen
10. WebGL build/run: ชื่อ Profile, input, animation, terminal states, result, navigation และวัด performance บนเครื่องสาธิตจริง

Mockup เป็น reference ให้เทียบ style/silhouette/HUD จากภาพเกมจริงภายหลัง ไม่ใช้ภาพ mockup ทั้งใบแทน gameplay

## 14. ลำดับส่งมอบในเฟส Unity

1. Playable core: scene flow, Keyboard, food/trash, HP, timer, Score/Combo และ terminal states
2. Menu/Profile/Result และสถิติที่มี source/units ถูกต้อง
3. Production artwork/animation, kitchen ambient, audio และ visual polish ตาม reference
4. WebGL build และ playtest/tuning พร้อมคู่มือเปิดโปรเจกต์และทดสอบ

ยึดขอบเขตนี้สำหรับ implementation plan ถัดไป จัดทำแผนหลังผู้ใช้ตรวจ spec ฉบับนี้แล้ว
