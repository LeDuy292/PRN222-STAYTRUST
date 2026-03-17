using System;
using System.Reflection;
using System.Linq;

try
{
    var path = @"C:\Users\ad\.nuget\packages\payos\2.0.1\lib\net8.0\PayOS.dll";
    var assembly = Assembly.LoadFrom(path);
    var namespaces = assembly.GetTypes()
        .Select(t => t.Namespace)
        .Where(n => !string.IsNullOrEmpty(n))
        .Distinct()
        .OrderBy(n => n);

    Console.WriteLine("Namespaces found in PayOS.dll:");
    foreach (var ns in namespaces)
    {
        Console.WriteLine($"- {ns}");
    }

    Console.WriteLine("\nTypes in first 3 namespaces:");
    foreach (var ns in namespaces.Take(3))
    {
        Console.WriteLine($"\nNamespace: {ns}");
        var types = assembly.GetTypes().Where(t => t.Namespace == ns).Take(10);
        foreach (var t in types)
        {
            Console.WriteLine($"  - {t.Name}");
        }
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
    if (ex is ReflectionTypeLoadException re)
    {
        foreach (var le in re.LoaderExceptions)
        {
            Console.WriteLine($"  LoaderException: {le.Message}");
        }
    }
}
