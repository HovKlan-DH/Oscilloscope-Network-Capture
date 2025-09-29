using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace Oscilloscope_Network_Capture.Core.Configuration
{
    [Serializable]
    public class AppConfiguration
    {
        public bool EnableDelete { get; set; } = false; // default off (optional)
        public string AdjustToGridStep { get; set; }

        // Selected driver
        public string Vendor { get; set; }
        public string Model { get; set; }

        // Optional contact email (for feedback)
        public string Email { get; set; } = string.Empty;

        // Misc behavior options
        public bool EnableBeep { get; set; } = true;
        public bool ForceAcquisition { get; set; } = false;
        public bool ForceClear { get; set; } = false;
        public bool DeleteDoubleUnderscore { get; set; } = true;
        public bool TrimUnderscore { get; set; } = true; // new: delete underscore/whitespace at start/end before saving
        public int DelayMs { get; set; } = -1; // -1 means "unset"; use UI designer default on first run

        // Window layout
        public bool WindowMaximized { get; set; } = false;
        public int WindowWidth { get; set; } = 0;
        public int WindowHeight { get; set; } = 0;

        // File capture
        public string CaptureFolder { get; set; } = string.Empty;
        public string FilenameFormat { get; set; } = string.Empty; // persisted format from textBox1

        // Measurements variables
        public int VariableCount { get; set; } = 0; // 0..5
        public List<string> VariableNames { get; set; } = new List<string>(); // indexes 0..VariableCount-1
        public List<string> VariableValues { get; set; } = new List<string>(); // indexes 0..VariableCount-1
        public string NumberValue { get; set; } = "0"; // persisted value of numericUpDown1 (variable name: NUMBER)

        // Do not persist these legacy/internal fields
        [XmlIgnore]
        public string LastResource { get; set; }
        [XmlIgnore]
        public int LogLevel { get; set; } = 20; // Info
        [XmlIgnore]
        public List<string> RecentResources { get; set; } = new List<string>();
        [XmlIgnore]
        public int RecentResourcesMax { get; set; } = 10;

        // Default connection values
        public string ScopeIp { get; set; } = "192.168.0.100";
        public int ScopePort { get; set; } = 5025;

        // Persist last selected tab index
        public int LastTabIndex { get; set; } = 0;

        // SCPI overrides per vendor+model
        public List<ScpiProfileOverride> ScpiProfiles { get; set; } = new List<ScpiProfileOverride>();

        // Overridable SCPI test suites (ordered command names)
        public List<ScpiTestSuiteOverride> ScpiTestSuites { get; set; } = new List<ScpiTestSuiteOverride>();
    }

    [Serializable]
    public class ScpiProfileOverride
    {
        public string Vendor { get; set; }
        public string Model { get; set; } // pattern, e.g. "*" or specific model
        public List<ScpiCommandOverride> Overrides { get; set; } = new List<ScpiCommandOverride>();
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
