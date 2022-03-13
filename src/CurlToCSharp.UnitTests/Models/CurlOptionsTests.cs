using CurlToCSharp.Models;

namespace CurlToCSharp.UnitTests.Models;

public class CurlOptionsTests
{
    [Fact]
    public void GetFullUrl_ForceGetDefaultPort_Success()
    {
        var curlOptions = new CurlOptions();

        curlOptions.Url = new Uri("https://google.com/");
        curlOptions.ForceGet = true;
        curlOptions.UploadData.Add(new UploadData("test"));

        Assert.Equal("https://google.com/?test", curlOptions.GetFullUrl());
    }

    [Fact]
    public void GetFullUrl_ForceGetNonDefaultPort_Success()
    {
        var curlOptions = new CurlOptions();

        curlOptions.Url = new Uri("https://google.com:1986/");
        curlOptions.ForceGet = true;
        curlOptions.UploadData.Add(new UploadData("test"));

        Assert.Equal("https://google.com:1986/?test", curlOptions.GetFullUrl());
    }
}
