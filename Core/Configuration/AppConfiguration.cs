using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace Oscilloscope_Network_Capture.Core.Configuration
{
    [Serializable]
    public class AppConfiguration
    {
        public bool EnableDelete { get; set; } = false;
        public string AdjustToGridStep { get; set; }

        public string Vendor { get; set; }
        public string Model { get; set; }

        public string Email { get; set; } = string.Empty;

        public bool EnableBeep { get; set; } = true;
        public bool ForceAcquisition { get; set; } = true;
        public bool ForceClear { get; set; } = false;
        public bool DoNotClearWhenStop { get; set; } = true;
        public bool DeleteDoubleUnderscore { get; set; } = true;
        public bool TrimUnderscore { get; set; } = true;
        public int DelayMs { get; set; } = -1;
        public bool MaskSerial { get; set; } = true;

        public bool WindowMaximized { get; set; } = false;
        public int WindowWidth { get; set; } = 0;
        public int WindowHeight { get; set; } = 0;

        public string CaptureFolder { get; set; } = string.Empty;
        public string FilenameFormat { get; set; } = string.Empty;

        public int VariableCount { get; set; } = 0;
        public List<string> VariableNames { get; set; } = new List<string>();
        public List<string> VariableValues { get; set; } = new List<string>();
        public string NumberValue { get; set; } = "0";

        [XmlIgnore]
        public string LastResource { get; set; }
        [XmlIgnore]
        public int LogLevel { get; set; } = 20;
        [XmlIgnore]
        public List<string> RecentResources { get; set; } = new List<string>();
        [XmlIgnore]
        public int RecentResourcesMax { get; set; } = 10;

        public string ScopeIp { get; set; } = "192.168.0.100";
        public int ScopePort { get; set; } = 5025;

        public int LastTabIndex { get; set; } = 0;

        // Important: reference the top-level type here
        public List<Oscilloscope_Network_Capture.Core.Configuration.ScpiProfileOverride> ScpiProfiles { get; set; }
            = new List<Oscilloscope_Network_Capture.Core.Configuration.ScpiProfileOverride>();

        public List<ScpiTestSuiteOverride> ScpiTestSuites { get; set; } = new List<ScpiTestSuiteOverride>();
    }

    [Serializable]
    public class ScpiProfileOverride
    {
        public string Vendor { get; set; }
        public string Model { get; set; } // pattern, e.g. "*" or specific model
        public List<ScpiCommandOverride> Overrides { get; set; } = new List<ScpiCommandOverride>();

        // Persisted TIME/DIV values text (e.g., "2NS, 5NS, 10NS, ...")
        public string TimeDivValues { get; set; }
    }

    [Serializable]
    public class ScpiCommandOverride
    {
        public string Command { get; set; } // ScopeCommand enum name
        public string Value { get; set; }
    }

    [Serializable]
    public class ScpiTestSuiteOverride
    {
        public string Id { get; set; } // suite id (enum name)
        public List<string> Steps { get; set; } = new List<string>(); // ordered ScopeCommand enum names
    }
}
