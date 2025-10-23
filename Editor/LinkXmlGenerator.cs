using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace Insthync.PerformanceImprovementTools
{
    public static class LinkXmlGenerator
    {
        private static readonly string[] ReflectionPatterns = new[]
        {
            "Type.GetType(",
            "System.Type.GetType(",
            "Activator.CreateInstance(",
            ".GetMethod(",
            ".GetProperty(",
            ".GetField(",
            ".Invoke(",
            "Assembly.Load(",
            "Assembly.GetType(",
            "InvokeMember(",
            "GetValue(",
            "SetValue(",
            "JsonConvert.",
            "FromJson<",
            "Addressables.",
            "CreateInstance(",
        };

        private static readonly Regex nsRegex = new Regex(@"^\s*namespace\s+([A-Za-z0-9_.]+)", RegexOptions.Compiled);

        [MenuItem("Tools/Performance Tools/Generate link.xml From Reflection Usage")]
        public static void GenerateLinkXml()
        {
            try
            {
                Debug.Log("Scanning scripts for reflection usage...");

                string[] guids = AssetDatabase.FindAssets("t:MonoScript");
                var assemblyToNamespaces = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
                var matches = new List<string>();

                foreach (string guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    MonoScript script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
                    if (script == null) continue;

                    // Use CompilationPipeline to get the exact assembly name
                    string assemblyName = CompilationPipeline.GetAssemblyNameFromScriptPath(path);

                    // Fallback for scripts not recognized by CompilationPipeline
                    if (string.IsNullOrEmpty(assemblyName))
                        assemblyName = path.Contains("/Editor/") ? "Assembly-CSharp-Editor" : "Assembly-CSharp";

                    // Strip any ".dll" suffix automatically
                    if (assemblyName.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                        assemblyName = assemblyName.Substring(0, assemblyName.Length - 4);

                    string[] lines = File.ReadAllLines(path);
                    string fileNamespace = null;
                    bool fileHasReflectionUse = false;

                    for (int i = 0; i < lines.Length; i++)
                    {
                        string line = lines[i];
                        if (string.IsNullOrWhiteSpace(line)) continue;

                        if (fileNamespace == null)
                        {
                            var m = nsRegex.Match(line);
                            if (m.Success) fileNamespace = m.Groups[1].Value;
                        }

                        if (line.TrimStart().StartsWith("//")) continue;

                        foreach (var pattern in ReflectionPatterns)
                        {
                            if (line.Contains(pattern))
                            {
                                fileHasReflectionUse = true;
                                string ns = string.IsNullOrEmpty(fileNamespace) ? "<global>" : fileNamespace;
                                matches.Add($"{path} (line {i + 1}), (namespace {ns}): {line.Trim()}");
                            }
                        }
                    }

                    if (fileHasReflectionUse)
                    {
                        string ns = string.IsNullOrEmpty(fileNamespace) ? "<global>" : fileNamespace;

                        if (!assemblyToNamespaces.TryGetValue(assemblyName, out var set))
                        {
                            set = new HashSet<string>();
                            assemblyToNamespaces[assemblyName] = set;
                        }
                        set.Add(ns);
                    }
                }

                var config = LinkXmlGeneratorConfig.GetConfig();

                // Always preserve known reflection-based assemblies (remove .dll if present)
                var alwaysPreserveAssemblies = config.alwaysPreserveAssemblies;

                string linkXmlPath = Path.Combine(Application.dataPath, "link.xml");
                using (var sw = new StreamWriter(linkXmlPath, false))
                {
                    sw.WriteLine("<linker>");

                    foreach (var a in alwaysPreserveAssemblies)
                    {
                        string cleanName = a.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) ? a.Substring(0, a.Length - 4) : a;
                        sw.WriteLine($"  <assembly fullname=\"{cleanName}\" preserve=\"all\" />");
                    }
                    sw.WriteLine();

                    foreach (var kv in assemblyToNamespaces.OrderBy(k => k.Key))
                    {
                        string cleanAssemblyName = kv.Key.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)
                            ? kv.Key.Substring(0, kv.Key.Length - 4)
                            : kv.Key;

                        if (config.IsExcludeAssemblyName(cleanAssemblyName))
                            continue;

                        sw.WriteLine($"  <assembly fullname=\"{cleanAssemblyName}\">");
                        foreach (var ns in kv.Value.OrderBy(x => x))
                        {
                            if (config.IsExcludeNamespace(ns))
                                continue;

                            if (ns == "<global>")
                                sw.WriteLine("    <!-- types in global namespace detected -->");
                            else
                                sw.WriteLine($"    <namespace fullname=\"{ns}\" preserve=\"all\" />");
                        }
                        sw.WriteLine("  </assembly>");
                    }

                    sw.WriteLine("</linker>");
                }

                string reportPath = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "ReflectionReport.txt");
                using (var sw = new StreamWriter(reportPath, false))
                {
                    sw.WriteLine($"Reflection Usage Report - {DateTime.Now}");
                    sw.WriteLine("Scanned scripts: " + guids.Length);
                    sw.WriteLine("Reflection usages found: " + matches.Count);
                    sw.WriteLine();
                    foreach (var line in matches)
                        sw.WriteLine(line);
                }

                AssetDatabase.Refresh();
                Debug.Log("link.xml generated at Assets/link.xml. ReflectionReport.txt generated at project root.");
                EditorUtility.RevealInFinder(linkXmlPath);
            }
            catch (Exception ex)
            {
                Debug.LogError("Error generating link.xml: " + ex);
            }
        }
    }
}
