using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Printing;
using System.Globalization;
using System.IO;
using System.IO.Ports;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace ScaleDisplay
{
    public partial class Form1 : Form
    {
        // UDP receiver
        private UdpClient _udp;
        private IPEndPoint _udpEP = new IPEndPoint(IPAddress.Any, 5005);
        private Thread _udpThread;
        private volatile bool _udpRunning = false;
        private enum ConnectionMode { None, Serial, Udp }
        private volatile ConnectionMode _connectionMode = ConnectionMode.None;
        private DateTime _lastUdpReceived = DateTime.MinValue;
        private System.Windows.Forms.Timer _udpWatchdog;

        private const string AppVersion = "1.0.0";

        private static readonly string PositionFile = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ScaleDisplay", "position.txt");

        private static readonly string PortFile = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ScaleDisplay", "port.txt");

        private static readonly string EmptyWeightFile = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ScaleDisplay", "emptyweight.txt");

        private static readonly string CropSettingsFile = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ScaleDisplay", "crop.txt");

        private static readonly string AutoWeighSettingsFile = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ScaleDisplay", "autoweigh.txt");

        private static readonly string CsvDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "ScaleDisplay");

        // Bushel weights in lb/bu — always pounds per bushel regardless of scale units
        private static readonly Dictionary<string, decimal> CropBushelWeights = new Dictionary<string, decimal>
        {
            { "Wheat",     60m },
            { "Oats",      34m },
            { "Barley",    48m },
            { "Flax",      56m },
            { "Canola",    50m },
            { "Peas",      60m },
            { "Rye",       56m },
            { "Soybeans",  60m },
            { "Sunflower", 25m },
            { "Corn",      56m },
            { "None",       0m },
        };

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

        // Gross weight captured for the current load (not persisted).
        private float _grossWeightKg = 0f;
        private bool _suppressGrossWeightSave = false;

        private bool _settingsLoaded = false; // suppress saves until persisted values are restored

        // Notes keyed by load number string; rebuilt on each RefreshTodayWeights.
        private Dictionary<string, string> _notesByLoad = new Dictionary<string, string>();
        private string _displayedNoteLoad = null; // load# whose note is in txtNote

        // Notes for the currently loaded report date.
        private Dictionary<string, string> _reportNotesByLoad = new Dictionary<string, string>();
        private string _reportDisplayedLoad = null;

        private bool _suppressMinWeightSave = false;

        // Cached for safe access from the DataReceived background thread
        private volatile bool _autoWeighEnabled;
        private volatile int _minWeightLbs = 10000;
        private volatile int _minIntervalSeconds = 0;
        private volatile int _stabilityToleranceLbs = 50;
        private volatile string _manualUnits = "lb";
        private DateTime _lastCaptureTime = DateTime.MinValue;
        private float _stabilityRefWeightLb = 0f; // "lb" or "kg"; user-controlled
        private volatile int _stabilitySeconds = 15;
        private volatile int _signalDurationSeconds = 5;

        // Split weighing
        private const int SplitMaxSections = 5;
        private readonly List<float> _splitGrossSections = new List<float>();
        private readonly List<float> _splitTruckSections = new List<float>();
        private bool _splitNoteActive;

        private BigDisplayForm _bigDisplay;
        private bool _appClosing;

        // Print state
        private List<string[]> _printRows;
        private DateTime _printDate;
        private int _printRowIndex;
        private string _printTotalWeight = "0";
        private string _printTotalBushels = "0";

        public SerialComm CommPort;

        public Form1()
        {
            InitializeComponent();
            StartUdpReceiver();
            Text = "Scale Reader [Version " + AppVersion + "]";
            SetupGrids();
            InitializeCropCombo();
            RefreshPorts();

            _signalTimer = new System.Windows.Forms.Timer();
            _signalTimer.Tick += OnSignalTimerTick;

            InitializeLoadNumber();
            RefreshTodayWeights();
        }
        public void OpenPort()
        {
            try
            {
                if (CommPort == null || !CommPort.IsOpen)
                {
                    int PortBaud = int.TryParse(cmbBaud.SelectedItem.ToString(), out int bb) ? bb : 9600;
                    CommPort = new SerialComm(this, cmbPort.SelectedItem.ToString(), PortBaud);
                    CommPort.PortDisconnected += CommPort_PortDisconnected;

                    if (CommPort.IsOpen)
                    {
                        _connectionMode = ConnectionMode.Serial;
                        lblConnectionStatus.Text = "Serial: " + cmbPort.SelectedItem;
                        btnConnect.Text = "Disconnect";
                        cmbPort.Enabled = false;
                        cmbBaud.Enabled = false;
                        btnRefresh.Enabled = false;
                        UpdateManualControls();
                        Properties.Settings.Default.SerialPort = cmbPort.SelectedItem.ToString();
                        Properties.Settings.Default.Baud = PortBaud;
                        Properties.Settings.Default.Save();
                    }
                    else
                    {
                        MessageBox.Show("Could not open comm port.");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("OpenPort: " + ex.Message);
            }
        }
        private void CommPort_PortDisconnected()
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(CommPort_PortDisconnected));
                return;
            }
            _currentWeight = 0f;
            SetWeightDisplay("0 " + _currentUnits);
            lblWeightValue.BackColor = SystemColors.Control;
            _connectionMode = ConnectionMode.None;
            lblConnectionStatus.Text = "No connection";
            ResetStateMachine();
            UpdateForm();
        }
        private void UpdateForm(bool UpdateCombo = true)
        {
            if (CommPort != null && CommPort.IsOpen)
            {
                btnConnect.Text = "Disconnect";
                cmbPort.Enabled = false;
                cmbBaud.Enabled = false;
                btnRefresh.Enabled = false;
            }
            else
            {
                btnConnect.Text = "Connect";
                cmbPort.Enabled = true;
                cmbBaud.Enabled = true;
                btnRefresh.Enabled = true;
            }

            txtCommand.Enabled = !ckLP7515.Checked;
            btnSendCommand.Enabled = !ckLP7515.Checked;

            if (UpdateCombo) RefreshPorts();

            UpdateManualControls();
        }

        // ── Grid setup ───────────────────────────────────────────────────

        private void SetupGrids()
        {
            SetupGrid(dgvReport,
                new[] { "Date / Time", "Load #", "Crop", "Weight (kg)", "Bushels" },
                new[] { 34, 9, 20, 23, 14 });
            RightAlignColumns(dgvReport, 3, 4);

            SetupGrid(dgvTodayWeights,
                new[] { "Time", "Load #", "Crop", "Weight (kg)", "Bushels" },
                new[] { 18, 14, 22, 24, 22 });
            RightAlignColumns(dgvTodayWeights, 3, 4);
        }

        private static void RightAlignColumns(DataGridView grid, params int[] indices)
        {
            foreach (int i in indices)
            {
                grid.Columns[i].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                grid.Columns[i].HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleRight;
            }
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
                try { numBushelWeight.Value = bw; } catch { }
            SaveCropSettings();
            UpdateBushels();
        }

        private void numBushelWeight_ValueChanged(object sender, EventArgs e)
        {
            SaveCropSettings();
            UpdateBushels();
        }

        private void SaveCropSettings()
        {
            if (!_settingsLoaded) return;
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(CropSettingsFile));
                File.WriteAllText(CropSettingsFile,
                    cmbCrop.Text + "," + numBushelWeight.Value.ToString(CultureInfo.InvariantCulture));
            }
            catch { }
        }

        private void LoadCropSettings()
        {
            try
            {
                if (!File.Exists(CropSettingsFile)) return;
                string[] parts = File.ReadAllText(CropSettingsFile).Split(',');
                if (parts.Length < 2) return;

                string crop = parts[0];
                if (cmbCrop.Items.Contains(crop))
                {
                    cmbCrop.SelectedItem = crop;   // sets default bw via SelectedIndexChanged
                }
                // Override with the saved custom bushel weight
                if (decimal.TryParse(parts[1], NumberStyles.Any,
                        CultureInfo.InvariantCulture, out decimal bw))
                    try { numBushelWeight.Value = bw; } catch { }
            }
            catch { }
        }

        // ── Empty / net weight ───────────────────────────────────────────

        // Called whenever scale units are known or change. Updates labels and the
        // empty weight display without triggering a file save.
        private void ApplyUnits(string units)
        {
            if (IsDisposed || units == _lastDisplayUnits) return;
            _lastDisplayUnits = units;

            lblGrossLabel.Text = "Gross (" + units + "):";
            lblEmptyLabel.Text = "Truck (" + units + "):";
            lblNetLabel.Text = "Net (" + units + "):";
            lblMinWeight.Text = "Min Weight (" + units + "):";
            lblTolerance.Text = "Tolerance (" + units + "):";
            label3.Text = "Gross (" + units + "):";
            label4.Text = "Truck (" + units + "):";
            dgvTodayWeights.Columns[3].HeaderText = "Weight (" + units + ")";
            dgvReport.Columns[3].HeaderText = "Weight (" + units + ")";
            SetGrossWeightDisplay(_grossWeightKg);
            SetEmptyWeightDisplay(_emptyWeightKg);
            SetMinWeightDisplay();
        }

        private void SetMinWeightDisplay()
        {
            _suppressMinWeightSave = true;
            float displayVal = _currentUnits == "kg" ? _minWeightLbs / 2.20462f : _minWeightLbs;
            decimal d = Math.Max(numMinWeight.Minimum, Math.Min(numMinWeight.Maximum, (decimal)Math.Round(displayVal)));
            numMinWeight.Value = d;
            _suppressMinWeightSave = false;
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

        private void btnCaptureTruck_Click(object sender, EventArgs e)
        {
            if (_currentWeight <= 0) return;
            if (ckSplitMode.Checked)
            {
                if (_splitTruckSections.Count >= SplitMaxSections) return;
                float sectionKg = _currentUnits == "lb" ? _currentWeight / 2.20462f : _currentWeight;
                _splitTruckSections.Add(sectionKg);
                float totalKg = SumList(_splitTruckSections);
                _emptyWeightKg = totalKg;
                SetEmptyWeightDisplay(totalKg);
                UpdateNet();
                UpdateBushels();
                UpdateSplitNote();
                btnDeleteTruckSplit.Enabled = true;
                UpdateManualControls();
            }
            else
            {
                decimal val = Math.Max(numEmptyWeight.Minimum,
                              Math.Min(numEmptyWeight.Maximum, (decimal)Math.Round(_currentWeight)));
                numEmptyWeight.Value = val;
            }
        }

        private void SetGrossWeightDisplay(float kg)
        {
            _suppressGrossWeightSave = true;
            float displayVal = _currentUnits == "lb" ? kg * 2.20462f : kg;
            decimal d = Math.Max(numGrossWeight.Minimum,
                        Math.Min(numGrossWeight.Maximum, (decimal)Math.Round(displayVal)));
            numGrossWeight.Value = d;
            _suppressGrossWeightSave = false;
        }

        private void numGrossWeight_ValueChanged(object sender, EventArgs e)
        {
            if (_suppressGrossWeightSave) return;
            float displayVal = (float)numGrossWeight.Value;
            _grossWeightKg = _currentUnits == "lb" ? displayVal / 2.20462f : displayVal;
            UpdateNet();
            UpdateBushels();
        }

        private void btnCaptureGross_Click(object sender, EventArgs e)
        {
            if (_currentWeight <= 0) return;
            if (ckSplitMode.Checked)
            {
                if (_splitGrossSections.Count >= SplitMaxSections) return;
                float sectionKg = _currentUnits == "lb" ? _currentWeight / 2.20462f : _currentWeight;
                _splitGrossSections.Add(sectionKg);
                float totalKg = SumList(_splitGrossSections);
                _grossWeightKg = totalKg;
                SetGrossWeightDisplay(totalKg);
                UpdateNet();
                UpdateBushels();
                UpdateSplitNote();
                btnDeleteGrossSplit.Enabled = true;
                UpdateManualControls();
            }
            else
            {
                decimal val = Math.Max(numGrossWeight.Minimum,
                              Math.Min(numGrossWeight.Maximum, (decimal)Math.Round(_currentWeight)));
                numGrossWeight.Value = val;
            }
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

        // Net from captured gross (for CSV logging and bushels)
        private float GetNetWeightKg() => _grossWeightKg - _emptyWeightKg;
        private float GetNetWeightLb() => GetNetWeightKg() * 2.20462f;
        private float GetNetWeightDisplay() => _currentUnits == "lb" ? GetNetWeightLb() : GetNetWeightKg();

        private void UpdateNet()
        {
            float net = GetNetWeightDisplay();
            lblNetValue.Text = (_grossWeightKg > 0 && net >= 0) ? net.ToString("N0") : "---";
        }

        private void UpdateBushels()
        {
            float bw = (float)numBushelWeight.Value; // lb/bu
            float net = GetNetWeightLb();              // always lbs for bushel math
            if (bw > 0 && net > 0)
                lblBushelsValue.Text = (net / bw).ToString("N0");
            else
                lblBushelsValue.Text = "---";
        }

        // ── Today's weights ──────────────────────────────────────────────

        private void RefreshTodayWeights()
        {
            _notesByLoad.Clear();
            dgvTodayWeights.Rows.Clear();
            lbDailyWeight.Text = "0";
            lbDailyBushels.Text = "0";
            string path = GetCsvPath(DateTime.Today);
            if (!File.Exists(path)) return;

            string[] lines = File.ReadAllLines(path);
            for (int i = 1; i < lines.Length; i++)
            {
                string[] parts = lines[i].Split(',');
                // Current: DateTime,Load,Crop,WeightKg,Bushels,Note (note may contain commas)
                // Old 6-col: DateTime,Load,Crop,Weight,Units,Bushels
                // Old 5-col: DateTime,Load,Crop,WeightKg,Bushels
                // Legacy 4-col: DateTime,Load,Weight,Units
                if (parts.Length >= 4)
                {
                    string time = parts[0].Length >= 19 ? parts[0].Substring(11, 8) : parts[0];
                    DateTime t = DateTime.Parse(time);
                    time = t.ToString("h:mm tt");
                    string load = parts[1];
                    string crop, weight, bushels, note;

                    if (parts.Length >= 6 && IsUnitsField(parts[4]))
                    {
                        // old 6-col (Units column is "lb" or "kg")
                        crop = parts[2];
                        weight = parts[3];
                        bushels = parts[5];
                        note = "";
                    }
                    else if (parts.Length >= 5)
                    {
                        // current 5+ col: DateTime,Load,Crop,WeightKg,Bushels[,Note...]
                        crop = parts[2];
                        weight = parts[3];
                        bushels = parts[4];
                        note = parts.Length >= 6
                            ? string.Join(",", parts, 5, parts.Length - 5)
                            : "";
                    }
                    else
                    {
                        // 4-col legacy
                        crop = "";
                        weight = parts[2];
                        bushels = "";
                        note = "";
                    }

                    _notesByLoad[load] = note;
                    dgvTodayWeights.Rows.Add(time, load, crop, FormatKgStr(weight), FormatBushels(bushels));
                }
            }

            if (dgvTodayWeights.Rows.Count > 0)
            {
                int last = dgvTodayWeights.Rows.Count - 1;
                dgvTodayWeights.FirstDisplayedScrollingRowIndex = last;
                dgvTodayWeights.ClearSelection();
                dgvTodayWeights.Rows[last].Selected = true;
            }

            UpdateTodayTotals();
        }

        private static bool IsUnitsField(string s) => s == "lb" || s == "kg";

        private static string FormatBushels(string raw) =>
            float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out float v)
                ? (v > 0 ? v.ToString("N0") : "---") : raw;

        private void UpdateTodayTotals()
        {
            decimal totalWeight = 0m;
            decimal totalBushels = 0m;

            foreach (DataGridViewRow row in dgvTodayWeights.Rows)
            {
                if (row.IsNewRow) continue;

                if (decimal.TryParse(row.Cells[3].Value?.ToString(), NumberStyles.Any,
                    CultureInfo.CurrentCulture, out decimal weight))
                    totalWeight += weight;

                if (decimal.TryParse(row.Cells[4].Value?.ToString(), NumberStyles.Any,
                    CultureInfo.CurrentCulture, out decimal bushels))
                    totalBushels += bushels;
            }

            lbDailyWeight.Text = totalWeight.ToString("N0", CultureInfo.CurrentCulture);
            lbDailyBushels.Text = totalBushels.ToString("N0", CultureInfo.CurrentCulture);
        }

        private void UpdateReportTotals()
        {
            decimal totalWeight = 0m;
            decimal totalBushels = 0m;

            foreach (DataGridViewRow row in dgvReport.Rows)
            {
                if (row.IsNewRow) continue;

                if (decimal.TryParse(row.Cells[3].Value?.ToString(), NumberStyles.Any,
                    CultureInfo.CurrentCulture, out decimal weight))
                    totalWeight += weight;

                if (decimal.TryParse(row.Cells[4].Value?.ToString(), NumberStyles.Any,
                    CultureInfo.CurrentCulture, out decimal bushels))
                    totalBushels += bushels;
            }

            lbTotalWeight.Text = totalWeight.ToString("N0", CultureInfo.CurrentCulture);
            lbTotalBushels.Text = totalBushels.ToString("N0", CultureInfo.CurrentCulture);
        }

        private void FlushCurrentNote()
        {
            if (_splitNoteActive) return;
            if (_displayedNoteLoad == null) return;
            string current = txtNote.Text.Replace("\r\n", " ").Replace("\n", " ").Trim();
            if (!_notesByLoad.TryGetValue(_displayedNoteLoad, out string stored) || current != stored)
            {
                _notesByLoad[_displayedNoteLoad] = current;
                SaveNoteToCSV(_displayedNoteLoad, current);
            }
        }

        private void dgvTodayWeights_SelectionChanged(object sender, EventArgs e)
        {
            if (_splitNoteActive) return;

            FlushCurrentNote();

            if (dgvTodayWeights.SelectedRows.Count == 0)
            {
                _displayedNoteLoad = null;
                txtNote.Text = "";
                return;
            }

            string load = dgvTodayWeights.SelectedRows[0].Cells[1].Value?.ToString() ?? "";
            _displayedNoteLoad = load;
            txtNote.Text = ExpandNoteForDisplay(_notesByLoad.TryGetValue(load, out string note) ? note : "");
        }

        private void SaveNoteToCSV(string load, string note) =>
            SaveNoteToCSV(load, note, DateTime.Today);

        private void SaveNoteToCSV(string load, string note, DateTime date)
        {
            try
            {
                string path = GetCsvPath(date);
                if (!File.Exists(path)) return;

                string[] lines = File.ReadAllLines(path);
                for (int i = 1; i < lines.Length; i++)
                {
                    string[] parts = lines[i].Split(',');
                    if (parts.Length < 2 || parts[1] != load) continue;

                    // Rebuild the line: keep first 5 fields, replace/add note
                    int keep = Math.Min(parts.Length, 5);
                    string[] core = new string[5];
                    for (int j = 0; j < 5; j++)
                        core[j] = j < parts.Length ? parts[j] : "";
                    lines[i] = string.Join(",", core) + "," + note;
                    break;
                }
                File.WriteAllLines(path, lines);
            }
            catch { }
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
            try
            {
                if (CommPort != null && CommPort.IsOpen)
                {
                    CommPort.ClosePort();
                    btnConnect.Text = "Connect";
                    cmbPort.Enabled = true;
                    btnRefresh.Enabled = true;
                    UpdateManualControls();
                    SetWeightDisplay("---");
                    lblWeightValue.BackColor = SystemColors.Control;
                    lblBushelsValue.Text = "---";
                    lblNetValue.Text = "---";
                    ResetStateMachine();
                    _connectionMode = ConnectionMode.None;
                    lblConnectionStatus.Text = "No connection";
                }
                else
                {
                    OpenPort();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not open comm port.\n " + ex.Message);
            }
            UpdateForm();
        }

        // ── Serial read thread ───────────────────────────────────────────

        public void AppendSerialOutput(string line)
        {
            if (txtSerialOutput.InvokeRequired)
                txtSerialOutput.Invoke(new Action(() => AppendSerialOutput(line)));
            else
            {
                txtSerialOutput.AppendText(line + "\r\n");
                // Keep last 20 lines
                var lines = txtSerialOutput.Lines;
                if (lines.Length > 20)
                    txtSerialOutput.Text = string.Join("\r\n", lines, lines.Length - 20, 20);
                txtSerialOutput.SelectionStart = txtSerialOutput.Text.Length;
                txtSerialOutput.ScrollToCaret();
            }
        }

        // Parses the weight field from a WT sentence (shared by serial and UDP paths).
        // Returns false if the sentence is malformed or the units suffix is unrecognised.
        private static bool ParseWtSentence(string line, out float weightVal, out string scaleUnits)
        {
            weightVal = 0f;
            scaleUnits = "";
            string[] parts = line.Split(',');
            if (parts.Length < 4 || parts[0] != "WT") return false;

            string field = parts[3].Trim();
            if (field.EndsWith("lb", StringComparison.OrdinalIgnoreCase))
                scaleUnits = "lb";
            else if (field.EndsWith("kg", StringComparison.OrdinalIgnoreCase))
                scaleUnits = "kg";
            else return false;

            field = field.Substring(0, field.Length - 2);
            return float.TryParse(field, NumberStyles.Float,
                CultureInfo.InvariantCulture, out weightVal);
        }

        // Converts, updates state machine, and posts the UI update.
        // extraUiAction (optional) runs inside the Invoke before the standard weight labels.
        public void ApplyWeightReading(float weightVal, string scaleUnits,
                                        Action extraUiAction = null)
        {
            string displayUnits = _manualUnits;
            float displayWeight = (scaleUnits == "kg" && displayUnits == "lb") ? weightVal * 2.20462f
                                : (scaleUnits == "lb" && displayUnits == "kg") ? weightVal / 2.20462f
                                : weightVal;

            _currentWeight = displayWeight;

            if (_autoWeighEnabled)
                RunStateMachine(weightVal, scaleUnits);

            Invoke(new Action(() =>
            {
                extraUiAction?.Invoke();
                ApplyUnits(displayUnits);
                SetWeightDisplay(displayWeight.ToString("N0") + " " + displayUnits);
                UpdateNet();
                UpdateBushels();
            }));
        }

        // ── Auto-weigh state machine ─────────────────────────────────────

        private void RunStateMachine(float weight, string units)
        {
            int minWeight = _minWeightLbs;
            int stabilityNeeded = _stabilitySeconds;
            float tolerance = _stabilityToleranceLbs;

            float weightLb = units == "kg" ? weight * 2.20462f : weight;

            switch (_state)
            {
                case ScaleState.Empty:
                    bool intervalElapsed = _minIntervalSeconds == 0 ||
                        (DateTime.Now - _lastCaptureTime).TotalSeconds >= _minIntervalSeconds;
                    if (weightLb >= minWeight && intervalElapsed)
                    {
                        _state = ScaleState.Settling;
                        _stabilityRefWeightLb = weightLb;
                    }
                    break;

                case ScaleState.Settling:
                    if (weightLb < minWeight)
                    {
                        _state = ScaleState.Empty;
                        _stableStart = null;
                    }
                    else if (Math.Abs(weightLb - _stabilityRefWeightLb) <= tolerance)
                    {
                        if (_stableStart == null)
                            _stableStart = DateTime.Now;
                        else if ((DateTime.Now - _stableStart.Value).TotalSeconds >= stabilityNeeded)
                        {
                            _state = ScaleState.Captured;
                            _lastCaptureTime = DateTime.Now;
                            CaptureWeight(weight, units);
                        }
                    }
                    else
                    {
                        _stabilityRefWeightLb = weightLb;
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
            string crop = "";
            string note = "";
            float bw = 60f;
            float emKg = 0f;
            Invoke(new Action(() =>
            {
                crop = cmbCrop.Text;
                bw = (float)numBushelWeight.Value;
                emKg = _emptyWeightKg;
                note = _displayedNoteLoad == null
                    ? txtNote.Text.Replace("\r\n", " ").Replace("\n", " ").Trim()
                    : "";
                FlushCurrentNote();
            }));

            float grossKg = units == "kg" ? weight : weight / 2.20462f;
            float netKg = grossKg - emKg;
            float netLb = netKg * 2.20462f;
            float bushels = bw > 0 && netLb > 0 ? netLb / bw : 0f;

            LogCapture(netKg, crop, bushels, note);
            SendRelay(true);
            Invoke(new Action(() =>
            {
                SetGrossWeightDisplay(grossKg); // show captured gross in the box
                lblWeightValue.BackColor = Color.LightGreen;
                _signalTimer.Interval = _signalDurationSeconds * 1000;
                _signalTimer.Start();
                RefreshTodayWeights(); // selects new row → SelectionChanged sets _displayedNoteLoad correctly
            }));
        }

        private void btnManualWeigh_Click(object sender, EventArgs e)
        {
            if (_grossWeightKg <= 0)
            {
                MessageBox.Show("Capture the gross weight first.", "No Gross Weight",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            float netKg = GetNetWeightKg();
            if (netKg <= 0)
            {
                MessageBox.Show("Net weight is zero or negative. Check gross and truck weights.",
                    "Invalid Weight", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            float bw = (float)numBushelWeight.Value;
            float netLb = netKg * 2.20462f;
            float bushels = bw > 0 ? netLb / bw : 0f;
            // If a load is selected, txtNote shows that load's note — don't carry it to the new record.
            // If nothing is selected the user typed a note intended for the new record — keep it.
            string pendingNote = _displayedNoteLoad == null
                ? txtNote.Text.Replace("\r\n", " ").Replace("\n", " ").Trim()
                : "";
            FlushCurrentNote(); // save edits for the currently selected load first
            LogCapture(netKg, cmbCrop.Text, bushels, pendingNote);
            // Prevent auto-weigh from also firing for this truck
            _state = ScaleState.Captured;
            _stableStart = null;
            lblWeightValue.BackColor = Color.LightGreen;
            _signalTimer.Interval = _signalDurationSeconds * 1000;
            _signalTimer.Start();
            SendRelay(true);
            RefreshTodayWeights(); // selects new row → SelectionChanged sets _displayedNoteLoad correctly

            if (ckSplitMode.Checked)
            {
                _splitGrossSections.Clear();
                _splitTruckSections.Clear();
                _splitNoteActive = false;
                btnDeleteGrossSplit.Enabled = false;
                btnDeleteTruckSplit.Enabled = false;
                numericUpDown1.Value = 0;
                numericUpDown2.Value = 0;
                // Load the note for the newly logged row (contains the split breakdown)
                if (dgvTodayWeights.SelectedRows.Count > 0)
                {
                    string newLoad = dgvTodayWeights.SelectedRows[0].Cells[1].Value?.ToString() ?? "";
                    _displayedNoteLoad = newLoad;
                    txtNote.Text = ExpandNoteForDisplay(_notesByLoad.TryGetValue(newLoad, out string n) ? n : "");
                }
                UpdateManualControls();
            }
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
            btnLoadReport_Click(this, EventArgs.Empty);
        }

        private void btnPrintReceipt_Click(object sender, EventArgs e)
        {
            FlushCurrentNote();
            if (dgvTodayWeights.SelectedRows.Count == 0)
            {
                MessageBox.Show("Select a load to print.", "Nothing Selected",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var row = dgvTodayWeights.SelectedRows[0];
            string load = row.Cells[1].Value?.ToString() ?? "";
            string crop = row.Cells[2].Value?.ToString() ?? "";
            string bushels = row.Cells[4].Value?.ToString() ?? "---";

            float netKg = 0f;
            string dateStr = DateTime.Today.ToString("MMMM d, yyyy");
            string timeStr = row.Cells[0].Value?.ToString() ?? "";

            // Read raw values directly from CSV to avoid formatted-string parse issues
            try
            {
                string path = GetCsvPath(DateTime.Today);
                foreach (string line in File.ReadAllLines(path))
                {
                    string[] p = line.Split(',');
                    if (p.Length >= 4 && p[1] == load && !line.StartsWith("DateTime"))
                    {
                        float.TryParse(p[3], NumberStyles.Float,
                            CultureInfo.InvariantCulture, out netKg);
                        if (p[0].Length >= 19 &&
                            DateTime.TryParseExact(p[0].Substring(11, 8), "HH:mm:ss",
                                CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime t))
                            timeStr = t.ToString("h:mm tt");
                        break;
                    }
                }
            }
            catch { }

            float grossKg = netKg + _emptyWeightKg;

            var receipt = new ReceiptData
            {
                Date = dateStr,
                Time = timeStr,
                Load = load,
                Crop = crop,
                GrossKg = grossKg,
                TruckKg = _emptyWeightKg,
                NetKg = netKg,
                Bushels = bushels,
                Note = ExpandNoteForDisplay(_notesByLoad.TryGetValue(load, out string rn) ? rn : ""),
            };

            PrintDocument pd = new PrintDocument();
            pd.PrintPage += (s, pe) => PrintReceipt(pe, receipt);

            PrintDialog dlg = new PrintDialog { Document = pd };
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                try { pd.Print(); }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        "Print failed. If printing to a file, close it in any viewer and try again.\n\n" + ex.Message,
                        "Print Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }

        private struct ReceiptData
        {
            public string Date, Time, Load, Crop, Bushels, Note;
            public float GrossKg, TruckKg, NetKg;
        }

        private void PrintReceipt(PrintPageEventArgs e, ReceiptData r)
        {
            Graphics g = e.Graphics;
            float x = e.MarginBounds.Left;
            float y = e.MarginBounds.Top;
            float pageW = e.MarginBounds.Width;
            float labelCol = x;
            float valueCol = x + 120;

            Font titleFont = new Font("Arial", 16, FontStyle.Bold);
            Font headFont = new Font("Arial", 10, FontStyle.Bold);
            Font bodyFont = new Font("Arial", 10);
            Font bigFont = new Font("Arial", 14, FontStyle.Bold);
            Pen divPen = new Pen(Color.Black, 1);

            // Title
            string title = "SCALE RECEIPT";
            SizeF ts = g.MeasureString(title, titleFont);
            g.DrawString(title, titleFont, Brushes.Black, x + (pageW - ts.Width) / 2, y);
            y += ts.Height + 6;

            g.DrawLine(divPen, x, y, x + pageW, y);
            y += 10;

            float rightEdge = x + pageW;
            StringFormat rightFmt = new StringFormat { Alignment = StringAlignment.Far };

            void Row(string label, string value, Font vFont = null, bool rightAlign = false)
            {
                g.DrawString(label, headFont, Brushes.Black, labelCol, y);
                Font vf = vFont ?? bodyFont;
                if (rightAlign)
                    g.DrawString(value, vf, Brushes.Black,
                        new RectangleF(valueCol, y, rightEdge - valueCol, vf.GetHeight(g) + 4), rightFmt);
                else
                    g.DrawString(value, vf, Brushes.Black, valueCol, y);
                y += headFont.GetHeight(g) + 4;
            }

            Row("Date:", r.Date);
            Row("Time:", r.Time);
            Row("Load #:", r.Load);
            Row("Crop:", string.IsNullOrEmpty(r.Crop) ? "---" : r.Crop);
            y += 6;

            g.DrawLine(divPen, x, y, x + pageW, y);
            y += 10;

            // Weights
            string weightUnit = _manualUnits;
            Row("Gross:", FormatKg(r.GrossKg) + " " + weightUnit, bigFont, rightAlign: true);
            Row("Truck:", FormatKg(r.TruckKg) + " " + weightUnit, bigFont, rightAlign: true);
            y += 4;
            g.DrawLine(divPen, x, y, x + pageW, y);
            y += 6;
            Row("Net:", FormatKg(r.NetKg) + " " + weightUnit, bigFont, rightAlign: true);
            y += 6;

            g.DrawLine(divPen, x, y, x + pageW, y);
            y += 10;

            Row("Bushels:", r.Bushels + " bu", bigFont, rightAlign: true);

            if (!string.IsNullOrWhiteSpace(r.Note))
            {
                y += 6;
                g.DrawLine(divPen, x, y, x + pageW, y);
                y += 10;
                g.DrawString("Note:", headFont, Brushes.Black, labelCol, y);
                y += headFont.GetHeight(g) + 2;
                g.DrawString(r.Note, bodyFont, Brushes.Black,
                    new RectangleF(x, y, pageW, e.MarginBounds.Bottom - y));
            }

            titleFont.Dispose(); headFont.Dispose(); bodyFont.Dispose();
            bigFont.Dispose(); divPen.Dispose(); rightFmt.Dispose();
            e.HasMorePages = false;
        }

        private float KgToDisplay(float kg) =>
            _manualUnits == "lb" ? kg * 2.20462f : kg;

        private string FormatKg(float kg) =>
            KgToDisplay(kg).ToString("N0", CultureInfo.CurrentCulture);

        private string FormatKgStr(string raw) =>
            float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out float v)
                ? FormatKg(v) : raw;

        private void SendRelay(bool on) => SendCommand(on ? "RELAY:1" : "RELAY:0");

        private void SendCommand(string cmd)
        {
            if (CommPort != null && CommPort.IsOpen)
            {
                CommPort.WriteLine(cmd);
            }
            else if (_connectionMode == ConnectionMode.Udp)
            {
                try
                {
                    byte[] bytes = Encoding.ASCII.GetBytes(cmd);
                    _udp.Send(bytes, bytes.Length, new IPEndPoint(IPAddress.Broadcast, 5005));
                }
                catch { }
            }
        }

        private void btnSendCommand_Click(object sender, EventArgs e)
        {
            string cmd = txtCommand.Text.Trim();
            if (cmd.Length == 0) return;

            if (CommPort != null && CommPort.IsOpen)
            {
                AppendSerialOutput("> " + cmd + " [serial]");
                SendCommand(cmd);
            }
            else if (_connectionMode == ConnectionMode.Udp)
            {
                AppendSerialOutput("> " + cmd + " [UDP]");
                SendCommand(cmd);
            }
            else
            {
                AppendSerialOutput("Not connected");
            }
        }

        private void txtCommand_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                btnSendCommand_Click(sender, e);
                e.SuppressKeyPress = true;
            }
        }

        private void ResetStateMachine()
        {
            _state = ScaleState.Empty;
            _stableStart = null;
            _signalTimer.Stop();
            SendRelay(false);
        }

        // ── Split weighing ───────────────────────────────────────────────

        // Restores line break between Gross and Truck split lines collapsed by CSV storage.
        private static string ExpandNoteForDisplay(string note)
        {
            if (note.StartsWith("Gross:") && note.Contains(" Truck:"))
                note = note.Replace(" Truck:", "\r\nTruck:");
            return note;
        }

        private static float SumList(List<float> list)
        {
            float total = 0f;
            foreach (float v in list) total += v;
            return total;
        }

        private void UpdateSplitNote()
        {
            // Separate any existing user text from the auto-generated split lines
            string[] lines = txtNote.Text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            var userLines = new List<string>();
            foreach (string line in lines)
            {
                string t = line.TrimStart();
                if (!t.StartsWith("Gross:") && !t.StartsWith("Truck:"))
                    userLines.Add(line);
            }
            while (userLines.Count > 0 && string.IsNullOrWhiteSpace(userLines[0]))
                userLines.RemoveAt(0);
            while (userLines.Count > 0 && string.IsNullOrWhiteSpace(userLines[userLines.Count - 1]))
                userLines.RemoveAt(userLines.Count - 1);

            var sb = new StringBuilder();

            if (_splitGrossSections.Count > 0)
            {
                sb.Append("Gross:");
                for (int i = 0; i < _splitGrossSections.Count; i++)
                {
                    sb.Append(i == 0 ? "  " : " / ");
                    sb.Append(FormatKg(_splitGrossSections[i]));
                }
                if (_splitGrossSections.Count > 1)
                    sb.Append("  Total: " + FormatKg(SumList(_splitGrossSections)));
                sb.Append("\r\n");
            }

            if (_splitTruckSections.Count > 0)
            {
                sb.Append("Truck:");
                for (int i = 0; i < _splitTruckSections.Count; i++)
                {
                    sb.Append(i == 0 ? "  " : " / ");
                    sb.Append(FormatKg(_splitTruckSections[i]));
                }
                if (_splitTruckSections.Count > 1)
                    sb.Append("  Total: " + FormatKg(SumList(_splitTruckSections)));
                sb.Append("\r\n");
            }

            if (userLines.Count > 0)
            {
                if (sb.Length > 0) sb.Append("\r\n");
                sb.Append(string.Join("\r\n", userLines));
            }

            _splitNoteActive = _splitGrossSections.Count > 0 || _splitTruckSections.Count > 0;
            txtNote.Text = sb.ToString().TrimEnd('\r', '\n');
            _displayedNoteLoad = null;
        }

        private void ckSplitMode_CheckedChanged(object sender, EventArgs e)
        {
            if (ckSplitMode.Checked)
            {
                if (chkAutoWeigh.Checked)
                    chkAutoWeigh.Checked = false;
                chkAutoWeigh.Enabled = false;
                _splitGrossSections.Clear();
                _splitTruckSections.Clear();
                _splitNoteActive = false;
                btnDeleteGrossSplit.Enabled = false;
                btnDeleteTruckSplit.Enabled = false;
                _grossWeightKg = 0f;
                SetGrossWeightDisplay(0f);
                _emptyWeightKg = 0f;
                SetEmptyWeightDisplay(0f);
                UpdateNet();
                UpdateBushels();
            }
            else
            {
                _splitGrossSections.Clear();
                _splitTruckSections.Clear();
                UpdateSplitNote(); // strips split lines, keeps user text
                btnDeleteGrossSplit.Enabled = false;
                btnDeleteTruckSplit.Enabled = false;
                numericUpDown1.Value = 0;
                numericUpDown2.Value = 0;
                chkAutoWeigh.Enabled = true;
            }
            UpdateManualControls();
        }

        private void btnDeleteGrossSplit_Click(object sender, EventArgs e)
        {
            if (_splitGrossSections.Count == 0) return;
            _splitGrossSections.RemoveAt(_splitGrossSections.Count - 1);
            float totalKg = SumList(_splitGrossSections);
            _grossWeightKg = totalKg;
            SetGrossWeightDisplay(totalKg);
            UpdateNet();
            UpdateBushels();
            UpdateSplitNote();
            btnDeleteGrossSplit.Enabled = _splitGrossSections.Count > 0;
            UpdateManualControls();
        }

        private void btnDeleteTruckSplit_Click(object sender, EventArgs e)
        {
            if (_splitTruckSections.Count == 0) return;
            _splitTruckSections.RemoveAt(_splitTruckSections.Count - 1);
            float totalKg = SumList(_splitTruckSections);
            _emptyWeightKg = totalKg;
            SetEmptyWeightDisplay(totalKg);
            UpdateNet();
            UpdateBushels();
            UpdateSplitNote();
            btnDeleteTruckSplit.Enabled = _splitTruckSections.Count > 0;
            UpdateManualControls();
        }

        private void SplitEntry_Enter(object sender, EventArgs e)
        {
            NumericUpDown nud = (NumericUpDown)sender;
            nud.BeginInvoke(new Action(() => nud.Select(0, nud.Text.Length)));
        }

        private void NumWeight_Enter(object sender, EventArgs e)
        {
            NumericUpDown nud = (NumericUpDown)sender;
            nud.BeginInvoke(new Action(() => nud.Select(0, nud.Text.Length)));
        }

        private void btnAddGrossSplit_Click(object sender, EventArgs e)
        {
            if (_splitGrossSections.Count >= SplitMaxSections) return;
            float sectionDisplay = (float)numericUpDown1.Value;
            if (sectionDisplay <= 0) return;
            float sectionKg = _currentUnits == "lb" ? sectionDisplay / 2.20462f : sectionDisplay;
            _splitGrossSections.Add(sectionKg);
            float totalKg = SumList(_splitGrossSections);
            _grossWeightKg = totalKg;
            SetGrossWeightDisplay(totalKg);
            UpdateNet();
            UpdateBushels();
            UpdateSplitNote();
            numericUpDown1.Value = 0;
            btnDeleteGrossSplit.Enabled = true;
            UpdateManualControls();
        }

        private void btnAddTruckSplit_Click(object sender, EventArgs e)
        {
            if (_splitTruckSections.Count >= SplitMaxSections) return;
            float sectionDisplay = (float)numericUpDown2.Value;
            if (sectionDisplay <= 0) return;
            float sectionKg = _currentUnits == "lb" ? sectionDisplay / 2.20462f : sectionDisplay;
            _splitTruckSections.Add(sectionKg);
            float totalKg = SumList(_splitTruckSections);
            _emptyWeightKg = totalKg;
            SetEmptyWeightDisplay(totalKg);
            UpdateNet();
            UpdateBushels();
            UpdateSplitNote();
            numericUpDown2.Value = 0;
            btnDeleteTruckSplit.Enabled = true;
            UpdateManualControls();
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

                _loadNumber = 0;
                foreach (string line in File.ReadAllLines(path))
                {
                    if (line.StartsWith("DateTime")) continue;
                    string[] p = line.Split(',');
                    if (p.Length >= 2 && int.TryParse(p[1], out int n))
                        _loadNumber = Math.Max(_loadNumber, n);
                }
            }
            catch { _loadNumber = 0; }
        }

        // Weight is always saved in kg. CSV format: DateTime,Load,Crop,WeightKg,Bushels,Note
        private int LogCapture(float weightKg, string crop, float bushels, string note = "")
        {
            try
            {
                Directory.CreateDirectory(CsvDirectory);
                string path = GetCsvPath(DateTime.Today);

                if (!File.Exists(path))
                {
                    _loadNumber = 0;
                    File.AppendAllText(path, "DateTime,Load,Crop,WeightKg,Bushels,Note\r\n");
                }

                _loadNumber++;
                string safeNote = note.Replace("\r\n", " ").Replace("\n", " ").Trim();

                File.AppendAllText(path, string.Format(CultureInfo.InvariantCulture,
                    "{0},{1},{2},{3:F1},{4:F1},{5}\r\n",
                    DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    _loadNumber, crop, weightKg, bushels, safeNote));
            }
            catch { }

            return _loadNumber;
        }

        // ── Auto-weigh control events ────────────────────────────────────

        private void UpdateManualControls()
        {
            bool hasLiveWeight = (CommPort != null && CommPort.IsOpen) || _connectionMode == ConnectionMode.Udp;
            bool autoWeigh = chkAutoWeigh.Checked;
            bool splitMode = ckSplitMode.Checked;
            btnCaptureGross.Enabled = hasLiveWeight && !autoWeigh
                && (!splitMode || _splitGrossSections.Count < SplitMaxSections);
            btnCaptureTruck.Enabled = hasLiveWeight && !autoWeigh
                && (!splitMode || _splitTruckSections.Count < SplitMaxSections);
            numGrossWeight.Enabled = !autoWeigh && !splitMode;
            numEmptyWeight.Enabled = !splitMode;
            btnManualWeigh.Enabled = !autoWeigh;
            grpSplit.Enabled = splitMode;
            btnAddGrossSplit.Enabled = _splitGrossSections.Count < SplitMaxSections;
            btnAddTruckSplit.Enabled = _splitTruckSections.Count < SplitMaxSections;
        }

        private void chkAutoWeigh_CheckedChanged(object sender, EventArgs e)
        {
            _autoWeighEnabled = chkAutoWeigh.Checked;
            numMinWeight.Enabled = chkAutoWeigh.Checked;
            numStability.Enabled = chkAutoWeigh.Checked;
            numSignal.Enabled = chkAutoWeigh.Checked;
            numInterval.Enabled = chkAutoWeigh.Checked;
            numTolerance.Enabled = chkAutoWeigh.Checked;

            if (!chkAutoWeigh.Checked)
            {
                ResetStateMachine();
                lblWeightValue.BackColor = SystemColors.Control;
            }
            UpdateManualControls();
            SaveAutoWeighSettings();
        }

        private void rdoUnits_CheckedChanged(object sender, EventArgs e)
        {
            if (!rdoImperial.Checked && !rdoMetric.Checked) return;
            _manualUnits = rdoMetric.Checked ? "kg" : "lb";
            _currentUnits = _manualUnits;
            ApplyUnits(_manualUnits);
            RefreshTodayWeights();
            SaveAutoWeighSettings();
        }

        private void numMinWeight_ValueChanged(object sender, EventArgs e)
        {
            if (_suppressMinWeightSave) return;
            float displayVal = (float)numMinWeight.Value;
            _minWeightLbs = (int)Math.Round(_currentUnits == "kg" ? displayVal * 2.20462f : displayVal);
            SaveAutoWeighSettings();
        }

        private void numStability_ValueChanged(object sender, EventArgs e)
        {
            _stabilitySeconds = (int)numStability.Value;
            SaveAutoWeighSettings();
        }

        private void numSignal_ValueChanged(object sender, EventArgs e)
        {
            _signalDurationSeconds = (int)numSignal.Value;
            SaveAutoWeighSettings();
        }

        private void numInterval_ValueChanged(object sender, EventArgs e)
        {
            _minIntervalSeconds = (int)numInterval.Value;
            SaveAutoWeighSettings();
        }

        private void numTolerance_ValueChanged(object sender, EventArgs e)
        {
            float displayVal = (float)numTolerance.Value;
            _stabilityToleranceLbs = (int)Math.Round(_currentUnits == "kg" ? displayVal * 2.20462f : displayVal);
            SaveAutoWeighSettings();
        }

        private void SaveAutoWeighSettings()
        {
            if (!_settingsLoaded) return;
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(AutoWeighSettingsFile));
                File.WriteAllText(AutoWeighSettingsFile, string.Format(
                    CultureInfo.InvariantCulture, "{0},{1},{2},{3},{4},{5},{6}",
                    chkAutoWeigh.Checked ? 1 : 0,
                    numMinWeight.Value,
                    numStability.Value,
                    numSignal.Value,
                    _manualUnits,
                    numInterval.Value,
                    numTolerance.Value));
            }
            catch { }
        }

        private void LoadAutoWeighSettings()
        {
            try
            {
                if (!File.Exists(AutoWeighSettingsFile)) return;
                string[] parts = File.ReadAllText(AutoWeighSettingsFile).Split(',');
                if (parts.Length < 4) return;

                // Set units first so ValueChanged handlers convert correctly when controls are loaded
                if (parts.Length >= 5 && (parts[4] == "lb" || parts[4] == "kg"))
                {
                    _manualUnits = parts[4];
                    _currentUnits = _manualUnits;
                    rdoMetric.Checked = _manualUnits == "kg";
                    rdoImperial.Checked = _manualUnits == "lb";
                }

                if (int.TryParse(parts[0], out int en))
                    chkAutoWeigh.Checked = en == 1;
                if (decimal.TryParse(parts[1], NumberStyles.Any, CultureInfo.InvariantCulture, out decimal minW))
                    numMinWeight.Value = Math.Max(numMinWeight.Minimum, Math.Min(numMinWeight.Maximum, minW));
                if (decimal.TryParse(parts[2], NumberStyles.Any, CultureInfo.InvariantCulture, out decimal stab))
                    numStability.Value = Math.Max(numStability.Minimum, Math.Min(numStability.Maximum, stab));
                if (decimal.TryParse(parts[3], NumberStyles.Any, CultureInfo.InvariantCulture, out decimal sig))
                    numSignal.Value = Math.Max(numSignal.Minimum, Math.Min(numSignal.Maximum, sig));
                if (parts.Length >= 6 && decimal.TryParse(parts[5], NumberStyles.Any, CultureInfo.InvariantCulture, out decimal interval))
                    numInterval.Value = Math.Max(numInterval.Minimum, Math.Min(numInterval.Maximum, interval));
                if (parts.Length >= 7 && decimal.TryParse(parts[6], NumberStyles.Any, CultureInfo.InvariantCulture, out decimal tol))
                    numTolerance.Value = Math.Max(numTolerance.Minimum, Math.Min(numTolerance.Maximum, tol));
            }
            catch { }
        }

        // ── Report tab ───────────────────────────────────────────────────

        private void tabControl_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (tabControl.SelectedTab == tabReport)
                btnLoadReport_Click(this, EventArgs.Empty);
        }

        private void btnLoadReport_Click(object sender, EventArgs e)
        {
            dgvReport.Rows.Clear();
            _reportNotesByLoad.Clear();
            _reportDisplayedLoad = null;
            textBox1.Text = "";
            lbTotalWeight.Text = "0";
            lbTotalBushels.Text = "0";
            string path = GetCsvPath(dtpReport.Value.Date);

            if (!File.Exists(path))
            {
                // Suppress the message box when called automatically on startup
                if (sender != this)
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
                    string load = parts[1];
                    string crop, weight, bushels, note;

                    if (parts.Length >= 6 && IsUnitsField(parts[4]))
                    {
                        // old 6-col (Units column is "lb" or "kg")
                        crop = parts[2];
                        weight = parts[3];
                        bushels = parts[5];
                        note = "";
                    }
                    else if (parts.Length >= 5)
                    {
                        // current: DateTime,Load,Crop,WeightKg,Bushels[,Note...]
                        crop = parts[2];
                        weight = parts[3];
                        bushels = parts[4];
                        note = parts.Length >= 6
                            ? string.Join(",", parts, 5, parts.Length - 5)
                            : "";
                    }
                    else
                    {
                        // 4-col legacy
                        crop = "";
                        weight = parts[2];
                        bushels = "";
                        note = "";
                    }

                    _reportNotesByLoad[load] = note;
                    string dtDisplay = parts[0];
                    if (DateTime.TryParse(parts[0], out DateTime dt))
                        dtDisplay = dt.ToString("yyyy-MM-dd h:mm tt");
                    dgvReport.Rows.Add(dtDisplay, load, crop, FormatKgStr(weight), FormatBushels(bushels));
                }
            }

            UpdateReportTotals();
        }

        private void dgvReport_SelectionChanged(object sender, EventArgs e)
        {
            // Flush edits for the previously selected row
            if (_reportDisplayedLoad != null)
            {
                string edited = textBox1.Text.Replace("\r\n", " ").Replace("\n", " ").Trim();
                if (!_reportNotesByLoad.TryGetValue(_reportDisplayedLoad, out string stored)
                    || edited != stored)
                {
                    _reportNotesByLoad[_reportDisplayedLoad] = edited;
                    SaveNoteToCSV(_reportDisplayedLoad, edited, dtpReport.Value.Date);
                }
            }

            if (dgvReport.SelectedRows.Count == 0) { textBox1.Text = ""; _reportDisplayedLoad = null; return; }
            string load = dgvReport.SelectedRows[0].Cells[1].Value?.ToString() ?? "";
            _reportDisplayedLoad = load;
            textBox1.Text = ExpandNoteForDisplay(_reportNotesByLoad.TryGetValue(load, out string note) ? note : "");
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
                {
                    string load = row.Cells[1].Value?.ToString() ?? "";
                    string note = _reportNotesByLoad.TryGetValue(load, out string n) ? n : "";

                    string dt = row.Cells[0].Value?.ToString() ?? "";
                    _printRows.Add(new string[] {
                        dt,
                        load,
                        row.Cells[2].Value?.ToString() ?? "",
                        row.Cells[3].Value?.ToString() ?? "",
                        row.Cells[4].Value?.ToString() ?? "",
                        note
                    });
                }
            }
            _printDate = dtpReport.Value.Date;
            _printRowIndex = 0;
            _printTotalWeight = lbTotalWeight.Text;
            _printTotalBushels = lbTotalBushels.Text;

            PrintDocument pd = new PrintDocument();
            pd.PrintPage += PrintPage;

            PrintDialog dlg = new PrintDialog { Document = pd };
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                try { pd.Print(); }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        "Print failed. If printing to a file, close it in any viewer and try again.\n\n" + ex.Message,
                        "Print Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }

        private void PrintPage(object sender, PrintPageEventArgs e)
        {
            Font titleFont = new Font("Arial", 13, FontStyle.Bold);
            Font headerFont = new Font("Arial", 9, FontStyle.Bold);
            Font dataFont = new Font("Arial", 9);
            Font totalsFont = new Font("Arial", 10, FontStyle.Bold);
            float lineH = dataFont.GetHeight(e.Graphics) + 3;
            float x = e.MarginBounds.Left;
            float y = e.MarginBounds.Top;

            if (_printRowIndex == 0)
            {
                string title = "Scale Report  —  " + _printDate.ToString("MMMM d, yyyy");
                e.Graphics.DrawString(title, titleFont, Brushes.Black, x, y);
                y += titleFont.GetHeight(e.Graphics) + 8;

                DrawPrintRow(e.Graphics, headerFont, x, y, e.MarginBounds.Right,
                    "Date / Time", "Load #", "Crop", "Weight (" + _manualUnits + ")", "Bushels", "Note");
                y += lineH;
                e.Graphics.DrawLine(Pens.Black, x, y, e.MarginBounds.Right, y);
                y += 4;
            }

            while (_printRowIndex < _printRows.Count)
            {
                string[] row = _printRows[_printRowIndex];
                DrawPrintRow(e.Graphics, dataFont, x, y, e.MarginBounds.Right,
                    row[0], row[1], row[2], row[3], row[4], row[5]);
                y += lineH;
                _printRowIndex++;

                if (y + lineH > e.MarginBounds.Bottom)
                {
                    e.HasMorePages = true;
                    totalsFont.Dispose();
                    return;
                }
            }

            y += 6;
            e.Graphics.DrawLine(Pens.Black, x, y, e.MarginBounds.Right, y);
            y += 6;

            StringFormat right = new StringFormat { Alignment = StringAlignment.Far };
            e.Graphics.DrawString("Totals", totalsFont, Brushes.Black, x + 183, y);
            e.Graphics.DrawString(_printTotalWeight, totalsFont, Brushes.Black,
                new RectangleF(x + 258, y, 85, totalsFont.GetHeight(e.Graphics) + 2), right);
            e.Graphics.DrawString(_printTotalBushels, totalsFont, Brushes.Black,
                new RectangleF(x + 348, y, 60, totalsFont.GetHeight(e.Graphics) + 2), right);
            right.Dispose();

            e.HasMorePages = false;
            totalsFont.Dispose();
        }

        private void DrawPrintRow(Graphics g, Font font, float x, float y, float pageRight,
            string dateTime, string load, string crop, string weight, string bushels, string note)
        {
            StringFormat right = new StringFormat { Alignment = StringAlignment.Far };
            float rowH = font.GetHeight(g) + 2;

            g.DrawString(dateTime, font, Brushes.Black, x, y);
            g.DrawString(load, font, Brushes.Black, x + 130, y);
            g.DrawString(crop, font, Brushes.Black, x + 183, y);
            g.DrawString(weight, font, Brushes.Black, new RectangleF(x + 258, y, 85, rowH), right);
            g.DrawString(bushels, font, Brushes.Black, new RectangleF(x + 348, y, 60, rowH), right);
            if (!string.IsNullOrEmpty(note))
                g.DrawString(note, font, Brushes.Black,
                    new RectangleF(x + 413, y, pageRight - x - 413, rowH));

            right.Dispose();
        }

        // ── Position persistence ─────────────────────────────────────────


        protected override void OnFormClosing(FormClosingEventArgs e)
        {

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(PositionFile));
                File.WriteAllText(PositionFile, string.Format("{0},{1}", Left, Top));
            }
            catch { }
            base.OnFormClosing(e);
        }

        private void StartUdpReceiver()
        {
            try
            {
                _udp = new UdpClient(5005);
                _udp.EnableBroadcast = true;
                _udpRunning = true;

                _udpThread = new Thread(UdpListenLoop);
                _udpThread.IsBackground = true;
                _udpThread.Start();

                _udpWatchdog = new System.Windows.Forms.Timer { Interval = 1000 };
                _udpWatchdog.Tick += OnUdpWatchdogTick;
                _udpWatchdog.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not start UDP listener: " + ex.Message);
            }
        }

        private void OnUdpWatchdogTick(object sender, EventArgs e)
        {
            if (_connectionMode != ConnectionMode.Udp) return;
            if ((DateTime.Now - _lastUdpReceived).TotalSeconds < 2) return;

            _currentWeight = 0f;
            _connectionMode = ConnectionMode.None;
            lblConnectionStatus.Text = "No connection";
            SetWeightDisplay("0 " + _currentUnits);
            lblWeightValue.BackColor = SystemColors.Control;
            ResetStateMachine();
            UpdateManualControls();
        }

        private void UdpListenLoop()
        {
            while (_udpRunning)
            {
                try
                {
                    byte[] data = _udp.Receive(ref _udpEP);
                    string line = Encoding.ASCII.GetString(data).Trim();

                    // Example: WT,1.234V,5.678mA,12345.6lb
                    ProcessUdpSentence(line);
                }
                catch
                {
                    // ignore socket errors during shutdown
                }
            }
        }

        private void ProcessUdpSentence(string line)
        {
            if (_connectionMode == ConnectionMode.Serial) return;
            if (!ParseWtSentence(line, out float weightVal, out string scaleUnits)) return;

            bool justActivated = _connectionMode != ConnectionMode.Udp;
            _connectionMode = ConnectionMode.Udp;
            _lastUdpReceived = DateTime.Now;

            Action extra = justActivated ? () =>
            {
                lblConnectionStatus.Text = "UDP active";
                UpdateManualControls();
            }
            : (Action)null;

            ApplyWeightReading(weightVal, scaleUnits, extra);
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            _appClosing = true;
            _udpWatchdog?.Stop();
            _udpWatchdog?.Dispose();
            _udpRunning = false;
            try { _udp?.Close(); } catch { }
            if (_bigDisplay != null && !_bigDisplay.IsDisposed)
                _bigDisplay.Close();
        }

        public void DrawGroupBox(GroupBox box, Graphics g, Color BackColor, Color textColor, Color borderColor, float borderWidth = 1)
        {
            // useage:
            // point the Groupbox paint event to this sub:
            // private void groupBox1_Paint(object sender, PaintEventArgs e)
            //{
            //    GroupBox box = sender as GroupBox;
            // mf.Tls.DrawGroupBox(box, e.Graphics, this.BackColor, Color.Black, Color.Red, 3); // Red border with thickness 3
            //}
            if (box != null)
            {
                using (Brush textBrush = new SolidBrush(textColor))
                using (Pen borderPen = new Pen(borderColor, borderWidth))
                {
                    SizeF strSize = g.MeasureString(box.Text, box.Font);
                    Rectangle rect = new Rectangle(box.ClientRectangle.X,
                                                   box.ClientRectangle.Y + (int)(strSize.Height / 2),
                                                   box.ClientRectangle.Width - 1,
                                                   box.ClientRectangle.Height - (int)(strSize.Height / 2) - 1);

                    // Clear text and border
                    g.Clear(BackColor);

                    // Draw text
                    g.DrawString(box.Text, box.Font, textBrush, box.Padding.Left, 0);

                    // Drawing Border
                    // Left
                    g.DrawLine(borderPen, rect.Location, new Point(rect.X, rect.Y + rect.Height));
                    // Right
                    g.DrawLine(borderPen, new Point(rect.X + rect.Width, rect.Y), new Point(rect.X + rect.Width, rect.Y + rect.Height));
                    // Bottom
                    g.DrawLine(borderPen, new Point(rect.X, rect.Y + rect.Height), new Point(rect.X + rect.Width, rect.Y + rect.Height));
                    // Top1
                    g.DrawLine(borderPen, new Point(rect.X, rect.Y), new Point(rect.X + box.Padding.Left, rect.Y));
                    // Top2
                    g.DrawLine(borderPen, new Point(rect.X + box.Padding.Left + (int)(strSize.Width), rect.Y), new Point(rect.X + rect.Width, rect.Y));
                }
            }
        }

        private void gbData_Paint(object sender, PaintEventArgs e)
        {
            DrawGroupBox((GroupBox)sender, e.Graphics, this.BackColor, Color.Black, Color.Blue);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            ckLP7515.Checked = Properties.Settings.Default.LP7515;
            ckLargeDisplay.Checked = Properties.Settings.Default.LargeDisplay;
            LoadEmptyWeight();
            LoadCropSettings();
            LoadAutoWeighSettings();
            _settingsLoaded = true;
            _lastDisplayUnits = "";   // force ApplyUnits to run regardless of default
            ApplyUnits(_manualUnits);
            UpdateManualControls();
            // ApplyUnits guards against re-entry; set report header explicitly in case it was skipped
            dgvReport.Columns[3].HeaderText = "Weight (" + _manualUnits + ")";
            dgvTodayWeights.Columns[3].HeaderText = "Weight (" + _manualUnits + ")";

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
                string savedPort = Properties.Settings.Default.SerialPort;
                if (cmbPort.Items.Contains(savedPort))
                {
                    cmbPort.SelectedItem = savedPort;
                }

                string SavedBaud = Properties.Settings.Default.Baud.ToString();
                if (cmbBaud.Items.Contains(SavedBaud))
                {
                    cmbBaud.SelectedItem = SavedBaud;
                }
                btnConnect_Click(this, EventArgs.Empty);
            }
            catch { }

            // Pre-populate report with today's loads
            btnLoadReport_Click(this, EventArgs.Empty);
        }

        private void ckLP7515_CheckedChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.LP7515 = ckLP7515.Checked;
            Properties.Settings.Default.Save();

            txtCommand.Enabled = !ckLP7515.Checked;
            btnSendCommand.Enabled = !ckLP7515.Checked;
        }

        // ── Large display ────────────────────────────────────────────────

        private void SetWeightDisplay(string text)
        {
            lblWeightValue.Text = text;
            if (_bigDisplay != null && !_bigDisplay.IsDisposed)
                _bigDisplay.UpdateWeight(text);
        }

        private void ckLargeDisplay_CheckedChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.LargeDisplay = ckLargeDisplay.Checked;
            Properties.Settings.Default.Save();

            if (ckLargeDisplay.Checked)
            {
                if (_bigDisplay == null || _bigDisplay.IsDisposed)
                {
                    _bigDisplay = new BigDisplayForm { Owner = this };
                    _bigDisplay.FormClosed += (s, ea) =>
                    {
                        _bigDisplay = null;
                        if (!_appClosing && ckLargeDisplay.Checked)
                            ckLargeDisplay.Checked = false;
                    };
                    _bigDisplay.Show(this);
                    _bigDisplay.UpdateWeight(lblWeightValue.Text);
                }
                SetWeightDisplay("32,563");
            }
            else
            {
                if (_bigDisplay != null && !_bigDisplay.IsDisposed)
                    _bigDisplay.Close();
            }
        }
    }
}
