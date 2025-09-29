using Oscilloscope_Network_Capture.Core.Scopes.Attributes;
using Oscilloscope_Network_Capture.Core.Transport;

namespace Oscilloscope_Network_Capture.Core.Scopes.Implementations
{
    [ScopeDriver("Siglent", "*")]
    public sealed class SiglentScope : ScpiScopeBase
    {
        public SiglentScope() : base(new SocketInstrumentTransport())
        {
            Vendor = "Siglent";
            Model = "*";
        }
    }
}
