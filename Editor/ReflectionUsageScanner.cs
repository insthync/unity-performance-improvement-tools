using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

namespace Insthync.PerformanceImprovementTools
{
    public class ReflectionUsageScanner
    {
        private static readonly string[] ReflectionKeywords =
        {
        "System.Type::GetType",
        "System.Reflection.MethodInfo::Invoke",
        "System.Reflection.PropertyInfo::GetValue",
        "System.Reflection.PropertyInfo::SetValue",
        "System.Reflection.FieldInfo::GetValue",
        "System.Reflection.FieldInfo::SetValue",
        "System.Activator::CreateInstance",
        "System.Reflection.Assembly::Load",
        "System.Reflection.Assembly::GetType",
        "System.Reflection.MemberInfo::InvokeMember"
    };

        [MenuItem("Tools/Scan Reflection Usage")]
        public static void ScanReflectionUsage()
        {
            Debug.Log("Scanning assemblies for reflection usage...");

            var assemblies = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a =>
                    !a.FullName.StartsWith("UnityEditor") &&
                    !a.FullName.StartsWith("UnityEngine") &&
                    !a.IsDynamic)
                .ToArray();

            List<string> results = new List<string>();

            int totalHits = 0;
            foreach (var assembly in assemblies)
            {
                try
                {
                    var types = assembly.GetTypes();
                    foreach (var type in types)
                    {
                        MethodInfo[] methods;
                        try
                        {
                            methods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                        }
                        catch
                        {
                            continue;
                        }

                        foreach (var method in methods)
                        {
                            if (method == null) continue;
                            var body = method.GetMethodBody();
                            if (body == null) continue;

                            var il = body.GetILAsByteArray();
                            if (il == null) continue;

                            string disasm = BitConverter.ToString(il);

                            foreach (string keyword in ReflectionKeywords)
                            {
                                if (disasm.Contains("Call") && disasm.Contains(keyword))
                                {
                                    string entry = $"{assembly.GetName().Name} -> {type.FullName}.{method.Name} | {keyword}";
                                    results.Add(entry);
                                    totalHits++;
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    results.Add($"Skipped assembly {assembly.FullName} due to error: {ex.Message}");
                }
            }

            results.Sort();
            string reportPath = Path.Combine(Application.dataPath, "../ReflectionReport.txt");

            using (StreamWriter writer = new StreamWriter(reportPath, false))
            {
                writer.WriteLine($"Reflection Usage Report - {DateTime.Now}");
                writer.WriteLine("========================================");
                writer.WriteLine($"Scanned Assemblies: {assemblies.Length}");
                writer.WriteLine($"Detected Reflection Calls: {totalHits}");
                writer.WriteLine();

                if (totalHits == 0)
                {
                    writer.WriteLine("No reflection usage detected. Safe to enable High stripping.");
                }
                else
                {
                    foreach (string line in results)
                        writer.WriteLine(line);
                }
            }

            if (totalHits == 0)
            {
                Debug.Log($"No reflection usage detected. Report saved to: {reportPath}");
            }
            else
            {
                Debug.LogWarning($"Found {totalHits} reflection usages. See report at: {reportPath}");
            }

            EditorUtility.RevealInFinder(reportPath);
        }
    }
}