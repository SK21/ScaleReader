using System;
using System.Globalization;
using System.IO.Ports;
using System.Text;
using System.Windows.Forms;

namespace ScaleDisplay
{
    public class SerialComm
    {
        private readonly StringBuilder LogBuilder = new StringBuilder();
        private readonly object LogLock = new object();
        private readonly SerialPort Sport;
        private Form1 mf;

        public SerialComm(Form1 CallingForm, string portName, int baudRate)
        {
            mf = CallingForm;

            Sport = new SerialPort(portName, baudRate, Parity.None, 8, StopBits.One)
            {
                ReadTimeout = 500,
                WriteTimeout = 500,
                DtrEnable = true,
                RtsEnable = true
            };
            OpenPort();
        }

        public event Action PortDisconnected;

        public bool IsOpen
        { get { return Sport.IsOpen; } }


        public void ClosePort()
        {
            try
            {
                if (Sport.IsOpen)
                {
                    Sport.DataReceived -= Sport_DataReceived;
                    Sport.Close();
                    AddToLog("\nPort closed.\n");
                    PortDisconnected?.Invoke();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("SerialComm/ClosePort: " + ex.Message);
            }
            Sport.Dispose();
        }

        public bool WriteLine(string Data)
        {
            bool Result = false;
            try
            {
                Sport.WriteLine(Data);
                Result = true;
            }
            catch
            {
            }

            return Result;
        }

        public bool Send(byte[] Data)
        {
            bool Result = false;
            try
            {
                Sport.Write(Data, 0, Data.Length);
                Result = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("SerialComm/Send: " + ex.Message);
            }
            return Result;
        }

        private void AddToLog(string NewData)
        {
            try
            {
                lock (LogLock)
                {
                    LogBuilder.Append(NewData);
                    if (LogBuilder.Length > 100000) LogBuilder.Remove(0, LogBuilder.Length - 25000);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("SerialComm/AddToLog: " + ex.Message);
            }
        }

        private bool OpenPort()
        {
            bool Result = false;
            try
            {
                if (!Sport.IsOpen)
                {
                    Sport.Open();
                    AddToLog("\nPort open.\n");

                    Sport.DataReceived += Sport_DataReceived;
                    Sport.DiscardOutBuffer();
                    Sport.DiscardInBuffer();
                    Result = true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("SerialComm/OpenPort: " + ex.Message);
            }
            return Result;
        }

        private void Sport_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            if (Sport.IsOpen)
            {
                try
                {
                    bool WeightFound = false;
                    string line = Sport.ReadLine().Trim();

                    if (Properties.Settings.Default.LP7515)
                    {
                        if (line.Length > 0)
                        {
                            // Expected LP7515 format:
                            // S1,S2,S3DataS4
                            // Example: ST,GS,+1234.5lb
                            string[] parts = line.Split(',');

                            if (parts.Length == 3)
                            {
                                string s1 = parts[0].Trim();   // ST, US, OL
                                string s2 = parts[1].Trim();   // GS, NT
                                string payload = parts[2].Trim(); // +1234.5lb

                                if (payload.Length >= 4)
                                {
                                    char sign = payload[0];
                                    string units = payload.Substring(payload.Length - 2).ToLowerInvariant();
                                    string dataText = payload.Substring(1, payload.Length - 3).Trim();

                                    if ((sign == '+' || sign == '-') &&
                                        (units == "lb" || units == "kg") &&
                                        float.TryParse(dataText, NumberStyles.Float,
                                            CultureInfo.InvariantCulture, out float weightVal))
                                    {
                                        if (sign == '-') weightVal = -weightVal;

                                        mf.ApplyWeightReading(weightVal, units);
                                        WeightFound = true;
                                    }
                                }
                            }

                            if (!WeightFound) mf.AppendSerialOutput(line);
                        }
                    }
                    else
                    {
                        if (line.Length > 0)
                        {
                            string[] parts = line.Split(',');
                            if (parts.Length >= 2)
                            {
                                float.TryParse(parts[0], NumberStyles.Float,
                                    CultureInfo.InvariantCulture, out float weightVal);
                                string scaleUnits = parts[1];
                                if (scaleUnits == "lb" || scaleUnits == "kg")
                                {
                                    mf.ApplyWeightReading(weightVal, scaleUnits);
                                    WeightFound = true;
                                }
                            }
                            if (!WeightFound) mf.AppendSerialOutput(line);
                        }
                    }
                }
                catch (TimeoutException) { }
                catch (Exception ex)
                {
                    mf.AppendSerialOutput("ERR:" + ex.Message);
                }
            }
        }
    }
}
