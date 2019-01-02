#include <ESP8266WiFi.h>
//#include <EEPROM.h>
#include "src/config.h"
#include "src/extensions.h"
#include "src/cmd.h"

#define DEBUG_MSG(v)      \
  if (cfgEEPROM.DEBUG_EN) \
  {                       \
    Serial.println(v);    \
  }
#define DEBUG_MSGF(...)         \
  if (cfgEEPROM.DEBUG_EN)       \
  {                             \
    Serial.printf(__VA_ARGS__); \
  }

WiFiClient serverClients[MAX_SRV_CLIENTS];
WiFiServer server(WIFI_SERVER_PORT);

//debug led
#include <Ticker.h>
Ticker flipper;
typedef enum dbgLed_mode_e
{
  dbgLed_ON = 0,     //always on
  dbgLed_Connecting, //fast blink
  dbgLed_Connected   //long blink
} dbgLed_mode_e;

#define DBG_LED_ON() analogWrite(LED_BUILTIN, 1000) //max 1023 is off
#define DBG_LED_OFF() digitalWrite(LED_BUILTIN, HIGH)

bool _flipLatest;
void flip()
{
  _flipLatest = !_flipLatest;
  if (_flipLatest)
    DBG_LED_ON();
  else
    DBG_LED_OFF();
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
    DBG_LED_ON();
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

void setup(void)
{
  //debug led
  pinMode(LED_BUILTIN, OUTPUT);
  setDbgLed(dbgLed_ON);
  //analogWriteFreq(new_frequency); 1kHz by default

  //eeprom
  EEPROM_EXT.begin(512);
  Cmd.begin(WiFi);
  Cmd.readFromEEPROM(); //wifi sleep mode here

  Serial.begin(cfgEEPROM.UART_BAUD);
  //Serial1.begin(UART_BAUD); //Tx1 or GPIO2; Rx1 is not accessible

  if (cfgEEPROM.DEBUG_EN)
    delay(500); //delay for debuging in ArduinoIDE
  DEBUG_MSG("\nstarting...");

  //chip info
  uint32_t chipRealSize = ESP.getFlashChipRealSize();
  uint32_t chipSize = ESP.getFlashChipSize();
  if (chipRealSize < chipSize)
    Serial.printf("!!!Chip size (%u) is wrong. Real is %u\n", chipSize, chipRealSize);

  DEBUG_MSGF("CpuFreq: %u MHz\n", ESP.getCpuFreqMHz());
  DEBUG_MSGF("Flash speed: %u MHz\n", ESP.getFlashChipSpeed()/1000/1000);
  FlashMode_t ideMode = ESP.getFlashChipMode();
  DEBUG_MSGF("Flash mode: %s\n", (ideMode == FM_QIO ? "QIO" : ideMode == FM_QOUT ? "QOUT" : ideMode == FM_DIO ? "DIO" : ideMode == FM_DOUT ? "DOUT" : "UNKNOWN"));

  //pin setup
  pinMode(IO_OUT1, OUTPUT);
  digitalWrite(IO_OUT1, LOW);
  pinMode(IO_OUT2, OUTPUT);
  digitalWrite(IO_OUT2, LOW);
  //pinMode(IO_IN1, INPUT_PULLUP); bool in1 = digitalRead(IO_IN1);

  //wifi setup
  stationConnectedHandler = WiFi.onStationModeConnected(&onStationConnected);
  stationGotIPHandler = WiFi.onStationModeGotIP(&onStationGotIp);

  server.begin();
  server.setNoDelay(true);

  WiFi.mode(WIFI_STA);
  WiFi.persistent(false);
}

bool _isWiFiFirst = true;
bool _isFirstConnect = true;
bool _isWiFiReconnect = false;
uint8_t _lastStatus;
bool WiFi_TryConnect(void)
{
  uint8_t status = WiFi.status();
  if (status == WL_CONNECTED && !_isWiFiReconnect)
    return true;

  if (_isFirstConnect || status == WL_NO_SSID_AVAIL || status == WL_CONNECT_FAILED || _isWiFiReconnect)
  {
    if (!_isFirstConnect)
    {
      DEBUG_MSG("ConnectionFailed to '" + WiFi.SSID() + "': " + status);
    }
    else if (_isWiFiReconnect)
    {
      _isWiFiReconnect = false;
      DEBUG_MSG("Connection to server failed in '" + WiFi.SSID() + "'")
    }

    const char *ssid;
    const char *pass;
    if (_isWiFiFirst)
    {
      ssid = (const char *)cfgEEPROM.WIFI_SSID_1;
      pass = (const char *)cfgEEPROM.WIFI_PASS_1;
    }
    else
    {
      ssid = WIFI_SSID_2;
      pass = WIFI_PASS_2;
    }
    WiFi.begin(ssid, pass);
    setDbgLed(dbgLed_Connecting);
    DEBUG_MSG("Connecting to '" + String(ssid) + "'...");

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
      else if (b == Cmd.strStart[i] || b == Cmd.strStartUpper[i])
      {
        ++i;
        if (i == Cmd.strStart.length())
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
        return true;
      }
    }
  }

  bool isOk = Cmd.execute(stream, str);
  stream.print(isOk ? "OK: " : "Error: ");
  stream.println(str);

  return true;
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
    client.println("I am (" + String(cfgEEPROM.MY_SN) + ')');
    t.reset();
    while (!t.isPassed(2000, true)) // timeout 2000ms
    {
      if (client.available())
      {
        client.setTimeout(500); // timeout 500ms
        String line = client.readStringUntil('\n');
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

uint8_t cnt_TCPSendNumber = 0; // counts for trying to send number
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

    IPAddress serverIP = IPAddress(ipAddress[0], ipAddress[1], ipAddress[2], cfgEEPROM.SERVER_IP_LAST);
    for (i = 0; i < 3; ++i) //3 times for different ports
    {
      if (TCP_SendNumber(serverIP, cfgEEPROM.SERVER_PORT + i))
      {
        isNeedSendIp = false;
        cnt_TCPSendNumber = 0;
        t_isNeedSendIp.reset();
        break;
      }
    }
    if (isNeedSendIp)
    {
      if (++cnt_TCPSendNumber > 2)
      {
        _isWiFiReconnect = true; //change WiFi SSID if server didn't respond
        cnt_TCPSendNumber = 0;
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
      bool isHasBytes = listenStream(serverClients[i], Serial); //todo what if UART response doesn't have enough time
      if (isHasBytes)                                           //miss next listening if we have bytes for Serial
      {
        lastClientNum = i;
        t_isNeedSendIp.reset();                   //reset timeLaps if we have bytes
        serverClients[i].println("testResponse"); //todo remove after testing;
        break;
      }
    }
  }
}

void scan()
{
  Serial.println("scan start");

  // WiFi.scanNetworks will return the number of networks found
  int n = WiFi.scanNetworks();
  Serial.println("scan done");
  for (int i = 0; i < n; ++i)
  {
    // Print SSID and RSSI for each network found
    Serial.print(i + 1);
    Serial.print(": ");
    Serial.print(WiFi.SSID(i));
    Serial.print(" (");
    Serial.print(WiFi.RSSI(i));
    Serial.print(")");
    Serial.println((WiFi.encryptionType(i) == ENC_TYPE_NONE) ? " " : "*");
    delay(1);
  }
  Serial.println("");

  // Wait a bit before scanning again
  delay(1000);
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
  //  scan();
}
