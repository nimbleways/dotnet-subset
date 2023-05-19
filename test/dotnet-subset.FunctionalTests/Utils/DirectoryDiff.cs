namespace Nimbleways.Tools.Subset.Utils;

internal static class DirectoryDiff
{
    public static bool AreDirectoriesIdentical(DirectoryInfo left, DirectoryInfo right)
    {
        if (!left.Exists || !right.Exists)
        {
            return false;
        }

        FileInfo[] sortedLeftFiles = left.EnumerateFiles().OrderBy(f => f.Name).ToArray();
        FileInfo[] sortedRightFiles = right.EnumerateFiles().OrderBy(f => f.Name).ToArray();
        DirectoryInfo[] sortedLeftDirectories = left.EnumerateDirectories().OrderBy(d => d.Name).ToArray();
        DirectoryInfo[] sortedRightDirectories = right.EnumerateDirectories().OrderBy(d => d.Name).ToArray();
        if (sortedLeftFiles.Length != sortedRightFiles.Length ||
            sortedLeftDirectories.Length != sortedRightDirectories.Length)
        {
            return false;
        }

        // Compare files in the current directory
        for (int i = 0; i < sortedLeftFiles.Length; i++)
        {
            if (sortedLeftFiles[i].Name != sortedRightFiles[i].Name
                || sortedLeftFiles[i].Length != sortedRightFiles[i].Length)
            {
                return false;
            }

            using FileStream leftStream = sortedLeftFiles[i].OpenRead();
            using FileStream rightStream = sortedRightFiles[i].OpenRead();
            if (!StreamContentsEqual(leftStream, rightStream))
            {
                return false;
            }
        }

        // Recursively compare subdirectories
        for (int i = 0; i < sortedLeftDirectories.Length; i++)
        {
            if (sortedLeftDirectories[i].Name != sortedRightDirectories[i].Name
                || !AreDirectoriesIdentical(sortedLeftDirectories[i], sortedRightDirectories[i]))
            {
                return false;
            }
        }

        return true;
    }

    private static bool StreamContentsEqual(Stream left, Stream right)
    {
        const int bufferSize = 4096;

        byte[] leftBuffer = new byte[bufferSize];
        byte[] rightBuffer = new byte[bufferSize];

        while (true)
        {
            int leftBytesRead = left.Read(leftBuffer, 0, bufferSize);
            int rightBytesRead = right.Read(rightBuffer, 0, bufferSize);

            if (leftBytesRead != rightBytesRead)
            {
                return false;
            }
            if (leftBytesRead == 0)
            {
                return true;
            }
            if (!leftBuffer.AsSpan(0, leftBytesRead).SequenceEqual(rightBuffer.AsSpan(0, leftBytesRead)))
            {
                return false;
            }
        }
    }

}
