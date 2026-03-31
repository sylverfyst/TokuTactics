using System;
using TokuTactics.Tests;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Starting Toku Tactics test suite...");
        Console.WriteLine();

        try
        {
            TestRunner.RunAll();
            Console.WriteLine();
            Console.WriteLine("✓ All tests completed successfully!");
            Environment.Exit(0);
        }
        catch (Exception ex)
        {
            Console.WriteLine();
            Console.WriteLine($"✗ Test run failed: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            Environment.Exit(1);
        }
    }
}
