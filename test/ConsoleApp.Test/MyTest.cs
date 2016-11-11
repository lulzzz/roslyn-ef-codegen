using System;
using Xunit;

namespace ConsoleApp.Test
{
    public class MyTest
    {
        [Fact]
        public void TestMethod()
        {
            Console.WriteLine("Serious testing:");
            ConsoleApp.Program.Main(new string[] {});
        }
    }
}
