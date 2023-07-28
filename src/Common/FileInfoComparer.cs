namespace Common;

public sealed class FileInfoComparer : IEqualityComparer<FileInfo>
{
    public static FileInfoComparer Instance { get; } = new FileInfoComparer();

    private FileInfoComparer()
    {
    }

    public bool Equals(FileInfo? x, FileInfo? y)
    {
        return string.Equals(x?.FullName, y?.FullName, StringComparison.Ordinal);
    }

    public int GetHashCode(FileInfo? obj)
    {
        return obj is null ? 0 : obj.FullName.GetHashCode(StringComparison.Ordinal);
    }
}
