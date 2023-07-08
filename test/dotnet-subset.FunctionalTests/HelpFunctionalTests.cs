using Nimbleways.Tools.Subset.Helpers;

namespace Nimbleways.Tools.Subset;

[UsesVerify]
public class HelpFunctionalTests
{
    [Fact]
    public async Task ShowGlobalHelp()
    {
        var result = DotnetSubsetRunner.Run(new[] { "--help" }, new DirectoryInfo(Environment.CurrentDirectory));
        Assert.Equal(0, result.ExitCode);
        await result.VerifyOutput();
    }

    [Fact]
    public async Task ShowRestoreHelp()
    {
        var result = DotnetSubsetRunner.Run(new[] { "restore", "--help" }, new DirectoryInfo(Environment.CurrentDirectory));
        Assert.Equal(0, result.ExitCode);
        await result.VerifyOutput();
    }
}
