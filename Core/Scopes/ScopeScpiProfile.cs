using System.Collections.Generic;

namespace Oscilloscope_Network_Capture.Core.Scopes
{
    public sealed class ScopeScpiProfile
    {
        public string Vendor { get; }
        public string ModelPattern { get; }

        private readonly Dictionary<ScopeCommand, string> _commands = new Dictionary<ScopeCommand, string>();

        public ScopeScpiProfile(string vendor, string modelPattern = "*")
        {
            Vendor = vendor;
            ModelPattern = string.IsNullOrWhiteSpace(modelPattern) ? "*" : modelPattern;
        }

        public ScopeScpiProfile Map(ScopeCommand cmd, string scpi)
        {
            _commands[cmd] = scpi;
            return this;
        }

        public bool TryGet(ScopeCommand cmd, out string scpi) => _commands.TryGetValue(cmd, out scpi);
    }
}
