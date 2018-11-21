#include "ESP8266WiFi.h"
#include "EEPROM.h"


#define e_StrLen 30 //max-length 30byte
#define e_SSID_Addr 0 //eeprom address for WIFI_SSID_1
String WIFI_SSID_1 = ""; //stored into eeprom
#define e_PASS_Addr (e_SSID_Addr+e_StrLen) //eeprom address for WIFI_SSID_1
String WIFI_PASS_1 = ""; //stored into eeprom

#define WIFI_SSID_2 "ESPCfg" // the second wifi point if the first doesn't exist
#define WIFI_PASS_2 "atmel^8266"

#define UART_BAUD 115200

#define IO_OUT1 16
#define IO_OUT2 14
//#define LED_BUILTIN 2 //by default; Tx1 by default
//#define BUTTON_BUILTIN 0 //by default

void setup(void) {
  //debug led
  pinMode(LED_BUILTIN, OUTPUT);  digitalWrite(LED_BUILTIN, LOW);

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
  pinMode(IO_OUT1, OUTPUT); digitalWrite(IO_OUT1, LOW);
  pinMode(IO_OUT2, OUTPUT); digitalWrite(IO_OUT2, LOW);
  //pinMode(IO_IN1, INPUT_PULLUP); bool in1 = digitalRead(IO_IN1);

  //eeprom
  EEPROM.begin(512);
  WIFI_SSID_1 = readStrEeprom(e_SSID_Addr);
  WIFI_PASS_1 = readStrEeprom(e_PASS_Addr);
  //bool ok = writeStrEeprom(e_SSID_Addr, "qwerty");
  //Serial.println(ok);
}

uint8_t ledState = LOW;
unsigned long previousMillis = 0;
void setDebugLed(bool isOk) {
  if (isOk) {
    unsigned long currentMillis = millis();
    if (currentMillis - previousMillis >= 500) {
      previousMillis = currentMillis;
      if (ledState == LOW) {
        ledState = HIGH;  // Note that this switches the LED *off*
      } else {
        ledState = LOW;  // Note that this switches the LED *on*
      }
      digitalWrite(LED_BUILTIN, ledState);
    }
  }
  else {
   ledState = HIGH;
   digitalWrite(LED_BUILTIN, ledState);
  }
}

String readStrEeprom(int startAddress) {
  String str = "";
  for (int i = 0; i < e_StrLen; ++i) {
    char v = EEPROM.read(startAddress++);
    if (v == 0 || v == 255) {
      break;
    }
    str += String(v);
  }
  return str;
}

bool writeStrEeprom(int startAddress, String str) {
  unsigned int len = str.length();
  if (len > e_StrLen) {
    Serial.print("Err writeEEprom:maxLength ");
    Serial.print(len); Serial.print(" > "); Serial.println(e_StrLen);
    return false;
  }
  for (unsigned int i = 0; i < len; ++i) {
    EEPROM.write(startAddress++, str[i]);
  }
  if (len < e_StrLen) {
    EEPROM.write(startAddress, 0);
  }
  EEPROM.commit();
  return true;
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

bool WiFi_TryConnect(void) {
  byte status = WiFi.status();
  if (status == WL_CONNECTED) {
    return true; // todo getIp
  }

  int n = WiFi.scanNetworks();
  String ssid;
  String pass;
  if (WiFi_Exists(n, WIFI_SSID_1)) {
    ssid = WIFI_SSID_1;
    pass = WIFI_PASS_1;
  }
  else if (WiFi_Exists(n, WIFI_SSID_2)) {
    ssid = WIFI_SSID_2;
    pass = WIFI_PASS_2;
  }
  if (ssid) {    
    WiFi.begin(ssid.c_str(), pass.c_str());
    Serial.print("Connecting to :"); Serial.print(ssid); Serial.println(" ...");
  }

  return false;
}

void loop(void) {
  bool isConnected =  WiFi_TryConnect();
  setDebugLed(isConnected);


  delay(1000);
  //Serial.print(".");


}
