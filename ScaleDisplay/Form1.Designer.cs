using System.Drawing;
using System.Windows.Forms;

namespace ScaleDisplay
{
    partial class Form1
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            this.toolTip = new System.Windows.Forms.ToolTip(this.components);
            this.lblStability = new System.Windows.Forms.Label();
            this.numStability = new System.Windows.Forms.NumericUpDown();
            this.lblSignal = new System.Windows.Forms.Label();
            this.numSignal = new System.Windows.Forms.NumericUpDown();
            this.lblInterval = new System.Windows.Forms.Label();
            this.numInterval = new System.Windows.Forms.NumericUpDown();
            this.lblTolerance = new System.Windows.Forms.Label();
            this.numTolerance = new System.Windows.Forms.NumericUpDown();
            this.lblConnectionStatus = new System.Windows.Forms.Label();
            this.tabControl = new System.Windows.Forms.TabControl();
            this.tabWeight = new System.Windows.Forms.TabPage();
            this.lblWeightValue = new System.Windows.Forms.Label();
            this.btnManualWeigh = new System.Windows.Forms.Button();
            this.lblCrop = new System.Windows.Forms.Label();
            this.cmbCrop = new System.Windows.Forms.ComboBox();
            this.lblBushelWeightLabel = new System.Windows.Forms.Label();
            this.numBushelWeight = new System.Windows.Forms.NumericUpDown();
            this.lblEmptyLabel = new System.Windows.Forms.Label();
            this.numEmptyWeight = new System.Windows.Forms.NumericUpDown();
            this.btnCaptureTruck = new System.Windows.Forms.Button();
            this.lblGrossLabel = new System.Windows.Forms.Label();
            this.numGrossWeight = new System.Windows.Forms.NumericUpDown();
            this.btnCaptureGross = new System.Windows.Forms.Button();
            this.lblNetLabel = new System.Windows.Forms.Label();
            this.lblNetValue = new System.Windows.Forms.Label();
            this.lblBushels = new System.Windows.Forms.Label();
            this.lblBushelsValue = new System.Windows.Forms.Label();
            this.dgvTodayWeights = new System.Windows.Forms.DataGridView();
            this.lblNote = new System.Windows.Forms.Label();
            this.txtNote = new System.Windows.Forms.TextBox();
            this.btnDeleteWeight = new System.Windows.Forms.Button();
            this.btnPrintReceipt = new System.Windows.Forms.Button();
            this.tabSettings = new System.Windows.Forms.TabPage();
            this.lblPort = new System.Windows.Forms.Label();
            this.cmbPort = new System.Windows.Forms.ComboBox();
            this.btnRefresh = new System.Windows.Forms.Button();
            this.btnConnect = new System.Windows.Forms.Button();
            this.grpAutoWeigh = new System.Windows.Forms.GroupBox();
            this.chkAutoWeigh = new System.Windows.Forms.CheckBox();
            this.lblMinWeight = new System.Windows.Forms.Label();
            this.numMinWeight = new System.Windows.Forms.NumericUpDown();
            this.grpUnits = new System.Windows.Forms.GroupBox();
            this.rdoImperial = new System.Windows.Forms.RadioButton();
            this.rdoMetric = new System.Windows.Forms.RadioButton();
            this.tabReport = new System.Windows.Forms.TabPage();
            this.label1 = new System.Windows.Forms.Label();
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.lblDate = new System.Windows.Forms.Label();
            this.dtpReport = new System.Windows.Forms.DateTimePicker();
            this.btnLoadReport = new System.Windows.Forms.Button();
            this.dgvReport = new System.Windows.Forms.DataGridView();
            this.btnPrint = new System.Windows.Forms.Button();
            this.backgroundWorker1 = new System.ComponentModel.BackgroundWorker();
            this.gbData = new System.Windows.Forms.GroupBox();
            ((System.ComponentModel.ISupportInitialize)(this.numStability)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numSignal)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numInterval)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numTolerance)).BeginInit();
            this.tabControl.SuspendLayout();
            this.tabWeight.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numBushelWeight)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numEmptyWeight)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numGrossWeight)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvTodayWeights)).BeginInit();
            this.tabSettings.SuspendLayout();
            this.grpAutoWeigh.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numMinWeight)).BeginInit();
            this.grpUnits.SuspendLayout();
            this.tabReport.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvReport)).BeginInit();
            this.gbData.SuspendLayout();
            this.SuspendLayout();
            // 
            // lblStability
            // 
            this.lblStability.AutoSize = true;
            this.lblStability.Location = new System.Drawing.Point(10, 155);
            this.lblStability.Name = "lblStability";
            this.lblStability.Size = new System.Drawing.Size(82, 17);
            this.lblStability.TabIndex = 2;
            this.lblStability.Text = "Stability (s):";
            this.toolTip.SetToolTip(this.lblStability, "Seconds the weight must remain stable before a load is recorded");
            // 
            // numStability
            // 
            this.numStability.Enabled = false;
            this.numStability.Location = new System.Drawing.Point(130, 152);
            this.numStability.Maximum = new decimal(new int[] {
            60,
            0,
            0,
            0});
            this.numStability.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numStability.Name = "numStability";
            this.numStability.Size = new System.Drawing.Size(52, 23);
            this.numStability.TabIndex = 2;
            this.toolTip.SetToolTip(this.numStability, "Seconds the weight must remain stable before a load is recorded");
            this.numStability.Value = new decimal(new int[] {
            15,
            0,
            0,
            0});
            this.numStability.ValueChanged += new System.EventHandler(this.numStability_ValueChanged);
            // 
            // lblSignal
            // 
            this.lblSignal.AutoSize = true;
            this.lblSignal.Location = new System.Drawing.Point(10, 190);
            this.lblSignal.Name = "lblSignal";
            this.lblSignal.Size = new System.Drawing.Size(103, 17);
            this.lblSignal.TabIndex = 3;
            this.lblSignal.Text = "Notification (s):";
            this.toolTip.SetToolTip(this.lblSignal, "Seconds the output relay stays active after a load is captured");
            // 
            // numSignal
            // 
            this.numSignal.Enabled = false;
            this.numSignal.Location = new System.Drawing.Point(130, 187);
            this.numSignal.Maximum = new decimal(new int[] {
            60,
            0,
            0,
            0});
            this.numSignal.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numSignal.Name = "numSignal";
            this.numSignal.Size = new System.Drawing.Size(52, 23);
            this.numSignal.TabIndex = 3;
            this.toolTip.SetToolTip(this.numSignal, "Seconds the output relay stays active after a load is captured");
            this.numSignal.Value = new decimal(new int[] {
            5,
            0,
            0,
            0});
            this.numSignal.ValueChanged += new System.EventHandler(this.numSignal_ValueChanged);
            // 
            // lblInterval
            // 
            this.lblInterval.AutoSize = true;
            this.lblInterval.Location = new System.Drawing.Point(10, 85);
            this.lblInterval.Name = "lblInterval";
            this.lblInterval.Size = new System.Drawing.Size(105, 17);
            this.lblInterval.TabIndex = 5;
            this.lblInterval.Text = "Min Interval (s):";
            this.toolTip.SetToolTip(this.lblInterval, "Minimum seconds between captures — prevents double-recording a truck");
            // 
            // numInterval
            // 
            this.numInterval.Enabled = false;
            this.numInterval.Increment = new decimal(new int[] {
            30,
            0,
            0,
            0});
            this.numInterval.Location = new System.Drawing.Point(130, 82);
            this.numInterval.Maximum = new decimal(new int[] {
            3600,
            0,
            0,
            0});
            this.numInterval.Name = "numInterval";
            this.numInterval.Size = new System.Drawing.Size(65, 23);
            this.numInterval.TabIndex = 5;
            this.toolTip.SetToolTip(this.numInterval, "Minimum seconds between captures — prevents double-recording a truck");
            this.numInterval.ValueChanged += new System.EventHandler(this.numInterval_ValueChanged);
            // 
            // lblTolerance
            // 
            this.lblTolerance.AutoSize = true;
            this.lblTolerance.Location = new System.Drawing.Point(10, 120);
            this.lblTolerance.Name = "lblTolerance";
            this.lblTolerance.Size = new System.Drawing.Size(101, 17);
            this.lblTolerance.TabIndex = 6;
            this.lblTolerance.Text = "Tolerance (lb):";
            this.toolTip.SetToolTip(this.lblTolerance, "How much the weight can vary and still be considered stable");
            // 
            // numTolerance
            // 
            this.numTolerance.Enabled = false;
            this.numTolerance.Increment = new decimal(new int[] {
            10,
            0,
            0,
            0});
            this.numTolerance.Location = new System.Drawing.Point(129, 117);
            this.numTolerance.Maximum = new decimal(new int[] {
            10000,
            0,
            0,
            0});
            this.numTolerance.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numTolerance.Name = "numTolerance";
            this.numTolerance.Size = new System.Drawing.Size(65, 23);
            this.numTolerance.TabIndex = 6;
            this.toolTip.SetToolTip(this.numTolerance, "How much the weight can vary and still be considered stable");
            this.numTolerance.Value = new decimal(new int[] {
            50,
            0,
            0,
            0});
            this.numTolerance.ValueChanged += new System.EventHandler(this.numTolerance_ValueChanged);
            // 
            // lblConnectionStatus
            // 
            this.lblConnectionStatus.AutoSize = true;
            this.lblConnectionStatus.ForeColor = System.Drawing.SystemColors.GrayText;
            this.lblConnectionStatus.Location = new System.Drawing.Point(45, 76);
            this.lblConnectionStatus.Name = "lblConnectionStatus";
            this.lblConnectionStatus.Size = new System.Drawing.Size(99, 17);
            this.lblConnectionStatus.TabIndex = 99;
            this.lblConnectionStatus.Text = "No connection";
            // 
            // tabControl
            // 
            this.tabControl.Controls.Add(this.tabWeight);
            this.tabControl.Controls.Add(this.tabSettings);
            this.tabControl.Controls.Add(this.tabReport);
            this.tabControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabControl.Location = new System.Drawing.Point(0, 0);
            this.tabControl.Name = "tabControl";
            this.tabControl.SelectedIndex = 0;
            this.tabControl.Size = new System.Drawing.Size(520, 589);
            this.tabControl.TabIndex = 0;
            this.tabControl.SelectedIndexChanged += new System.EventHandler(this.tabControl_SelectedIndexChanged);
            // 
            // tabWeight
            // 
            this.tabWeight.Controls.Add(this.lblWeightValue);
            this.tabWeight.Controls.Add(this.btnManualWeigh);
            this.tabWeight.Controls.Add(this.lblCrop);
            this.tabWeight.Controls.Add(this.cmbCrop);
            this.tabWeight.Controls.Add(this.lblBushelWeightLabel);
            this.tabWeight.Controls.Add(this.numBushelWeight);
            this.tabWeight.Controls.Add(this.lblEmptyLabel);
            this.tabWeight.Controls.Add(this.numEmptyWeight);
            this.tabWeight.Controls.Add(this.btnCaptureTruck);
            this.tabWeight.Controls.Add(this.lblGrossLabel);
            this.tabWeight.Controls.Add(this.numGrossWeight);
            this.tabWeight.Controls.Add(this.btnCaptureGross);
            this.tabWeight.Controls.Add(this.lblNetLabel);
            this.tabWeight.Controls.Add(this.lblNetValue);
            this.tabWeight.Controls.Add(this.lblBushels);
            this.tabWeight.Controls.Add(this.lblBushelsValue);
            this.tabWeight.Controls.Add(this.dgvTodayWeights);
            this.tabWeight.Controls.Add(this.lblNote);
            this.tabWeight.Controls.Add(this.txtNote);
            this.tabWeight.Controls.Add(this.btnDeleteWeight);
            this.tabWeight.Controls.Add(this.btnPrintReceipt);
            this.tabWeight.Location = new System.Drawing.Point(4, 25);
            this.tabWeight.Name = "tabWeight";
            this.tabWeight.Padding = new System.Windows.Forms.Padding(5);
            this.tabWeight.Size = new System.Drawing.Size(512, 560);
            this.tabWeight.TabIndex = 0;
            this.tabWeight.Text = "Weight";
            // 
            // lblWeightValue
            // 
            this.lblWeightValue.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblWeightValue.Font = new System.Drawing.Font("Segoe UI", 36F, System.Drawing.FontStyle.Bold);
            this.lblWeightValue.Location = new System.Drawing.Point(8, 8);
            this.lblWeightValue.Name = "lblWeightValue";
            this.lblWeightValue.Size = new System.Drawing.Size(496, 105);
            this.lblWeightValue.TabIndex = 0;
            this.lblWeightValue.Text = "---";
            this.lblWeightValue.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // btnManualWeigh
            // 
            this.btnManualWeigh.Enabled = false;
            this.btnManualWeigh.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnManualWeigh.Location = new System.Drawing.Point(352, 137);
            this.btnManualWeigh.Name = "btnManualWeigh";
            this.btnManualWeigh.Size = new System.Drawing.Size(152, 54);
            this.btnManualWeigh.TabIndex = 5;
            this.btnManualWeigh.Text = "Record Load";
            this.btnManualWeigh.Click += new System.EventHandler(this.btnManualWeigh_Click);
            // 
            // lblCrop
            // 
            this.lblCrop.AutoSize = true;
            this.lblCrop.Location = new System.Drawing.Point(159, 266);
            this.lblCrop.Name = "lblCrop";
            this.lblCrop.Size = new System.Drawing.Size(42, 17);
            this.lblCrop.TabIndex = 7;
            this.lblCrop.Text = "Crop:";
            // 
            // cmbCrop
            // 
            this.cmbCrop.Location = new System.Drawing.Point(207, 262);
            this.cmbCrop.Name = "cmbCrop";
            this.cmbCrop.Size = new System.Drawing.Size(155, 24);
            this.cmbCrop.TabIndex = 8;
            this.cmbCrop.SelectedIndexChanged += new System.EventHandler(this.cmbCrop_SelectedIndexChanged);
            // 
            // lblBushelWeightLabel
            // 
            this.lblBushelWeightLabel.AutoSize = true;
            this.lblBushelWeightLabel.Location = new System.Drawing.Point(390, 266);
            this.lblBushelWeightLabel.Name = "lblBushelWeightLabel";
            this.lblBushelWeightLabel.Size = new System.Drawing.Size(43, 17);
            this.lblBushelWeightLabel.TabIndex = 9;
            this.lblBushelWeightLabel.Text = "lb/bu:";
            // 
            // numBushelWeight
            // 
            this.numBushelWeight.Location = new System.Drawing.Point(439, 263);
            this.numBushelWeight.Maximum = new decimal(new int[] {
            200,
            0,
            0,
            0});
            this.numBushelWeight.Name = "numBushelWeight";
            this.numBushelWeight.Size = new System.Drawing.Size(65, 23);
            this.numBushelWeight.TabIndex = 10;
            this.numBushelWeight.Value = new decimal(new int[] {
            60,
            0,
            0,
            0});
            this.numBushelWeight.ValueChanged += new System.EventHandler(this.numBushelWeight_ValueChanged);
            // 
            // lblEmptyLabel
            // 
            this.lblEmptyLabel.AutoSize = true;
            this.lblEmptyLabel.Location = new System.Drawing.Point(11, 176);
            this.lblEmptyLabel.Name = "lblEmptyLabel";
            this.lblEmptyLabel.Size = new System.Drawing.Size(73, 17);
            this.lblEmptyLabel.TabIndex = 13;
            this.lblEmptyLabel.Text = "Truck (lb):";
            // 
            // numEmptyWeight
            // 
            this.numEmptyWeight.Increment = new decimal(new int[] {
            10,
            0,
            0,
            0});
            this.numEmptyWeight.Location = new System.Drawing.Point(92, 173);
            this.numEmptyWeight.Maximum = new decimal(new int[] {
            200000,
            0,
            0,
            0});
            this.numEmptyWeight.Name = "numEmptyWeight";
            this.numEmptyWeight.Size = new System.Drawing.Size(95, 23);
            this.numEmptyWeight.TabIndex = 14;
            this.numEmptyWeight.ThousandsSeparator = true;
            this.numEmptyWeight.ValueChanged += new System.EventHandler(this.numEmptyWeight_ValueChanged);
            // 
            // btnCaptureTruck
            // 
            this.btnCaptureTruck.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnCaptureTruck.Location = new System.Drawing.Point(207, 164);
            this.btnCaptureTruck.Name = "btnCaptureTruck";
            this.btnCaptureTruck.Size = new System.Drawing.Size(117, 39);
            this.btnCaptureTruck.TabIndex = 21;
            this.btnCaptureTruck.Text = "Truck Weight";
            this.btnCaptureTruck.Click += new System.EventHandler(this.btnCaptureTruck_Click);
            // 
            // lblGrossLabel
            // 
            this.lblGrossLabel.AutoSize = true;
            this.lblGrossLabel.Location = new System.Drawing.Point(11, 129);
            this.lblGrossLabel.Name = "lblGrossLabel";
            this.lblGrossLabel.Size = new System.Drawing.Size(75, 17);
            this.lblGrossLabel.TabIndex = 22;
            this.lblGrossLabel.Text = "Gross (lb):";
            // 
            // numGrossWeight
            // 
            this.numGrossWeight.Increment = new decimal(new int[] {
            10,
            0,
            0,
            0});
            this.numGrossWeight.Location = new System.Drawing.Point(92, 126);
            this.numGrossWeight.Maximum = new decimal(new int[] {
            999999,
            0,
            0,
            0});
            this.numGrossWeight.Name = "numGrossWeight";
            this.numGrossWeight.Size = new System.Drawing.Size(95, 23);
            this.numGrossWeight.TabIndex = 23;
            this.numGrossWeight.ThousandsSeparator = true;
            this.numGrossWeight.ValueChanged += new System.EventHandler(this.numGrossWeight_ValueChanged);
            // 
            // btnCaptureGross
            // 
            this.btnCaptureGross.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnCaptureGross.Location = new System.Drawing.Point(207, 117);
            this.btnCaptureGross.Name = "btnCaptureGross";
            this.btnCaptureGross.Size = new System.Drawing.Size(117, 39);
            this.btnCaptureGross.TabIndex = 24;
            this.btnCaptureGross.Text = "Gross Weight";
            this.btnCaptureGross.Click += new System.EventHandler(this.btnCaptureGross_Click);
            // 
            // lblNetLabel
            // 
            this.lblNetLabel.AutoSize = true;
            this.lblNetLabel.Location = new System.Drawing.Point(11, 222);
            this.lblNetLabel.Name = "lblNetLabel";
            this.lblNetLabel.Size = new System.Drawing.Size(59, 17);
            this.lblNetLabel.TabIndex = 15;
            this.lblNetLabel.Text = "Net (lb):";
            // 
            // lblNetValue
            // 
            this.lblNetValue.AutoSize = true;
            this.lblNetValue.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold);
            this.lblNetValue.Location = new System.Drawing.Point(92, 220);
            this.lblNetValue.Name = "lblNetValue";
            this.lblNetValue.Size = new System.Drawing.Size(27, 20);
            this.lblNetValue.TabIndex = 16;
            this.lblNetValue.Text = "---";
            // 
            // lblBushels
            // 
            this.lblBushels.AutoSize = true;
            this.lblBushels.Location = new System.Drawing.Point(11, 266);
            this.lblBushels.Name = "lblBushels";
            this.lblBushels.Size = new System.Drawing.Size(62, 17);
            this.lblBushels.TabIndex = 11;
            this.lblBushels.Text = "Bushels:";
            // 
            // lblBushelsValue
            // 
            this.lblBushelsValue.AutoSize = true;
            this.lblBushelsValue.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold);
            this.lblBushelsValue.Location = new System.Drawing.Point(92, 264);
            this.lblBushelsValue.Name = "lblBushelsValue";
            this.lblBushelsValue.Size = new System.Drawing.Size(27, 20);
            this.lblBushelsValue.TabIndex = 12;
            this.lblBushelsValue.Text = "---";
            // 
            // dgvTodayWeights
            // 
            this.dgvTodayWeights.BackgroundColor = System.Drawing.SystemColors.Window;
            this.dgvTodayWeights.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.dgvTodayWeights.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvTodayWeights.Location = new System.Drawing.Point(8, 350);
            this.dgvTodayWeights.Name = "dgvTodayWeights";
            this.dgvTodayWeights.Size = new System.Drawing.Size(496, 170);
            this.dgvTodayWeights.TabIndex = 6;
            this.dgvTodayWeights.SelectionChanged += new System.EventHandler(this.dgvTodayWeights_SelectionChanged);
            // 
            // lblNote
            // 
            this.lblNote.AutoSize = true;
            this.lblNote.Location = new System.Drawing.Point(11, 292);
            this.lblNote.Name = "lblNote";
            this.lblNote.Size = new System.Drawing.Size(42, 17);
            this.lblNote.TabIndex = 19;
            this.lblNote.Text = "Note:";
            // 
            // txtNote
            // 
            this.txtNote.Location = new System.Drawing.Point(55, 292);
            this.txtNote.Multiline = true;
            this.txtNote.Name = "txtNote";
            this.txtNote.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtNote.Size = new System.Drawing.Size(449, 52);
            this.txtNote.TabIndex = 20;
            // 
            // btnDeleteWeight
            // 
            this.btnDeleteWeight.Location = new System.Drawing.Point(103, 526);
            this.btnDeleteWeight.Name = "btnDeleteWeight";
            this.btnDeleteWeight.Size = new System.Drawing.Size(130, 26);
            this.btnDeleteWeight.TabIndex = 17;
            this.btnDeleteWeight.Text = "Delete Selected";
            this.btnDeleteWeight.Click += new System.EventHandler(this.btnDeleteWeight_Click);
            // 
            // btnPrintReceipt
            // 
            this.btnPrintReceipt.Location = new System.Drawing.Point(259, 526);
            this.btnPrintReceipt.Name = "btnPrintReceipt";
            this.btnPrintReceipt.Size = new System.Drawing.Size(130, 26);
            this.btnPrintReceipt.TabIndex = 18;
            this.btnPrintReceipt.Text = "Print Receipt";
            this.btnPrintReceipt.Click += new System.EventHandler(this.btnPrintReceipt_Click);
            // 
            // tabSettings
            // 
            this.tabSettings.Controls.Add(this.gbData);
            this.tabSettings.Controls.Add(this.grpAutoWeigh);
            this.tabSettings.Controls.Add(this.grpUnits);
            this.tabSettings.Location = new System.Drawing.Point(4, 25);
            this.tabSettings.Name = "tabSettings";
            this.tabSettings.Padding = new System.Windows.Forms.Padding(5);
            this.tabSettings.Size = new System.Drawing.Size(512, 560);
            this.tabSettings.TabIndex = 1;
            this.tabSettings.Text = "Settings";
            // 
            // lblPort
            // 
            this.lblPort.AutoSize = true;
            this.lblPort.Location = new System.Drawing.Point(45, 36);
            this.lblPort.Name = "lblPort";
            this.lblPort.Size = new System.Drawing.Size(73, 17);
            this.lblPort.TabIndex = 0;
            this.lblPort.Text = "COM Port:";
            // 
            // cmbPort
            // 
            this.cmbPort.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbPort.Location = new System.Drawing.Point(119, 32);
            this.cmbPort.Name = "cmbPort";
            this.cmbPort.Size = new System.Drawing.Size(85, 24);
            this.cmbPort.TabIndex = 1;
            // 
            // btnRefresh
            // 
            this.btnRefresh.Location = new System.Drawing.Point(210, 31);
            this.btnRefresh.Name = "btnRefresh";
            this.btnRefresh.Size = new System.Drawing.Size(76, 26);
            this.btnRefresh.TabIndex = 2;
            this.btnRefresh.Text = "Refresh";
            this.btnRefresh.Click += new System.EventHandler(this.btnRefresh_Click);
            // 
            // btnConnect
            // 
            this.btnConnect.Location = new System.Drawing.Point(291, 31);
            this.btnConnect.Name = "btnConnect";
            this.btnConnect.Size = new System.Drawing.Size(95, 26);
            this.btnConnect.TabIndex = 3;
            this.btnConnect.Text = "Connect";
            this.btnConnect.Click += new System.EventHandler(this.btnConnect_Click);
            // 
            // grpAutoWeigh
            // 
            this.grpAutoWeigh.Controls.Add(this.chkAutoWeigh);
            this.grpAutoWeigh.Controls.Add(this.lblInterval);
            this.grpAutoWeigh.Controls.Add(this.numInterval);
            this.grpAutoWeigh.Controls.Add(this.lblTolerance);
            this.grpAutoWeigh.Controls.Add(this.numTolerance);
            this.grpAutoWeigh.Controls.Add(this.lblMinWeight);
            this.grpAutoWeigh.Controls.Add(this.numMinWeight);
            this.grpAutoWeigh.Controls.Add(this.lblStability);
            this.grpAutoWeigh.Controls.Add(this.numStability);
            this.grpAutoWeigh.Controls.Add(this.lblSignal);
            this.grpAutoWeigh.Controls.Add(this.numSignal);
            this.grpAutoWeigh.Location = new System.Drawing.Point(14, 190);
            this.grpAutoWeigh.Name = "grpAutoWeigh";
            this.grpAutoWeigh.Size = new System.Drawing.Size(482, 226);
            this.grpAutoWeigh.TabIndex = 4;
            this.grpAutoWeigh.TabStop = false;
            this.grpAutoWeigh.Text = "Auto Weigh";
            // 
            // chkAutoWeigh
            // 
            this.chkAutoWeigh.AutoSize = true;
            this.chkAutoWeigh.Location = new System.Drawing.Point(10, 22);
            this.chkAutoWeigh.Name = "chkAutoWeigh";
            this.chkAutoWeigh.Size = new System.Drawing.Size(71, 21);
            this.chkAutoWeigh.TabIndex = 0;
            this.chkAutoWeigh.Text = "Enable";
            this.chkAutoWeigh.CheckedChanged += new System.EventHandler(this.chkAutoWeigh_CheckedChanged);
            // 
            // lblMinWeight
            // 
            this.lblMinWeight.AutoSize = true;
            this.lblMinWeight.Location = new System.Drawing.Point(10, 50);
            this.lblMinWeight.Name = "lblMinWeight";
            this.lblMinWeight.Size = new System.Drawing.Size(107, 17);
            this.lblMinWeight.TabIndex = 1;
            this.lblMinWeight.Text = "Min Weight (lb):";
            // 
            // numMinWeight
            // 
            this.numMinWeight.Enabled = false;
            this.numMinWeight.Increment = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            this.numMinWeight.Location = new System.Drawing.Point(130, 47);
            this.numMinWeight.Maximum = new decimal(new int[] {
            200000,
            0,
            0,
            0});
            this.numMinWeight.Name = "numMinWeight";
            this.numMinWeight.Size = new System.Drawing.Size(85, 23);
            this.numMinWeight.TabIndex = 1;
            this.numMinWeight.Value = new decimal(new int[] {
            10000,
            0,
            0,
            0});
            this.numMinWeight.ValueChanged += new System.EventHandler(this.numMinWeight_ValueChanged);
            // 
            // grpUnits
            // 
            this.grpUnits.Controls.Add(this.rdoImperial);
            this.grpUnits.Controls.Add(this.rdoMetric);
            this.grpUnits.Location = new System.Drawing.Point(14, 448);
            this.grpUnits.Name = "grpUnits";
            this.grpUnits.Size = new System.Drawing.Size(482, 66);
            this.grpUnits.TabIndex = 5;
            this.grpUnits.TabStop = false;
            this.grpUnits.Text = "Units";
            // 
            // rdoImperial
            // 
            this.rdoImperial.AutoSize = true;
            this.rdoImperial.Checked = true;
            this.rdoImperial.Location = new System.Drawing.Point(10, 22);
            this.rdoImperial.Name = "rdoImperial";
            this.rdoImperial.Size = new System.Drawing.Size(100, 21);
            this.rdoImperial.TabIndex = 0;
            this.rdoImperial.TabStop = true;
            this.rdoImperial.Text = "Imperial (lb)";
            this.rdoImperial.CheckedChanged += new System.EventHandler(this.rdoUnits_CheckedChanged);
            // 
            // rdoMetric
            // 
            this.rdoMetric.AutoSize = true;
            this.rdoMetric.Location = new System.Drawing.Point(150, 22);
            this.rdoMetric.Name = "rdoMetric";
            this.rdoMetric.Size = new System.Drawing.Size(93, 21);
            this.rdoMetric.TabIndex = 1;
            this.rdoMetric.Text = "Metric (kg)";
            this.rdoMetric.CheckedChanged += new System.EventHandler(this.rdoUnits_CheckedChanged);
            // 
            // tabReport
            // 
            this.tabReport.Controls.Add(this.label1);
            this.tabReport.Controls.Add(this.textBox1);
            this.tabReport.Controls.Add(this.lblDate);
            this.tabReport.Controls.Add(this.dtpReport);
            this.tabReport.Controls.Add(this.btnLoadReport);
            this.tabReport.Controls.Add(this.dgvReport);
            this.tabReport.Controls.Add(this.btnPrint);
            this.tabReport.Location = new System.Drawing.Point(4, 25);
            this.tabReport.Name = "tabReport";
            this.tabReport.Padding = new System.Windows.Forms.Padding(5);
            this.tabReport.Size = new System.Drawing.Size(512, 560);
            this.tabReport.TabIndex = 2;
            this.tabReport.Text = "Report";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(8, 49);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(42, 17);
            this.label1.TabIndex = 21;
            this.label1.Text = "Note:";
            // 
            // textBox1
            // 
            this.textBox1.Location = new System.Drawing.Point(55, 49);
            this.textBox1.Multiline = true;
            this.textBox1.Name = "textBox1";
            this.textBox1.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.textBox1.Size = new System.Drawing.Size(449, 52);
            this.textBox1.TabIndex = 22;
            // 
            // lblDate
            // 
            this.lblDate.AutoSize = true;
            this.lblDate.Location = new System.Drawing.Point(8, 13);
            this.lblDate.Name = "lblDate";
            this.lblDate.Size = new System.Drawing.Size(42, 17);
            this.lblDate.TabIndex = 0;
            this.lblDate.Text = "Date:";
            // 
            // dtpReport
            // 
            this.dtpReport.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.dtpReport.Location = new System.Drawing.Point(50, 8);
            this.dtpReport.Name = "dtpReport";
            this.dtpReport.Size = new System.Drawing.Size(130, 23);
            this.dtpReport.TabIndex = 0;
            // 
            // btnLoadReport
            // 
            this.btnLoadReport.Location = new System.Drawing.Point(188, 7);
            this.btnLoadReport.Name = "btnLoadReport";
            this.btnLoadReport.Size = new System.Drawing.Size(75, 26);
            this.btnLoadReport.TabIndex = 1;
            this.btnLoadReport.Text = "Load";
            this.btnLoadReport.Click += new System.EventHandler(this.btnLoadReport_Click);
            // 
            // dgvReport
            // 
            this.dgvReport.BackgroundColor = System.Drawing.SystemColors.Window;
            this.dgvReport.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.dgvReport.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvReport.Location = new System.Drawing.Point(8, 107);
            this.dgvReport.Name = "dgvReport";
            this.dgvReport.Size = new System.Drawing.Size(496, 445);
            this.dgvReport.TabIndex = 2;
            this.dgvReport.SelectionChanged += new System.EventHandler(this.dgvReport_SelectionChanged);
            // 
            // btnPrint
            // 
            this.btnPrint.Location = new System.Drawing.Point(429, 8);
            this.btnPrint.Name = "btnPrint";
            this.btnPrint.Size = new System.Drawing.Size(75, 25);
            this.btnPrint.TabIndex = 3;
            this.btnPrint.Text = "Print...";
            this.btnPrint.Click += new System.EventHandler(this.btnPrint_Click);
            // 
            // gbData
            // 
            this.gbData.Controls.Add(this.btnConnect);
            this.gbData.Controls.Add(this.lblConnectionStatus);
            this.gbData.Controls.Add(this.btnRefresh);
            this.gbData.Controls.Add(this.lblPort);
            this.gbData.Controls.Add(this.cmbPort);
            this.gbData.Location = new System.Drawing.Point(14, 41);
            this.gbData.Name = "gbData";
            this.gbData.Size = new System.Drawing.Size(482, 110);
            this.gbData.TabIndex = 100;
            this.gbData.TabStop = false;
            this.gbData.Text = "Data Source";
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(520, 589);
            this.Controls.Add(this.tabControl);
            this.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.Name = "Form1";
            this.Text = "Scale Reader";
            this.TopMost = true;
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_FormClosing);
            ((System.ComponentModel.ISupportInitialize)(this.numStability)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numSignal)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numInterval)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numTolerance)).EndInit();
            this.tabControl.ResumeLayout(false);
            this.tabWeight.ResumeLayout(false);
            this.tabWeight.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numBushelWeight)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numEmptyWeight)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numGrossWeight)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvTodayWeights)).EndInit();
            this.tabSettings.ResumeLayout(false);
            this.grpAutoWeigh.ResumeLayout(false);
            this.grpAutoWeigh.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numMinWeight)).EndInit();
            this.grpUnits.ResumeLayout(false);
            this.grpUnits.PerformLayout();
            this.tabReport.ResumeLayout(false);
            this.tabReport.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvReport)).EndInit();
            this.gbData.ResumeLayout(false);
            this.gbData.PerformLayout();
            this.ResumeLayout(false);

        }

        private System.Windows.Forms.ToolTip         toolTip;

        // Tabs
        private System.Windows.Forms.TabControl  tabControl;
        private System.Windows.Forms.TabPage     tabWeight;
        private System.Windows.Forms.TabPage     tabSettings;
        private System.Windows.Forms.TabPage     tabReport;

        // Weight tab
        private System.Windows.Forms.Label           lblWeightValue;
        private System.Windows.Forms.Button          btnManualWeigh;
        private System.Windows.Forms.Label           lblCrop;
        private System.Windows.Forms.ComboBox        cmbCrop;
        private System.Windows.Forms.Label           lblBushelWeightLabel;
        private System.Windows.Forms.NumericUpDown   numBushelWeight;
        private System.Windows.Forms.Label           lblEmptyLabel;
        private System.Windows.Forms.NumericUpDown   numEmptyWeight;
        private System.Windows.Forms.Button          btnCaptureTruck;
        private System.Windows.Forms.Label           lblGrossLabel;
        private System.Windows.Forms.NumericUpDown   numGrossWeight;
        private System.Windows.Forms.Button          btnCaptureGross;
        private System.Windows.Forms.Label           lblNetLabel;
        private System.Windows.Forms.Label           lblNetValue;
        private System.Windows.Forms.Label           lblBushels;
        private System.Windows.Forms.Label           lblBushelsValue;
        private System.Windows.Forms.DataGridView    dgvTodayWeights;
        private System.Windows.Forms.Label           lblNote;
        private System.Windows.Forms.TextBox         txtNote;
        private System.Windows.Forms.Button          btnDeleteWeight;
        private System.Windows.Forms.Button          btnPrintReceipt;

        // Settings tab
        private System.Windows.Forms.Label           lblConnectionStatus;
        private System.Windows.Forms.Label           lblPort;
        private System.Windows.Forms.ComboBox        cmbPort;
        private System.Windows.Forms.Button          btnRefresh;
        private System.Windows.Forms.Button          btnConnect;
        private System.Windows.Forms.GroupBox        grpAutoWeigh;
        private System.Windows.Forms.CheckBox        chkAutoWeigh;
        private System.Windows.Forms.Label           lblMinWeight;
        private System.Windows.Forms.NumericUpDown   numMinWeight;
        private System.Windows.Forms.Label           lblStability;
        private System.Windows.Forms.NumericUpDown   numStability;
        private System.Windows.Forms.Label           lblSignal;
        private System.Windows.Forms.NumericUpDown   numSignal;
        private System.Windows.Forms.Label           lblInterval;
        private System.Windows.Forms.NumericUpDown   numInterval;
        private System.Windows.Forms.Label           lblTolerance;
        private System.Windows.Forms.NumericUpDown   numTolerance;
        private System.Windows.Forms.GroupBox        grpUnits;
        private System.Windows.Forms.RadioButton     rdoImperial;
        private System.Windows.Forms.RadioButton     rdoMetric;

        // Report tab
        private System.Windows.Forms.Label           lblDate;
        private System.Windows.Forms.DateTimePicker  dtpReport;
        private System.Windows.Forms.Button          btnLoadReport;
        private System.Windows.Forms.DataGridView    dgvReport;
        private System.Windows.Forms.Button          btnPrint;
        private Label label1;
        private TextBox textBox1;
        private GroupBox gbData;
        private System.ComponentModel.BackgroundWorker backgroundWorker1;
    }
}
