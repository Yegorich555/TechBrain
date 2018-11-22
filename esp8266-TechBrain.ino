#include "ESP8266WiFi.h"
#include "EEPROM.h"

#define e_StrLen 30 //max-length 30byte
#define e_SSID_Addr 0 //eeprom address for WIFI_SSID_1
#define e_PASS_Addr (e_SSID_Addr+e_StrLen) //eeprom address for WIFI_SSID_1
String WIFI_SSID_1 = ""; //stored into eeprom
String WIFI_PASS_1 = ""; //stored into eeprom

#define WIFI_SSID_2 "ESPCfg" // the second wifi point if the first doesn't exist
#define WIFI_PASS_2 "atmel^8266"

#define UART_BAUD 115200

#define IO_OUT1 16
#define IO_OUT2 14

//debug led
#define LED_BUILTIN 2 //by default and also Tx1 by default
#include <Ticker.h>
Ticker flipper;
typedef enum dbgLed_mode_e {
  dbgLed_ON = 0, //always on
  dbgLed_Connecting, //fast blink
  dbgLed_Connected  //long blink
} dbgLed_mode_e;

void flip() {
  digitalWrite(LED_BUILTIN, !digitalRead(LED_BUILTIN));
}

void setDbgLed(uint8_t dbgLedMode) {
  switch (dbgLedMode) {
    case dbgLed_ON: flipper.detach(); digitalWrite(LED_BUILTIN, LOW); break;
    case dbgLed_Connecting: flipper.attach(0.4, flip); break;
    case dbgLed_Connected: flipper.attach(1, flip); break;
  }
}

void setup(void) {
  //debug led
  pinMode(LED_BUILTIN, OUTPUT); setDbgLed(dbgLed_ON);

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

unsigned long prevWifi = 0;
bool WiFi_TryConnect(void) {
  byte status = WiFi.status();
  if (status == WL_CONNECTED) {
    setDbgLed(dbgLed_Connected);
    return true; // todo getIp
  }

  unsigned long cur = millis();
  if (cur - prevWifi >= 1000) { //each second
    prevWifi = cur;
  } else {
    return false;
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
  if (ssid != "") {
    WiFi.begin(ssid.c_str(), pass.c_str());
    setDbgLed(dbgLed_Connecting);
    Serial.print("Connecting to "); Serial.print(ssid); Serial.println(" ...");
  }

  return false;
}

int8_t char_indexOf(char *str, const char symb, int8_t startIndex = 0 )
{
  int8_t index = startIndex;
  str += startIndex;
  while (*str)
  {
    if (*str == symb)
      return index;
    ++str;
    ++index;
  }
  return -1;
}

bool str_match(char *str, const char *search, uint8_t startIndex = 0)
{
  str += startIndex;
  while (*search)
  {
    if (*str != *search)
      return false;
    ++search;
    ++str;
  }

  return true;
}

const char strCmd_Start[] = "esp_";
const char* strCmd[3] = {
  "setout1",
  "setout2",
  "setoutall"
};

void listenSerial() {
  size_t len = Serial.available();
  //  Serial.print(sizeof(*strCmd)); Serial.print('_'); Serial.print(sizeof(strCmd_Start)); Serial.print('_');
  //  Serial.print('_');  Serial.println(strCmd[0]);
  if (!len) {
    return;
  }

  if (len < 5) { //very small parcel
    delay(5); //waiting for the rest part of parcel
    len = Serial.available();
  }

  char bytes[len + 1];
  for (unsigned int i = 0; i < len; ++i) {
    bytes[i] = (char)Serial.read();
  }
  bytes[len] = 0;

  bool ok = str_match(bytes, strCmd_Start); //esp_setout1(0)
  if (!ok) {
    Serial.print(bytes); //todo send to TCP
  } else  { //checkCmd
    for (unsigned int i = 0; i < sizeof(*strCmd) - 1; ++i) {
      if (str_match(bytes, strCmd[i], sizeof(strCmd_Start) - 1)) {
        Serial.print("match");
        Serial.println(i);
        break;
      }
    }
  }
}

void loop(void) {
  WiFi_TryConnect();
  listenSerial();

  delay(1000);
  //Serial.print(".");
}
