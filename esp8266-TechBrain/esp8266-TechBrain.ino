
#define UART_BAUD 115200

void setup(void) {
  delay(10); //delay for debuging in ArduinoIDE
  Serial.begin(UART_BAUD);
  Serial.println("\nstarting...");

  //chip info
  uint32_t chipRealSize = ESP.getFlashChipRealSize();
  uint32_t chipSize = ESP.getFlashChipSize();
  if (chipRealSize != chipSize) {
    Serial.printf("!!!Chip size (%u) is wrong. Real is %u\n", chipSize, chipRealSize);
  }
  Serial.printf("CpuFreq: %u MHz\n", ESP.getCpuFreqMHz());
  Serial.printf("Flash speed: %u Hz\n", ESP.getFlashChipSpeed());
  FlashMode_t ideMode = ESP.getFlashChipMode();
  Serial.printf("Flash mode:  %s\n", (ideMode == FM_QIO ? "QIO" : ideMode == FM_QOUT ? "QOUT" : ideMode == FM_DIO ? "DIO" : ideMode == FM_DOUT ? "DOUT" : "UNKNOWN"));
}

void loop(void) {
  delay(1000);
  Serial.println("loop");
}
