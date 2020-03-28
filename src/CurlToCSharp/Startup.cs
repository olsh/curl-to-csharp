using CurlToCSharp.Infrastructure;
using CurlToCSharp.Models;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace CurlToCSharp
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<ApplicationOptions>(Configuration);
            services.AddControllersWithViews();
            services.RegisterServices();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/error");
            }

            app.UseStaticFiles(
                new StaticFileOptions
                    {
                        OnPrepareResponse = ctx =>
                            {
                                ctx.Context.Response.Headers.Append(
                                    "Cache-Control",
                                    "public,max-age=31536000");
                            }
                    });

            app.UseRouting();
            app.UseEndpoints(builder => builder.MapControllers());
        }
    }
}
