#tool nuget:?package=MSBuild.SonarQube.Runner.Tool
#addin nuget:?package=Cake.Sonar

#addin "Cake.Yarn"

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
		.Add(settings => settings.Package("gulp").Globally())
        .Add(settings => settings.Package("snyk").Globally());
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

Task("Snyk")
  .IsDependentOn("Yarn")
  .IsDependentOn("Build")
  .Does(() =>
{
	var snykCommand = "snyk monitor --org=olsh";

	StartProcess("powershell", $"{snykCommand} --file={solutionFile}");
	StartProcess("powershell", $"{snykCommand} src/{projectName}/obj --project-name={projectName}");
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
