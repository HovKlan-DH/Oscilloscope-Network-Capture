using System;
using System.Windows.Forms;

namespace Oscilloscope_Network_Capture
{
    public partial class FormDebugView : Form
    {
        public FormDebugView()
        {
            InitializeComponent();
            this.Text = "Debug View";
            this.Width = 800;
            this.Height = 600;
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.FormBorderStyle = FormBorderStyle.SizableToolWindow;
            this.ResumeLayout(false);
        }
    }
}
