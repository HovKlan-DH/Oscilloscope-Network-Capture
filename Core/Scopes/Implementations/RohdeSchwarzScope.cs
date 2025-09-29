using Oscilloscope_Network_Capture.Core.Scopes.Attributes;
using Oscilloscope_Network_Capture.Core.Transport;

namespace Oscilloscope_Network_Capture.Core.Scopes.Implementations
{
    [ScopeDriver("Rohde & Schwarz", "*")]
    public sealed class RohdeSchwarzScope : ScpiScopeBase
    {
        public RohdeSchwarzScope() : base(new SocketInstrumentTransport())
        {
            Vendor = "Rohde & Schwarz";
            Model = "*";
        }
    }
}
