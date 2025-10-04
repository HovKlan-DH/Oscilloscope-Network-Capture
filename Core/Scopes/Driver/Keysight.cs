using Oscilloscope_Network_Capture.Core.Scopes.Attributes;
using Oscilloscope_Network_Capture.Core.Transport;

namespace Oscilloscope_Network_Capture.Core.Scopes.Implementations
{
    [ScopeDriver("Keysight", "*")]
    public sealed class Keysight : ScpiScopeBase
    {
        public Keysight() : base(new SocketInstrumentTransport())
        {
            Vendor = "Keysight";
            Model = "*";
        }
    }
}
