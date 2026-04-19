#include <SoftwareSerial.h>

#define RELAY_PIN 5

SoftwareSerial scaleSerial(3, -1);  // RX on D3, TX unused

String scaleLine = "";
bool inFrame = false;
String cmdBuffer = "";

void setup()
{
	Serial.begin(115200);
	scaleSerial.begin(4800);
	pinMode(RELAY_PIN, OUTPUT);
	digitalWrite(RELAY_PIN, LOW);
}

void loop()
{
	// Read scale data
	if (scaleSerial.available())
	{
		byte c = scaleSerial.read();
		c &= 0x7F;  // strip parity bit (7-O-2)

		if (c == 0x02)
		{
			// STX = start of frame
			scaleLine = "";
			inFrame = true;
			return;
		}

		if (inFrame)
		{
			if (c == '\n')
			{
				// LF = end of frame
				parseLine(scaleLine);
				inFrame = false;
			}
			else if (c != '\r')
			{
				// ignore CR
				scaleLine += (char)c;
			}
		}
	}

	// Handle relay commands from PC (RELAY:1 or RELAY:0)
	while (Serial.available())
	{
		char c = (char)Serial.read();
		if (c == '\n')
		{
			cmdBuffer.trim();
			if      (cmdBuffer == "RELAY:1") digitalWrite(RELAY_PIN, HIGH);
			else if (cmdBuffer == "RELAY:0") digitalWrite(RELAY_PIN, LOW);
			cmdBuffer = "";
		}
		else
		{
			cmdBuffer += c;
		}
	}
}

void parseLine(String s)
{
	/*
	  Field  | Spec                  | Code
	  -------+-----------------------+-------------------
	  POL    | space or -            | s[0]
	  DATA   | 7 chars incl. decimal | s.substring(1, 8)
	  L/K    | L or K                | s[8]
	  G/N    | G or N                | s[9]
	  STATUS | C,I,O,M, or space     | s[10]
	*/

	if (s.length() > 10)
	{
		char polarity = s[0];
		String data   = s.substring(1, 8);
		char units    = s[8];   // 'L' or 'K'
		char mode     = s[9];   // 'G' or 'N'
		char status   = s[10];  // 'M', 'O', 'I', 'C', or space

		float weight = data.toFloat();
		if (polarity == '-') weight = -weight;

		const char* unitStr = (units == 'L') ? "lb" : "kg";
		const char* modeStr = (mode  == 'G') ? "Gross" : "Net";
		const char* statusStr;
		switch (status)
		{
		case 'M': statusStr = "Motion";     break;
		case 'O': statusStr = "Over/Under"; break;
		case 'I': statusStr = "Invalid";    break;
		case 'C': statusStr = "Check Mode"; break;
		default:  statusStr = "Stable";     break;
		}

		// CSV: weight,units,mode,status
		Serial.print(weight, 1);
		Serial.print(',');
		Serial.print(unitStr);
		Serial.print(',');
		Serial.print(modeStr);
		Serial.print(',');
		Serial.println(statusStr);
	}
	else
	{
		Serial.println("Bad frame: " + s);
	}
}
