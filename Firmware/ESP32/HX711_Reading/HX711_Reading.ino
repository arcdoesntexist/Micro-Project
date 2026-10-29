#include "HX711.h"

const int LOADCELL1_DOUT_PIN = 32;
const int LOADCELL1_SCK_PIN  = 18;
const int LOADCELL2_DOUT_PIN = 33;
const int LOADCELL2_SCK_PIN  = 4;
const float LOADCELL1_COUNTS_PER_GRAM = -2449.84f;
const float LOADCELL2_COUNTS_PER_GRAM = -429.11f;
const float NEWTONS_PER_GRAM = 0.00980665f;
const uint8_t TARE_SAMPLES = 5;
const uint8_t READING_SAMPLES = 3;

HX711 scale1;
HX711 scale2;
bool calibrated1 = false;
bool calibrated2 = false;

bool tareScale(HX711& scale, const char* channel) {
  float total = 0;
  for (uint8_t i = 0; i < TARE_SAMPLES; i++) {
    if (!scale.wait_ready_timeout(1000, 1)) {
      Serial.print(channel);
      Serial.println(": tare failed; remove load and send t to retry.");
      return false;
    }
    total += scale.read();
  }
  scale.set_offset(static_cast<int32_t>(total / TARE_SAMPLES));
  Serial.print(channel);
  Serial.println(": tare OK.");
  return true;
}

void tareBoth() {
  Serial.println("Taring both channels. Keep both sensors unloaded and still.");
  calibrated1 = tareScale(scale1, "CH1");
  calibrated2 = tareScale(scale2, "CH2");
}

void reportScale(HX711& scale, const char* channel, bool calibrated) {
  Serial.print(channel);
  if (!calibrated) {
    Serial.println(": not calibrated; unload and send t.");
    return;
  }
  if (!scale.is_ready()) {
    Serial.println(": not ready.");
    return;
  }
  float total = 0;
  for (uint8_t i = 0; i < READING_SAMPLES; i++) {
    if (!scale.wait_ready_timeout(250, 1)) {
      Serial.println(": not ready; incomplete reading discarded.");
      return;
    }
    total += scale.get_units(1);
  }
  float grams = total / READING_SAMPLES;
  Serial.print(": ");
  Serial.print(grams, 2);
  Serial.print(" g | Estimated Force: ");
  Serial.print(grams * NEWTONS_PER_GRAM, 4);
  Serial.println(" N");
}

void setup() {
  Serial.begin(115200);
  scale1.begin(LOADCELL1_DOUT_PIN, LOADCELL1_SCK_PIN, true, false);
  scale2.begin(LOADCELL2_DOUT_PIN, LOADCELL2_SCK_PIN, true, false);
  scale1.power_down();
  scale2.power_down();
  scale1.power_up();
  scale2.power_up();
  scale1.set_scale(LOADCELL1_COUNTS_PER_GRAM);
  scale2.set_scale(LOADCELL2_COUNTS_PER_GRAM);
  Serial.println("CH1: DT32/SCK18, 1kg | CH2: DT33/SCK4, 5kg");
  Serial.println("Provisional calibration. Send t to tare BOTH unloaded sensors.");
  tareBoth();
}

void loop() {
  bool requestedTare = false;
  while (Serial.available() > 0) {
    int command = Serial.read();
    if (command == 't' || command == 'T') requestedTare = true;
  }
  if (requestedTare) tareBoth();
  reportScale(scale1, "CH1", calibrated1);
  reportScale(scale2, "CH2", calibrated2);
  delay(100);
}
