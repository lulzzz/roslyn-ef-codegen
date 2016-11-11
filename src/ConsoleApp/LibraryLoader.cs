namespace ConsoleApp
{
    using System;
    using System.IO;
    using System.Reflection;
    using System.Runtime.Loader;

    public class LibraryLoader
    {
        // https://github.com/dotnet/corefx/pull/8730
        // todo maybe wrong, but works for loading the dll from a stream
        private readonly AssemblyLoadContext _loader =
            AssemblyLoadContext.GetLoadContext(typeof(LibraryLoader).GetTypeInfo().Assembly);

        public static Lazy<LibraryLoader> Instance = new Lazy<LibraryLoader>(() => new LibraryLoader());

        public static Assembly LoadFromStream(Stream assemblyStream) 
        {
            return LibraryLoader.Instance.Value._loader.LoadFromStream(assemblyStream);
        }
    }
}
