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
            var cwd = System.IO.Directory.GetCurrentDirectory();
            Console.WriteLine("cwd: {0}", cwd);
            var sln = new ProjectJsonWorkspace(Path.Combine(cwd, "project.json"));
            // Console.WriteLine("sln: {0}", sln.FilePath);   
        }
        static string _source = @"namespace Generated { public class Hidden { } }";
    }
}
