namespace Common.UnitTests;

public class FileInfoComparerTests
{
    [Fact]
    public void TwoFileInfosWithDifferentPathsShouldNotBeEqual()
    {
        var fileA = GetNewFileInfoA();
        var fileB = GetNewFileInfoB();
        Assert.False(FileInfoComparer.Instance.Equals(fileA, fileB));
    }

    [Fact]
    public void TwoFileInfosWithSamePathsShouldBeEqual()
    {
        var fileA1 = GetNewFileInfoA();
        var fileA2 = GetNewFileInfoA();
        Assert.True(FileInfoComparer.Instance.Equals(fileA1, fileA2));
    }

    [Fact]
    public void TwoFileInfosWithSameReferenceShouldBeEqual()
    {
        var fileA = GetNewFileInfoA();
        Assert.True(FileInfoComparer.Instance.Equals(fileA, fileA));
    }

    [Fact]
    public void TwoFileInfosNullShouldBeEqual()
    {
        Assert.True(FileInfoComparer.Instance.Equals(null, null));
    }

    [Fact]
    public void NullAndNonNullFileInfosShouldNotBeEqual()
    {
        var fileA = GetNewFileInfoA();
        Assert.False(FileInfoComparer.Instance.Equals(fileA, null));
        Assert.False(FileInfoComparer.Instance.Equals(null, fileA));
    }

    [Fact]
    public void TwoFileInfosWithSamePathsShouldHaveTheSameHashCode()
    {
        var fileA1 = GetNewFileInfoA();
        var fileA2 = GetNewFileInfoA();
        Assert.Equal(FileInfoComparer.Instance.GetHashCode(fileA1), FileInfoComparer.Instance.GetHashCode(fileA2));
    }

    [Fact]
    public void NullFileInfoShouldHaveHashCodeEqualToZero()
    {
        Assert.Equal(0, FileInfoComparer.Instance.GetHashCode(null));
    }

    [Fact]
    public void TenRandomFileInfosShouldAtLeastHaveTwoUniqueHashCodes()
    {
        var fileInfos = GetPseudoRandomFileInfos().Take(10);
        var distinctHashcodes = fileInfos.Select(FileInfoComparer.Instance.GetHashCode).Distinct();
        Assert.True(distinctHashcodes.Count() > 1);
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Security", "CA5394:Do not use insecure randomness", Justification = "it's intentional")]
    private static IEnumerable<FileInfo> GetPseudoRandomFileInfos()
    {
        var randomCharSequence = new Random(0);
        var randomLengthSequence = new Random(0);
        return Enumerable.Range(0, 10).Select(_ => new FileInfo(Path.Combine(Path.GetTempPath(), GetRandomFilename(randomCharSequence, randomLengthSequence))));

        static string GetRandomFilename(Random randomCharSequence, Random randomLengthSequence)
        {
            string randomString = GetRandomString(randomCharSequence, randomLengthSequence.Next(3, 50));
            string fileName = GetRandomString(randomCharSequence, randomLengthSequence.Next(3, 50));
            string fileExtension = GetRandomString(randomCharSequence, randomLengthSequence.Next(1, 5));
            return fileName + "." + fileExtension;

            static string GetRandomString(Random randomCharSequence, int length)
            {
                const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
                string randomString = new(Enumerable.Repeat(chars, length).Select(s => s[randomCharSequence.Next(s.Length)]).ToArray());
                return randomString;
            }
        }
    }

    private static FileInfo GetNewFileInfoA()
    {
        return new FileInfo(Path.Combine(Path.GetTempPath(), "a.txt"));
    }

    private static FileInfo GetNewFileInfoB()
    {
        return new FileInfo(Path.Combine(Path.GetTempPath(), "b.txt"));
    }
}
