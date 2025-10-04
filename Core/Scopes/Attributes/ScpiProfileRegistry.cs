using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Oscilloscope_Network_Capture.Core.Scopes
{
    public static partial class ScpiProfileRegistry
    {
        private static readonly List<ScopeScpiProfile> _profiles = new List<ScopeScpiProfile>();
        private static readonly List<TimeDivEntry> _timeDivs = new List<TimeDivEntry>();
        private static bool _initialized;
        private static readonly object _sync = new object();

        private static void Ensure()
        {
            if (_initialized) return;
            lock (_sync)
            {
                if (_initialized) return;

                // Delegate all vendor/series registration to partial methods
                RegisterKeysightData();
                RegisterRigolData();
                RegisterRohdeSchwarzData();
                RegisterSiglentData();

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

            for (; ; )
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

        // Create identical SCPI profiles for multiple series in one go
        private static void AddSeriesProfiles(string vendor, Action<ScopeScpiProfile> map, params string[] modelSeries)
        {
            if (modelSeries == null || map == null) return;
            foreach (var series in modelSeries)
            {
                if (string.IsNullOrWhiteSpace(series)) continue;
                var profile = new ScopeScpiProfile(vendor, series);
                map(profile);
                _profiles.Add(profile);
            }
        }

        // Duplicate the same TIME/DIV token list across multiple series
        private static void AddTimeDivTokens(string vendor, IEnumerable<string> tokens, params string[] modelSeries)
        {
            if (modelSeries == null) return;
            var tokList = (tokens ?? Enumerable.Empty<string>()).ToArray();
            foreach (var series in modelSeries)
            {
                if (string.IsNullOrWhiteSpace(series)) continue;
                _timeDivs.Add(new TimeDivEntry(vendor, series, tokList));
            }
        }

        // Optional: numeric TIME/DIV duplication across series
        private static void AddTimeDivValues(string vendor, IEnumerable<double> values, params string[] modelSeries)
        {
            if (modelSeries == null) return;
            var vals = (values ?? Enumerable.Empty<double>()).ToArray();
            foreach (var series in modelSeries)
            {
                if (string.IsNullOrWhiteSpace(series)) continue;
                _timeDivs.Add(new TimeDivEntry(vendor, series, vals));
            }
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

        // Vendor data registration hooks (implemented in partials)
        static partial void RegisterKeysightData();
        static partial void RegisterRigolData();
        static partial void RegisterRohdeSchwarzData();
        static partial void RegisterSiglentData();
    }
}
