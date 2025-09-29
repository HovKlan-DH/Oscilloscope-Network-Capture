using System;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Oscilloscope_Network_Capture.Core
{
    // Simple keyboard helper to centralize capture-mode key handling
    public static class Keyboard
    {
        public static void HandleCaptureModeKeyDown(bool captureMode, KeyEventArgs e, Func<Task> captureAction, Action exitAction)
        {
            if (!captureMode) return;
            if (e == null) return;

            if (e.KeyCode == Keys.Enter)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                _ = (captureAction?.Invoke());
            }
            else if (e.KeyCode == Keys.Escape)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                exitAction?.Invoke();
            }
        }
    }
}
