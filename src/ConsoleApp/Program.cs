using System;
using System.Reflection;
using System.Linq;
using System.IO;
using Microsoft.DotNet.ProjectModel.Workspaces;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;


namespace ConsoleApp
{
    public interface IGenerated
    {
        int DoIt();
    }
    public class Foo 
    {
        public static void Bar() 
        {
            Console.WriteLine("hello from main");
        }
    }

    public class Program
    {
        public static int Main(string[] args)
        {
            var sqlitePath = Path.Combine(System.IO.Directory.GetCurrentDirectory(), "world.sqlite");
            var connStr = string.Format("Data Source = {0}", sqlitePath);
            var schemaSrc = SchemaSource.Get(connStr, "SqliteWorld");
            // Console.WriteLine(schemaSrc);
            var schema = Build("schema", schemaSrc);
            var query = Build("query", _source, schema.Item2);
            var programType = query.Item1.GetTypes().Single(t => t.Name == "Generated");
            var programInstance = (IGenerated) Activator.CreateInstance(programType);
            return programInstance.DoIt();
        }

        static Tuple<Assembly, MetadataReference> Build(string assmName, string source, MetadataReference schema = null)
        {
            var cwd = System.IO.Directory.GetCurrentDirectory();
            Console.WriteLine("project.json: {0}", Path.Combine(cwd, "src/ConsoleApp/project.json"));
            var references = new ProjectJsonWorkspace(Path.Combine(cwd, "src/ConsoleApp/project.json")).CurrentSolution.Projects.SelectMany(p => p.MetadataReferences);
            var currentAssembly = typeof(Program).GetTypeInfo().Assembly;
            var fileUri = "file:///";
            // pretty dumb test for windows platform
            if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("TEMP")))
            {
                fileUri = "file://";
            }
            var asmPath = Path.GetFullPath(currentAssembly.CodeBase.Substring(fileUri.Length));

            var compilerOptions = new CSharpCompilationOptions(outputKind: OutputKind.DynamicallyLinkedLibrary);
            var trees = new SyntaxTree[] {
                CSharpSyntaxTree.ParseText(source),
            };

            var compilation = CSharpCompilation.Create(assmName)
                .WithOptions(compilerOptions)
                .WithReferences(references.Concat(new [] {
                    MetadataReference.CreateFromFile(asmPath) 
                }.Concat(schema != null ? 
                    new [] { schema } : new MetadataReference[] {}
                )))
                .AddSyntaxTrees(trees);

            var stream = new MemoryStream();
            var compilationResult = compilation.Emit(stream, options: new EmitOptions());
            Console.WriteLine("Success: {0}", compilationResult.Success);
            foreach(var diag in compilationResult.Diagnostics)
            {
                if (diag.Severity == DiagnosticSeverity.Error)
                {
                    Console.WriteLine("Error: {0}", diag.GetMessage());
                }
            }
            stream.Position = 0;
            var asm = LibraryLoader.LoadFromStream(stream);
            stream.Position = 0;
            var metaRef = MetadataReference.CreateFromStream(stream);
            return Tuple.Create(asm, metaRef as MetadataReference);
        }
        static string _source = @"
namespace SomeNs
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.ComponentModel.DataAnnotations.Schema;
    using ConsoleApp;

    public class Generated : IGenerated
    {
        public int DoIt()
        {
            Foo.Bar();
            using (var context = new SqliteWorld.Ctx())
            {
                var count = context.City.Where(x => x.Name.StartsWith(""Ca"")).Count();
                Console.WriteLine(""Cities starting with 'Ca': {0}"", count);
                return count;
            }
        }
    }
}";
    }
}
