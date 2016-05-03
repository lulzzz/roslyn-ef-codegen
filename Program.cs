using System;
using System.Threading.Tasks;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.DotNet.ProjectModel.Workspaces;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Scaffolding.Internal;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Design.Internal;

namespace ConsoleApplication
{
    public class Program
    {
        public static void Main(string[] args)
        {
        	var assemblyName = "test";
        	var sln = new ProjectJsonWorkspace(@"C:\dd").CurrentSolution.Projects.First();
            // foreach(var r in sln.MetadataReferences)
            // {
            //     Console.WriteLine("Reference: {0}, {1}", r.Display, r.GetHashCode());
            // }

            var compilerOptions = new CSharpCompilationOptions(outputKind: OutputKind.DynamicallyLinkedLibrary);
            var programSource = _source + "\n" + string.Join("\n", DbFiles());
            Console.WriteLine("Program: {0}", programSource);
            var trees = new SyntaxTree[] {
                CSharpSyntaxTree.ParseText(programSource),
            };

			var compilation = CSharpCompilation.Create(assemblyName)
                .WithOptions(compilerOptions)
                .WithReferences(sln.MetadataReferences)
                .AddSyntaxTrees(trees);
            var stream = new MemoryStream();
            var compilationResult = compilation.Emit(stream, options: new EmitOptions());
            stream.Position = 0;

            foreach(var r in compilationResult.Diagnostics) 
            {
            	Console.WriteLine("Diag: {0}", r);
            }

            // net45 specific?
            var asm = Assembly.Load(stream.GetBuffer());
            var programType = asm.GetTypes().Single(t => t.Name == "Program");
            var method = programType.GetMethod("Main");
            var programInstance = Activator.CreateInstance(programType);
            var res = method.Invoke(programInstance, new object[] { }) as string;
            Console.WriteLine("query: {0}", res);
        }

        private static IEnumerable<string> DbFiles() 
        {
            var loggerFactory = new LoggerFactory().AddConsole();

            var ssTypeMap = new Microsoft.EntityFrameworkCore.Storage.Internal.SqlServerTypeMapper();
            var ssDbFac = new SqlServerDatabaseModelFactory(loggerFactory: loggerFactory);
            var ssScaffoldFac = new SqlServerScaffoldingModelFactory(
                loggerFactory: loggerFactory,
                typeMapper: ssTypeMap,
                databaseModelFactory: ssDbFac
            );

            var ssAnnotationProvider = new Microsoft.EntityFrameworkCore.Metadata.SqlServerAnnotationProvider();
            var csUtils = new CSharpUtilities();
            var scaffUtils = new ScaffoldingUtilities();

            var confFac = new ConfigurationFactory(
                extensionsProvider: ssAnnotationProvider,
                cSharpUtilities: csUtils,
                scaffoldingUtilities: scaffUtils
            );
            var fs = new InMemoryFileService();
            var sb = new StringBuilderCodeWriter(
                fileService: fs,
                dbContextWriter: new DbContextWriter(
                    scaffoldingUtilities: scaffUtils,
                    cSharpUtilities: csUtils
                ),
                entityTypeWriter: new EntityTypeWriter(cSharpUtilities: csUtils)
            );

            var rGen = new ReverseEngineeringGenerator(
                loggerFactory: loggerFactory,
                scaffoldingModelFactory: ssScaffoldFac,
                configurationFactory: confFac,
                codeWriter: sb
            );

            var outputPath = @"C:\temp";
            var programName = "Program";
            var conf = new ReverseEngineeringConfiguration 
            {
                ConnectionString = @"Data Source=.\sqlexpress;Integrated Security=True;Initial Catalog=eftest",
                ContextClassName = "Program",
                ProjectPath = "na",
                ProjectRootNamespace = "Foo",
                OutputPath = outputPath
            };

            var files = new List<string>();
            var resFiles = rGen.GenerateAsync(conf);
            resFiles.Wait();
            files.Add(StripHeaderLines(2, fs.RetrieveFileContents(outputPath, programName + ".cs")));
            foreach(var fpath in resFiles.Result.EntityTypeFiles)
            {
                files.Add(StripHeaderLines(4, fs.RetrieveFileContents(outputPath, System.IO.Path.GetFileName(fpath))));
            }
            return files;
        }

        static string StripHeaderLines(int lines, string contents) 
        {
            return string.Join("\n", contents.Split('\n').Skip(lines));
        }

        static string _source = @"
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Foo
{
    public partial class Program
    {
        public static string Main()
        {
            using (var ctx = new Program()) 
            {
                return ctx.eftable.First().MyValue;
            }
            //Console.WriteLine(""Hello world"");
        }
    }
}
";
    }

}

namespace ConsoleApplication
{
    public class InMemoryFileService : IFileService
    {
        // maps directory name to a dictionary mapping file name to file contents
        private readonly Dictionary<string, Dictionary<string, string>> _nameToContentMap
            = new Dictionary<string, Dictionary<string, string>>();

        public virtual bool DirectoryExists(string directoryName)
        {
            Dictionary<string, string> filesMap;
            return _nameToContentMap.TryGetValue(directoryName, out filesMap);
        }

        public virtual bool FileExists(string directoryName, string fileName)
        {
            Dictionary<string, string> filesMap;
            if (!_nameToContentMap.TryGetValue(directoryName, out filesMap))
            {
                return false;
            }

            string _;
            return filesMap.TryGetValue(fileName, out _);
        }

        public virtual bool IsFileReadOnly(string outputDirectoryName, string outputFileName) => false;

        public virtual string RetrieveFileContents(string directoryName, string fileName)
        {
            Dictionary<string, string> filesMap;
            if (!_nameToContentMap.TryGetValue(directoryName, out filesMap))
            {
                throw new DirectoryNotFoundException("Could not find directory " + directoryName);
            }

            string contents;
            if (!filesMap.TryGetValue(fileName, out contents))
            {
                throw new FileNotFoundException("Could not find file " + Path.Combine(directoryName, fileName));
            }

            return contents;
        }

        public virtual string OutputFile(string directoryName,
            string fileName, string contents)
        {
            Dictionary<string, string> filesMap;
            if (!_nameToContentMap.TryGetValue(directoryName, out filesMap))
            {
                filesMap = new Dictionary<string, string>();
                _nameToContentMap[directoryName] = filesMap;
            }

            filesMap[fileName] = contents;

            return Path.Combine(directoryName, fileName);
        }
    }

}
