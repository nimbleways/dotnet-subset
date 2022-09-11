# dotnet-subset
[![NuGet version (dotnet-subset)](https://img.shields.io/nuget/v/dotnet-subset.svg?style=flat-square)](https://www.nuget.org/packages/dotnet-subset/)
[![GitHub workflow](https://github.com/nimbleways/dotnet-subset/actions/workflows/workflow.yml/badge.svg?branch=main)](https://github.com/nimbleways/dotnet-subset/actions/workflows/workflow.yml?query=branch%3Amain)


`dotnet-subset` is a .NET tool that copies a subset of files from a repository to a directory.

The tool is mainly used in Dockerfiles to optimize the docker build caching for "dotnet restore" instructions.

## Motivation

To learn more about the motivation behind `dotnet-subset`, please read [the related blog post](https://blog.nimbleways.com/docker-build-caching-for-dotnet-applications-done-right-with-dotnet-subset/).

## Installation
### From NuGet
```
dotnet tool install --global dotnet-subset
```
### From source
Prerequisite: .NET SDK 6

1. Clone this repository
2. Open a terminal in the repository's root
3. `dotnet pack --configuration Release`
4. `dotnet tool install dotnet-subset --global --add-source ./artifacts/Release/nupkg/dotnet-subset/`

## Usage
```
Description:
  Create a subset for the restore operation.

Usage:
  dotnet-subset restore <projectOrSolution> [options]

Arguments:
  <projectOrSolution>  Project or solution to restore.

Options:
  --root-directory <root-directory> (REQUIRED)  Directory from where the files will be copied, usually the 
                                                repository's root.
  --output <output> (REQUIRED)                  Directory where the subset files will be copied,
                                                preserving the original hierarchy.
  -?, -h, --help                                Show help and usage information
```

Example with a project:
```
dotnet subset restore /source/complexapp/complexapp.csproj --root-directory /source/ --output /tmp/restore_subset/
```
Example with a solution:
```
dotnet subset restore /source/complexapp.sln --root-directory /source/ --output /tmp/restore_subset/
```

## dotnet-subset + docker
Please check these pull requests to see how to use `dotnet-subset` in your `Dockerfile`:
- https://github.com/othmane-kinane-nw/modular-monolith-with-ddd/pull/1/files?diff=split&w=0
- https://github.com/othmane-kinane-nw/dotnet-docker/pull/1/files?diff=split&w=0

## Roadmap
- [ ] Add tests
- [ ] Automate the versionning
- [ ] Automate the deployment to NuGet
- [ ] Refactor the codebase
- [ ] Add "build" algorithm

## License

Copyright © [Nimbleways](https://www.nimbleways.com/), [Othmane Kinane](https://github.com/othmane-kinane-nw) and contributors.

`dotnet-subset` is provided as-is under the MIT license. For more information see [LICENSE](https://github.com/nimbleways/dotnet-subset/blob/main/LICENSE).

* For Microsoft.Build, see https://github.com/dotnet/msbuild/blob/main/LICENSE
* For Microsoft.Build.Locator, see https://github.com/microsoft/MSBuildLocator/blob/master/LICENSE
* For System.CommandLine, see https://github.com/dotnet/command-line-api/blob/main/LICENSE.md
