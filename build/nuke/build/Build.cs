using System;
using System.Linq;
using Nuke.Common;
using Nuke.Common.CI;
using Nuke.Common.Execution;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Utilities.Collections;
using Serilog;
using static Nuke.Common.EnvironmentInfo;
using static Nuke.Common.IO.PathConstruction;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

class Build : NukeBuild
{
    /// Support plugins are available for:
    ///   - JetBrains ReSharper        https://nuke.build/resharper
    ///   - JetBrains Rider            https://nuke.build/rider
    ///   - Microsoft VisualStudio     https://nuke.build/visualstudio
    ///   - Microsoft VSCode           https://nuke.build/vscode

    public static int Main () => Execute<Build>(x => x.Compile);

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

    [Solution("dotnet/TestSolution.sln")] readonly Solution Solution;

    AbsolutePath SourceDirectory => RootDirectory / "dotnet";
    AbsolutePath TestsDirectory => RootDirectory / "dotnet";
    AbsolutePath ArtifactsDirectory => RootDirectory / "artifacts";
    AbsolutePath PublishDirectory => ArtifactsDirectory / "publish";

    Target Clean => _ => _
        .Before(Restore)
        .Executes(() =>
        {
            ArtifactsDirectory.CreateOrCleanDirectory();
            DotNetClean(s => s.SetProject(SourceDirectory));
        });

    Target Restore => _ => _
        .Executes(() =>
        {
            DotNetRestore(s => s.SetProjectFile(SourceDirectory));
        });

    Target Compile => _ => _
        .DependsOn(Restore)
        .Executes(() =>
        {
            DotNetBuild(s => s
                .SetProjectFile(SourceDirectory)
                .SetConfiguration(Configuration)
                .SetTreatWarningsAsErrors(true)
                .EnableNoRestore());
        });

    Target Test => _ => _
        .DependsOn(Compile)
        .Executes(() =>
        {
            DotNetTest(s => s
                .SetProjectFile(SourceDirectory)
                .SetConfiguration(Configuration)
                .EnableNoBuild()
                .SetVerbosity(DotNetVerbosity.normal));
        });

    Target Package => _ => _
        .DependsOn(Test)
        .Executes(() =>
        {
            DotNetPublish(s => s
                .SetProject(SourceDirectory)
                .SetConfiguration(Configuration)
                .SetOutput(PublishDirectory)
                .EnableNoBuild());
        });

    Target Deploy => _ => _
        .DependsOn(Package)
        .Executes(() =>
        {
            Log.Information("Deployment pipeline ready - artifacts available in: {0}", PublishDirectory);
            Log.Information("Deploy target completed - ready for container-native deployment");
        });

}
