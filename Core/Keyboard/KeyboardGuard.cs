using System;
using System.Linq;
using System.Windows.Forms;

namespace Oscilloscope_Network_Capture
{
    public partial class Main : Form
    {
        // When true, capture-mode hotkeys are suspended and keystrokes go to the focused editor.
        private bool _suspendCaptureHotkeys;

        protected override void OnLoad(EventArgs e)
        {
            // Let the existing Main_Load run first (base.OnLoad raises the Load event),
            // then wire the focus guards.
            base.OnLoad(e);

            try { WireCaptureEditingFocusGuards(); } catch { }

            try
            {
                if (tabMain != null)
                {
                    tabMain.SelectedIndexChanged -= TabMain_SelectedIndexChanged_ResetHotkeys;
                    tabMain.SelectedIndexChanged += TabMain_SelectedIndexChanged_ResetHotkeys;
                }
            }
            catch { }
        }

        private void TabMain_SelectedIndexChanged_ResetHotkeys(object sender, EventArgs e)
        {
            // Switching tabs should never keep hotkeys suspended
            _suspendCaptureHotkeys = false;
            UpdateCaptureModeIndicators();
        }

        private void WireCaptureEditingFocusGuards()
        {
            try
            {
                // Filename format textbox
                if (_tbFilenameFormat == null)
                {
                    _tbFilenameFormat = this.Controls.Find("textBoxFilenameFormat", true).FirstOrDefault() as TextBox
                                        ?? this.Controls.Find("textBox1", true).FirstOrDefault() as TextBox;
                }
                if (_tbFilenameFormat != null)
                {
                    _tbFilenameFormat.Enter -= CaptureEditor_Enter;
                    _tbFilenameFormat.Leave -= CaptureEditor_Leave;
                    _tbFilenameFormat.Enter += CaptureEditor_Enter;
                    _tbFilenameFormat.Leave += CaptureEditor_Leave;
                }

                // NUMBER numeric up/down
                if (numericUpDown1 != null)
                {
                    numericUpDown1.Enter -= CaptureEditor_Enter;
                    numericUpDown1.Leave -= CaptureEditor_Leave;
                    numericUpDown1.Enter += CaptureEditor_Enter;
                    numericUpDown1.Leave += CaptureEditor_Leave;
                }

                // Bind for existing dynamic variable value textboxes on Capturing tab
                BindVarTextHandlersRecursive(tabCapturing);

                // Also bind for any future additions on Capturing tab
                if (tabCapturing != null)
                {
                    tabCapturing.ControlAdded -= TabCapturing_ControlAdded_BindVarEditors;
                    tabCapturing.ControlAdded += TabCapturing_ControlAdded_BindVarEditors;
                }
            }
            catch { /* best-effort */ }
        }

        private void TabCapturing_ControlAdded_BindVarEditors(object sender, ControlEventArgs e)
        {
            TryBindVarTextBox(e.Control);
        }

        private void BindVarTextHandlersRecursive(Control root)
        {
            if (root == null) return;
            foreach (Control c in root.Controls)
            {
                TryBindVarTextBox(c);
                if (c.HasChildren) BindVarTextHandlersRecursive(c);
            }
        }

        private void TryBindVarTextBox(Control c)
        {
            if (c == null) return;
            try
            {
                // Dynamic variable value editors on Capturing tab are named "varTxt*"
                if (c is TextBox && (c.Name ?? string.Empty).StartsWith(VarTextPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    c.Enter -= CaptureEditor_Enter;
                    c.Leave -= CaptureEditor_Leave;
                    c.Enter += CaptureEditor_Enter;
                    c.Leave += CaptureEditor_Leave;
                }
            }
            catch { }
        }

        private void CaptureEditor_Enter(object sender, EventArgs e)
        {
            _suspendCaptureHotkeys = true;
            UpdateCaptureModeIndicators();
        }

        private void CaptureEditor_Leave(object sender, EventArgs e)
        {
            _suspendCaptureHotkeys = false;
            UpdateCaptureModeIndicators();
        }
    }
}