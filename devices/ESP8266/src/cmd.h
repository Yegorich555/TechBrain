#ifndef CMD_H_
#define CMD_H_

#include <Arduino.h>
#include <ESP8266WiFi.h>
#include <stdint.h>
#include <string.h>

struct structCmd
{
  String strCommand;
  bool (*execute)(Stream &stream, String strValue);
};

class CmdClass
{
public:
  void begin(ESP8266WiFiClass &wifi);
  bool execute(Stream &stream, String str);
  void readFromEEPROM();
  const String strStart = "esp_";
  const String strStartUpper = "ESP_";
  ESP8266WiFiClass _wifi;
};

extern CmdClass Cmd;

#endif /* CMD_H_ */