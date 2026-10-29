# USB controllers: Unity Editor and WebGL

## Versus: Keyboard + FSR สองบอร์ด (Windows)

ส่วนนี้สำหรับ Micro Versus ส่วนคำแนะนำ Single Player เดิมอยู่ด้านล่าง

1. เปิดเกมจาก Boot แล้วเลือก Versus จากนั้นกด **CONTROLLERS** มุมขวาบน
2. ปิด Serial Monitor/terminal ที่ถือ COM ไว้ก่อน กด **REFRESH PORTS** แล้วกด **CHOOSE COM PORT** ของแต่ละคนเพื่อวนเลือกพอร์ต ไม่มีการเลือกบอร์ดตามลำดับ COM ให้อัตโนมัติ
3. P1 เลือก ESP32 แล้ว **CONNECT**; P2 เลือก NUCLEO-G071RB แล้ว **CONNECT** บอร์ด Nucleo ที่ทดสอบอยู่ใช้ COM4 แต่ Windows อาจเปลี่ยนหมายเลขได้
4. เมื่อสถานะ FSR connected ให้กด DONE แล้วทดสอบเดินและ Ready ในพื้นที่ฝึก กดซ้าย+ขวาพร้อมกันเพื่อ Ready โดยใช้คีย์บอร์ด/FSR ผสมกันได้
5. จะเล่นคีย์บอร์ดอย่างเดียวก็ได้ ไม่ต้องเชื่อมต่อ USB หากเลือกเชื่อมต่อแล้วข้อมูลยังไม่พร้อม ต้องเชื่อมให้สำเร็จหรือกด DISCONNECT ก่อนเริ่ม

| ผู้เล่น | ซ้าย | ขวา |
| --- | --- | --- |
| P1 / ESP32 | A หรือ FSR GPIO34 | D หรือ FSR GPIO35 |
| P2 / STM32 | ลูกศรซ้าย; ปิด FSR ซ้ายที่เสียไว้ | ลูกศรขวา หรือ FSR PA1/A1 |

คีย์บอร์ดกับ FSR รวมด้วย OR ไม่บวกแรงเพิ่ม เมื่อมีปุ่มทิศเดียวกันกดอย่างใดอย่างหนึ่งก็เคลื่อนที่ กดทั้งสองทิศจะไม่เคลื่อนที่และใช้ Ready ได้ การพิมพ์ชื่อและเปิดหน้าต่าง Controllers จะหยุดการควบคุมในพื้นที่ฝึก

ESP32 ใช้เฟิร์มแวร์ JSON v1 ตัวเดิม ส่วน STM32 ใช้โปรเจกต์ `Firmware/STM32/Micro-Project` ใน repo ที่พิมพ์ `FSR raw L=... R=...` ได้เลย ไม่ต้อง Flash เพิ่มในขั้นนี้ ตัวรับ raw แยกจากตัวตรวจ JSON เดิม ข้อมูลผิดรูปแบบ เกินช่วง หรือเก่าจะไม่ต่ออายุข้อมูลควบคุม

เมื่อ Serial ที่ใช้อยู่หลุดหรือข้อมูลเก่าถึง .75 วินาที เกมจะ Pause ทั้งสองฝั่งและปล่อยอินพุตจากเซนเซอร์ เปิด **CONTROLLERS** เพื่อเชื่อมใหม่แล้วกด **KEEP COOKING** หรือเลือก **CONTINUE WITH KEYBOARD** เพื่อปิดการเชื่อมต่อ Versus ทั้งสองบอร์ดและเล่นด้วยคีย์บอร์ดต่อ การกลับมาเล่นต้องผ่านนับถอยหลัง ไม่มีการ Resume อัตโนมัติเมื่อข้อมูลกลับมา

ปรับที่ `Assets/CPS/Resources/VersusControllerSettings.asset` ใน Inspector:

- `Esp32 Left/Right Enabled`: เปิด FSR ของ P1
- `Stm32 Left Enabled`: ปิดไว้ระหว่างที่ FSR ซ้ายเสีย เปิดหลังเปลี่ยนและตรวจเซนเซอร์แล้ว
- `Stm32 Right Enabled`: เปิด FSR ขวาของ P2
- `Stm32 Released Raw` / `Stm32 Reference Raw`: ค่าเริ่มต้นชั่วคราว 4095 / 100 อ้างอิงช่วงค่าจาก log ต้องยืนยันตอนปล่อยและกดจริง ไม่ใช่การคาลิเบรตแรง N
- `Press Percent` / `Release Percent`: 20 / 15 กันการสั่นใกล้จุดกด
- `Stale Seconds`: .75 วินาที ปรับให้สั้นลงได้

การเชื่อมต่อ USB สองบอร์ดใน Versus รองรับ Windows Unity Editor/Windows player ในขั้นนี้ WebGL ของ Versus ยังใช้คีย์บอร์ด ส่วน Single Player ยังคงมี Web Serial ตามระบบเดิม FSR และคีย์บอร์ดไม่มีค่าประมาณแรง N; สถิติแรงสัมพัทธ์ใช้เฉพาะข้อมูลเซนเซอร์ที่มีจริง

## เตรียม ESP32

อัปโหลด `Firmware/ESP32/Sensor_Controller/Sensor_Controller.ino` จาก repo นี้ลง ESP32 บอร์ดเดียว เลือก board/port ให้ตรงกับของจริง ใช้ HX711 by Rob Tillaart 0.6.5 ที่ติดตั้งอยู่ ไม่ต้องแฟลชสลับเมื่อเปลี่ยน FSR/Load Cell ในเกม ปิด Serial Monitor ก่อนเชื่อมจาก Unity หรือ Browser เพราะพอร์ตเดียวเปิดพร้อมกันไม่ได้

| ช่อง | Pin | Calibration ชั่วคราว |
|---|---|---|
| FSR ซ้าย (ตัวใหม่ใน 2.1.0) | GPIO34 | ปล่อย 4095, กดอ้างอิง 156.5 |
| FSR ขวา | GPIO35 | ปล่อย 4095, กดอ้างอิง 156.5 |
| Load Cell ซ้าย 1 kg | DT32 / SCK18 | −2449.84 counts/g |
| Load Cell ขวา 5 kg | DT33 / SCK4 | −429.11 counts/g |

สเก็ตช์ส่ง JSON หนึ่งบรรทัดประมาณ 20 ครั้ง/วินาที ที่ 115200 baud ค่า HX711 อัปเดตตามความเร็วตัวเซนเซอร์ ดูรายละเอียดเพิ่มเติมใน `Firmware/ESP32/Sensor_Controller/README.md`

## Unity Editor บน Windows

1. เปิด โฟลเดอร์ `Unity` ภายใน repo ที่มี `Assets`, `Packages`, `ProjectSettings` ด้วย Unity 6000.3.25f1 แล้ว Play จาก `Assets/CPS/Scenes/Boot.unity`
2. หน้า Home กด **Controller** แล้วเลือก Keyboard / FSR / Load Cell ค่านี้เป็น Global ไม่ผูกกับ Profile
3. สำหรับเซนเซอร์ กด Refresh เลือก COM ของ ESP32 แล้ว Connect USB ไม่มีการเปิดพอร์ตหรืออัปโหลดบอร์ดให้อัตโนมัติ
4. FSR แสดงระดับสัมพัทธ์ 0–100% พร้อมใช้งานเมื่อมีข้อมูลสดทั้งสองฝั่ง ไม่แปลงเป็น N
5. Load Cell: เอาแรงกด/น้ำหนักออกจาก **ทั้งสองตัว** แล้วกด Tare รอทั้งสองช่องพร้อม จึงเริ่มเล่น ค่าเป็น **Estimated Force (N)** ไม่มีเปอร์เซ็นต์สมมติ
6. เลือก Profile/ความยาก แล้ว Start Cooking ใช้ตัวควบคุมที่เลือกได้ทั้งพื้นที่ฝึกและในเกม

Keyboard ยังใช้ A / ลูกศรซ้าย และ D / B / ลูกศรขวา ส่วน Spacebar / Escape ใช้ Pause / Resume ไม่ทำงานระหว่างพิมพ์ชื่อหรือเปิด Settings/Controller การ Resume นับถอยหลัง 3 วินาที

## WebGL

ใช้ Browser ที่รองรับ Web Serial เช่น Chrome บน Desktop เปิดเกมผ่าน HTTPS หรือ localhost จากนั้นเข้า Controller และกด Connect USB เพื่อเลือกพอร์ตจากหน้าต่างของ Browser ต้องกดด้วยตนเอง Browser ที่ไม่มี Web Serial ยังเล่น Keyboard ได้

การอนุญาต USB เป็นของแต่ละ Browser/origin ไม่แชร์กับ Unity Editor และไม่ใช่ Bluetooth/MQTT ปิด Editor/Serial Monitor ที่ใช้พอร์ตนั้นก่อนเชื่อม ไม่รองรับการเชื่อม USB นี้จาก iPhone

## เมื่อข้อมูลขาดหาย

เกมหยุดอัตโนมัติเมื่อข้อมูลเก่า/ช่องที่เลือกไม่พร้อม/พอร์ตขาด/บอร์ดรีบูต โดยไม่เดินเวลา เสีย HP หรือสะสมสถิติต่อ เปิด **Controller** จากหน้า Pause เพื่อเชื่อมใหม่หรือ Tare หลังข้อมูลกลับมาเกมยังรอให้กด Keep Cooking หรือ Spacebar เอง เปลี่ยนชนิด Controller ได้เมื่อกลับ Home เท่านั้น

## ปรับ threshold ใน Inspector

เปิด `Unity/Assets/CPS/Resources/ControllerSettings.asset` จากราก Micro หรือ `Assets/CPS/Resources/ControllerSettings.asset` ภายใน Unity:

- FSR เริ่มกดที่ 20% และปล่อยที่ 15% แยกซ้าย/ขวา
- Load Cell เริ่มกดที่ 0.5 N และปล่อยที่ 0.3 N แยกซ้าย/ขวา
- `Stale Seconds` เริ่มต้น 0.75 วินาที ตั้งสั้นลงได้ แต่ข้อมูลที่เก่าถึง 0.75 วินาทีจะถูกปฏิเสธเสมอ

Press ต้องมากกว่า Release การแยกสองค่านี้ช่วยกันการสั่นบริเวณ threshold ค่าถูกสำเนาเมื่อเริ่มรอบ การแก้ระหว่างเล่นมีผลรอบถัดไป

## ตรวจของจริงก่อนใช้งาน

ค่าปัจจุบันสืบทอดจาก CPS Version 2.1.0 ซึ่งผู้ใช้ยืนยันว่าเล่นได้แล้วหลังเปลี่ยน FSR ซ้าย การตรวจอัตโนมัติไม่ได้อัปโหลดหรือเปิดพอร์ตจริง หากประกอบชุดใหม่ ให้ลองปล่อย → กดซ้าย → ปล่อย → กดขวา ตรวจทิศทางและระดับ จากนั้นลองถอด USB ระหว่างเล่นและเชื่อมใหม่ ต้องหยุดและรอ Resume ทดสอบ Load Cell ด้วยน้ำหนักที่ทราบค่าหลัง Tare โดยเฉพาะตัว 1 kg ห้ามเกินพิกัด ค่า calibration ยังเป็นชั่วคราวเพราะจุด 83 g ไม่สอดคล้องกับจุด 228/525 g และ FSR ทั้งสองข้างปล่อยอยู่ที่ ADC เพดาน 4095 จึงแยกสายหลุดบางแบบจากการปล่อยไม่ได้

Results ระบุแหล่งข้อมูลจริง: Keyboard/FSR ไม่มีสถิติแรง N ส่วน Load Cell มี Max/Average N และไม่สร้างสถิติเปอร์เซ็นต์เพิ่ม โครงสร้างคอลัมน์ Sheets เดิมไม่เปลี่ยน

