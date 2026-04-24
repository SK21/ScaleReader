#include <Wire.h>
#include <Adafruit_ADS1X15.h>
#include <NativeEthernet.h>
#include <NativeEthernetUdp.h>

#define RELAY_PIN 5

Adafruit_ADS1115 ads;

String Ver="v2026-04-24";

// ---------------------------------------------------------
// HARDWARE HOOKUP + INDICATOR CALIBRATION NOTES
// ---------------------------------------------------------
//
//  UMC 2000-54 Indicator (4–20 mA isolated analog output)
//  ------------------------------------------------------
//  • Pin 7  =  +4–20 mA output
//  • Pin 8  =  – return (isolated)
//
//  HOW TO CALIBRATE THE INDICATOR'S ANALOG OUTPUT
//  ----------------------------------------------
//  The UMC‑2000 has three analog trim pots on the A/D board:
//
//      ZERO        → sets 4.00 mA at empty scale
//      GROSS SPAN  → sets approx. 20.00 mA at full capacity
//      FINE SPAN   → fine‑tunes the 20 mA point
//
//  Calibration procedure (from the manual):
//
//    1. EMPTY SCALE
//       - Ensure the scale platform is empty.
//       - Measure the current between Pin 7 (+) and Pin 8 (–).
//       - Adjust the ZERO pot until the output reads **4.00 mA**.
//
//    2. FULL‑CAPACITY LOAD
//       - Apply a known full‑scale load (or as close as possible).
//       - Adjust the GROSS SPAN pot until the output is near **20.00 mA**.
//       - Use the FINE SPAN pot to dial it in precisely.
//
//  After calibration:
//      4.00 mA = 0 lb
//      20.00 mA = 60,000 lb   (your scale’s rated capacity)
//
//  Your Teensy code uses this exact mapping.
//
//
//  PCB (ADS1115 analog board)
//  --------------------------
//  • "Analog 4–20" input → ADS1115 AIN2
//  • R17 = 250 Ω shunt converts 4–20 mA → 1–5 V
//  • BAV99 diodes protect ADS1115
//  • PCB GND must connect to Indicator Pin 8 (return)
//
//  Teensy 4.1
//  ----------
//  • SDA = pin 18 → PCA9306 → ADS1115 SDA
//  • SCL = pin 19 → PCA9306 → ADS1115 SCL
//  • GND tied to PCB GND
//  • Ethernet jack → PC network
//
// ---------------------------------------------------------


// -------------------------------
// USER SETTINGS
// -------------------------------
const float SHUNT_OHMS = 250.0f;     // R17 on your PCB
const float FS_WEIGHT = 60000.0f;   // Real scale capacity (60,000 lb)
const uint16_t UDP_PORT = 5005;      // PC app listens here

// Ethernet
byte mac[6] = { 0x04, 0xE9, 0xE5, 0x12, 0x34, 0x56 };
IPAddress ip(192, 168, 1, 53);            // Teensy static IP (fallback if DHCP fails)

EthernetUDP udp;

// -------------------------------
// TEST MODE  — set false for real hardware
// -------------------------------
bool TEST_MODE = true;
float testWeight = 49523;
float testStep = 500.0f;   // lb per update

uint32_t LoopTime = 200;
uint32_t LastTime;
uint32_t LastBlink;
bool BlinkState;
String serialCmd = "";

void setup() {
	Serial.begin(115200);
	delay(500);
	Serial.println("");
	Serial.print("Version: ");
	Serial.println(Ver);

	// -------------------------------
	// I2C + ADS1115
	// -------------------------------
	Wire.begin();
	ads.begin();
	ads.setGain(GAIN_TWOTHIRDS);   // ±6.144 V range (needed for 5V max)
	Serial.println("ADS1115 ready");

	// -------------------------------
	// Ethernet
	// -------------------------------
	pinMode(RELAY_PIN, OUTPUT);
	digitalWrite(RELAY_PIN, LOW);

	// Try DHCP first (gets an IP on whatever subnet the PC is on).
	// begin(mac) blocks up to ~60 s waiting for a lease, so we start
	// with no IP, wait briefly for a link, then attempt DHCP only if
	// a cable is present — keeping the timeout to a few seconds.
	Ethernet.begin(mac, 0);
	delay(500);

	if (Ethernet.linkStatus() == LinkON)
	{
		Serial.print("Link detected, trying DHCP");
		bool dhcp = false;
		uint32_t dhcpStart = millis();
		while (!dhcp && millis() - dhcpStart < 5000)
		{
			dhcp = (Ethernet.begin(mac) != 0);
			if (!dhcp) { Serial.print("."); delay(500); }
		}
		Serial.println();

		if (dhcp)
		{
			Serial.println("DHCP OK.");
		}
		else
		{
			// Fall back to static IP
			Ethernet.begin(mac, 0);
			Ethernet.setLocalIP(ip);
			Serial.println("DHCP failed, using static IP.");
		}

		udp.begin(UDP_PORT);
		Serial.print("IP: ");
		Serial.println(Ethernet.localIP());
	}
	else
	{
		Serial.println("No Ethernet link — serial only.");
		udp.begin(UDP_PORT);   // bind anyway so socket is ready if link comes up
	}

	Serial.print("Test Mode: ");
	if (TEST_MODE)
	{
		Serial.println("ON");
	}
	else
	{
		Serial.println("OFF");
	}

	Serial.println("Setup complete.");
	Serial.println("");
}

void loop()
{
	if (millis() - LastTime > LoopTime)
	{
		LastTime = millis();
		float volts, current_mA, weight;

		if (TEST_MODE)
		{
			// -------------------------------
			// Generate synthetic test data
			// -------------------------------
			weight = testWeight;

			// Convert weight → current → volts (reverse of real math)
			current_mA = (weight / FS_WEIGHT) * 16.0f + 4.0f;
			volts = (current_mA / 1000.0f) * SHUNT_OHMS;

			// Ramp test weight up/down
			//testWeight += testStep;
			//if (testWeight >= FS_WEIGHT || testWeight <= 0)
			//	testStep = -testStep;

		}
		else
		{
			// -------------------------------
			// REAL ADS1115 DATA
			// -------------------------------
			int16_t raw = ads.readADC_SingleEnded(2);

			volts = raw * 6.144f / 32768.0f;
			current_mA = (volts / SHUNT_OHMS) * 1000.0f;
			weight = (current_mA - 4.0f) / 16.0f * FS_WEIGHT;
		}

		// Clamp: sub-4 mA noise produces negative weight
		if (weight < 0.0f) weight = 0.0f;

		// -------------------------------
		// Build communication sentence
		// -------------------------------
		char sentence[64];
		snprintf(sentence, sizeof(sentence),
			"WT,%.3fV,%.3fmA,%.1flb\n",
			volts, current_mA, weight);

		// -------------------------------
		// Send UDP packet to subnet broadcast
		// -------------------------------
		if (Ethernet.linkStatus() == LinkON)
		{
			IPAddress bcast = Ethernet.localIP();
			bcast[3] = 255;
			udp.beginPacket(bcast, UDP_PORT);
			udp.write(sentence);
			udp.endPacket();
		}

		// USB serial — same weight,units format as ScaleReader so the app needs no format detection
		Serial.printf("%.1f,lb\n", weight);
	}

	// Incoming UDP from PC: relay commands 
	if (Ethernet.linkStatus() == LinkON)
	{
		int packetSize = udp.parsePacket();
		if (packetSize > 0)
		{
			char cmd[16] = {};
			int len = udp.read(cmd, sizeof(cmd) - 1);
			cmd[len] = '\0';
			if (strcmp(cmd, "RELAY:1") == 0)
				digitalWrite(RELAY_PIN, HIGH);
			else if (strcmp(cmd, "RELAY:0") == 0)
				digitalWrite(RELAY_PIN, LOW);
		}
	}

	// Relay commands from PC via USB serial (same RELAY:1 / RELAY:0 protocol)
	while (Serial.available())
	{
		char c = (char)Serial.read();
		if (c == '\n')
		{
			serialCmd.trim();
			if (serialCmd == "RELAY:1")      digitalWrite(RELAY_PIN, HIGH);
			else if (serialCmd == "RELAY:0") digitalWrite(RELAY_PIN, LOW);
			serialCmd = "";
		}
		else
		{
			serialCmd += c;
		}
	}

	if (millis() - LastBlink > 1000)
	{
		LastBlink = millis();
		BlinkState = !BlinkState;
		digitalWrite(LED_BUILTIN, BlinkState);
	}
}
