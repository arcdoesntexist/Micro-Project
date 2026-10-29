# Google Sheets Result Upload Design

วันที่: 2026-10-20
สถานะ: อนุมัติแล้วเมื่อ 2026-10-20

## 1. เป้าหมาย

ส่งผลหลังจบเกมของ Profile ที่กำลังเล่นจาก Unity ไปยัง Google Sheet `CPS-DB` โดยเก็บหนึ่งแถวต่อหนึ่งรอบ เกมต้องเปิดหน้า Results ได้ทันทีแม้การส่งล้มเหลว และการออกจาก Pause กลับ Main Menu ต้องไม่สร้างข้อมูล

ระบบนี้ใช้สำหรับงานมหาวิทยาลัยและ Dashboard ภายหลัง ไม่ใช่ระบบบัญชีผู้ใช้หรือฐานข้อมูลที่ต้องรับประกันการส่งทุกครั้ง

## 2. ขอบเขต

รวมในงาน:

- สร้างชีต `Results` พร้อมหัวคอลัมน์คงที่
- สร้าง Google Apps Script Web App สำหรับรับ JSON และเพิ่มแถว
- ป้องกันข้อมูลซ้ำด้วย `sessionId`
- เพิ่ม Unity uploader ที่ฟังผลจาก `GameSessionController.Finished`
- ส่งแบบ asynchronous โดยไม่รอผลก่อนเปิด Result Panel
- log warning เมื่อส่งไม่สำเร็จ
- ทดสอบ serialization, trigger, duplicate protection และ failure behavior

ไม่รวมในงาน:

- retry queue หรือ offline synchronization
- การแก้/ลบข้อมูลจาก Unity
- Dashboard, Node-RED หรือ MQTT
- ระบบ login, OAuth หรือการซ่อน secret ใน client
- การส่ง raw input ทุก frame

## 3. Data flow

1. `GameSessionController.FinishRound()` ตรึง `SessionResult` และส่ง event `Finished` เพียงครั้งเดียว
2. `GoogleSheetsResultUploader` รับสำเนา result และ serialize เป็น JSON
3. Unity ส่ง `POST` ไปยัง Apps Script โดยใช้ body แบบ `text/plain` เพื่อหลีกเลี่ยง CORS preflight ใน WebGL
4. Apps Script parse JSON, validate ฟิลด์บังคับ และใช้ script lock ป้องกันการเขียนพร้อมกัน
5. Apps Script ตรวจ `sessionId`; ถ้ามีแล้วตอบสถานะ duplicate โดยไม่เพิ่มแถว
6. ถ้ายังไม่มี ให้ append หนึ่งแถวและตอบ JSON ว่าสำเร็จ
7. Unity บันทึก warning เมื่อ HTTP/network/response ล้มเหลว แต่ไม่เปลี่ยนผลเกมหรือ navigation

## 4. Sheet schema

ชีตชื่อ `Results` ใช้หัวคอลัมน์ต่อไปนี้ตามลำดับ:

| Column | Field |
|---:|---|
| A | receivedAtUtc |
| B | schemaVersion |
| C | sessionId |
| D | playerId |
| E | displayName |
| F | difficulty |
| G | endReason |
| H | endedAtUtc |
| I | playedSeconds |
| J | score |
| K | remainingHp |
| L | foodCaught |
| M | foodMissed |
| N | trashEaten |
| O | maxCombo |
| P | highestRank |
| Q | inputSource |
| R | leftMaxStrength01 |
| S | leftMeanStrength01 |
| T | leftPressedSeconds |
| U | leftMaxForceN |
| V | leftMeanForceN |
| W | rightMaxStrength01 |
| X | rightMeanStrength01 |
| Y | rightPressedSeconds |
| Z | rightMaxForceN |
| AA | rightMeanForceN |
| AB | bothPressedSeconds |
| AC | leftBalance01 |

ค่าสถิติที่ไม่มีข้อมูล เช่นแรง N จาก Keyboard ให้เขียนเป็นช่องว่าง ไม่สร้างค่าแรงจำลอง `receivedAtUtc` สร้างที่ server ส่วน `endedAtUtc` มาจาก Unity

### การจัดรูปแบบเพื่อให้อ่านง่าย

ใช้สีอ่อนแยกกลุ่มคอลัมน์แทนการกำหนดสีต่างกันทุกเซลล์:

- A–H ข้อมูลระบบและ Profile: ฟ้าอ่อน
- I–Q ผลการเล่น: เขียวอ่อน
- R–AC สถิติ input/แรง: ส้มอ่อน

หัวตารางใช้เฉดเข้มกว่าพื้นข้อมูลเล็กน้อยและตัวอักษรเข้ม โดยไม่ใช้สีสด พื้นข้อมูลของแต่ละกลุ่มใช้สีประจำกลุ่มสองระดับอ่อนสลับกันเพื่อช่วยไล่อ่านโดยไม่ลบการแบ่งกลุ่ม Freeze แถวหัวตาราง เปิด filter และกำหนดความกว้างคอลัมน์ให้อ่านค่าหลักได้โดยไม่ตัดข้อความสำคัญ รูปแบบวันที่ เวลา และตัวเลขใช้รูปแบบเดียวกันทั้งคอลัมน์

## 5. Apps Script behavior

Web App รับเฉพาะ `POST` ที่ body เป็น JSON และต้องมีอย่างน้อย:

- `schemaVersion` เท่ากับ `1`
- `sessionId`, `playerId`, `displayName`, `endedAtUtc` ไม่ว่าง

ค่าตัวเลขต้อง finite และชนิด enum/string ต้องแปลงเป็นข้อความที่อ่านได้ก่อนส่งจาก Unity Apps Script ไม่เชื่อชื่อหัวคอลัมน์จาก client และประกอบแถวตาม schema ฝั่ง server เท่านั้น

ผลตอบกลับมีรูปแบบ:

```json
{"ok":true,"status":"created"}
```

หรือ `status` เป็น `duplicate` สำหรับ `sessionId` ที่เคยบันทึกแล้ว กรณี payload ไม่ถูกต้องตอบ `ok:false` พร้อมข้อความสั้นที่ไม่เปิดเผยข้อมูลใน Sheet

Web App deploy เป็น execute as เจ้าของ Sheet และเปิด access แบบ `Anyone` เพื่อให้ Unity Editor/WebGL ส่งได้โดยไม่ login การ deployment นี้ไม่มีการรับรองผู้ส่ง จึงเหมาะกับ classroom demo เท่านั้น

## 6. Unity integration

เพิ่ม component `GoogleSheetsResultUploader` ใน Gameplay scene/prefab และกำหนด endpoint ผ่าน serialized field ตัว component subscribe/unsubscribe `GameSessionController.Finished` ตาม lifecycle ของ Unity

Uploader ต้อง:

- ไม่แก้ `SessionResult`
- ป้องกันการเริ่ม request ซ้ำสำหรับ `sessionId` เดียวภายใน instance
- ใช้ `UnityWebRequest` และไม่ block main thread
- ตั้ง timeout ที่เหมาะกับ demo
- ถือว่า Apps Script `created` และ `duplicate` เป็นผลสำเร็จ
- ไม่ retry อัตโนมัติ
- ไม่แสดง error modal ต่อผู้เล่น

URL deployment จะอยู่ใน prefab เพื่อให้เปลี่ยน deployment ภายหลังได้โดยไม่แก้ source code URL นี้ไม่ถือเป็น secret

## 7. ความปลอดภัยและข้อมูลส่วนบุคคล

ข้อมูลที่ส่งประกอบด้วยชื่อ Profile, ID ภายในเกม, ผลเกม และสถิติ input ไม่มี password, email หรือข้อมูลเซนเซอร์ดิบ อย่างไรก็ตามชื่อ Profile อาจเป็นข้อมูลระบุตัวบุคคล ผู้สาธิตควรแจ้งผู้เล่นว่าผลจะถูกเก็บใน Google Sheet และหลีกเลี่ยงการใช้ชื่อจริงหากไม่จำเป็น

เพราะ endpoint เปิด public บุคคลภายนอกที่ทราบ URL สามารถส่งข้อมูลขยะได้ Shared token ใน Unity ไม่แก้ปัญหาอย่างแท้จริงเพราะดึงจาก build ได้ หากต้องเผยแพร่สู่สาธารณะต้องเพิ่ม backend authentication หรือ proxy ที่เชื่อถือได้ภายหลัง

## 8. Error handling

- Network/HTTP failure: log warning และคงหน้า Results ตามปกติ
- JSON ไม่ถูกต้องหรือ field ขาด: Apps Script ปฏิเสธและไม่ append
- `sessionId` ซ้ำ: ตอบ duplicate และไม่ append
- Header หายหรือผิด: setup function สร้าง/ตรวจ header ก่อน deploy; handler ไม่เดาคอลัมน์ใหม่ระหว่าง request
- เขียน Sheet พร้อมกัน: ใช้ `LockService.getScriptLock()` และปล่อย lock ใน `finally`
- Sheet หรือชื่อ tab หาย: ตอบ error และไม่สร้าง tab เงียบ ๆ ระหว่าง request

## 9. Verification

1. EditMode test ยืนยัน payload mapping และค่าที่ unavailable เป็นค่าว่าง/null ตาม contract
2. PlayMode test ยืนยันส่งเมื่อ `Finished` ครั้งเดียว และไม่ส่งเมื่อ Pause → Main Menu
3. Apps Script manual test ส่ง session ใหม่แล้วเพิ่มหนึ่งแถว
4. ส่ง payload เดิมซ้ำแล้วจำนวนแถวไม่เพิ่ม
5. ส่ง payload ขาด `sessionId` แล้วจำนวนแถวไม่เพิ่ม
6. ปิดเน็ตหรือใช้ URL ผิดแล้วหน้า Results/Restart/Main Menu ยังทำงาน
7. ทดสอบ Unity Editor และ WebGL build กับ deployment URL จริง
8. ตรวจว่าสีแบ่งกลุ่มคอลัมน์, alternating rows, frozen header, filter และรูปแบบตัวเลขยังอ่านง่ายเมื่อมีข้อมูลหลายแถว

## 10. การส่งมอบ

- Apps Script ถูกผูกกับ Sheet `CPS-DB`
- ชีต `Results` มี header และแถวทดสอบที่ระบุชัดเจน; ลบแถวทดสอบก่อนส่งมอบ
- Unity prefab เก็บ deployment URL ล่าสุด
- README ระบุ Sheet, วิธี redeploy และข้อจำกัด public endpoint
