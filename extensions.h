#ifndef EXTENSIONS_H_
#define EXTENSIONS_H_
#include <EEPROM.h>
#include <stdint.h>
#include <string.h>

#define strArrLength(v) sizeof(v) / sizeof(v[0])

class EEPROM_EXTClass : public EEPROMClass
{
public:
  byte readByte(int address);
  int readInt(int address);
  String readStr(int startAddress, uint8_t length);
  bool write(int address, uint8_t value);
  bool write(int address, uint16_t value);
  bool write(int startAddress, String str, uint8_t maxlength);
};

extern EEPROM_EXTClass EEPROM_EXT;
#endif /* EXTENSIONS_H_ */