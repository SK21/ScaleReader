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

        private static readonly string PortFile = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ScaleDisplay", "port.txt");

        private static readonly string EmptyWeightFile = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ScaleDisplay", "emptyweight.txt");

        private static readonly string CsvDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "ScaleDisplay");

        // Bushel weights in lb/bu — always pounds per bushel regardless of scale units
        private static readonly Dictionary<string, decimal> CropBushelWeights = new Dictionary<string, decimal>
        {
            { "Wheat",     60m },
            { "Corn",      56m },
            { "Soybeans",  60m },
            { "Barley",    48m },
            { "Oats",      32m },
            { "Canola",    50m },
            { "Flax",      56m },
            { "Rye",       56m },
            { "Sunflower", 25m },
            { "Peas",      60m },
            { "None",       0m },
        };

        private SerialPort _port;
        private System.Windows.Forms.Timer _signalTimer;

        private enum ScaleState { Empty, Settling, Captured }
        private ScaleState _state = ScaleState.Empty;
        private DateTime? _stableStart;
        private int _loadNumber;
        private float _currentWeight;           // in whatever units the scale sends
        private string _currentUnits = "lb";    // "lb" or "kg"
        private string _lastDisplayUnits = "";  // detect unit change to update labels

        // Empty weight is always stored internally and on disk in kg.
        // The numEmptyWeight control shows it converted to _currentUnits.
        private float _emptyWeightKg = 0f;
        private bool _suppressEmptyWeightSave = false;

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
            InitializeCropCombo();
            RefreshPorts();

            _signalTimer = new System.Windows.Forms.Timer();
            _signalTimer.Tick += OnSignalTimerTick;

            InitializeLoadNumber();
            RefreshTodayWeights();
        }

        // ── Grid setup ───────────────────────────────────────────────────

        private void SetupGrids()
        {
            SetupGrid(dgvReport,
                new[] { "Date / Time", "Load #", "Crop", "Weight (kg)", "Bushels" },
                new[] { 40, 9, 20, 17, 14 });

            SetupGrid(dgvTodayWeights,
                new[] { "Time", "Load #", "Crop", "Weight (kg)", "Bushels" },
                new[] { 18, 14, 22, 24, 22 });
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

        // ── Crop combo ───────────────────────────────────────────────────

        private void InitializeCropCombo()
        {
            foreach (var crop in CropBushelWeights.Keys)
                cmbCrop.Items.Add(crop);
            cmbCrop.SelectedIndex = 0;
        }

        private void cmbCrop_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (CropBushelWeights.TryGetValue(cmbCrop.Text, out decimal bw))
                numBushelWeight.Value = bw;
            UpdateBushels();
        }

        private void numBushelWeight_ValueChanged(object sender, EventArgs e) => UpdateBushels();

        // ── Empty / net weight ───────────────────────────────────────────

        // Called whenever scale units are known or change. Updates labels and the
        // empty weight display without triggering a file save.
        private void ApplyUnits(string units)
        {
            if (units == _lastDisplayUnits) return;
            _lastDisplayUnits = units;

            lblEmptyLabel.Text = "Truck (" + units + "):";
            lblNetLabel.Text   = "Net (" + units + "):";
            SetEmptyWeightDisplay(_emptyWeightKg);
        }

        // Sets numEmptyWeight to the display-unit equivalent of the given kg value.
        private void SetEmptyWeightDisplay(float kg)
        {
            _suppressEmptyWeightSave = true;
            float displayVal = _currentUnits == "lb" ? kg * 2.20462f : kg;
            decimal d = Math.Max(numEmptyWeight.Minimum,
                        Math.Min(numEmptyWeight.Maximum, (decimal)Math.Round(displayVal)));
            numEmptyWeight.Value = d;
            _suppressEmptyWeightSave = false;
        }

        private void numEmptyWeight_ValueChanged(object sender, EventArgs e)
        {
            if (_suppressEmptyWeightSave) return;
            float displayVal = (float)numEmptyWeight.Value;
            _emptyWeightKg = _currentUnits == "lb" ? displayVal / 2.20462f : displayVal;
            SaveEmptyWeight();
            UpdateNet();
            UpdateBushels();
        }

        private void SaveEmptyWeight()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(EmptyWeightFile));
                File.WriteAllText(EmptyWeightFile, _emptyWeightKg.ToString("F2",
                    CultureInfo.InvariantCulture));
            }
            catch { }
        }

        private void LoadEmptyWeight()
        {
            try
            {
                if (File.Exists(EmptyWeightFile) &&
                    float.TryParse(File.ReadAllText(EmptyWeightFile).Trim(),
                        NumberStyles.Float, CultureInfo.InvariantCulture, out float kg))
                {
                    _emptyWeightKg = kg;
                    SetEmptyWeightDisplay(kg);
                }
            }
            catch { }
        }

        // Net weight in kg (for CSV logging)
        private float GetNetWeightKg()
        {
            float grossKg = _currentUnits == "lb" ? _currentWeight / 2.20462f : _currentWeight;
            return grossKg - _emptyWeightKg;
        }

        // Net weight in lbs (for bushel calculation — bushel weights are lb/bu)
        private float GetNetWeightLb()
        {
            return GetNetWeightKg() * 2.20462f;
        }

        // Net weight in current scale display units
        private float GetNetWeightDisplay()
        {
            return _currentUnits == "lb" ? GetNetWeightLb() : GetNetWeightKg();
        }

        private void UpdateNet()
        {
            float net = GetNetWeightDisplay();
            lblNetValue.Text = net >= 0 ? net.ToString("N1") : "---";
        }

        private void UpdateBushels()
        {
            float bw  = (float)numBushelWeight.Value; // lb/bu
            float net = GetNetWeightLb();              // always lbs for bushel math
            if (bw > 0 && net > 0)
                lblBushelsValue.Text = (net / bw).ToString("N1");
            else
                lblBushelsValue.Text = "---";
        }

        // ── Today's weights ──────────────────────────────────────────────

        private void RefreshTodayWeights()
        {
            dgvTodayWeights.Rows.Clear();
            string path = GetCsvPath(DateTime.Today);
            if (!File.Exists(path)) return;

            string[] lines = File.ReadAllLines(path);
            for (int i = 1; i < lines.Length; i++)
            {
                string[] parts = lines[i].Split(',');
                // New format: DateTime,Load,Crop,WeightKg,Bushels (5 cols)
                // Old format: DateTime,Load,Crop,Weight,Units,Bushels (6 cols)
                // Older:      DateTime,Load,Weight,Units (4 cols)
                if (parts.Length >= 4)
                {
                    string time    = parts[0].Length >= 19 ? parts[0].Substring(11, 8) : parts[0];
                    string load    = parts[1];
                    string crop, weight, bushels;

                    if (parts.Length >= 6)
                    {
                        // old 6-col format
                        crop    = parts[2];
                        weight  = parts[3];
                        bushels = parts[5];
                    }
                    else if (parts.Length == 5)
                    {
                        // new 5-col format
                        crop    = parts[2];
                        weight  = parts[3];
                        bushels = parts[4];
                    }
                    else
                    {
                        // 4-col legacy
                        crop    = "";
                        weight  = parts[2];
                        bushels = "";
                    }

                    dgvTodayWeights.Rows.Add(time, load, crop, weight, bushels);
                }
            }

            if (dgvTodayWeights.Rows.Count > 0)
            {
                int last = dgvTodayWeights.Rows.Count - 1;
                dgvTodayWeights.FirstDisplayedScrollingRowIndex = last;
                dgvTodayWeights.ClearSelection();
                dgvTodayWeights.Rows[last].Selected = true;
            }
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
                lblBushelsValue.Text = "---";
                lblNetValue.Text = "---";
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
                try
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(PortFile));
                    File.WriteAllText(PortFile, cmbPort.SelectedItem.ToString());
                }
                catch { }
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

                float.TryParse(parts[0], NumberStyles.Float,
                    CultureInfo.InvariantCulture, out float weightVal);

                string weightDisplay = weightVal.ToString("N1") + " " + parts[1];
                string mode   = parts[2];
                string status = parts[3];
                string units  = parts[1];

                Color statusColor;
                switch (status)
                {
                    case "Stable": statusColor = Color.Green;      break;
                    case "Motion": statusColor = Color.DarkOrange; break;
                    default:       statusColor = Color.Red;         break;
                }

                _currentWeight = weightVal;
                _currentUnits  = units;

                if (_autoWeighEnabled)
                    RunStateMachine(weightVal, units, status);

                Invoke(new Action(() =>
                {
                    ApplyUnits(units);
                    lblWeightValue.Text      = weightDisplay;
                    lblModeValue.Text        = mode;
                    lblStatusValue.Text      = status;
                    lblStatusValue.ForeColor = statusColor;
                    UpdateNet();
                    UpdateBushels();
                }));
            }
            catch { }
        }

        // ── Auto-weigh state machine ─────────────────────────────────────

        private void RunStateMachine(float weight, string units, string status)
        {
            int minWeight       = _minWeightLbs;
            int stabilityNeeded = _stabilitySeconds;

            // Compare against minimum using lbs regardless of scale units
            float weightLb = units == "kg" ? weight * 2.20462f : weight;

            switch (_state)
            {
                case ScaleState.Empty:
                    if (weightLb >= minWeight)
                        _state = ScaleState.Settling;
                    break;

                case ScaleState.Settling:
                    if (weightLb < minWeight)
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
                        _stableStart = null;
                    }
                    break;

                case ScaleState.Captured:
                    if (weightLb < minWeight)
                    {
                        _state = ScaleState.Empty;
                        _stableStart = null;
                    }
                    break;
            }
        }

        private void CaptureWeight(float weight, string units)
        {
            string crop  = "";
            float  bw    = 60f;
            float  emKg  = 0f;
            Invoke(new Action(() =>
            {
                crop = cmbCrop.Text;
                bw   = (float)numBushelWeight.Value;
                emKg = _emptyWeightKg;
            }));

            float grossKg = units == "kg" ? weight : weight / 2.20462f;
            float netKg   = grossKg - emKg;
            float netLb   = netKg * 2.20462f;
            float bushels = bw > 0 && netLb > 0 ? netLb / bw : 0f;

            LogCapture(netKg, crop, bushels);
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
            float netKg = GetNetWeightKg();
            if (netKg <= 0) return;
            float bw      = (float)numBushelWeight.Value;
            float netLb   = netKg * 2.20462f;
            float bushels = bw > 0 ? netLb / bw : 0f;
            LogCapture(netKg, cmbCrop.Text, bushels);
            lblWeightValue.BackColor = Color.LightGreen;
            _signalTimer.Interval = _signalDurationSeconds * 1000;
            _signalTimer.Start();
            SendRelay(true);
            RefreshTodayWeights();
        }

        private void btnDeleteWeight_Click(object sender, EventArgs e)
        {
            if (dgvTodayWeights.SelectedRows.Count == 0) return;

            string load = dgvTodayWeights.SelectedRows[0].Cells[1].Value?.ToString() ?? "";

            if (MessageBox.Show($"Delete Load #{load}?", "Confirm Delete",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                return;

            string path = GetCsvPath(DateTime.Today);
            if (!File.Exists(path)) return;

            var lines = new System.Collections.Generic.List<string>(File.ReadAllLines(path));
            lines.RemoveAll(line =>
            {
                if (line.StartsWith("DateTime")) return false;
                string[] p = line.Split(',');
                return p.Length >= 2 && p[1] == load;
            });

            File.WriteAllLines(path, lines);

            // Update _loadNumber to the highest remaining load number
            _loadNumber = 0;
            foreach (string line in lines)
            {
                if (line.StartsWith("DateTime")) continue;
                string[] p = line.Split(',');
                if (p.Length >= 2 && int.TryParse(p[1], out int n))
                    _loadNumber = Math.Max(_loadNumber, n);
            }

            RefreshTodayWeights();
        }

        private void btnPrintReceipt_Click(object sender, EventArgs e)
        {
            if (dgvTodayWeights.SelectedRows.Count == 0)
            {
                MessageBox.Show("Select a load to print.", "Nothing Selected",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var row = dgvTodayWeights.SelectedRows[0];
            string load    = row.Cells[1].Value?.ToString() ?? "";
            string crop    = row.Cells[2].Value?.ToString() ?? "";
            string weightStr = row.Cells[3].Value?.ToString() ?? "0";
            string bushels = row.Cells[4].Value?.ToString() ?? "---";

            // Reconstruct gross from net + truck weight (both in kg)
            float.TryParse(weightStr, NumberStyles.Float,
                CultureInfo.InvariantCulture, out float netKg);
            float grossKg  = netKg + _emptyWeightKg;
            string dateStr = DateTime.Today.ToString("MMMM d, yyyy");

            // Find full timestamp from CSV
            string timeStr = row.Cells[0].Value?.ToString() ?? "";
            try
            {
                string path = GetCsvPath(DateTime.Today);
                foreach (string line in File.ReadAllLines(path))
                {
                    string[] p = line.Split(',');
                    if (p.Length >= 2 && p[1] == load && !line.StartsWith("DateTime"))
                    {
                        if (p[0].Length >= 19 &&
                            DateTime.TryParseExact(p[0].Substring(11, 8), "HH:mm:ss",
                                CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out DateTime t))
                            timeStr = t.ToString("h:mm tt");
                        break;
                    }
                }
            }
            catch { }

            var receipt = new ReceiptData
            {
                Date    = dateStr,
                Time    = timeStr,
                Load    = load,
                Crop    = crop,
                GrossKg = grossKg,
                TruckKg = _emptyWeightKg,
                NetKg   = netKg,
                Bushels = bushels,
            };

            PrintDocument pd = new PrintDocument();
            pd.PrintPage += (s, pe) => PrintReceipt(pe, receipt);

            PrintDialog dlg = new PrintDialog { Document = pd };
            if (dlg.ShowDialog() == DialogResult.OK)
                pd.Print();
        }

        private struct ReceiptData
        {
            public string Date, Time, Load, Crop, Bushels;
            public float GrossKg, TruckKg, NetKg;
        }

        private void PrintReceipt(PrintPageEventArgs e, ReceiptData r)
        {
            Graphics g      = e.Graphics;
            float x         = e.MarginBounds.Left;
            float y         = e.MarginBounds.Top;
            float pageW     = e.MarginBounds.Width;
            float labelCol  = x;
            float valueCol  = x + 120;

            Font titleFont  = new Font("Arial", 16, FontStyle.Bold);
            Font headFont   = new Font("Arial", 10, FontStyle.Bold);
            Font bodyFont   = new Font("Arial", 10);
            Font bigFont    = new Font("Arial", 14, FontStyle.Bold);
            Pen  divPen     = new Pen(Color.Black, 1);

            // Title
            string title = "SCALE RECEIPT";
            SizeF ts = g.MeasureString(title, titleFont);
            g.DrawString(title, titleFont, Brushes.Black, x + (pageW - ts.Width) / 2, y);
            y += ts.Height + 6;

            g.DrawLine(divPen, x, y, x + pageW, y);
            y += 10;

            // Header fields
            void Row(string label, string value, Font vFont = null)
            {
                g.DrawString(label, headFont, Brushes.Black, labelCol, y);
                g.DrawString(value, vFont ?? bodyFont, Brushes.Black, valueCol, y);
                y += headFont.GetHeight(g) + 4;
            }

            Row("Date:",   r.Date);
            Row("Time:",   r.Time);
            Row("Load #:", r.Load);
            Row("Crop:",   string.IsNullOrEmpty(r.Crop) ? "---" : r.Crop);
            y += 6;

            g.DrawLine(divPen, x, y, x + pageW, y);
            y += 10;

            // Weights
            Row("Gross:",   FormatKg(r.GrossKg) + " kg", bigFont);
            Row("Truck:",   FormatKg(r.TruckKg)  + " kg", bigFont);
            y += 4;
            g.DrawLine(divPen, x, y, x + pageW, y);
            y += 6;
            Row("Net:",     FormatKg(r.NetKg)    + " kg", bigFont);
            y += 6;

            g.DrawLine(divPen, x, y, x + pageW, y);
            y += 10;

            Row("Bushels:", r.Bushels + " bu", bigFont);

            titleFont.Dispose(); headFont.Dispose(); bodyFont.Dispose();
            bigFont.Dispose();   divPen.Dispose();
            e.HasMorePages = false;
        }

        private static string FormatKg(float kg) =>
            kg.ToString("N1", CultureInfo.CurrentCulture);

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

        // Weight is always saved in kg. CSV format: DateTime,Load,Crop,WeightKg,Bushels
        private int LogCapture(float weightKg, string crop, float bushels)
        {
            try
            {
                Directory.CreateDirectory(CsvDirectory);
                string path = GetCsvPath(DateTime.Today);

                if (!File.Exists(path))
                {
                    _loadNumber = 0;
                    File.AppendAllText(path, "DateTime,Load,Crop,WeightKg,Bushels\r\n");
                }

                _loadNumber++;

                File.AppendAllText(path, string.Format(CultureInfo.InvariantCulture,
                    "{0},{1},{2},{3:F1},{4:F1}\r\n",
                    DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    _loadNumber, crop, weightKg, bushels));
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
            numSignal.Enabled    = chkAutoWeigh.Checked;

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
                if (parts.Length >= 4)
                {
                    string crop, weight, bushels;

                    if (parts.Length >= 6)
                    {
                        // old 6-col format (Weight, Units separate)
                        crop    = parts[2];
                        weight  = parts[3];
                        bushels = parts[5];
                    }
                    else if (parts.Length == 5)
                    {
                        // new 5-col format
                        crop    = parts[2];
                        weight  = parts[3];
                        bushels = parts[4];
                    }
                    else
                    {
                        // 4-col legacy (no crop)
                        crop    = "";
                        weight  = parts[2];
                        bushels = "";
                    }

                    dgvReport.Rows.Add(parts[0], parts[1], crop, weight, bushels);
                }
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
                        row.Cells[3].Value?.ToString() ?? "",
                        row.Cells[4].Value?.ToString() ?? ""
                    });
            }
            _printDate    = dtpReport.Value.Date;
            _printRowIndex = 0;

            PrintDocument pd = new PrintDocument();
            pd.PrintPage += PrintPage;

            PrintDialog dlg = new PrintDialog { Document = pd };
            if (dlg.ShowDialog() == DialogResult.OK)
                pd.Print();
        }

        private void PrintPage(object sender, PrintPageEventArgs e)
        {
            Font titleFont  = new Font("Arial", 13, FontStyle.Bold);
            Font headerFont = new Font("Arial", 9, FontStyle.Bold);
            Font dataFont   = new Font("Arial", 9);
            float lineH = dataFont.GetHeight(e.Graphics) + 3;
            float x = e.MarginBounds.Left;
            float y = e.MarginBounds.Top;

            if (_printRowIndex == 0)
            {
                string title = "Scale Report  —  " + _printDate.ToString("MMMM d, yyyy");
                e.Graphics.DrawString(title, titleFont, Brushes.Black, x, y);
                y += titleFont.GetHeight(e.Graphics) + 8;

                DrawPrintRow(e.Graphics, headerFont, x, y,
                    "Date / Time", "Load #", "Crop", "Weight (kg)", "Bushels");
                y += lineH;
                e.Graphics.DrawLine(Pens.Black, x, y, e.MarginBounds.Right, y);
                y += 4;
            }

            while (_printRowIndex < _printRows.Count)
            {
                string[] row = _printRows[_printRowIndex];
                DrawPrintRow(e.Graphics, dataFont, x, y,
                    row[0], row[1], row[2], row[3], row[4]);
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
            string dateTime, string load, string crop, string weight, string bushels)
        {
            g.DrawString(dateTime, font, Brushes.Black, x,       y);
            g.DrawString(load,     font, Brushes.Black, x + 130, y);
            g.DrawString(crop,     font, Brushes.Black, x + 175, y);
            g.DrawString(weight,   font, Brushes.Black, x + 295, y);
            g.DrawString(bushels,  font, Brushes.Black, x + 355, y);
        }

        // ── Position persistence ─────────────────────────────────────────

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            LoadEmptyWeight();

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

            // Auto-connect to last good port
            try
            {
                if (File.Exists(PortFile))
                {
                    string savedPort = File.ReadAllText(PortFile).Trim();
                    if (cmbPort.Items.Contains(savedPort))
                    {
                        cmbPort.SelectedItem = savedPort;
                        btnConnect_Click(this, EventArgs.Empty);
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
