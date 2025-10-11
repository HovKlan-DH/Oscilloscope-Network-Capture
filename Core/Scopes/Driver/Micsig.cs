#if !DISABLE_MICSIG
using Oscilloscope_Network_Capture.Core.Scopes.Attributes;
using Oscilloscope_Network_Capture.Core.Transport;

namespace Oscilloscope_Network_Capture.Core.Scopes.Implementations
{
    [ScopeDriver("Micsig", "*")]
    public sealed class Micsig : ScpiScopeBase
    {
        public Micsig() : base(new SocketInstrumentTransport())
        {
            Vendor = "Micsig";
            Model = "*";
        }
    }
}
#endif