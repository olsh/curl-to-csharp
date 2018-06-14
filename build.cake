var target = Argument("target", "Default");

var buildConfiguration = "Release";
var projectName = "CurlToCSharp";
var testProjectName = "CurlToCSharp.Tests";
var projectFile = string.Format("./src/{0}/{0}.csproj", projectName);
var testProjectFile = string.Format("./src/{0}/{0}.csproj", testProjectName);
var tempPublishDirectory = "./publish";
var tempPublishArchive = "publish.zip";

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
         Configuration = buildConfiguration
     };

     DotNetCoreTest(testProjectFile, settings);
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

Task("Default")
    .IsDependentOn("Test");

Task("CI")
    .IsDependentOn("Test")
    .IsDependentOn("Pack");

RunTarget(target);
