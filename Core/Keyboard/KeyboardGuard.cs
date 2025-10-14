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

            // NEW: resume capture-mode when clicking anywhere outside the input fields on Capturing tab
            try { WireResumeOnClickOutsideEditors(); } catch { }

            try
            {
                if (tabMain != null)
                {
                    tabMain.SelectedIndexChanged -= TabMain_SelectedIndexChanged_ResetHotkeys;
                    tabMain.SelectedIndexChanged += TabMain_SelectedIndexChanged_ResetHotkeys;
                }
            }
            catch { }

            labelCaptureModeInactive.Text = "Capture mode inactive." + Environment.NewLine + Environment.NewLine +
                "Click outside input field" + Environment.NewLine + "to reactivate capture mode.";
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

        // ---------------------------
        // NEW: Resume when clicking anywhere outside the input editors on the Capturing tab
        // ---------------------------

        private void WireResumeOnClickOutsideEditors()
        {
            if (tabCapturing == null) return;

            // Clicking the empty area of the tab
            tabCapturing.MouseDown -= Control_MouseDown_ResumeIfNotEditor;
            tabCapturing.MouseDown += Control_MouseDown_ResumeIfNotEditor;

            // Clicking any existing non-editor child control
            HookMouseDownRecursively(tabCapturing);

            // Also cover dynamically added controls (labels for variables, etc.)
            tabCapturing.ControlAdded -= TabCapturing_ControlAdded_HookMouse;
            tabCapturing.ControlAdded += TabCapturing_ControlAdded_HookMouse;
        }

        private void TabCapturing_ControlAdded_HookMouse(object sender, ControlEventArgs e)
        {
            HookMouseDownRecursively(e.Control);
        }

        private void HookMouseDownRecursively(Control root)
        {
            if (root == null) return;

            foreach (Control c in root.Controls)
            {
                // Attach to non-editor controls only
//                if (!IsCaptureEditor(c))
                // Skip if it's an editor, or a Button (or you can broaden to any control needing its own Click)
                if (!IsCaptureEditor(c) && !(c is Button) && !(c is NumericUpDown))
                {
                    c.MouseDown -= Control_MouseDown_ResumeIfNotEditor;
                    c.MouseDown += Control_MouseDown_ResumeIfNotEditor;
                }

                if (c.HasChildren)
                    HookMouseDownRecursively(c);
            }
        }

        private void Control_MouseDown_ResumeIfNotEditor(object sender, MouseEventArgs e)
        {
            try
            {
                var ctrl = sender as Control;
                if (ctrl == null) return;

                // Ignore clicks inside input editors; resume otherwise
                if (!IsCaptureEditor(ctrl))
                {
                    // Reuse your existing helper
                    removeFocus();
                }
            }
            catch { /* best-effort */ }
        }

        private bool IsCaptureEditor(Control c)
        {
            if (c == null) return false;

            // Filename format editor
            if (ReferenceEquals(c, _tbFilenameFormat)) return true;
            if (c is TextBox && ((c.Name ?? "").Equals("textBoxFilenameFormat", StringComparison.OrdinalIgnoreCase)
                              || (c.Name ?? "").Equals("textBox1", StringComparison.OrdinalIgnoreCase)))
                return true;

            // NUMBER numeric up/down
            if (ReferenceEquals(c, numericUpDown1)) return true;

            // Dynamic variable value editors ("varTxt*")
            if (c is TextBox && (c.Name ?? string.Empty).StartsWith(VarTextPrefix, StringComparison.OrdinalIgnoreCase))
                return true;

            return false;
        }
    }
}