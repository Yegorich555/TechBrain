#include "ESP8266WiFi.h"

#define WIFI_SSID_1 "iTechArt-free" //store into eeprom
#define WIFI_PASS_1 "password" //store into eeprom
#define WIFI_SSID_2 "ESPConf" // the second wifi point if the first doesn't exist
#define WIFI_PASS_2 "atmel^8266"

#define UART_BAUD 115200

#define IO_OUT1 4 //soft SDA by default
#define IO_OUT2 5 //soft SCL by default; 
#define IO_OUT3 15
#define IO_OUT4 12
#define IO_OUT5 13
#define IO_IN1 14
#define IO_IN4 16
//#define LED_BUILTIN 2 //by default; Tx1 by default
//#define BUTTON_BUILTIN 0 //by default

void setup(void) {
  delay(100); //delay for debuging in ArduinoIDE
  Serial.begin(UART_BAUD);
  Serial.println("\nstarting...");

  //Serial1.begin(UART_BAUD); //Tx1 or GPIO2; Rx1 is not accessible

  //chip info
  uint32_t chipRealSize = ESP.getFlashChipRealSize();
  uint32_t chipSize = ESP.getFlashChipSize();
  if (chipRealSize < chipSize) {
    Serial.printf("!!!Chip size (%u) is wrong. Real is %u\n", chipSize, chipRealSize);
  }
  Serial.printf("CpuFreq: %u MHz\n", ESP.getCpuFreqMHz());
  Serial.printf("Flash speed: %u Hz\n", ESP.getFlashChipSpeed());
  FlashMode_t ideMode = ESP.getFlashChipMode();
  Serial.printf("Flash mode:  %s\n", (ideMode == FM_QIO ? "QIO" : ideMode == FM_QOUT ? "QOUT" : ideMode == FM_DIO ? "DIO" : ideMode == FM_DOUT ? "DOUT" : "UNKNOWN"));

  //pin setup
  pinMode(LED_BUILTIN, OUTPUT); //debug led
  digitalWrite(LED_BUILTIN, LOW);

  pinMode(IO_OUT1, OUTPUT); digitalWrite(IO_OUT1, LOW);
  pinMode(IO_OUT2, OUTPUT); digitalWrite(IO_OUT2, LOW);
  //pinMode(IO_OUT3, OUTPUT); digitalWrite(IO_OUT3, HIGH);
  //pinMode(IO_OUT4, OUTPUT); digitalWrite(IO_OUT4, HIGH);
  //pinMode(IO_OUT5, OUTPUT); digitalWrite(IO_OUT5, HIGH);

  pinMode(IO_IN1, INPUT_PULLUP);
}

bool WiFi_Exists(int num, String ssid) {
  for (int i = 0; i < num; ++i) {
    if (WiFi.SSID(i) == ssid) {
      Serial.print("Found SSID: ");  Serial.print(ssid); Serial.print(" (");  Serial.print(WiFi.RSSI(i)); Serial.println(')');
      return true;
    }
  }
  Serial.print("Not found SSID: "); Serial.println(ssid);
  return false;
}

String WiFi_Scan(void) {
  //  WiFi.mode(WIFI_STA);
  //  WiFi.disconnect();

  int n = WiFi.scanNetworks();
  if (WiFi_Exists(n, WIFI_SSID_1)) return WIFI_SSID_1; //todo store into eeprom from UART
  if (WiFi_Exists(n, WIFI_SSID_2)) return WIFI_SSID_2;

  return "";
}

void loop(void) {
  //  if (!digitalRead(IO_IN1)) {
  //    digitalWrite(IO_OUT1, LOW);
  //  } else {
  //    digitalWrite(IO_OUT1, HIGH);
  //  }
  String ssid = WiFi_Scan();
  if (ssid == "") {
    Serial.println("Test: no");
  } else {
    Serial.print("Test:yes"); Serial.println(ssid);
  }

  delay(1000);
  //Serial.print(".");


}
