// https://www.daveaglick.com/posts/running-a-design-time-build-with-msbuild-apis
// https://docs.microsoft.com/en-us/visualstudio/msbuild/updating-an-existing-application?view=vs-2022#use-microsoftbuildlocator

using System.CommandLine;
using System.CommandLine.Parsing;

using Microsoft.Build.Locator;

namespace Nimbleways.Tools.Subset;

public static class Program
{
    static Program()
    {
        MSBuildLocator.RegisterDefaults();
    }

    public static int Main(string[] args)
    {
        Parser parser = CommandLineParser.GetCommandLineParser();
        return parser.Invoke(args);
    }
}
