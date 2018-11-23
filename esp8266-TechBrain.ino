#include "ESP8266WiFi.h"
#include "EEPROM.h"

#define e_StrLen 30                          //max-length 30byte
#define e_SSID_Addr 0                        //eeprom address for WIFI_SSID_1
#define e_PASS_Addr (e_SSID_Addr + e_StrLen) //eeprom address for WIFI_SSID_1
String WIFI_SSID_1 = "";                     //stored into eeprom
String WIFI_PASS_1 = "";                     //stored into eeprom

#define WIFI_SSID_2 "ESPCfg" // the second wifi point if the first doesn't exist
#define WIFI_PASS_2 "atmel^8266"

#define UART_BAUD 115200

#define IO_OUT1 16
#define IO_OUT2 14

//debug led
#define LED_BUILTIN 2 //by default and also Tx1 by default
#include <Ticker.h>
Ticker flipper;
typedef enum dbgLed_mode_e
{
  dbgLed_ON = 0,     //always on
  dbgLed_Connecting, //fast blink
  dbgLed_Connected   //long blink
} dbgLed_mode_e;

//esp commands
struct structCmd
{
  String str;
  uint8_t type;
};
typedef enum cmd_e
{
  cmd_out1 = 0, //always on
  cmd_out2,     //fast blink
  cmd_outAll,   //long blink
  cmd_ssid,
  cmd_pass,
  cmd_rst
} cmd_e;
const String strCmd_Start = "esp_"; //Pattern: esp_out1(0)
const structCmd cmd[] = {
    //str only in lowerCase!!!
    {"out1", cmd_out1}, //from out1(0) to out1(100)
    {"out2", cmd_out2},
    {"outall", cmd_outAll},
    {"ssid", cmd_ssid},
    {"pass", cmd_pass},
    {"rst", cmd_rst},
};

#define strArrLength(v) sizeof(v) / sizeof(v[0])

void flip()
{
  digitalWrite(LED_BUILTIN, !digitalRead(LED_BUILTIN));
}

void setDbgLed(uint8_t dbgLedMode)
{
  switch (dbgLedMode)
  {
  case dbgLed_ON:
    flipper.detach();
    digitalWrite(LED_BUILTIN, LOW);
    break;
  case dbgLed_Connecting:
    flipper.attach(0.4, flip);
    break;
  case dbgLed_Connected:
    flipper.attach(1, flip);
    break;
  }
}

void setup(void)
{
  //debug led
  pinMode(LED_BUILTIN, OUTPUT);
  setDbgLed(dbgLed_ON);
  //analogWriteFreq(new_frequency); 1kHz by default

  delay(100); //delay for debuging in ArduinoIDE
  Serial.begin(UART_BAUD);
  Serial.println("\nstarting...");
  //Serial1.begin(UART_BAUD); //Tx1 or GPIO2; Rx1 is not accessible

  //chip info
  uint32_t chipRealSize = ESP.getFlashChipRealSize();
  uint32_t chipSize = ESP.getFlashChipSize();
  if (chipRealSize < chipSize)
  {
    Serial.printf("!!!Chip size (%u) is wrong. Real is %u\n", chipSize, chipRealSize);
  }
  Serial.printf("CpuFreq: %u MHz\n", ESP.getCpuFreqMHz());
  Serial.printf("Flash speed: %u Hz\n", ESP.getFlashChipSpeed());
  FlashMode_t ideMode = ESP.getFlashChipMode();
  Serial.printf("Flash mode:  %s\n", (ideMode == FM_QIO ? "QIO" : ideMode == FM_QOUT ? "QOUT" : ideMode == FM_DIO ? "DIO" : ideMode == FM_DOUT ? "DOUT" : "UNKNOWN"));

  //pin setup
  pinMode(IO_OUT1, OUTPUT);
  digitalWrite(IO_OUT1, LOW);
  pinMode(IO_OUT2, OUTPUT);
  digitalWrite(IO_OUT2, LOW);
  //pinMode(IO_IN1, INPUT_PULLUP); bool in1 = digitalRead(IO_IN1);

  //eeprom
  EEPROM.begin(512);
  WIFI_SSID_1 = readStrEeprom(e_SSID_Addr);
  WIFI_PASS_1 = readStrEeprom(e_PASS_Addr);
  //bool ok = writeStrEeprom(e_SSID_Addr, "qwerty");
  //Serial.println(ok);
}

String readStrEeprom(int startAddress)
{
  String str = "";
  for (int i = 0; i < e_StrLen; ++i)
  {
    char v = EEPROM.read(startAddress++);
    if (v == 0 || v == 255)
    {
      break;
    }
    str += String(v);
  }
  return str;
}

bool writeStrEeprom(int startAddress, String str)
{
  unsigned int len = str.length();
  if (len > e_StrLen)
  {
    Serial.print("Err writeEEprom:maxLength ");
    Serial.print(len);
    Serial.print(" > ");
    Serial.println(e_StrLen);
    return false;
  }
  for (unsigned int i = 0; i < len; ++i)
  {
    EEPROM.write(startAddress++, str[i]);
  }
  if (len < e_StrLen)
  {
    EEPROM.write(startAddress, 0);
  }
  EEPROM.commit();
  return true;
}

bool WiFi_Exists(int num, String ssid)
{
  for (int i = 0; i < num; ++i)
  {
    if (WiFi.SSID(i) == ssid)
    {
      Serial.print("Found SSID: '");
      Serial.print(ssid);
      Serial.print("' (");
      Serial.print(WiFi.RSSI(i));
      Serial.println(')');
      return true;
    }
  }
  Serial.print("Not found SSID: '");
  Serial.print(ssid);
  Serial.println('\'');
  return false;
}

bool WiFi_TryConnect(void)
{
  byte status = WiFi.status();
  if (status == WL_CONNECTED)
  {
    setDbgLed(dbgLed_Connected);
    return true; // todo getIp
  }

  int n = WiFi.scanNetworks(); //takes about 2184ms
  String ssid;
  String pass;
  if (WiFi_Exists(n, WIFI_SSID_1))
  {
    ssid = WIFI_SSID_1;
    pass = WIFI_PASS_1;
  }
  else if (WiFi_Exists(n, WIFI_SSID_2))
  {
    ssid = WIFI_SSID_2;
    pass = WIFI_PASS_2;
  }
  if (ssid != "")
  {
    WiFi.begin(ssid.c_str(), pass.c_str());
    setDbgLed(dbgLed_Connecting);
    Serial.print("Connecting to ");
    Serial.print(ssid);
    Serial.println(" ...");
  }

  return false;
}

void updatePort(const uint8_t num, String strVal)
{
  uint8_t v = (uint8_t)strVal.toInt();
  if (v == 100)
    digitalWrite(num, HIGH);
  else if (v == 0)
    digitalWrite(num, LOW);
  else
  {
    analogWrite(num, 1023 * v / 100);
  }
}

void listenSerial()
{
  size_t len = Serial.available();
  if (!len)
  {
    return;
  }

  if (len < 5)
  {           //very small parcel
    delay(5); //waiting for the rest part of parcel
    len = Serial.available();
  }

  char bytes[len + 1];
  for (unsigned int i = 0; i < len; ++i)
  {
    bytes[i] = (char)Serial.read();
  }
  bytes[len] = 0;
  String str = String(bytes);

  bool ok = str.startsWith(strCmd_Start); //esp_out1(0)
  if (!ok)
  {
    Serial.print(bytes); //todo send to TCP
  }
  else
  { //getCmd
    str = str.substring(strCmd_Start.length());
    str.toLowerCase(); //for ignoring case
    for (unsigned int i = 0; i < sizeof(cmd); ++i)
    {
      if (str.startsWith(cmd[i].str))
      {
        if (cmd[i].type == cmd_rst)
        {
          ESP.restart();
        }
        else
        {
          int fromIndex = cmd[i].str.length();
          int startIndex = str.indexOf('(', fromIndex) + 1;
          int endIndex = str.indexOf(")\n", startIndex + 1);
          if (startIndex == 0 || endIndex == -1)
            break;

          str = str.substring(startIndex, endIndex);
          switch (cmd[i].type)
          {
          case cmd_out1:
            updatePort(IO_OUT1, str);
            break;
          case cmd_out2:
            updatePort(IO_OUT2, str);
            break;
          case cmd_outAll:
            updatePort(IO_OUT1, str);
            updatePort(IO_OUT2, str);
            break;
          case cmd_ssid:
            writeStrEeprom(e_SSID_Addr, str);
            break;
          case cmd_pass:
            writeStrEeprom(e_PASS_Addr, str);
            break;
          }
          Serial.println("OK");
          return;
        }
      }
    }
    Serial.println("error esp_command");
  }
}

unsigned long prevWifi = 0;
void loop(void)
{
  unsigned long cur = millis();
  if (cur - prevWifi >= 1000)
  { //each second
    Serial.println("try");
    WiFi_TryConnect();
    prevWifi = millis();
  }

  listenSerial();

  //delay(1000);
  //Serial.print(".");
}
