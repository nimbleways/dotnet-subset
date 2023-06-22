﻿using System;
using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.Tools.Docker;
using Nuke.Common.Tools.DotNet;
using Utils;
using static Nuke.Common.IO.FileSystemTasks;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

public partial class Build
{
    Target CompileDbUpMigrator => _ => _
        .Executes(() =>
        {
            var dbUpMigratorProject = Solution.GetProject("DatabaseMigrator");
            DotNetBuild(s => s
                .SetProjectFile(dbUpMigratorProject)
                .SetConfiguration(Configuration)
                .SetOutputDirectory(OutputDbUbMigratorBuildDirectory)
            );
        });
    
    [Parameter("Modular Monolith database connection string")] readonly string DatabaseConnectionString;
    Target MigrateDatabase => _ => _
        .Requires(() => DatabaseConnectionString != null)
        .DependsOn(CompileDbUpMigrator)
        .Executes(() =>
        {
            var migrationsPath = DatabaseDirectory / "Migrations";

            DotNet($"{DbUpMigratorPath} {DatabaseConnectionString} {migrationsPath}");
        });
}