Microsoft.CodeAnalysis/EntityFrameworkCore query codegeneration [![Windows build status](https://ci.appveyor.com/api/projects/status/2l98lij5j93wf1q1?svg=true)](https://ci.appveyor.com/project/stofte/roslyn-ef-codegen)
---------------------------------------------------------------

This repository contains some rough POC code that
shows how to use CodeAnalysis and EFCore to create dynamic
queries at runtime against databases chosen at runtime.
.NET Core 1.1 is required to run the code.

The code is linear and starts in `Program.Main`:

- Generate a SQLite schema/context in-memory using EF's reverse engineering tools
- Use CodeAnalysis to compile and load the schema
- Compile a query `SomeNs.Generated : IGenerated` and link the EF context
- Create an instance of the generated query and calls its main method
- The instance first calls back to the host library (to test that "static" host classes can be referenced)
- It then creates an instance of the EF context, and counts the number of city names starting with "Ca"

The Xunit test project just calls the main projects `Main` method (mostly to test that everything connects).

Usage
-----

    dotnet restore
    dotnet test test/ConsoleApp.Test

Output should be

    Serious testing:
    Success: True
    Success: True
    hello from main
    Cities starting with 'Ca': 83