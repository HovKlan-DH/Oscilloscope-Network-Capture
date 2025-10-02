using Oscilloscope_Network_Capture.Core.Scopes.Attributes;
using Oscilloscope_Network_Capture.Core.Transport;

namespace Oscilloscope_Network_Capture.Core.Scopes.Implementations
{
    [ScopeDriver("Keysight", "*")]
    public sealed class KeysightScope : ScpiScopeBase
    {
        public KeysightScope() : base(new SocketInstrumentTransport())
        {
            Vendor = "Keysight";
            Model = "*";
        }
    }
}
