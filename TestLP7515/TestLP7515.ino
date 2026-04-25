// Arduino Nano LP7515 test sender
// Sends LP7515-style frames over USB serial at 9600 baud.

const unsigned long intervalMs = 10000;
unsigned long lastSend = millis() - intervalMs;
int stepIndex = 0;

const char* samples[] =
{
  "ST,GS,+48900lb",
  "ST,GS,+1234.5lb",
  "ST,GS,+9876.0lb",
  "ST,NT,+567.8kg",
  "US,GS,+1200.0lb",
  "ST,GS,-25.0lb"
};

const int sampleCount = sizeof(samples) / sizeof(samples[0]);

void setup()
{
	Serial.begin(9600);
	Serial.println("Finished Setup.");
}

void loop()
{
	unsigned long now = millis();
	if (now - lastSend >= intervalMs)
	{
		lastSend = now;

		Serial.print(samples[stepIndex]);
		Serial.print("\r\n");

		stepIndex++;
		if (stepIndex >= sampleCount)
		{
			stepIndex = 0;
		}
	}
}
