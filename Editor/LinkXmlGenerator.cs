using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

public static class LinkXmlGenerator
{
    // Common reflection calls to detect
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

    [MenuItem("Tools/Generate link.xml From Reflection Usage")]
    public static void GenerateLinkXml()
    {
        Debug.Log("Scanning assemblies for reflection usage to generate link.xml ...");

        var assemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a =>
                !a.FullName.StartsWith("UnityEditor") &&
                !a.FullName.StartsWith("UnityEngine") &&
                !a.IsDynamic)
            .ToArray();

        // This will hold all assemblies/namespaces/types detected
        Dictionary<string, HashSet<string>> assemblyToNamespaces = new Dictionary<string, HashSet<string>>();

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
                                if (!assemblyToNamespaces.TryGetValue(assembly.GetName().Name, out var namespaces))
                                {
                                    namespaces = new HashSet<string>();
                                    assemblyToNamespaces[assembly.GetName().Name] = namespaces;
                                }

                                if (!string.IsNullOrEmpty(type.Namespace))
                                    namespaces.Add(type.Namespace);

                                totalHits++;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Skipped assembly {assembly.FullName}: {ex.Message}");
            }
        }

        // Generate link.xml text
        string xmlPath = Path.Combine(Application.dataPath, "link.xml");
        using (StreamWriter writer = new StreamWriter(xmlPath, false))
        {
            writer.WriteLine("<linker>");

            foreach (var kv in assemblyToNamespaces)
            {
                writer.WriteLine($"  <assembly fullname=\"{kv.Key}\">");
                foreach (var ns in kv.Value.OrderBy(x => x))
                {
                    writer.WriteLine($"    <namespace fullname=\"{ns}\" preserve=\"all\" />");
                }
                writer.WriteLine("  </assembly>");
            }

            writer.WriteLine("</linker>");
        }

        if (totalHits == 0)
        {
            Debug.Log($"No reflection usage detected. No link.xml was generated (your game is safe).");
        }
        else
        {
            Debug.LogWarning($"Found {totalHits} reflection usages. Generated link.xml with {assemblyToNamespaces.Count} assemblies at:\n{xmlPath}");
        }

        EditorUtility.RevealInFinder(xmlPath);
    }
}
