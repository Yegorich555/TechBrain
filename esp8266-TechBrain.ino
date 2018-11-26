#include "ESP8266WiFi.h"
#include "EEPROM.h"

#define e_StrLen 30                          //max-length 30byte
#define e_SSID_Addr 0                        //eeprom address for WIFI_SSID_1
#define e_PASS_Addr (e_SSID_Addr + e_StrLen) //eeprom address for WIFI_PASS_1
#define e_DBG_Addr (e_PASS_Addr + e_StrLen)  //eeprom address for Debug (ON/OFF)
#define e_SN_Addr (e_DBG_Addr + 1)           //eeprom address for Serial Number (max 2 bytes)
#define e_SRVPORT_Addr (e_SN_Addr + 2)       //eeprom address for Server_PORT (2 bytes)
#define e_SRVIPL_Addr (e_SRVPORT_Addr + 2)   //eeprom address for SERVER_IP_LAST

String WIFI_SSID_1 = ""; //stored into eeprom
String WIFI_PASS_1 = ""; //stored into eeprom

#define WIFI_SSID_2 "ESPCfg" // the second wifi point if the first doesn't exist
#define WIFI_PASS_2 "atmel^8266"

uint8_t WIFI_SERVER_PORT = 80;
uint16_t SERVER_PORT = 1234; //stored into eeprom
uint8_t SERVER_IP_LAST = 1;  //stored into eeprom //mask server IP = x.x.x.SERVER_IP_LAST
uint8_t MY_SN = 0;           //stored into eeprom - SerialNumber

#define UART_BAUD 115200
bool DEBUG_EN = true; //stored into eeprom
#define DEBUG_MSG(v)   \
  if (DEBUG_EN)        \
  {                    \
    Serial.println(v); \
  }
#define DEBUG_MSGF(...)         \
  if (DEBUG_EN)                 \
  {                             \
    Serial.printf(__VA_ARGS__); \
  }

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
  cmd_out1 = 0, //change output 1
  cmd_out2,     //change output 2
  cmd_outAll,   //change all outputs
  cmd_ssid,     //set WiFi ssid
  cmd_pass,     //set WiFi password
  cmd_rst,      // reset chip
  cmd_dbg,      // turn debug on/off
  cmd_sn,       // set serial number
  cmd_port,     // set serverPort,
  cmd_ipl       //last number of server's IP address
} cmd_e;
const String strCmd_Start = "esp_"; //Pattern: esp_out1(0)
const structCmd cmd[] = {
    //str only in lowerCase!!!
    {"out1", cmd_out1},     //from out1(0) to out1(100)
    {"out2", cmd_out2},     //from out2(0) to out2(100)
    {"outall", cmd_outAll}, //for all outs
    {"ssid", cmd_ssid},     //esp_pass(ssidName)
    {"pass", cmd_pass},     //esp_pass(password)
    {"rst", cmd_rst},       //esp_rst()
    {"dbg", cmd_dbg},       //esp_dbg(0) - esp_dbg(1)
    {"sn", cmd_sn},         //esp_sn(serialNumber)
    {"port", cmd_port},     //esp_port(portNumber)
    {"ipl", cmd_ipl},       //esp_ipl(ipLastNumber)
};

#define strArrLength(v) sizeof(v) / sizeof(v[0])

void flip()
{
  digitalWrite(LED_BUILTIN, !digitalRead(LED_BUILTIN));
}

uint8_t _lastDbgLedMode;
void setDbgLed(uint8_t dbgLedMode)
{
  if (dbgLedMode == _lastDbgLedMode)
    return;
  _lastDbgLedMode = dbgLedMode;

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

WiFiEventHandler stationConnectedHandler;
WiFiEventHandler stationGotIPHandler;

void onStationConnected(const WiFiEventStationModeConnected &evt)
{
  setDbgLed(dbgLed_Connected);
  DEBUG_MSG("Connected to '" + evt.ssid + "' (" + WiFi.RSSI() + "db)");
}

IPAddress ipAddress;
bool isNeedSendIp = true;
void onStationGotIp(const WiFiEventStationModeGotIP &evt)
{
  if (ipAddress != evt.ip)
  {
    isNeedSendIp = true;
    ipAddress = evt.ip;
  }
  DEBUG_MSG("IP address: " + ipAddress.toString());
}

void setup(void)
{
  //debug led
  pinMode(LED_BUILTIN, OUTPUT);
  setDbgLed(dbgLed_ON);
  //analogWriteFreq(new_frequency); 1kHz by default

  delay(100); //delay for debuging in ArduinoIDE
  Serial.begin(UART_BAUD);
  DEBUG_MSG("\nstarting...");
  //Serial1.begin(UART_BAUD); //Tx1 or GPIO2; Rx1 is not accessible

  //chip info
  uint32_t chipRealSize = ESP.getFlashChipRealSize();
  uint32_t chipSize = ESP.getFlashChipSize();
  if (chipRealSize < chipSize)
  {
    Serial.printf("!!!Chip size (%u) is wrong. Real is %u\n", chipSize, chipRealSize);
  }
  DEBUG_MSGF("CpuFreq: %u MHz\n", ESP.getCpuFreqMHz());
  DEBUG_MSGF("Flash speed: %u Hz\n", ESP.getFlashChipSpeed());
  FlashMode_t ideMode = ESP.getFlashChipMode();
  DEBUG_MSGF("Flash mode:  %s\n", (ideMode == FM_QIO ? "QIO" : ideMode == FM_QOUT ? "QOUT" : ideMode == FM_DIO ? "DIO" : ideMode == FM_DOUT ? "DOUT" : "UNKNOWN"));

  //pin setup
  pinMode(IO_OUT1, OUTPUT);
  digitalWrite(IO_OUT1, LOW);
  pinMode(IO_OUT2, OUTPUT);
  digitalWrite(IO_OUT2, LOW);
  //pinMode(IO_IN1, INPUT_PULLUP); bool in1 = digitalRead(IO_IN1);

  //eeprom
  EEPROM.begin(512);
  WIFI_SSID_1 = EEPROM_ReadStr(e_SSID_Addr, e_StrLen);
  WIFI_PASS_1 = EEPROM_ReadStr(e_PASS_Addr, e_StrLen);
  DEBUG_EN = EEPROM.read(e_DBG_Addr) != 0;
  MY_SN = EEPROM.read(e_SN_Addr);
  // SERVER_PORT = EEPROM_ReadInt(e_SRVPORT_Addr);
  // SERVER_IP_LAST = EEPROM.read(e_SRVIPL_Addr);

  stationConnectedHandler = WiFi.onStationModeConnected(&onStationConnected);
  stationGotIPHandler = WiFi.onStationModeGotIP(&onStationGotIp);
}

//This function will read a 2 byte integer from the eeprom at the specified address and address + 1
unsigned int EEPROM_ReadInt(int p_address)
{
  byte lowByte = EEPROM.read(p_address);
  byte highByte = EEPROM.read(p_address + 1);

  return ((lowByte << 0) & 0xFF) + ((highByte << 8) & 0xFF00);
}

String EEPROM_ReadStr(int startAddress, uint8_t length)
{
  String str = "";
  for (uint8_t i = 0; i < length; ++i)
  {
    char v = EEPROM.read(startAddress++);
    if (v == 0 || v == 255)
      break;

    str += String(v);
  }
  return str;
}

void EEPROM_Write(int address, uint8_t value)
{
  EEPROM.write(address, value);
  EEPROM.commit();
}

void EEPROM_Write(int address, uint16_t value)
{
  byte lowByte = ((value >> 0) & 0xFF);
  byte highByte = ((value >> 8) & 0xFF);

  EEPROM.write(address, lowByte);
  EEPROM.write(address + 1, highByte);
  EEPROM.commit();
}

bool EEPROM_Write(int startAddress, String str)
{
  unsigned int len = str.length();
  if (len > e_StrLen)
  {
    Serial.println("Err writeEEprom: maxLength " + len + String(" > ") + e_StrLen);
    return false;
  }
  for (unsigned int i = 0; i < len; ++i)
    EEPROM.write(startAddress++, str[i]);

  if (len < e_StrLen)
    EEPROM.write(startAddress, 0);

  EEPROM.commit();
  return true;
}

bool _isWiFiFirst = true;
bool _isFirstConnect = true;
bool WiFi_TryConnect(void)
{
  uint8_t status = WiFi.status();
  if (status == WL_CONNECTED)
    return true;

  if (_isFirstConnect || status == WL_NO_SSID_AVAIL || status == WL_CONNECT_FAILED)
  {
    if (!_isFirstConnect)
      DEBUG_MSG("ConnectionFailed to '" + WiFi.SSID() + "': " + status);

    String ssid;
    String pass;
    if (_isWiFiFirst)
    {
      ssid = WIFI_SSID_1;
      pass = WIFI_PASS_1;
    }
    else
    {
      ssid = WIFI_SSID_2;
      pass = WIFI_PASS_2;
    }

    WiFi.begin(ssid.c_str(), pass.c_str());
    setDbgLed(dbgLed_Connecting);
    DEBUG_MSG("Connecting to '" + ssid + "'...");

    _isWiFiFirst = !_isWiFiFirst;
    _isFirstConnect = false;
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
    analogWrite(num, 1023 * v / 100);
}

void listenSerial()
{
  size_t len = Serial.available();
  if (!len)
    return;

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
    Serial.print(bytes); //todo send to TCP
  else
  { //getCmd
    str = str.substring(strCmd_Start.length());
    int startIndex = str.indexOf('(');
    int endIndex = str.indexOf(")\n", startIndex + 1);
    if (startIndex != -1 && endIndex != -1)
    {
      String cmdStr = str.substring(0, startIndex);
      cmdStr.toLowerCase();
      for (unsigned int i = 0; i < sizeof(cmd); ++i)
      {
        if (cmdStr == cmd[i].str)
        {
          if (cmd[i].type == cmd_rst)
            ESP.restart();
          else
          {
            str = str.substring(startIndex + 1, endIndex);
            uint8_t v = 0;
            uint16_t v16 = 0;
            switch (cmd[i].type)
            {
            case cmd_ssid:
              if (EEPROM_Write(e_SSID_Addr, str))
                WIFI_SSID_1 = str;
              break;
            case cmd_pass:
              if (EEPROM_Write(e_PASS_Addr, str))
                WIFI_PASS_1 = str;
              break;

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

            case cmd_dbg:
              v = (uint8_t)str.toInt();
              EEPROM_Write(e_DBG_Addr, v);
              DEBUG_EN = v != 0;
              break;
            case cmd_sn:
              v = (uint8_t)str.toInt();
              EEPROM_Write(e_SN_Addr, v);
              MY_SN = v;
              break;
            case cmd_port:
              v16 = (uint16_t)str.toInt();
              EEPROM_Write(e_SRVPORT_Addr, v16);
              SERVER_PORT = v16;
              break;
            case cmd_ipl:
              v = (uint8_t)str.toInt();
              EEPROM_Write(e_SRVIPL_Addr, v);
              SERVER_IP_LAST = v;
              break;
            }
            Serial.println("OK");
            return;
          }
        }
      }
    }
    Serial.println("Error esp_command");
  }
}

bool TCP_SendNumber(IPAddress ipAddr, uint16_t port)
{
  DEBUG_MSG("TCP > connecting to '" + ipAddr.toString() + ':' + port + "'...");

  WiFiClient client;
  if (!client.connect(ipAddr, port))
  {
    DEBUG_MSG("TCP > connection failed");
    return false;
  }

  client.println("I am (" + String(MY_SN) + ')');
  unsigned long timeout = millis();
  while (millis() - timeout < 5000) // timeout 5000ms
  {
    if (client.available())
    {
      String line = client.readStringUntil('\r'); //default timeout 1000ms
      if (line.equalsIgnoreCase("OK"))
      {
        isNeedSendIp = false;
        client.stop();
        DEBUG_MSG("TCP > sending is ok");
        return true;
      }
      else
        DEBUG_MSG("TCP > sending failed: '" + line + '\'');
    }
  }

  DEBUG_MSG("TCP > timeout");
  client.stop();
  return false;
}

WiFiServer server(WIFI_SERVER_PORT);
void TCP_Loop()
{
  if (isNeedSendIp)
  {
    IPAddress serverIP = IPAddress(ipAddress);
    serverIP[3] = SERVER_IP_LAST;
    for (uint8_t i = 0; i < 3; ++i) //3 times for different ports
    {
      if (TCP_SendNumber(serverIP, SERVER_PORT + i))
      {
        isNeedSendIp = false;
        break;
      }
    }
  }
  if (isNeedSendIp)
    return;

  server.begin();
  DEBUG_MSG("Server started: " + String(server.status()));
}

unsigned long _prevWifi = 0;
void loop(void)
{
  unsigned long cur = millis();
  if (cur - _prevWifi >= 500)
  {
    bool isConnected = WiFi_TryConnect();
    if (isConnected)
      TCP_Loop();
    _prevWifi = millis();
  }
  listenSerial();
}
