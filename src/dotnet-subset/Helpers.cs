using System.Reflection;

namespace Nimbleways.Tools.Subset;
internal static class Helpers
{
    public static void PrintContext()
    {
        string toolName = Path.GetFileNameWithoutExtension(Environment.GetCommandLineArgs()[0]);
        Version version = Assembly.GetExecutingAssembly().GetName().Version;
        Console.WriteLine($"{toolName} {version.ToString(3)} (.NET Runtime {Environment.Version})");
        Console.WriteLine();
    }
}
