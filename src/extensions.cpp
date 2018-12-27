#include <Arduino.h>
#include "extensions.h"

String EEPROM_EXTClass::readStr(int startAddress, const uint8_t maxlength)
{
  String str = "";
  for (uint8_t i = 0; i < maxlength; ++i)
  {
    char v = read(startAddress++);
    if (v == 0 || v == 255)
      break;

    str += String(v);
  }
  return str;
}

bool EEPROM_EXTClass::write(int startAddress, String str, const uint8_t maxlength)
{
  unsigned int len = str.length();
  if (len > maxlength)
  {
    //todo String result = "Err writeEEprom: maxLength " + len + String(" > ") + maxlength; //todo set to debug
    return false;
  }
  for (unsigned int i = 0; i < len; ++i)
    baseWrite(startAddress++, str[i]);

  if (len < maxlength) //write terminator
    baseWrite(startAddress, 0);

  commit();
  return true;
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

EEPROM_EXTClass EEPROM_EXT;