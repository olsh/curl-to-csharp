#tool nuget:?package=MSBuild.SonarQube.Runner.Tool
#addin nuget:?package=Cake.Sonar

#addin "Cake.Yarn"

var target = Argument("target", "Default");

var buildConfiguration = "Release";
var projectName = "CurlToCSharp";
var testProjectName = "CurlToCSharp.Tests";
var projectFile = string.Format("./src/{0}/{0}.csproj", projectName);
var testProjectFile = string.Format("./src/{0}/{0}.csproj", testProjectName);
var tempPublishDirectory = "./publish";
var tempPublishArchive = "publish.zip";

Task("Yarn")
    .Does(() =>
    {
        Yarn.Add(settings => settings.Package("gulp").Globally());
    });

Task("Build")
  .Does(() =>
{
    var settings = new DotNetCoreBuildSettings
    {
        Configuration = buildConfiguration
    };

    DotNetCoreBuild(string.Format("./src/{0}.sln", projectName), settings);
});

Task("Test")
  .IsDependentOn("Build")
  .Does(() =>
{
     var settings = new DotNetCoreTestSettings
     {
         Configuration = buildConfiguration,
         NoBuild = true
     };

     DotNetCoreTest(testProjectFile, settings);
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
    .IsDependentOn("Test");

Task("Sonar")
  .IsDependentOn("SonarBegin")
  .IsDependentOn("Build")
  .IsDependentOn("Test")
  .IsDependentOn("SonarEnd");

Task("CI")
    .IsDependentOn("Sonar")
    .IsDependentOn("CreateArtifact");

RunTarget(target);
