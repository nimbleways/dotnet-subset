# dotnet-subset

`dotnet-subset` is a .NET tool that copies a subset of files from a repository to a directory.

The tool is mainly used in Dockerfiles to optimize the docker build caching for "dotnet restore" instructions.

# Motivation

To learn more about the motivation behind `dotnet-subset`, please read [the related blog post](https://blog.nimbleways.com/p/45d44a69-5460-4fb3-aacb-be7419b27aad/).

# Roadmap
[ ] Add tests
[ ] Refactor the codebase
[ ] Add "build" algorithm
