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
            var count = ConsoleApp.Program.Main(new string[] {});
            Assert.Equal(83, count);
        }
    }
}
