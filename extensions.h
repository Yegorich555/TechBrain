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
  int readInt(const int address);
  String readStr(const int startAddress, const uint8_t length);
  bool write(const int address, uint8_t value);
  bool write(const int address, uint16_t value);
  bool write(const int startAddress, String str, const uint8_t maxlength);

private:
  void baseWrite(int const address, uint8_t const value)
  {
    EEPROMClass::write(address, value);
  }
};

extern EEPROM_EXTClass EEPROM_EXT;
#endif /* EXTENSIONS_H_ */