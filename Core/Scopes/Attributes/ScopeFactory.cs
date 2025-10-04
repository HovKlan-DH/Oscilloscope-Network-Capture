using Oscilloscope_Network_Capture.Core.Scopes;
using Oscilloscope_Network_Capture.Core.Scopes.Attributes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Oscilloscope_Network_Capture.Core.Scopes
{
    public static class ScopeFactory
    {
        private static readonly object _sync = new object();
        private static bool _initialized;
        private static readonly List<Type> _scopeTypes = new List<Type>();

        public static IEnumerable<ScopeDescriptor> GetAvailableScopes()
        {
            EnsureLoaded();
            foreach (var type in _scopeTypes)
            {
                foreach (var attr in type.GetCustomAttributes(typeof(ScopeDriverAttribute), false).Cast<ScopeDriverAttribute>())
                {
                    yield return new ScopeDescriptor(attr.Vendor, attr.ModelPattern);
                }
            }
        }

        public static IScope Create(string vendor, string model, string resource)
        {
            EnsureLoaded();

            foreach (var type in _scopeTypes)
            {
                var attrs = type.GetCustomAttributes(typeof(ScopeDriverAttribute), false).Cast<ScopeDriverAttribute>();
                foreach (var attr in attrs)
                {
                    if (!attr.Vendor.Equals(vendor, StringComparison.OrdinalIgnoreCase))
                        continue;
                    if (!ModelMatches(attr.ModelPattern, model))
                        continue;

                    var scope = (IScope)Activator.CreateInstance(type);
                    scope.GetType().GetProperty("Vendor")?.SetValue(scope, vendor, null);
                    scope.GetType().GetProperty("Model")?.SetValue(scope, model, null);
                    scope.Resource = resource;
                    return scope;
                }
            }

            throw new InvalidOperationException($"No scope driver found for {vendor} {model}.");
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

        private static void EnsureLoaded()
        {
            if (_initialized) return;
            lock (_sync)
            {
                if (_initialized) return;

                LoadFromAssemblies(AppDomain.CurrentDomain.GetAssemblies());

                var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                var scopesDir = Path.Combine(baseDir, "Scopes");
                if (Directory.Exists(scopesDir))
                {
                    foreach (var dll in Directory.EnumerateFiles(scopesDir, "*.dll"))
                    {
                        try
                        {
                            var asm = Assembly.LoadFrom(dll);
                            LoadFromAssemblies(new[] { asm });
                        }
                        catch
                        {
                        }
                    }
                }

                _initialized = true;
            }
        }

        private static void LoadFromAssemblies(IEnumerable<Assembly> assemblies)
        {
            foreach (var asm in assemblies)
            {
                Type[] types = Type.EmptyTypes;
                try { types = asm.GetTypes(); } catch { }
                foreach (var t in types)
                {
                    if (t.IsAbstract || t.IsInterface) continue;
                    if (!typeof(IScope).IsAssignableFrom(t)) continue;
                    if (t.GetCustomAttributes(typeof(ScopeDriverAttribute), false).Any())
                    {
                        _scopeTypes.Add(t);
                    }
                }
            }
        }
    }
}
