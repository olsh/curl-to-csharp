using System.IO;
using System.Text;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Net.Http.Headers;

namespace CurlToCSharp.IntegrationTests
{
    public class Startup
    {
        public void Configure(IApplicationBuilder app)
        {
            app.Map(
                "/echo",
                builder =>
                    {
                        builder.Run(
                            context =>
                                {
                                    var syncIoFeature = context.Features.Get<IHttpBodyControlFeature>();
                                    if (syncIoFeature != null)
                                    {
                                        syncIoFeature.AllowSynchronousIO = true;
                                    }

                                    var request = context.Request;
                                    var response = context.Response;

                                    var stringBuilder = new StringBuilder();
                                    stringBuilder.AppendLine($"{request.Method}: {context.Request.GetDisplayUrl()}");
                                    foreach (var header in request.Headers)
                                    {
                                        // Curl sends these headers by default, but this behavior is not documented
                                        // So we skip the headers for tests
                                        if ((header.Key == HeaderNames.UserAgent && header.Value.ToString().StartsWith("curl"))
                                            || (header.Key == HeaderNames.Accept && header.Value == "*/*"))
                                        {
                                            continue;
                                        }

                                        // Skip these headers for now
                                        if (header.Key == HeaderNames.Expect)
                                        {
                                            continue;
                                        }

                                        stringBuilder.AppendLine(header.ToString());
                                    }

                                    using (var streamReader = new StreamReader(request.Body))
                                    {
                                        stringBuilder.AppendLine(streamReader.ReadToEnd());
                                    }

                                    return response.WriteAsync(stringBuilder.ToString());
                                });
                    });
        }
    }
}
