using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Printing;
using System.Globalization;
using System.IO;
using System.IO.Ports;
using System.Windows.Forms;

namespace ScaleDisplay
{
    public partial class Form1 : Form
    {
        private static readonly string PositionFile = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ScaleDisplay", "position.txt");

        private static readonly string CsvDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "ScaleDisplay");

        private SerialPort _port;
        private System.Windows.Forms.Timer _signalTimer;

        private enum ScaleState { Empty, Settling, Captured }
        private ScaleState _state = ScaleState.Empty;
        private DateTime? _stableStart;
        private int _loadNumber;
        private float _currentWeight;
        private string _currentUnits = "lb";

        // Cached for safe access from the DataReceived background thread
        private volatile bool _autoWeighEnabled;
        private volatile int _minWeightLbs = 10000;
        private volatile int _stabilitySeconds = 3;
        private volatile int _signalDurationSeconds = 5;

        // Print state
        private List<string[]> _printRows;
        private DateTime _printDate;
        private int _printRowIndex;

        public Form1()
        {
            InitializeComponent();
            SetupGrids();
            RefreshPorts();

            _signalTimer = new System.Windows.Forms.Timer();
            _signalTimer.Tick += OnSignalTimerTick;

            InitializeLoadNumber();
            RefreshTodayWeights();
        }

        // ── Grid setup ───────────────────────────────────────────────────

        private void SetupGrids()
        {
            SetupGrid(dgvReport, new[] { "Date / Time", "Load #", "Weight", "Units" },
                                 new[] { 40, 15, 30, 15 });

            SetupGrid(dgvTodayWeights, new[] { "Time", "Load #", "Weight", "Units" },
                                       new[] { 30, 15, 40, 15 });
        }

        private static void SetupGrid(DataGridView grid, string[] headers, int[] weights)
        {
            grid.AllowUserToAddRows = false;
            grid.ReadOnly = true;
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            grid.RowHeadersVisible = false;
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            for (int i = 0; i < headers.Length; i++)
            {
                grid.Columns.Add("col" + i, headers[i]);
                grid.Columns["col" + i].FillWeight = weights[i];
            }
        }

        private void RefreshTodayWeights()
        {
            dgvTodayWeights.Rows.Clear();
            string path = GetCsvPath(DateTime.Today);
            if (!File.Exists(path)) return;

            string[] lines = File.ReadAllLines(path);
            for (int i = 1; i < lines.Length; i++)
            {
                string[] parts = lines[i].Split(',');
                if (parts.Length == 4)
                {
                    string time = parts[0].Length >= 19 ? parts[0].Substring(11, 8) : parts[0];
                    dgvTodayWeights.Rows.Add(time, parts[1], parts[2], parts[3]);
                }
            }

            if (dgvTodayWeights.Rows.Count > 0)
                dgvTodayWeights.FirstDisplayedScrollingRowIndex = dgvTodayWeights.Rows.Count - 1;
        }

        // ── Signal timer ─────────────────────────────────────────────────

        private void OnSignalTimerTick(object sender, EventArgs e)
        {
            _signalTimer.Stop();
            SendRelay(false);
            lblWeightValue.BackColor = SystemColors.Control;
        }

        // ── Port ─────────────────────────────────────────────────────────

        private void RefreshPorts()
        {
            cmbPort.Items.Clear();
            foreach (var p in SerialPort.GetPortNames())
                cmbPort.Items.Add(p);
            if (cmbPort.Items.Count > 0)
                cmbPort.SelectedIndex = 0;
        }

        private void btnRefresh_Click(object sender, EventArgs e) => RefreshPorts();

        private void btnConnect_Click(object sender, EventArgs e)
        {
            if (_port != null && _port.IsOpen)
            {
                _port.DataReceived -= Port_DataReceived;
                _port.Close();
                _port.Dispose();
                _port = null;
                btnConnect.Text = "Connect";
                cmbPort.Enabled = true;
                btnRefresh.Enabled = true;
                btnManualWeigh.Enabled = false;
                lblWeightValue.Text = "---";
                lblWeightValue.BackColor = SystemColors.Control;
                lblModeValue.Text = "---";
                lblStatusValue.Text = "---";
                ResetStateMachine();
                return;
            }

            if (cmbPort.SelectedItem == null) return;

            _port = new SerialPort(cmbPort.SelectedItem.ToString(), 115200) { NewLine = "\n" };
            _port.DataReceived += Port_DataReceived;

            try
            {
                _port.Open();
                btnConnect.Text = "Disconnect";
                cmbPort.Enabled = false;
                btnRefresh.Enabled = false;
                btnManualWeigh.Enabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not open port: " + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                _port.Dispose();
                _port = null;
            }
        }

        // ── Data received ────────────────────────────────────────────────

        private void Port_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                string line = _port.ReadLine().Trim();
                string[] parts = line.Split(',');
                if (parts.Length != 4) return;

                string weightDisplay = parts[0] + " " + parts[1];
                string mode = parts[2];
                string status = parts[3];

                float.TryParse(parts[0], NumberStyles.Float,
                    CultureInfo.InvariantCulture, out float weightVal);

                Color statusColor;
                switch (status)
                {
                    case "Stable": statusColor = Color.Green; break;
                    case "Motion": statusColor = Color.DarkOrange; break;
                    default: statusColor = Color.Red; break;
                }

                _currentWeight = weightVal;
                _currentUnits = parts[1];

                if (_autoWeighEnabled)
                    RunStateMachine(weightVal, parts[1], status);

                Invoke(new Action(() =>
                {
                    lblWeightValue.Text = weightDisplay;
                    lblModeValue.Text = mode;
                    lblStatusValue.Text = status;
                    lblStatusValue.ForeColor = statusColor;
                }));
            }
            catch { }
        }

        // ── Auto-weigh state machine ─────────────────────────────────────

        private void RunStateMachine(float weight, string units, string status)
        {
            int minWeight = _minWeightLbs;
            int stabilityNeeded = _stabilitySeconds;

            switch (_state)
            {
                case ScaleState.Empty:
                    if (weight >= minWeight)
                        _state = ScaleState.Settling;
                    break;

                case ScaleState.Settling:
                    if (weight < minWeight)
                    {
                        _state = ScaleState.Empty;
                        _stableStart = null;
                    }
                    else if (status == "Stable")
                    {
                        if (_stableStart == null)
                            _stableStart = DateTime.Now;
                        else if ((DateTime.Now - _stableStart.Value).TotalSeconds >= stabilityNeeded)
                        {
                            _state = ScaleState.Captured;
                            CaptureWeight(weight, units);
                        }
                    }
                    else
                    {
                        _stableStart = null;  // motion — restart stability timer
                    }
                    break;

                case ScaleState.Captured:
                    if (weight < minWeight)
                    {
                        _state = ScaleState.Empty;
                        _stableStart = null;
                    }
                    break;
            }
        }

        private void CaptureWeight(float weight, string units)
        {
            LogCapture(weight, units);
            SendRelay(true);
            Invoke(new Action(() =>
            {
                lblWeightValue.BackColor = Color.LightGreen;
                _signalTimer.Interval = _signalDurationSeconds * 1000;
                _signalTimer.Start();
                RefreshTodayWeights();
            }));
        }

        private void btnManualWeigh_Click(object sender, EventArgs e)
        {
            if (_currentWeight <= 0) return;
            LogCapture(_currentWeight, _currentUnits);
            lblWeightValue.BackColor = Color.LightGreen;
            _signalTimer.Interval = _signalDurationSeconds * 1000;
            _signalTimer.Start();
            SendRelay(true);
            RefreshTodayWeights();
        }

        private void SendRelay(bool on)
        {
            if (_port != null && _port.IsOpen)
            {
                try { _port.WriteLine(on ? "RELAY:1" : "RELAY:0"); }
                catch { }
            }
        }

        private void ResetStateMachine()
        {
            _state = ScaleState.Empty;
            _stableStart = null;
            _signalTimer.Stop();
            SendRelay(false);
        }

        // ── CSV logging ──────────────────────────────────────────────────

        private string GetCsvPath(DateTime date) =>
            Path.Combine(CsvDirectory, date.ToString("yyyy-MM-dd") + ".csv");

        private void InitializeLoadNumber()
        {
            try
            {
                string path = GetCsvPath(DateTime.Today);
                if (!File.Exists(path)) { _loadNumber = 0; return; }

                int count = 0;
                foreach (string line in File.ReadAllLines(path))
                    if (!line.StartsWith("DateTime")) count++;
                _loadNumber = count;
            }
            catch { _loadNumber = 0; }
        }

        private int LogCapture(float weight, string units)
        {
            try
            {
                Directory.CreateDirectory(CsvDirectory);
                string path = GetCsvPath(DateTime.Today);

                if (!File.Exists(path))
                {
                    _loadNumber = 0;
                    File.AppendAllText(path, "DateTime,Load,Weight,Units\r\n");
                }

                _loadNumber++;

                File.AppendAllText(path, string.Format("{0},{1},{2:F1},{3}\r\n",
                    DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    _loadNumber, weight, units));
            }
            catch { }

            return _loadNumber;
        }

        // ── Auto-weigh control events ────────────────────────────────────

        private void chkAutoWeigh_CheckedChanged(object sender, EventArgs e)
        {
            _autoWeighEnabled = chkAutoWeigh.Checked;
            numMinWeight.Enabled = chkAutoWeigh.Checked;
            numStability.Enabled = chkAutoWeigh.Checked;
            numSignal.Enabled = chkAutoWeigh.Checked;

            if (!chkAutoWeigh.Checked)
            {
                ResetStateMachine();
                lblWeightValue.BackColor = SystemColors.Control;
            }
        }

        private void numMinWeight_ValueChanged(object sender, EventArgs e) =>
            _minWeightLbs = (int)numMinWeight.Value;

        private void numStability_ValueChanged(object sender, EventArgs e) =>
            _stabilitySeconds = (int)numStability.Value;

        private void numSignal_ValueChanged(object sender, EventArgs e) =>
            _signalDurationSeconds = (int)numSignal.Value;

        // ── Report tab ───────────────────────────────────────────────────

        private void btnLoadReport_Click(object sender, EventArgs e)
        {
            dgvReport.Rows.Clear();
            string path = GetCsvPath(dtpReport.Value.Date);

            if (!File.Exists(path))
            {
                MessageBox.Show("No data file for " + dtpReport.Value.ToString("yyyy-MM-dd"),
                    "Not Found", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string[] lines = File.ReadAllLines(path);
            for (int i = 1; i < lines.Length; i++)
            {
                string[] parts = lines[i].Split(',');
                if (parts.Length == 4)
                    dgvReport.Rows.Add(parts[0], parts[1], parts[2], parts[3]);
            }
        }

        private void btnPrint_Click(object sender, EventArgs e)
        {
            if (dgvReport.Rows.Count == 0)
            {
                MessageBox.Show("No data to print. Load a report first.",
                    "Nothing to Print", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            _printRows = new List<string[]>();
            foreach (DataGridViewRow row in dgvReport.Rows)
            {
                if (!row.IsNewRow)
                    _printRows.Add(new string[] {
                        row.Cells[0].Value?.ToString() ?? "",
                        row.Cells[1].Value?.ToString() ?? "",
                        row.Cells[2].Value?.ToString() ?? "",
                        row.Cells[3].Value?.ToString() ?? ""
                    });
            }
            _printDate = dtpReport.Value.Date;
            _printRowIndex = 0;

            PrintDocument pd = new PrintDocument();
            pd.PrintPage += PrintPage;

            PrintDialog dlg = new PrintDialog { Document = pd };
            if (dlg.ShowDialog() == DialogResult.OK)
                pd.Print();
        }

        private void PrintPage(object sender, PrintPageEventArgs e)
        {
            Font titleFont = new Font("Arial", 13, FontStyle.Bold);
            Font headerFont = new Font("Arial", 9, FontStyle.Bold);
            Font dataFont = new Font("Arial", 9);
            float lineH = dataFont.GetHeight(e.Graphics) + 3;
            float x = e.MarginBounds.Left;
            float y = e.MarginBounds.Top;

            if (_printRowIndex == 0)
            {
                string title = "Scale Report  —  " + _printDate.ToString("MMMM d, yyyy");
                e.Graphics.DrawString(title, titleFont, Brushes.Black, x, y);
                y += titleFont.GetHeight(e.Graphics) + 8;

                DrawPrintRow(e.Graphics, headerFont, x, y, "Date / Time", "Load #", "Weight", "Units");
                y += lineH;
                e.Graphics.DrawLine(Pens.Black, x, y, e.MarginBounds.Right, y);
                y += 4;
            }

            while (_printRowIndex < _printRows.Count)
            {
                string[] row = _printRows[_printRowIndex];
                DrawPrintRow(e.Graphics, dataFont, x, y, row[0], row[1], row[2], row[3]);
                y += lineH;
                _printRowIndex++;

                if (y + lineH > e.MarginBounds.Bottom)
                {
                    e.HasMorePages = true;
                    return;
                }
            }

            e.HasMorePages = false;
        }

        private void DrawPrintRow(Graphics g, Font font, float x, float y,
            string dateTime, string load, string weight, string units)
        {
            g.DrawString(dateTime, font, Brushes.Black, x, y);
            g.DrawString(load, font, Brushes.Black, x + 155, y);
            g.DrawString(weight, font, Brushes.Black, x + 215, y);
            g.DrawString(units, font, Brushes.Black, x + 295, y);
        }

        // ── Position persistence ─────────────────────────────────────────

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            try
            {
                if (File.Exists(PositionFile))
                {
                    string[] parts = File.ReadAllText(PositionFile).Split(',');
                    if (parts.Length == 2 &&
                        int.TryParse(parts[0], out int x) &&
                        int.TryParse(parts[1], out int y))
                    {
                        Point saved = new Point(x, y);
                        foreach (Screen screen in Screen.AllScreens)
                        {
                            if (screen.WorkingArea.Contains(saved))
                            {
                                Location = saved;
                                break;
                            }
                        }
                    }
                }
            }
            catch { }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _port?.Close();
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(PositionFile));
                File.WriteAllText(PositionFile, string.Format("{0},{1}", Left, Top));
            }
            catch { }
            base.OnFormClosing(e);
        }
    }
}
