namespace ScaleDisplay
{
    partial class BigDisplayForm
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
            this.lblWeight = new System.Windows.Forms.Label();
            this.SuspendLayout();
            //
            // lblWeight
            //
            this.lblWeight.BackColor = System.Drawing.Color.Black;
            this.lblWeight.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblWeight.ForeColor = System.Drawing.Color.White;
            this.lblWeight.Location = new System.Drawing.Point(0, 0);
            this.lblWeight.Name = "lblWeight";
            this.lblWeight.Size = new System.Drawing.Size(800, 450);
            this.lblWeight.TabIndex = 0;
            this.lblWeight.Text = "---";
            this.lblWeight.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.lblWeight.UseMnemonic = false;
            this.lblWeight.DoubleClick += new System.EventHandler(this.lblWeight_DoubleClick);
            //
            // BigDisplayForm
            //
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.BackColor = System.Drawing.Color.Black;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.lblWeight);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.KeyPreview = true;
            this.Name = "BigDisplayForm";
            this.Text = "Scale Display";
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.BigDisplayForm_KeyDown);
            this.Shown += new System.EventHandler(this.BigDisplayForm_Shown);
            this.ResumeLayout(false);
        }

        private System.Windows.Forms.Label lblWeight;
    }
}
