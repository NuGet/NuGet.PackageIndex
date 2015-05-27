using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApplication1
{
    class Program
    {
        private static void GetMembers(INamespaceSymbol ns)
        {
            foreach (var typeName in ns.GetTypeMembers())
            {
                var tt = typeName;

                foreach (var member in typeName.GetMembers())
                {
                    var method = member as IMethodSymbol;
                    if (method == null || !method.IsStatic)
                    {
                        continue;
                    }

                    if (method.ReducedFrom != null)
                    {
                        var qq = method.Parameters[0].Type.Name;

                        var ww = qq;
                    }
                }
            }

            foreach (var n in ns.GetNamespaceMembers())
            {
                if (n.NamespaceKind == NamespaceKind.Module)
                {
                    GetMembers(n);
                }
            }
        }

        static void Main(string[] args)
        {
            var metadata = MetadataReference.CreateFromFile(@"C:\Users\antonpis\.dnx\packages4\EntityFramework.Core\7.0.0-beta5-13032\lib\dnx451\EntityFramework.Core.dll");
            var compilation = CSharpCompilation.Create("test.dll", references: new[] { metadata });
            var assemblySymbol = (IAssemblySymbol)compilation.GetAssemblyOrModuleSymbol(metadata);
            GetMembers(assemblySymbol.GlobalNamespace);

        }
    }
}
