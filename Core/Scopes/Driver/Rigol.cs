using Oscilloscope_Network_Capture.Core.Scopes.Attributes;
using Oscilloscope_Network_Capture.Core.Transport;

namespace Oscilloscope_Network_Capture.Core.Scopes.Implementations
{
    [ScopeDriver("Rigol", "*")]
    public sealed class Rigol : ScpiScopeBase
    {
        public Rigol() : base(new SocketInstrumentTransport())
        {
            Vendor = "Rigol";
            Model = "*";
        }
    }
}
