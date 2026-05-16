using System;
using System.Drawing;
using System.Windows.Forms;

namespace ScaleDisplay
{
    public partial class BigDisplayForm : Form
    {
        private Font _scaledFont;

        public BigDisplayForm()
        {
            InitializeComponent();
        }

        public void UpdateWeight(string text)
        {
            if (IsDisposed) return;
            if (InvokeRequired) { BeginInvoke(new Action(() => UpdateWeight(text))); return; }
            lblWeight.Text = text;
            ScaleFont();
        }

        private void BigDisplayForm_Shown(object sender, EventArgs e)
        {
            PlaceOnScreen();
        }

        private void PlaceOnScreen()
        {
            Screen target = null;
            if (Owner != null)
            {
                Screen ownerScreen = Screen.FromHandle(Owner.Handle);
                foreach (Screen s in Screen.AllScreens)
                {
                    if (s.DeviceName != ownerScreen.DeviceName)
                    { target = s; break; }
                }
            }
            if (target == null) target = Screen.PrimaryScreen;
            Bounds = target.Bounds;
            ScaleFont();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            ScaleFont();
        }

        private void ScaleFont()
        {
            if (!IsHandleCreated || ClientSize.Width <= 0 || ClientSize.Height <= 0) return;
            string text = lblWeight.Text;
            if (string.IsNullOrEmpty(text)) return;

            float lo = 10f, hi = 3000f;
            while (hi - lo > 1f)
            {
                float mid = (lo + hi) / 2f;
                using (var f = new Font("Arial", mid, FontStyle.Bold, GraphicsUnit.Pixel))
                {
                    Size sz = TextRenderer.MeasureText(text, f);
                    if (sz.Width < ClientSize.Width * 1.0f && sz.Height < ClientSize.Height * 0.88f)
                        lo = mid;
                    else
                        hi = mid;
                }
            }

            Font newFont = new Font("Arial", lo, FontStyle.Bold, GraphicsUnit.Pixel);
            Font old = _scaledFont;
            _scaledFont = newFont;
            lblWeight.Font = newFont;
            old?.Dispose();
        }

        private void BigDisplayForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape) Close();
        }

        private void lblWeight_DoubleClick(object sender, EventArgs e)
        {
            Close();
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _scaledFont?.Dispose();
            base.OnFormClosed(e);
        }
    }
}
