using System;
using System.Linq;
using System.Reflection;

namespace NamespaceChecker
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                // Try to load the assembly if it's not already loaded
                var assembly = Assembly.Load("PayOS");
                if (assembly != null)
                {
                    Console.WriteLine($"Found assembly: {assembly.FullName}");
                    var namespaces = assembly.GetTypes()
                        .Select(t => t.Namespace)
                        .Distinct()
                        .Where(n => n != null)
                        .OrderBy(n => n);
                    
                    Console.WriteLine("Namespaces found:");
                    foreach (var ns in namespaces)
                    {
                        Console.WriteLine($"  - {ns}");
                    }

                    Console.WriteLine("\nTypes in Net.payOS.Types (if exists):");
                    var types = assembly.GetTypes()
                        .Where(t => t.Namespace == "Net.payOS.Types")
                        .Select(t => t.Name);
                    foreach (var t in types)
                    {
                        Console.WriteLine($"  - {t}");
                    }
                    
                    Console.WriteLine("\nTypes in PayOS.Types (if exists):");
                    types = assembly.GetTypes()
                        .Where(t => t.Namespace == "PayOS.Types")
                        .Select(t => t.Name);
                    foreach (var t in types)
                    {
                        Console.WriteLine($"  - {t}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }
}
