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
            this.tabControl = new System.Windows.Forms.TabControl();
            this.tabWeight = new System.Windows.Forms.TabPage();
            this.lblWeightValue = new System.Windows.Forms.Label();
            this.lblMode = new System.Windows.Forms.Label();
            this.lblModeValue = new System.Windows.Forms.Label();
            this.lblStatus = new System.Windows.Forms.Label();
            this.lblStatusValue = new System.Windows.Forms.Label();
            this.btnManualWeigh = new System.Windows.Forms.Button();
            this.dgvTodayWeights = new System.Windows.Forms.DataGridView();
            this.tabSettings = new System.Windows.Forms.TabPage();
            this.lblPort = new System.Windows.Forms.Label();
            this.cmbPort = new System.Windows.Forms.ComboBox();
            this.btnRefresh = new System.Windows.Forms.Button();
            this.btnConnect = new System.Windows.Forms.Button();
            this.grpAutoWeigh = new System.Windows.Forms.GroupBox();
            this.chkAutoWeigh = new System.Windows.Forms.CheckBox();
            this.lblMinWeight = new System.Windows.Forms.Label();
            this.numMinWeight = new System.Windows.Forms.NumericUpDown();
            this.lblStability = new System.Windows.Forms.Label();
            this.numStability = new System.Windows.Forms.NumericUpDown();
            this.lblSignal = new System.Windows.Forms.Label();
            this.numSignal = new System.Windows.Forms.NumericUpDown();
            this.tabReport = new System.Windows.Forms.TabPage();
            this.lblDate = new System.Windows.Forms.Label();
            this.dtpReport = new System.Windows.Forms.DateTimePicker();
            this.btnLoadReport = new System.Windows.Forms.Button();
            this.dgvReport = new System.Windows.Forms.DataGridView();
            this.btnPrint = new System.Windows.Forms.Button();
            this.tabControl.SuspendLayout();
            this.tabWeight.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvTodayWeights)).BeginInit();
            this.tabSettings.SuspendLayout();
            this.grpAutoWeigh.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numMinWeight)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numStability)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numSignal)).BeginInit();
            this.tabReport.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvReport)).BeginInit();
            this.SuspendLayout();
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
            this.tabControl.Size = new System.Drawing.Size(418, 505);
            this.tabControl.TabIndex = 0;
            // 
            // tabWeight
            // 
            this.tabWeight.Controls.Add(this.lblWeightValue);
            this.tabWeight.Controls.Add(this.lblMode);
            this.tabWeight.Controls.Add(this.lblModeValue);
            this.tabWeight.Controls.Add(this.lblStatus);
            this.tabWeight.Controls.Add(this.lblStatusValue);
            this.tabWeight.Controls.Add(this.btnManualWeigh);
            this.tabWeight.Controls.Add(this.dgvTodayWeights);
            this.tabWeight.Location = new System.Drawing.Point(4, 25);
            this.tabWeight.Name = "tabWeight";
            this.tabWeight.Padding = new System.Windows.Forms.Padding(5);
            this.tabWeight.Size = new System.Drawing.Size(410, 476);
            this.tabWeight.TabIndex = 0;
            this.tabWeight.Text = "Weight";
            // 
            // lblWeightValue
            // 
            this.lblWeightValue.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblWeightValue.Font = new System.Drawing.Font("Segoe UI", 36F, System.Drawing.FontStyle.Bold);
            this.lblWeightValue.Location = new System.Drawing.Point(8, 8);
            this.lblWeightValue.Name = "lblWeightValue";
            this.lblWeightValue.Size = new System.Drawing.Size(390, 105);
            this.lblWeightValue.TabIndex = 0;
            this.lblWeightValue.Text = "---";
            this.lblWeightValue.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblMode
            // 
            this.lblMode.AutoSize = true;
            this.lblMode.Location = new System.Drawing.Point(8, 122);
            this.lblMode.Name = "lblMode";
            this.lblMode.Size = new System.Drawing.Size(47, 17);
            this.lblMode.TabIndex = 1;
            this.lblMode.Text = "Mode:";
            // 
            // lblModeValue
            // 
            this.lblModeValue.AutoSize = true;
            this.lblModeValue.Location = new System.Drawing.Point(70, 122);
            this.lblModeValue.Name = "lblModeValue";
            this.lblModeValue.Size = new System.Drawing.Size(23, 17);
            this.lblModeValue.TabIndex = 2;
            this.lblModeValue.Text = "---";
            // 
            // lblStatus
            // 
            this.lblStatus.AutoSize = true;
            this.lblStatus.Location = new System.Drawing.Point(8, 159);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(52, 17);
            this.lblStatus.TabIndex = 3;
            this.lblStatus.Text = "Status:";
            // 
            // lblStatusValue
            // 
            this.lblStatusValue.AutoSize = true;
            this.lblStatusValue.Location = new System.Drawing.Point(70, 159);
            this.lblStatusValue.Name = "lblStatusValue";
            this.lblStatusValue.Size = new System.Drawing.Size(23, 17);
            this.lblStatusValue.TabIndex = 4;
            this.lblStatusValue.Text = "---";
            // 
            // btnManualWeigh
            // 
            this.btnManualWeigh.Enabled = false;
            this.btnManualWeigh.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnManualWeigh.Location = new System.Drawing.Point(210, 125);
            this.btnManualWeigh.Name = "btnManualWeigh";
            this.btnManualWeigh.Size = new System.Drawing.Size(188, 54);
            this.btnManualWeigh.TabIndex = 5;
            this.btnManualWeigh.Text = "Record Weight";
            this.btnManualWeigh.Click += new System.EventHandler(this.btnManualWeigh_Click);
            // 
            // dgvTodayWeights
            // 
            this.dgvTodayWeights.BackgroundColor = System.Drawing.SystemColors.Window;
            this.dgvTodayWeights.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.dgvTodayWeights.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvTodayWeights.Location = new System.Drawing.Point(8, 193);
            this.dgvTodayWeights.Name = "dgvTodayWeights";
            this.dgvTodayWeights.Size = new System.Drawing.Size(390, 272);
            this.dgvTodayWeights.TabIndex = 6;
            // 
            // tabSettings
            // 
            this.tabSettings.Controls.Add(this.lblPort);
            this.tabSettings.Controls.Add(this.cmbPort);
            this.tabSettings.Controls.Add(this.btnRefresh);
            this.tabSettings.Controls.Add(this.btnConnect);
            this.tabSettings.Controls.Add(this.grpAutoWeigh);
            this.tabSettings.Location = new System.Drawing.Point(4, 25);
            this.tabSettings.Name = "tabSettings";
            this.tabSettings.Padding = new System.Windows.Forms.Padding(5);
            this.tabSettings.Size = new System.Drawing.Size(410, 476);
            this.tabSettings.TabIndex = 1;
            this.tabSettings.Text = "Settings";
            // 
            // lblPort
            // 
            this.lblPort.AutoSize = true;
            this.lblPort.Location = new System.Drawing.Point(27, 36);
            this.lblPort.Name = "lblPort";
            this.lblPort.Size = new System.Drawing.Size(73, 17);
            this.lblPort.TabIndex = 0;
            this.lblPort.Text = "COM Port:";
            // 
            // cmbPort
            // 
            this.cmbPort.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbPort.Location = new System.Drawing.Point(101, 32);
            this.cmbPort.Name = "cmbPort";
            this.cmbPort.Size = new System.Drawing.Size(85, 24);
            this.cmbPort.TabIndex = 1;
            // 
            // btnRefresh
            // 
            this.btnRefresh.Location = new System.Drawing.Point(192, 31);
            this.btnRefresh.Name = "btnRefresh";
            this.btnRefresh.Size = new System.Drawing.Size(76, 26);
            this.btnRefresh.TabIndex = 2;
            this.btnRefresh.Text = "Refresh";
            this.btnRefresh.Click += new System.EventHandler(this.btnRefresh_Click);
            // 
            // btnConnect
            // 
            this.btnConnect.Location = new System.Drawing.Point(273, 31);
            this.btnConnect.Name = "btnConnect";
            this.btnConnect.Size = new System.Drawing.Size(95, 26);
            this.btnConnect.TabIndex = 3;
            this.btnConnect.Text = "Connect";
            this.btnConnect.Click += new System.EventHandler(this.btnConnect_Click);
            // 
            // grpAutoWeigh
            // 
            this.grpAutoWeigh.Controls.Add(this.chkAutoWeigh);
            this.grpAutoWeigh.Controls.Add(this.lblMinWeight);
            this.grpAutoWeigh.Controls.Add(this.numMinWeight);
            this.grpAutoWeigh.Controls.Add(this.lblStability);
            this.grpAutoWeigh.Controls.Add(this.numStability);
            this.grpAutoWeigh.Controls.Add(this.lblSignal);
            this.grpAutoWeigh.Controls.Add(this.numSignal);
            this.grpAutoWeigh.Location = new System.Drawing.Point(12, 74);
            this.grpAutoWeigh.Name = "grpAutoWeigh";
            this.grpAutoWeigh.Size = new System.Drawing.Size(385, 160);
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
            // lblStability
            // 
            this.lblStability.AutoSize = true;
            this.lblStability.Location = new System.Drawing.Point(10, 83);
            this.lblStability.Name = "lblStability";
            this.lblStability.Size = new System.Drawing.Size(82, 17);
            this.lblStability.TabIndex = 2;
            this.lblStability.Text = "Stability (s):";
            // 
            // numStability
            // 
            this.numStability.Enabled = false;
            this.numStability.Location = new System.Drawing.Point(130, 80);
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
            this.numStability.Value = new decimal(new int[] {
            3,
            0,
            0,
            0});
            this.numStability.ValueChanged += new System.EventHandler(this.numStability_ValueChanged);
            // 
            // lblSignal
            // 
            this.lblSignal.AutoSize = true;
            this.lblSignal.Location = new System.Drawing.Point(10, 116);
            this.lblSignal.Name = "lblSignal";
            this.lblSignal.Size = new System.Drawing.Size(72, 17);
            this.lblSignal.TabIndex = 3;
            this.lblSignal.Text = "Signal (s):";
            // 
            // numSignal
            // 
            this.numSignal.Enabled = false;
            this.numSignal.Location = new System.Drawing.Point(130, 113);
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
            this.numSignal.Value = new decimal(new int[] {
            5,
            0,
            0,
            0});
            this.numSignal.ValueChanged += new System.EventHandler(this.numSignal_ValueChanged);
            // 
            // tabReport
            // 
            this.tabReport.Controls.Add(this.lblDate);
            this.tabReport.Controls.Add(this.dtpReport);
            this.tabReport.Controls.Add(this.btnLoadReport);
            this.tabReport.Controls.Add(this.dgvReport);
            this.tabReport.Controls.Add(this.btnPrint);
            this.tabReport.Location = new System.Drawing.Point(4, 25);
            this.tabReport.Name = "tabReport";
            this.tabReport.Padding = new System.Windows.Forms.Padding(5);
            this.tabReport.Size = new System.Drawing.Size(410, 476);
            this.tabReport.TabIndex = 2;
            this.tabReport.Text = "Report";
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
            this.dgvReport.Location = new System.Drawing.Point(8, 42);
            this.dgvReport.Name = "dgvReport";
            this.dgvReport.Size = new System.Drawing.Size(390, 426);
            this.dgvReport.TabIndex = 2;
            // 
            // btnPrint
            // 
            this.btnPrint.Location = new System.Drawing.Point(323, 8);
            this.btnPrint.Name = "btnPrint";
            this.btnPrint.Size = new System.Drawing.Size(75, 25);
            this.btnPrint.TabIndex = 3;
            this.btnPrint.Text = "Print...";
            this.btnPrint.Click += new System.EventHandler(this.btnPrint_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(418, 505);
            this.Controls.Add(this.tabControl);
            this.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Name = "Form1";
            this.Text = "Scale Reader";
            this.TopMost = true;
            this.tabControl.ResumeLayout(false);
            this.tabWeight.ResumeLayout(false);
            this.tabWeight.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvTodayWeights)).EndInit();
            this.tabSettings.ResumeLayout(false);
            this.tabSettings.PerformLayout();
            this.grpAutoWeigh.ResumeLayout(false);
            this.grpAutoWeigh.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numMinWeight)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numStability)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numSignal)).EndInit();
            this.tabReport.ResumeLayout(false);
            this.tabReport.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvReport)).EndInit();
            this.ResumeLayout(false);

        }

        // Tabs
        private System.Windows.Forms.TabControl  tabControl;
        private System.Windows.Forms.TabPage     tabWeight;
        private System.Windows.Forms.TabPage     tabSettings;
        private System.Windows.Forms.TabPage     tabReport;

        // Weight tab
        private System.Windows.Forms.Label           lblWeightValue;
        private System.Windows.Forms.Label           lblMode;
        private System.Windows.Forms.Label           lblModeValue;
        private System.Windows.Forms.Label           lblStatus;
        private System.Windows.Forms.Label           lblStatusValue;
        private System.Windows.Forms.Button          btnManualWeigh;
        private System.Windows.Forms.DataGridView    dgvTodayWeights;

        // Settings tab
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

        // Report tab
        private System.Windows.Forms.Label           lblDate;
        private System.Windows.Forms.DateTimePicker  dtpReport;
        private System.Windows.Forms.Button          btnLoadReport;
        private System.Windows.Forms.DataGridView    dgvReport;
        private System.Windows.Forms.Button          btnPrint;
    }
}
