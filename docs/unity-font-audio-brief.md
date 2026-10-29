# Font & Audio Brief — IoTE Kitchen Catch

สถานะ: ใช้ `FFSpokenTrial-Regular.ttf` กับ UI ทั้งหมดและ MP3 ที่ผู้ใช้ให้มา 6 ไฟล์เป็น asset ชั่วคราวสำหรับสาธิตในห้องเรียน ยังไม่มี license ที่ยืนยันสิทธิ์ embed/แจกจ่าย จึงไม่ถือว่าผ่าน production

สำหรับ demo ในห้องเรียน โค้ด Unity จะโหลด Lobby, Gameplay, Running, Triumph, Death และ Chomping จาก `Assets/CPS/Resources/LocalDemoAudio` ถ้ามีไฟล์ในเครื่อง ดู URL ต้นทางและข้อจำกัดสิทธิ์ใน [README](../README.md) การระบุแหล่งที่มาไม่ใช่ใบอนุญาต โดยเฉพาะ Death ที่สืบย้อนถึง Rockstar Games / *Red Dead Redemption 2* ก่อนสร้าง build เพื่อแจกจ่าย ให้ย้ายหรือลบโฟลเดอร์นี้ออกจาก Unity project แล้ว build ใหม่ เสียง hurt, rank-up, UI confirm, countdown และ go ยังไม่มี

## แนวเสียง

เพิ่มเสียงกินจาก `sound/Chomping.MP3` ของผู้ใช้แล้ว โดยนำเข้าเป็น `Assets/CPS/Resources/LocalDemoAudio/Chomping.MP3` และใช้เป็นค่าเริ่มต้นของ Mascot เมื่อ Eat Clip ยังไม่ได้กำหนดใน Inspector เสียงนี้ใช้การควบคุมเสียง global เดิมและไม่เปลี่ยนระดับเสียงอื่น

ครัวอบอุ่น สนุก ขี้เล่น แบบ cartoon ผสมเสียงเกมย้อนยุคเล็กน้อย ไม่ดุดันหรือหนักแบบเกมต่อสู้ Combo อิงความรู้สึกไต่ระดับของ DMC5 แต่ไม่ใช้เสียง เพลง หรือ voice line จากเกมนั้น

เพลงใช้ pluck, marimba/xylophone, bass นุ่ม และ percussion เบา ๆ เมโลดี้ไม่แน่นจนแย่งสมาธิ เสียงรับอาหารให้รู้สึกนุ่ม/น่ากิน เสียงรับขยะต้องแยกชัดแต่ไม่แสบหู ไม่มีเสียง scream หรือเสียงสมจริงรุนแรง

## ไฟล์ที่ต้องการ

| ชื่อไฟล์เสนอ | แนวเสียง / การใช้งาน | ความยาวเสนอ | ความสำคัญ |
|---|---|---|---|
| `bgm_gameplay.wav` | playful kitchen groove/chiptune-light ประมาณ 120–135 BPM จังหวะคงที่ ทุก difficulty ใช้เพลงเดียวได้ | 60–90s loop ไร้รอยต่อ | ต้องมี |
| `sfx_eat.wav` | pop + munch สั้น นุ่ม กระชับ ไม่เหมือนคำพูด | 0.10–0.25s | ต้องมี |
| `sfx_damage.wav` | cartoon bonk/boing โทนลง แยกจาก eat ชัด; ใช้ทั้ง miss food/รับขยะ | 0.20–0.40s | ต้องมี |
| `sfx_rank_up.wav` | ascending sparkle/chime ให้รู้สึก combo ดีขึ้น; เริ่มจากไฟล์เดียว ไม่ต้องหา 7 เสียง | 0.30–0.70s | ต้องมี |
| `sfx_triumph.wav` | fanfare สั้น อบอุ่น ร่าเริง จบด้วยคอร์ดสว่าง | 0.80–1.20s | ต้องมี |
| `sfx_death.wav` | descending cartoon wobble + crumble ตลก ไม่เศร้า/โหด; เข้ากับล้มและสลาย | 1.00–1.40s | ต้องมี |
| `sfx_ui_confirm.wav` | rounded click/pop ที่เบากว่าเสียงเกม | 0.05–0.15s | ต้องมี |
| `sfx_countdown.wav` | tick/beep สั้น ใช้ 3–2–1 | 0.08–0.15s | ต้องมี |
| `sfx_go.wav` | ding ชัดเจน โทนสูงกว่า countdown | 0.15–0.30s | ต้องมี |
| `bgm_menu.wav` | เพลงเดียวกับ gameplay แบบเบาลง หรือ loop อบอุ่นช้ากว่า | 30–60s loop | เพิ่มภายหลังได้ |
| `sfx_dissolve.wav` | soft pixel crackle/crumble แยกต่างหาก ถ้า death ยังไม่มีช่วงสลาย | 0.40–0.70s | เพิ่มภายหลังได้ |
| `amb_kitchen.wav` | simmer/bubble ต่ำมาก ไม่มีเสียงวัตถุตกที่สับสนกับ gameplay | 10–30s loop | เพิ่มภายหลังได้ |

ไม่ต้องหาเสียงเดินทุกก้าวหรือ voice announcer ในชุดแรก เพราะเสียงจะซ้อนกับอาหาร/Combo และเพิ่มงานโดยไม่จำเป็น เพลงต้องไม่เร่งตาม Rank เพื่อให้การควบคุมยังอ่านง่าย

## รูปแบบส่งไฟล์

- ส่ง WAV ต้นฉบับถ้ามี; SFX mono, เพลง stereo ได้ ไม่ต้องแปลงไฟล์เองถ้ามีแต่ OGG/MP3
- ไม่มีเสียง clipping, leading silence ยาว หรือ reverb tail ที่ขาดกะทันหัน; เพลง loop ระบุจุดเริ่ม/จบถ้าไฟล์ไม่ seamless
- ตั้งชื่อใกล้เคียงตารางเพื่อจับคู่ได้ง่าย ส่งหลายตัวเลือกต่อเสียงได้ แต่ไม่จำเป็น
- แนบ URL ต้นทาง/ชื่อผู้สร้าง/ใบอนุญาต และเงื่อนไข attribution ถ้ามี ต้องใช้และเผยแพร่ร่วมกับเกม demo ได้
- ไม่ใช้ไฟล์ที่ rip มาจาก Kirby, DMC5, Minecraft หรือเกมอื่น
- วางไฟล์ที่เลือกใน `C:\Users\Arc\Downloads\CPS\Unity_Project\incoming\audio` ได้ แล้วบอกผมเมื่อพร้อม ผมจะตรวจและนำเข้า Unity ภายหลัง

## Font ชั่วคราว

ไฟล์ `Assets/CPS/Fonts/FFSpokenTrial-Regular.ttf` ถูกนำเข้าจากฟอนต์ที่ติดตั้งในเครื่อง และใช้กับ Main Menu, Gameplay, Pause, Settings และ Results ทั้งหมด รองรับ glyph ภาษาไทยยังไม่ได้รับการยืนยัน ชื่อผู้เล่นภาษาไทยอาจแสดงไม่ครบ ต้องเปลี่ยนเป็นไฟล์ที่มี license และ Thai glyph coverage ก่อนเผยแพร่

## Global audio settings

Main Menu และ Pause ใช้ Settings panel เดียวกัน ค่า Master Volume และ Background Music บันทึกด้วย `PlayerPrefs` แบบ global ไม่ขึ้นกับ Profile ค่า 100 ให้ Master gain สูงสุด `+12 dB`; ปิด Background Music จะหยุดเฉพาะ Lobby/Gameplay music โดย Running, Triumph และ Death ยังเล่น

## Font production ที่ยังต้องเลือก

ต้องการ Font UI ที่อ่านง่ายทั้ง Latin, ตัวเลข และชื่อไทย รวมสระ/วรรณยุกต์ ใช้ไฟล์ TTF/OTF พร้อม license ที่อนุญาต embed/แจกจ่ายในเกม ไม่จำเป็นต้องเป็น pixel font ทุกข้อความ

แนะนำบทบาท: Font หลักสำหรับชื่อ/HUD/Stats และ display font สำหรับ Combo เป็นทางเลือก หากเลือก Font เดียวที่อ่านง่ายก็พอ Combo ใช้ขนาด/สี/เอฟเฟกต์สร้างความต่างได้

วางไฟล์และ license ใน `C:\Users\Arc\Downloads\CPS\Unity_Project\incoming\fonts` ได้ เมื่อได้รับไฟล์จะตรวจ glyph ชื่อไทย, markup เป็น plain text, ตัวเลขเล็ก และการจัดวรรณยุกต์ใน Unity ก่อนถือว่าผ่าน UI ภาษาไทย

ระหว่างรอ ใช้ Font ที่มีอยู่กับ Unity สำหรับ UI ชั่วคราวและไม่ใส่ Audio ที่ไม่มีสิทธิ์ ไม่ถือว่าเสียงหรือ Thai font production ผ่านแล้ว
