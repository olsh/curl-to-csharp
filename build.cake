#tool nuget:?package=MSBuild.SonarQube.Runner.Tool&version=4.8.0

#addin nuget:?package=Cake.Sonar&version=1.1.30
#addin nuget:?package=Cake.Yarn&version=0.4.8
#addin nuget:?package=Cake.Docker&version=1.1.2

var target = Argument("target", "Default");
var cypressConfigurationFile = Argument("cypressConfigurationFile", "cypress.json");
var cypressEnableRecording = Argument("cypressEnableRecording", false);

var buildConfiguration = "Release";
var projectName = "CurlToCSharp";
var unitTestProjectName = $"{projectName}.UnitTests";
var integrationTestProjectName = $"{projectName}.IntegrationTests";
var projectDirectory = $"./src/{projectName}/";
var projectFile = $"{projectDirectory}{projectName}.csproj";
var unitTestProjectFile = string.Format("./src/{0}/{0}.csproj", unitTestProjectName);
var integrationTestProjectFile = string.Format("./src/{0}/{0}.csproj", integrationTestProjectName);
var solutionFile = string.Format("./src/{0}.sln", projectName);
var tempPublishDirectory = "./publish";
var tempPublishArchive = "publish.zip";
var dockerImageTag = "olsh/curl-to-csharp";
var dockerContainerName = "curl-to-csharp";
var dockerBuildContainerName = $"{dockerContainerName}-build";

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
    var artifactFileName = $"CurlToCSharp-{BuildSystem.AppVeyor.Environment.Build.Version}.zip";
    MoveFile(tempPublishArchive, artifactFileName);
    BuildSystem.AppVeyor.UploadArtifact(artifactFileName);
});

Task("SonarBegin")
  .Does(() => {
     SonarBegin(new SonarBeginSettings {
        Url = "https://sonarcloud.io",
        Login = EnvironmentVariable("sonar_apikey"),
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
        Login = EnvironmentVariable("sonar_apikey")
     });
  });

Task("DockerBuild")
  .IsDependentOn("Pack")
  .Does(() => {
     DockerBuild(new DockerImageBuildSettings() {
       File = "Dockerfile",
       Tag = new string [] { dockerImageTag }
     },
     tempPublishDirectory);
  });

Task("DockerPush")
  .IsDependentOn("Pack")
  .Does(() => {
    DockerLogin(EnvironmentVariable("docker_login"), EnvironmentVariable("docker_password"));

    StartProcess("docker", new ProcessSettings { Arguments = $"buildx create --use --name {dockerBuildContainerName}" });
    var dockerBuildArguments = $"buildx build {tempPublishDirectory} -f Dockerfile --push --platform linux/amd64,windows/amd64 -t {dockerImageTag} --progress plain";
    StartProcess("docker", new ProcessSettings{ Arguments = dockerBuildArguments });
  });

Task("RunEndToEndTests")
  .IsDependentOn("DockerBuild")
  .Does(() => {
     DockerRun(new DockerContainerRunSettings() {
       Detach = true,
       Publish = new string [] { "8080:80" },
       Name = dockerContainerName
     },
     dockerImageTag,
     null);

    var currentWorkingDirectory = Context.Environment.WorkingDirectory;
    Context.Environment.WorkingDirectory = MakeAbsolute(Directory(projectDirectory));
    var cypressCommand = $"cypress run --config-file {cypressConfigurationFile}";
    if (cypressEnableRecording)
    {
        cypressCommand += " --record";
    }
    Yarn.RunScript(cypressCommand);
    Context.Environment.WorkingDirectory = currentWorkingDirectory;
  });

Setup(context =>
{
  TryRemoveContainers();
});

Teardown(context =>
{
  TryRemoveContainers();
});

public void TryRemoveContainers()
{
  try
  {
      StartProcess("docker", new ProcessSettings { Arguments = $"buildx rm {dockerBuildContainerName}" });
      DockerStop(new string [] { dockerContainerName });
      DockerRm(new string [] { dockerContainerName });
  }
  catch
  {
  }
}

Task("Default")
    .IsDependentOn("Pack");

Task("Test")
  .IsDependentOn("UnitTest")
  .IsDependentOn("IntegrationTest")
  .IsDependentOn("RunEndToEndTests");

Task("Sonar")
  .IsDependentOn("SonarBegin")
  .IsDependentOn("Build")
  .IsDependentOn("Test")
  .IsDependentOn("SonarEnd");

Task("CI")
    .IsDependentOn("Sonar")
    .IsDependentOn("CreateArtifact")
    .IsDependentOn("DockerPush");

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
