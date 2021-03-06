#ifndef SaturnSpy_h
#define SaturnSpy_h

#include "ControllerSpy.h"

class SaturnSpy : public ControllerSpy {
public:
	void loop();
	void writeSerial();
	void debugSerial();
	void updateState();

private:
	byte ssState1 = 0;
	byte ssState2 = 0;
	byte ssState3 = 0;
	byte ssState4 = 0;
};

#endif
