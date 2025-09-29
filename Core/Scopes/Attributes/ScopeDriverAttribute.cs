using System;

namespace Oscilloscope_Network_Capture.Core.Scopes.Attributes
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class ScopeDriverAttribute : Attribute
    {
        public string Vendor { get; }
        public string ModelPattern { get; }

        public ScopeDriverAttribute(string vendor, string modelPattern = "*")
        {
            Vendor = vendor ?? "Unknown";
            ModelPattern = string.IsNullOrWhiteSpace(modelPattern) ? "*" : modelPattern;
        }
    }
}
