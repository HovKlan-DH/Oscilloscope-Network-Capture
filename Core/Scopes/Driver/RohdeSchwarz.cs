using Oscilloscope_Network_Capture.Core.Scopes.Attributes;
using Oscilloscope_Network_Capture.Core.Transport;

namespace Oscilloscope_Network_Capture.Core.Scopes.Implementations
{
    [ScopeDriver("Rohde & Schwarz", "*")]
    public sealed class RohdeSchwarz : ScpiScopeBase
    {
        public RohdeSchwarz() : base(new SocketInstrumentTransport())
        {
            Vendor = "Rohde & Schwarz";
            Model = "*";
        }
    }
}
