#include <ESP8266WiFi.h>
//#include <EEPROM.h> //defined in extensions.h
#include "extensions.h"

//todo sleepMode

#define e_StrLen 30                          //max-length 30byte
#define e_SSID_Addr 0                        //eeprom address for WIFI_SSID_1
#define e_PASS_Addr (e_SSID_Addr + e_StrLen) //eeprom address for WIFI_PASS_1
#define e_DBG_Addr (e_PASS_Addr + e_StrLen)  //eeprom address for Debug (ON/OFF)
#define e_SN_Addr (e_DBG_Addr + 1)           //eeprom address for Serial Number (max 2 bytes)
#define e_SRVPORT_Addr (e_SN_Addr + 2)       //eeprom address for Server_PORT (2 bytes)
#define e_SRVIPL_Addr (e_SRVPORT_Addr + 2)   //eeprom address for SERVER_IP_LAST
#define e_BAUD_Addr (e_SRVIPL_Addr + 1)      //eeprom address for UART baudRate

String WIFI_SSID_1 = ""; //stored into eeprom
String WIFI_PASS_1 = ""; //stored into eeprom

#define WIFI_SSID_2 "ESPCfg" // the second wifi point if the first doesn't exist
#define WIFI_PASS_2 "atmel^8266"

#define MAX_SRV_CLIENTS 2
WiFiClient serverClients[MAX_SRV_CLIENTS];

uint8_t WIFI_SERVER_PORT = 80;
uint16_t SERVER_PORT = 1234; //stored into eeprom
uint8_t SERVER_IP_LAST = 1;  //stored into eeprom //mask server IP = x.x.x.SERVER_IP_LAST
uint8_t MY_SN = 0;           //stored into eeprom - SerialNumber
WiFiServer server(WIFI_SERVER_PORT);

unsigned long UART_BAUD = 115200; //stored in eeprom
bool DEBUG_EN = true;             //stored into eeprom
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

uint8_t outStates[2];
#define IO_OUT1 16 //todo redefine
#define IO_OUT2 14 //todo redefine

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
  cmd_rst,      //reset chip
  cmd_dbg,      //turn debug on/off
  cmd_sn,       //set serial number
  cmd_port,     //set serverPort,
  cmd_ipl,      //last number of server's IP address
  cmd_baud,     //baudRate for UART

  cmd_ping, //ping - only for testing esp-connection
  cmd_state //info about current state
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
    {"baud", cmd_baud},     //cmd_baud(serialBaudRate)

    {"ping", cmd_ping},   //cmd_ping()
    {"state", cmd_state}, //cmd_state()
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

bool isNeedSendIp = true;
TimeLaps t_isNeedSendIp;
IPAddress ipAddress;
void onStationGotIp(const WiFiEventStationModeGotIP &evt)
{
  if (ipAddress != evt.ip)
  {
    isNeedSendIp = true;
    ipAddress = evt.ip;
  }
  DEBUG_MSG("IP address: " + ipAddress.toString());
}

unsigned long checkTimeLast = 0;
void CheckTime(const String txt = "")
{
  if (txt != "")
  {
    unsigned long v = millis() - checkTimeLast;
    //if (v > 10) //only if more than 10ms
    // {
    Serial.print(txt);
    Serial.println(v);
    // }
  }
  checkTimeLast = millis();
}

void setup(void)
{
  //debug led
  pinMode(LED_BUILTIN, OUTPUT);
  setDbgLed(dbgLed_ON);
  //analogWriteFreq(new_frequency); 1kHz by default

  Serial.begin(UART_BAUD);
  //Serial1.begin(UART_BAUD); //Tx1 or GPIO2; Rx1 is not accessible

  //eeprom
  EEPROM_EXT.begin(512);
  DEBUG_EN = EEPROM_EXT.readByte(e_DBG_Addr) != 0;

  UART_BAUD = BaudRate::fromNum(EEPROM_EXT.read(e_BAUD_Addr), UART_BAUD);
  Serial.begin(UART_BAUD);

  if (DEBUG_EN)
    delay(500); //delay for debuging in ArduinoIDE
  DEBUG_MSG("\nstarting...");

  WIFI_SSID_1 = EEPROM_EXT.readStr(e_SSID_Addr, e_StrLen);
  WIFI_PASS_1 = EEPROM_EXT.readStr(e_PASS_Addr, e_StrLen);
  MY_SN = EEPROM_EXT.readByte(e_SN_Addr);
  SERVER_PORT = EEPROM_EXT.readInt(e_SRVPORT_Addr);
  SERVER_IP_LAST = EEPROM_EXT.readByte(e_SRVIPL_Addr);

  //chip info
  uint32_t chipRealSize = ESP.getFlashChipRealSize();
  uint32_t chipSize = ESP.getFlashChipSize();
  if (chipRealSize < chipSize)
    Serial.printf("!!!Chip size (%u) is wrong. Real is %u\n", chipSize, chipRealSize);

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

  //wifi setup
  stationConnectedHandler = WiFi.onStationModeConnected(&onStationConnected);
  stationGotIPHandler = WiFi.onStationModeGotIP(&onStationGotIp);

  delay(1);
  server.begin();
  server.setNoDelay(true);
}

bool _isWiFiFirst = true;
bool _isFirstConnect = true;
uint8_t _lastStatus;
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
  else if (_lastStatus != status)
  {
    DEBUG_MSG("Connection status: " + String(status));
    _lastStatus = status;
  }

  return false;
}

void updatePort(const uint8_t arrNum, const uint8_t num, uint8_t v)
{
  outStates[arrNum - 1] = v;
  if (v == 100)
    digitalWrite(num, HIGH);
  else if (v == 0)
    digitalWrite(num, LOW);
  else
    analogWrite(num, 1023 * v / 100);
}

bool listenStream(Stream &stream, Stream &outStream)
{
  if (!stream.available())
    return false;

  uint8_t i = 0;
  String str = "";
  TimeLaps tBetweenBytes;
  TimeLaps tRead;
  bool isGetCmd = false;
  while (1)
  {
    if (stream.available())
    {
      tBetweenBytes.reset();

      char b = (char)stream.read();
      if (isGetCmd)
      {
        if (b == '\n' || b == '\r') //wait end of parcel
          break;
        else
          str += b;
      }
      else if (b == strCmd_Start[i])
      { //todo compare with upper case
        ++i;
        if (i == strCmd_Start.length())
        {
          isGetCmd = true;
          str = "";
        }
        else
          str += b;
      }
      else
      {
        outStream.print(str);
        outStream.print(b);
        str = "";
        i = 0;
      }
    }
    else
    {
      if (i >= 255 || tBetweenBytes.isPassed(5, true) || tRead.isPassed(100, true)) //wait 5ms between bytes or 100ms total
      {
        if (isGetCmd)
          DEBUG_MSG("Stream. Reading timeout")
        outStream.print(str);
        return false;
      }
    }
  }

  int startIndex = str.indexOf('(');
  int endIndex = str.indexOf(")", startIndex + 1);
  bool isError = false;
  if (startIndex == -1 || endIndex == -1)
    isError = true;
  else
  {
    String cmdStr = str.substring(0, startIndex);
    cmdStr.toLowerCase();
    uint8_t i;
    for (i = 0; i < sizeof(cmd); ++i)
    {
      if (cmdStr == cmd[i].str)
        break;
    }

    if (cmd[i].type == cmd_rst)
      ESP.restart();
    else if (cmd[i].type == cmd_ping)
      ;
    else if (cmd[i].type == cmd_state)
    {
      stream.print("State: SN=");
      stream.print(MY_SN);
      stream.print(",baud=");
      stream.print(UART_BAUD);
      stream.print(",dbg=");
      stream.print(DEBUG_EN);

      stream.print(",out1=");
      stream.print(outStates[0]);
      stream.print(",out2=");
      stream.println(outStates[1]);
    }
    else
    {
      str = str.substring(startIndex + 1, endIndex);
      uint8_t v = 0;
      uint16_t v16 = 0;

      switch (cmd[i].type)
      {
      case cmd_ssid:
        if (EEPROM_EXT.write(e_SSID_Addr, str, e_StrLen))
          WIFI_SSID_1 = str;
        break;
      case cmd_pass:
        if (EEPROM_EXT.write(e_PASS_Addr, str, e_StrLen))
          WIFI_PASS_1 = str;
        break;

      case cmd_out1:
        v = (uint8_t)str.toInt();
        updatePort(1, IO_OUT1, v);
        break;
      case cmd_out2:
        v = (uint8_t)str.toInt();
        updatePort(2, IO_OUT2, v);
        break;
      case cmd_outAll:
        v = (uint8_t)str.toInt();
        updatePort(1, IO_OUT1, v);
        updatePort(2, IO_OUT2, v);
        break;

      case cmd_dbg:
        v = (uint8_t)str.toInt();
        EEPROM_EXT.write(e_DBG_Addr, v);
        DEBUG_EN = v != 0;
        break;
      case cmd_sn:
        v = (uint8_t)str.toInt();
        EEPROM_EXT.write(e_SN_Addr, v);
        MY_SN = v;
        break;
      case cmd_port:
        v16 = (uint16_t)str.toInt();
        EEPROM_EXT.write(e_SRVPORT_Addr, v16);
        SERVER_PORT = v16;
        break;
      case cmd_ipl:
        v = (uint8_t)str.toInt();
        EEPROM_EXT.write(e_SRVIPL_Addr, v);
        SERVER_IP_LAST = v;
        break;
      case cmd_baud:
      {
        uint32_t v32 = (uint32_t)str.toInt();
        if (BaudRate::isValid(v32))
          EEPROM_EXT.write(e_BAUD_Addr, BaudRate::toNum(v32));
        else
          isError = true;
      }
      break;

      default:
        isError = true;
        break;
      }
    }
  }
  stream.print(isError ? "Error: " : "OK: ");
  stream.println(str);

  return false;
}

bool TCP_SendNumber(IPAddress ipAddr, uint16_t port)
{
  DEBUG_MSG("TCP. Connecting to '" + ipAddr.toString() + ':' + port + "'...");

  WiFiClient client;
  if (!client.connect(ipAddr, port))
  {
    DEBUG_MSG("TCP. Connection failed");
    return false;
  }

  TimeLaps t;
  for (uint8_t i = 0; i < 3; ++i) //3 times for repeat
  {
    client.println("I am (" + String(MY_SN) + ')');
    t.reset();
    while (!t.isPassed(2000, true)) // timeout 2000ms
    {
      if (client.available())
      {
        String line = client.readStringUntil('\n'); //todo wait for \r also //default timeout 1000ms
        if (line.equalsIgnoreCase("OK"))
        {
          isNeedSendIp = false;
          client.stop();
          DEBUG_MSG("TCP. Sending ok");
          return true;
        }
        else
          DEBUG_MSG("TCP. Sending failed: '" + line + '\'');

        client.flush();
      }
    }
  }

  DEBUG_MSG("TCP. Timeout");
  client.stop();
  return false;
}

uint8_t lastClientNum = 0;
TimeLaps _t_sendIpT;
void TCP_Loop()
{
  uint8_t i;

  if (!isNeedSendIp && t_isNeedSendIp.isPassed(5 * 60 * 1000, true)) //each 5 minutes
    isNeedSendIp = true;

  if (isNeedSendIp && ipAddress != 0)
  {
    if (!_t_sendIpT.isPassed(2000)) // sendNumber each 2 seconds;
      return;

    IPAddress serverIP = IPAddress(ipAddress[0], ipAddress[1], ipAddress[2], SERVER_IP_LAST);
    for (i = 0; i < 3; ++i) //3 times for different ports
    {
      if (TCP_SendNumber(serverIP, SERVER_PORT + i))
      {
        isNeedSendIp = false;
        t_isNeedSendIp.reset();
        break;
      }
    }
  }
  if (isNeedSendIp)
    return;

  //check if there are any new clients
  if (server.hasClient())
  {
    for (i = 0; i < MAX_SRV_CLIENTS; ++i)
    {
      //find free/disconnected spot
      if (!serverClients[i] || !serverClients[i].connected())
      {
        if (serverClients[i]) //stop if client was disconnected
        {
          serverClients[i].stop();
          DEBUG_MSG("TCP. Client is not connected");
        }

        serverClients[i] = server.available();
        DEBUG_MSG("TCP. New client: " + serverClients[i].remoteIP().toString());
        break;
      }
    }
    //no free/disconnected spot so reject
    if (i == MAX_SRV_CLIENTS)
    {
      WiFiClient serverClient = server.available();
      serverClient.stop();
      DEBUG_MSG("TCP. Max clients - connection rejected: " + serverClient.remoteIP().toString());
    }
  }

  //check clients for data
  for (i = 0; i < MAX_SRV_CLIENTS; ++i)
  {
    if (serverClients[i] && serverClients[i].connected())
    {
      if (serverClients[i].available()) //reset timeLaps if we have bytes
        t_isNeedSendIp.reset();

      bool isBytesDirect = listenStream(serverClients[i], Serial); //todo what if UART response doesn't have enough time
      if (isBytesDirect)                                           //miss next listening if we have bytes for Serial
      {
        lastClientNum = i;
        serverClients[i].println("testResponse"); //todo remove after testing;
        break;
      }
    }
  }
}

TimeLaps _tWifi;
void loop(void)
{
  if (_tWifi.isPassed(500))
  {
    WiFi_TryConnect();    
    _tWifi.reset();
  }

  listenStream(Serial, serverClients[lastClientNum]);
  TCP_Loop();
  delay(1);
}
