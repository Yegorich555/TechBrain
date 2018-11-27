#ifndef EXTENSIONS_H_
#define EXTENSIONS_H_
#include <EEPROM.h>
#include <stdint.h>
#include <string.h>

#define strArrLength(v) sizeof(v) / sizeof(v[0])
class EEPROM_EXTClass : public EEPROMClass
{
public:
  unsigned int ReadInt(int p_address);
  String ReadStr(int startAddress, uint8_t length);
  bool Write(int address, uint8_t value);
  bool Write(int address, uint16_t value);
  bool Write(int startAddress, String str, uint8_t maxlength);
};

extern EEPROM_EXTClass EEPROM_EXT;
#endif /* EXTENSIONS_H_ */