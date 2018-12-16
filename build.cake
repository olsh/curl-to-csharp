#tool nuget:?package=MSBuild.SonarQube.Runner.Tool&version=4.3.1

#addin nuget:?package=Cake.Sonar&version=1.1.18
#addin nuget:?package=Cake.Yarn&version=0.4.4

var target = Argument("target", "Default");

var buildConfiguration = "Release";
var projectName = "CurlToCSharp";
var unitTestProjectName = "CurlToCSharp.UnitTests";
var integrationTestProjectName = "CurlToCSharp.IntegrationTests";
var projectFile = string.Format("./src/{0}/{0}.csproj", projectName);
var unitTestProjectFile = string.Format("./src/{0}/{0}.csproj", unitTestProjectName);
var integrationTestProjectFile = string.Format("./src/{0}/{0}.csproj", integrationTestProjectName);
var solutionFile = string.Format("./src/{0}.sln", projectName);
var tempPublishDirectory = "./publish";
var tempPublishArchive = "publish.zip";

Task("Yarn")
    .Does(() =>
    {
        Yarn
        .Add(settings => settings.Package("gulp").Globally());
    });

Task("Build")
  .Does(() =>
{
    var settings = new DotNetCoreBuildSettings
    {
        Configuration = buildConfiguration
    };

    DotNetCoreBuild(solutionFile, settings);
});

Task("UnitTest")
  .IsDependentOn("Build")
  .Does(() =>
{
     RunTests(unitTestProjectFile);
});

Task("IntegrationTest")
  .IsDependentOn("Build")
  .Does(() =>
{
     RunTests(integrationTestProjectFile);
});

Task("Pack")
  .IsDependentOn("Yarn")
  .Does(() =>
{
    var settings = new DotNetCorePublishSettings
    {
        Configuration = buildConfiguration,
        OutputDirectory = tempPublishDirectory
    };

    DotNetCorePublish(projectFile, settings);

    Zip(tempPublishDirectory, tempPublishArchive);
});

Task("CreateArtifact")
  .IsDependentOn("Pack")
  .WithCriteria(BuildSystem.AppVeyor.IsRunningOnAppVeyor)
  .Does(() =>
{
    BuildSystem.AppVeyor.UploadArtifact(tempPublishArchive);
});

Task("SonarBegin")
  .Does(() => {
     SonarBegin(new SonarBeginSettings {
        Url = "https://sonarcloud.io",
        Login = EnvironmentVariable("sonar:apikey"),
        Key = "curl-to-csharp",
        Name = "curl to C#",
        ArgumentCustomization = args => args
            .Append($"/o:olsh-github"),
        Version = "1.0.0.0"
     });
  });

Task("SonarEnd")
  .Does(() => {
     SonarEnd(new SonarEndSettings {
        Login = EnvironmentVariable("sonar:apikey")
     });
  });

Task("Default")
    .IsDependentOn("Pack");

Task("Test")
  .IsDependentOn("UnitTest")
  .IsDependentOn("IntegrationTest");

Task("Sonar")
  .IsDependentOn("SonarBegin")
  .IsDependentOn("Build")
  .IsDependentOn("Test")
  .IsDependentOn("SonarEnd");

Task("CI")
    .IsDependentOn("Sonar")
    .IsDependentOn("CreateArtifact");

RunTarget(target);

void RunTests(string projectFile)
{
     var settings = new DotNetCoreTestSettings
     {
         Configuration = buildConfiguration,
         NoBuild = true
     };

     DotNetCoreTest(projectFile, settings);
}
