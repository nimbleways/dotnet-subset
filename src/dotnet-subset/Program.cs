// https://www.daveaglick.com/posts/running-a-design-time-build-with-msbuild-apis
// https://docs.microsoft.com/en-us/visualstudio/msbuild/updating-an-existing-application?view=vs-2022#use-microsoftbuildlocator

using System.CommandLine;
using System.CommandLine.Parsing;

using Microsoft.Build.Locator;

namespace Nimbleways.Tools.Subset;

public static class Program
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0052:Remove unread private members", Justification = "<Pending>")]
    private static VisualStudioInstance? s_visualStudioInstance;

    public static int Main(string[] args)
    {
        SetVisualStudionInstance();
        Parser parser = CommandLineParser.GetCommandLineParser(SetVisualStudionInstance);
        return parser.Invoke(args);
    }

    private static void SetVisualStudionInstance()
    {
        s_visualStudioInstance ??= MSBuildLocator.RegisterDefaults();
    }
}
