using Oscilloscope_Network_Capture.Core.Scopes.Attributes;
using Oscilloscope_Network_Capture.Core.Transport;

namespace Oscilloscope_Network_Capture.Core.Scopes.Implementations
{
    [ScopeDriver("Siglent", "*")]
    public sealed class Siglent : ScpiScopeBase
    {
        public Siglent() : base(new SocketInstrumentTransport())
        {
            Vendor = "Siglent";
            Model = "*";
        }
    }
}
