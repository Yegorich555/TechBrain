#include <Arduino.h>
#include "extensions.h"

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

EEPROM_EXTClass EEPROM_EXT;