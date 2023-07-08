using System.CommandLine;
using System.Reflection;

namespace Nimbleways.Tools.Subset;
internal static class Helpers
{
    public static void PrintApplicationAndRuntimeVersions()
    {
        Version version = Assembly.GetExecutingAssembly().GetName().Version;
        Console.WriteLine($"{RootCommand.ExecutableName} {version.ToString(3)} (.NET Runtime {Environment.Version})");
        Console.WriteLine();
    }
}
