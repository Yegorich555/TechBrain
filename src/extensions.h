#ifndef EXTENSIONS_H_
#define EXTENSIONS_H_

#include <Arduino.h>
#include <stdint.h>
#include <string.h>
#include <EEPROM.h>

#define strArrLength(v) sizeof(v) / sizeof(v[0])

class EEPROM_EXTClass : public EEPROMClass
{
};
extern EEPROM_EXTClass EEPROM_EXT; //this is need for fixing init-duplicate EEPROMClass bug in Arduino

class TimeLaps
{
public:
  TimeLaps(void)
  {
    _last = 0;
  }

  bool isPassed(const unsigned int ms, bool ignoreNull = false)
  {
    if (ignoreNull && _last == 0)
      return false;
    if (_last == 0 || millis() - _last >= ms)
    {
      reset();
      return true;
    }
    return false;
  }
  void reset(void)
  {
    _last = millis();
  }

protected:
  unsigned long _last;
};

const unsigned long __bauds[] = {
    0, //for simplifying checking
    300,
    600,
    1200,
    2400,
    4800,
    9600,
    19200,
    38400,
    57600,
    115200,
    230400,
};

class BaudRate
{
public:
  static uint32_t fromNum(uint8_t num, uint32_t defBaud)
  {
    if (num >= sizeof(__bauds))
      return defBaud;
    return __bauds[num];
  };

  static uint8_t toNum(uint32_t baud)
  {
    for (uint8_t i = 1; i < sizeof(__bauds); ++i)
    {
      if (__bauds[i] == baud)
        return i;
    }
    return 0;
  };

  static bool isValid(uint32_t baud)
  {
    return toNum(baud) != 0;
  };
};

void CheckTime(const String txt);

#endif /* EXTENSIONS_H_ */