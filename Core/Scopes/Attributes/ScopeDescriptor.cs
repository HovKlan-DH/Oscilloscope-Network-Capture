namespace Oscilloscope_Network_Capture.Core.Scopes
{
    public sealed class ScopeDescriptor
    {
        public string Vendor { get; }
        public string Model { get; }

        public ScopeDescriptor(string vendor, string model)
        {
            Vendor = vendor;
            Model = model;
        }

        public override string ToString() => $"{Vendor} {Model}";
    }
}
