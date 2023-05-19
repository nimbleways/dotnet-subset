namespace Nimbleways.Tools.Subset.Utils;

internal sealed class DisposableTempDirectory : IDisposable
{
    public DisposableTempDirectory()
    {
        Value = new(Path.Combine(Path.GetTempPath(), "dotnet-subset", Path.GetRandomFileName()));
        Directory.CreateDirectory(Value.FullName);
    }

    public DirectoryInfo Value { get; }

    public void Dispose()
    {
        Directory.Delete(Value.FullName, true);
    }
}
