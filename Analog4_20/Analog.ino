

void ReadAnalog()
{
	static uint16_t Aread;
	static uint8_t ReadID = 0;
	// use ADS1115 through Teensy
	if (ADSfound)
	{
		// based on https://github.com/RalphBacon/ADS1115-ADC/blob/master/ADS1115_ADC_16_bit_SingleEnded.ino

		// read current value
		Wire.beginTransmission(ADS1115_Address);
		Wire.write(0b00000000); //Point to Conversion register
		Wire.endTransmission();
		Wire.requestFrom(ADS1115_Address, 2);
		Aread = (Wire.read() << 8 | Wire.read());

		Readings[ReadID] = Aread;
		ReadID = (ReadID + 1) & 0x03;     // wrap 0–3

		Wire.beginTransmission(ADS1115_Address);
		Wire.write(0b00000001); // Point to Config Register

		// Write the MSB + LSB of Config Register
		// MSB: Bits 15:8
		// Bit  15    0=No effect, 1=Begin Single Conversion (in power down mode)
		// Bits 14:12   How to configure A0 to A3 (comparator or single ended)
		// Bits 11:9  Programmable Gain 000=6.144v 001=4.096v 010=2.048v .... 111=0.256v
		// Bits 8     0=Continuous conversion mode, 1=Power down single shot

		uint8_t msb = 0b00000000;         // start with OS=0, MODE=0

		switch (ReadID)
		{
		case 0: msb = 0b01000000; break;   // AIN0
		case 1: msb = 0b01010000; break;   // AIN1
		case 2: msb = 0b01100000; break;   // AIN2
		case 3: msb = 0b01110000; break;   // AIN3
		}

		// PGA bits (11–9) = 000 → GAIN_TWOTHIRDS
		// Already zero in msb, so nothing to OR in.

		Wire.write(msb);

		// LSB: Bits 7:0
		// Bits 7:5 Data Rate (Samples per second) 000=8, 001=16, 010=32, 011=64,
		//      100=128, 101=250, 110=475, 111=860
		// Bit  4   Comparator Mode 0=Traditional, 1=Window
		// Bit  3   Comparator Polarity 0=low, 1=high
		// Bit  2   Latching 0=No, 1=Yes
		// Bits 1:0 Comparator # before Alert pin goes high
		//      00=1, 01=2, 10=4, 11=Disable this feature

		uint8_t lsb = 0b11100011;         // 860 SPS + disable comparator
		Wire.write(lsb);
		Wire.endTransmission();
	}
}

