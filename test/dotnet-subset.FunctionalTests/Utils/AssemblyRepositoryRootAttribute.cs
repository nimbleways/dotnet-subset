namespace Nimbleways.Tools.Subset.Utils;

[AttributeUsage(AttributeTargets.Assembly, Inherited = false)]
public sealed class AssemblyRepositoryRootAttribute : Attribute
{
    public AssemblyRepositoryRootAttribute(string repositoryRootRelativePathToAssembly)
    {
        RepositoryRootRelativePathToAssembly = repositoryRootRelativePathToAssembly;
    }

    public string RepositoryRootRelativePathToAssembly
    {
        get;
    }
}
