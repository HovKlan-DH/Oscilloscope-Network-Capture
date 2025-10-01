using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Oscilloscope_Network_Capture.Core.Scopes
{
    public static class ScpiProfileRegistry
    {
        private static readonly List<ScopeScpiProfile> _profiles = new List<ScopeScpiProfile>();

        // NEW: time/div profiles (seconds per division)
        private static readonly List<TimeDivEntry> _timeDivs = new List<TimeDivEntry>();

        private static bool _initialized;
        private static readonly object _sync = new object();      

        private static void Ensure()
        {
            if (_initialized) return;
            lock (_sync)
            {
                if (_initialized) return;

                // -------------------------------------------------
                // ----------------- SCPI COMMANDS -----------------
                // -------------------------------------------------

                // RIGOL defaults
                _profiles.Add(new ScopeScpiProfile("Rigol")
                    .Map(ScopeCommand.Identify, "*IDN?")
                    .Map(ScopeCommand.ClearStatistics, ":CLEAR")
                    .Map(ScopeCommand.QueryActiveTrigger, ":TRIGGER:STATUS?")
                    .Map(ScopeCommand.Stop, ":STOP")
                    .Map(ScopeCommand.Run, ":RUN")
                    .Map(ScopeCommand.Single, ":SINGLE")
                    .Map(ScopeCommand.QueryTriggerMode, ":TRIGGER:MODE?")
                    .Map(ScopeCommand.QueryTriggerLevel, ":TRIGGER:EDGE:LEVEL?")
                    .Map(ScopeCommand.SetTriggerLevel, ":TRIGGER:EDGE:LEVEL {0}")
                    .Map(ScopeCommand.QueryTimeDiv, ":TIMEBASE:SCALE?")
                    .Map(ScopeCommand.SetTimeDiv, ":TIMEBASE:SCALE {0}")
                    .Map(ScopeCommand.DumpImage, ":DISPLAY:DATA?")
                    .Map(ScopeCommand.PopLastSystemError, ":SYSTEM:ERROR?")
                    .Map(ScopeCommand.OperationComplete, "*OPC?"));

                // Rigol MSO2000A/DS2000A Series overrides
                _profiles.Add(new ScopeScpiProfile("Rigol", "MSO2000A/DS2000A Series")
                    .Map(ScopeCommand.ClearStatistics, ":MEASURE:STATISTIC:RESET"));

                // SIGLENT defaults
                _profiles.Add(new ScopeScpiProfile("Siglent")
                    .Map(ScopeCommand.Identify, "*IDN?")
                    .Map(ScopeCommand.ClearStatistics, "*CLS")
                    .Map(ScopeCommand.QueryActiveTrigger, "TRIG_MODE?")
                    .Map(ScopeCommand.Stop, "STOP")
                    .Map(ScopeCommand.Run, "TRIG_MODE AUTO")
                    .Map(ScopeCommand.Single, "TRIG_MODE SINGLE")
                    .Map(ScopeCommand.QueryTriggerMode, "TRIG_SELECT?")
                    .Map(ScopeCommand.QueryTriggerLevel, "TRIG_LEVEL?")
                    .Map(ScopeCommand.SetTriggerLevel, "TRIG_LEVEL {0}")
                    .Map(ScopeCommand.QueryTimeDiv, "TIME_DIV?")
                    .Map(ScopeCommand.SetTimeDiv, "TIME_DIV {0}")
                    .Map(ScopeCommand.DumpImage, "SCREEN_DUMP")
                    .Map(ScopeCommand.PopLastSystemError, ":SYST:ERR?")
                    .Map(ScopeCommand.OperationComplete, "*OPC?"));

                // Placeholders for Keysight/Agilent/R&S for later
                _profiles.Add(new ScopeScpiProfile("Keysight"));
                _profiles.Add(new ScopeScpiProfile("Agilent")); // historically Agilent -> Keysight
                _profiles.Add(new ScopeScpiProfile("Rohde & Schwarz"));

                // -------------------------------------------------
                // ---------------- TIME/DIV VALUES ----------------
                // -------------------------------------------------

                // Rigol generic TIME/DIV values (seconds per division)
                _timeDivs.Add(new TimeDivEntry("Rigol", "*", new[]
                {
                    "2e-9", "5e-9", "10e-9", "20e-9", "50e-9",
                    "100e-9", "200e-9", "500e-9", "1e-6", "2e-6", "5e-6",
                    "10e-6", "20e-6", "50e-6", "100e-6", "200e-6", "500e-6",
                    "1e-3", "2e-3", "5e-3", "10e-3", "20e-3", "50e-3",
                    "100e-3", "200e-3", "500e-3", "1", "2", "5", "10", "20", "50", "100",
                    "200", "500", "1000"
                }));
                _timeDivs.Add(new TimeDivEntry("Rigol", "MSO2000A/DS2000A Series", new[]
                {
                    "2nS", "5nS", "10nS", "20nS", "50nS",
                    "100nS", "200nS", "500nS", "1uS", "2uS", "5uS",
                    "10uS", "20uS", "50uS", "100uS", "200uS", "500uS",
                    "1mS", "2mS", "5mS", "10mS", "20mS", "50mS",
                    "100mS", "200mS", "500mS", "1S", "2S", "5S", "10S", "20S", "50S", "100S",
                    "200", "500", "1000"
                }));
                _timeDivs.Add(new TimeDivEntry("Siglent", "*", new[]
                {
                    "2nS", "100nS", "10mS", "20mS", "10uS",
                    "20uS", "10mS", "20mS", "1S", "10S", "500S"
                }));

                // Example: Rigol DS2202A could override the above if needed
                // _timeDivs.Add(new TimeDivEntry("Rigol", "DS2202A", new[]{ ... }));

                // Siglent generic placeholder (1-2-5 decades; replace with real values if different)
                _timeDivs.Add(new TimeDivEntry("Siglent", "*", Generate125Sequence(1e-9, 50)));

                // Keysight/Agilent/R&S placeholders until sequences are known
                _timeDivs.Add(new TimeDivEntry("Keysight", "*", Generate125Sequence("1NS", "2NS", "5NS", "10NS", "20NS", "50NS", "100NS", "200NS", "500NS", "1US", "2US", "5US", "10US", "20US", "50US", "100US", "200US", "500US", "1MS", "2MS", "5MS", "10MS", "20MS", "50MS", "100MS", "200MS", "500MS", "1S", "2S", "5S", "10S", "20S", "50S")));
                _timeDivs.Add(new TimeDivEntry("Agilent", "*", Generate125Sequence("1NS", "2NS", "5NS", "10NS", "20NS", "50NS", "100NS", "200NS", "500NS", "1US", "2US", "5US", "10US", "20US", "50US", "100US", "200US", "500US", "1MS", "2MS", "5MS", "10MS", "20MS", "50MS", "100MS", "200MS", "500MS", "1S", "2S", "5S", "10S", "20S", "50S")));
                _timeDivs.Add(new TimeDivEntry("Rohde & Schwarz", "*", Generate125Sequence(1e-9, 50)));

                _initialized = true;
            }
        }

        // Helper: format doubles as compact scientific tokens like "2e-9"
        private static string FormatDoubleAsToken(double v)
        {
            var s = v.ToString("G17", CultureInfo.InvariantCulture).ToLowerInvariant();
            s = Regex.Replace(s, @"e([+-])0+(\d+)", "e$1$2"); // e-09 -> e-9
            return s;
        }

        public static IEnumerable<ScopeScpiProfile> Profiles
        {
            get { Ensure(); return _profiles; }
        }

        // NEW: apply a runtime override for TIME/DIV tokens for a specific vendor+model.
        // Pass null/empty text to clear the override (reverts to defaults).
        public static void SetTimeDivTextOverride(string vendor, string model, string rawCsv)
        {
            Ensure();
            lock (_sync)
            {
                var keyExact = MakeOverrideKey(vendor, model);

                // Build tokens from CSV (may be empty)
                var tokens = (rawCsv ?? string.Empty)
                    .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(t => (t ?? string.Empty).Trim())
                    .Where(t => !string.IsNullOrWhiteSpace(t))
                    .ToList();

                // Always install an override entry, even if empty (disables stepping to defaults)
                var entry = new TimeDivEntry(vendor ?? string.Empty, string.IsNullOrWhiteSpace(model) ? "*" : model, tokens);

                TimeDivOverrides[keyExact] = entry;
            }
        }

        // NEW: returns raw tokens (prefer overrides; else defaults)
        public static IReadOnlyList<string> GetTimeDivTextValues(string vendor, string model)
        {
            Ensure();
            var td = FindTimeDivOverrideEntry(vendor, model) ?? FindTimeDivEntry(vendor, model);
            if (td == null)
                return Array.Empty<string>();
            return td.Tokens ?? (td.Values ?? Array.Empty<double>()).Select(FormatDoubleAsToken).ToArray();
        }

        public static ScopeScpiProfile Find(string vendor, string model)
        {
            Ensure();
            // Try most specific first (model match), then vendor default
            var matches = _profiles.Where(p => p.Vendor.Equals(vendor ?? string.Empty, StringComparison.OrdinalIgnoreCase))
                                   .OrderByDescending(p => Specificity(p.ModelPattern));
            foreach (var p in matches)
            {
                if (ModelMatches(p.ModelPattern, model))
                    return p;
            }
            return null;
        }

        // REPLACE: prefer overrides when returning numeric seconds list
        public static IReadOnlyList<double> GetTimeDivValues(string vendor, string model)
        {
            Ensure();
            var td = FindTimeDivOverrideEntry(vendor, model) ?? FindTimeDivEntry(vendor, model);
            return td?.Values ?? Array.Empty<double>();
        }

        // REPLACE: prefer overrides for next
        public static bool TryGetNextTimeDiv(string vendor, string model, double current, out double next)
        {
            Ensure();
            var td = FindTimeDivOverrideEntry(vendor, model) ?? FindTimeDivEntry(vendor, model);
            if (td == null || td.Values.Count == 0) { next = current; return false; }
            return TryGetNext(td.Values, current, out next);
        }

        // REPLACE: prefer overrides for previous
        public static bool TryGetPrevTimeDiv(string vendor, string model, double current, out double prev)
        {
            Ensure();
            var td = FindTimeDivOverrideEntry(vendor, model) ?? FindTimeDivEntry(vendor, model);
            if (td == null || td.Values.Count == 0) { prev = current; return false; }
            return TryGetPrev(td.Values, current, out prev);
        }

        // NEW: lookup runtime override (exact vendor+model, then vendor+* only if no specific default exists)
        private static TimeDivEntry FindTimeDivOverrideEntry(string vendor, string model)
        {
            lock (_sync)
            {
                var keyExact = MakeOverrideKey(vendor, model);
                if (TimeDivOverrides.TryGetValue(keyExact, out var e) && e != null)
                    return e;

                // If there is a specific default entry for this vendor+model, do NOT use the vendor-generic override.
                bool hasSpecificDefault = _timeDivs.Any(d =>
                    d.Vendor.Equals(vendor ?? string.Empty, StringComparison.OrdinalIgnoreCase) &&
                    ModelMatches(d.ModelPattern, model) &&
                    !string.Equals(d.ModelPattern, "*", StringComparison.Ordinal));

                if (!hasSpecificDefault)
                {
                    var keyGeneric = MakeOverrideKey(vendor, "*");
                    if (TimeDivOverrides.TryGetValue(keyGeneric, out e) && e != null)
                        return e;
                }

                return null;
            }
        }

        // NEW: stable key for override map
        private static string MakeOverrideKey(string vendor, string model)
        {
            return (vendor ?? string.Empty).Trim().ToUpperInvariant()
                 + "|"
                 + (string.IsNullOrWhiteSpace(model) ? "*" : model.Trim().ToUpperInvariant());
        }

        // NEW: holder for runtime overrides
        private static readonly Dictionary<string, TimeDivEntry> TimeDivOverrides
            = new Dictionary<string, TimeDivEntry>(StringComparer.OrdinalIgnoreCase);

        private static int Specificity(string pattern)
        {
            if (string.IsNullOrEmpty(pattern) || pattern == "*") return 0;
            return pattern.Replace("*", string.Empty).Length;
        }

        private static bool ModelMatches(string pattern, string model)
        {
            if (string.IsNullOrEmpty(pattern) || pattern == "*") return true;
            if (string.IsNullOrEmpty(model)) return false;
            pattern = pattern.ToLowerInvariant();
            model = model.ToLowerInvariant();
            if (pattern.StartsWith("*") && pattern.EndsWith("*"))
                return model.Contains(pattern.Trim('*'));
            if (pattern.StartsWith("*"))
                return model.EndsWith(pattern.TrimStart('*'));
            if (pattern.EndsWith("*"))
                return model.StartsWith(pattern.TrimEnd('*'));
            return model.Equals(pattern);
        }

        // ---------------- TIME/DIV helpers ----------------

        // Parses "1NS", "2 US", "5ms", "1e-3 s" ? seconds (double)
        private static bool TryParseTimeToSeconds(string text, out double seconds)
        {
            seconds = 0;
            if (string.IsNullOrWhiteSpace(text)) return false;

            var m = Regex.Match(text, @"^\s*([+-]?(?:\d+\.?\d*|\d*\.?\d+)(?:[eE][+-]?\d+)?)\s*([a-zA-Zµ?]*)\s*$");
            if (!m.Success) return false;

            if (!double.TryParse(m.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
                return false;

            var unit = (m.Groups[2].Value ?? string.Empty).Trim().ToUpperInvariant();

            double scale;
            switch (unit)
            {
                case "":      // no unit => seconds
                case "S":
                case "SEC":
                case "SECS":
                case "SECOND":
                case "SECONDS":
                    scale = 1.0; break;

                case "MS":
                case "MSEC":
                case "MSECS":
                case "MILLISECOND":
                case "MILLISECONDS":
                    scale = 1e-3; break;

                case "US":
                case "USEC":
                case "USECS":
                case "MICROSECOND":
                case "MICROSECONDS":
                case "?S":    // Greek mu uppercase fallback
                case "µS":    // micro sign + S
                    scale = 1e-6; break;

                case "NS":
                case "NSEC":
                case "NSECS":
                case "NANOSECOND":
                case "NANOSECONDS":
                    scale = 1e-9; break;

                default:
                    return false;
            }

            seconds = value * scale;
            return true;
        }

        private static TimeDivEntry FindTimeDivEntry(string vendor, string model)
        {
            var matches = _timeDivs
                .Where(p => p.Vendor.Equals(vendor ?? string.Empty, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(p => Specificity(p.ModelPattern));
            foreach (var p in matches)
            {
                if (ModelMatches(p.ModelPattern, model)) return p;
            }
            return null;
        }

        private static bool TryGetNext(IReadOnlyList<double> values, double current, out double next, double tol = 1e-12)
        {
            next = current;
            for (int i = 0; i < values.Count; i++)
            {
                var v = values[i];
                if (IsClose(v, current, tol))
                {
                    if (i + 1 < values.Count) { next = values[i + 1]; return true; }
                    return false;
                }
                if (v > current + tol)
                {
                    next = v; return true;
                }
            }
            return false;
        }

        private static bool TryGetPrev(IReadOnlyList<double> values, double current, out double prev, double tol = 1e-12)
        {
            prev = current;
            for (int i = 0; i < values.Count; i++)
            {
                var v = values[i];
                if (IsClose(v, current, tol))
                {
                    if (i - 1 >= 0) { prev = values[i - 1]; return true; }
                    return false;
                }
                if (v > current + tol)
                {
                    var j = i - 1;
                    if (j >= 0) { prev = values[j]; return true; }
                    return false;
                }
            }
            // current is above all -> previous is last
            if (values.Count > 0) { prev = values[values.Count - 1]; return true; }
            return false;
        }

        private static bool IsClose(double a, double b, double tol)
            => Math.Abs(a - b) <= Math.Max(tol, 1e-9 * Math.Max(Math.Abs(a), Math.Abs(b)));

        private static IReadOnlyList<double> Generate125Sequence(double minSeconds, double maxSeconds)
        {
            if (minSeconds <= 0) minSeconds = 1e-12;
            if (maxSeconds < minSeconds) maxSeconds = minSeconds;
            var list = new List<double>();
            var mantissas = new[] { 1.0, 2.0, 5.0 };

            // starting decade at or below min
            var decade = Math.Pow(10, Math.Floor(Math.Log10(minSeconds)));

            // ensure first value is >= min
            while (decade * mantissas[mantissas.Length - 1] < minSeconds)
                decade *= 10;

            for (;;)
            {
                foreach (var m in mantissas)
                {
                    var v = m * decade;
                    if (v + v * 1e-12 < minSeconds) continue;
                    if (v > maxSeconds * (1 + 1e-12)) return list;
                    list.Add(v);
                }
                decade *= 10;
            }
        }

        // Overload: build a sorted list from explicit time tokens like "1NS", "2US", ...
        private static IReadOnlyList<double> Generate125Sequence(params string[] values)
        {
            var list = new List<double>(values?.Length ?? 0);
            if (values != null)
            {
                foreach (var t in values)
                {
                    if (TryParseTimeToSeconds(t, out var s))
                        list.Add(s);
                }
            }
            list.Sort();
            return list;
        }

        // NEW: simple container for TIME/DIV grids
        private sealed class TimeDivEntry
        {
            public string Vendor { get; }
            public string ModelPattern { get; }

            private readonly List<double> _values;
            private readonly List<string> _tokens; // raw tokens for UI

            public IReadOnlyList<double> Values => _values;
            public IReadOnlyList<string> Tokens => _tokens;

            // Construct from numeric seconds list; auto-generate display tokens
            public TimeDivEntry(string vendor, string modelPattern, IEnumerable<double> values)
            {
                Vendor = vendor ?? string.Empty;
                ModelPattern = string.IsNullOrWhiteSpace(modelPattern) ? "*" : modelPattern;

                var vals = (values ?? Enumerable.Empty<double>()).ToList();
                _values = vals.OrderBy(v => v).ToList();
                _tokens = vals.Select(FormatDoubleAsToken).ToList();
            }

            // Construct from tokens; preserve tokens "as-is" for UI, compute numeric list for logic/sending
            public TimeDivEntry(string vendor, string modelPattern, IEnumerable<string> tokens)
            {
                Vendor = vendor ?? string.Empty;
                ModelPattern = string.IsNullOrWhiteSpace(modelPattern) ? "*" : modelPattern;

                var toks = (tokens ?? Enumerable.Empty<string>()).ToList();
                _tokens = toks; // exact as provided

                var parsed = new List<double>(toks.Count);
                foreach (var t in toks)
                {
                    if (TryParseTimeToSeconds(t, out var s))
                        parsed.Add(s);
                }
                _values = parsed.OrderBy(v => v).ToList();
            }
        }
    }
}
