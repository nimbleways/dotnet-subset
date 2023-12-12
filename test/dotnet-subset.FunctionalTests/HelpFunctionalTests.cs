using Nimbleways.Tools.Subset.Helpers;

namespace Nimbleways.Tools.Subset;

[UsesVerify]
public class HelpFunctionalTests
{
    private static readonly string[] HelpArgs = new[] { "--help" };
    private static readonly string[] RestoreHelpArgs = new[] { "restore", "--help" };

    [Fact]
    public async Task ShowGlobalHelp()
    {
        var result = DotnetSubsetRunner.Run(HelpArgs, new DirectoryInfo(Environment.CurrentDirectory));
        Assert.Equal(0, result.ExitCode);
        await result.VerifyOutput();
    }

    [Fact]
    public async Task ShowRestoreHelp()
    {
        var result = DotnetSubsetRunner.Run(RestoreHelpArgs, new DirectoryInfo(Environment.CurrentDirectory));
        Assert.Equal(0, result.ExitCode);
        await result.VerifyOutput();
    }
}
