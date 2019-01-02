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

size_t Print64(Print &pr, uint64_t number, int base)
{
  size_t n = 0;
  unsigned char buf[64];
  uint8_t i = 0;

  if (number == 0)
  {
    n += pr.print((char)'0');
    return n;
  }

  if (base < 2)
    base = 2;
  else if (base > 16)
    base = 16;

  while (number > 0)
  {
    uint64_t q = number / base;
    buf[i++] = number - q * base;
    number = q;
  }

  for (; i > 0; i--)
    n += pr.write((char)(buf[i - 1] < 10 ? '0' + buf[i - 1] : 'A' + buf[i - 1] - 10));

  return n;
}

EEPROM_EXTClass EEPROM_EXT;