#ifndef EXTENSIONS_H_
#define EXTENSIONS_H_

#include <EEPROM.h>
#include <stdint.h>
#include <string.h>

#define strArrLength(v) sizeof(v) / sizeof(v[0])

class EEPROM_EXTClass : public EEPROMClass
{
public:
  byte readByte(const int address);
  uint16_t readInt(const int address);
  uint32_t readLong(const int address);
  String readStr(const int startAddress, const uint8_t length);
  bool write(const int address, uint8_t value);
  bool write(const int address, uint16_t value);
  bool write(const int address, uint32_t value);
  bool write(const int startAddress, String str, const uint8_t maxlength);

private:
  void baseWrite(int const address, uint8_t const value)
  {
    EEPROMClass::write(address, value);
  }
};

extern EEPROM_EXTClass EEPROM_EXT;

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

#endif /* EXTENSIONS_H_ */