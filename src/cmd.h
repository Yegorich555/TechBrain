#ifndef CMD_H_
#define CMD_H_

#include <Arduino.h>
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
  bool execute(Stream &stream, String str);
  void readFromEEPROM();
  const String strStart = "esp_";
};

extern CmdClass Cmd;

#endif /* CMD_H_ */