using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using TechBrain;
using TechBrain.Services;

namespace WebApp
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<GzipCompressionProviderOptions>(options => options.Level = CompressionLevel.Optimal);
            services.AddResponseCompression(options => options.Providers.Add<GzipCompressionProvider>());

            services.AddMvc()
                .SetCompatibilityVersion(CompatibilityVersion.Version_2_1)
                .AddJsonOptions(options =>
             {
                 options.SerializerSettings.Formatting = Formatting.Indented;
                 options.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
                 options.SerializerSettings.Converters.Add(new StringEnumConverter { NamingStrategy = new CamelCaseNamingStrategy() });
                 options.SerializerSettings.Error = (object sender, Newtonsoft.Json.Serialization.ErrorEventArgs errorArgs) =>
                     { //Partial serialization
                         //todo Logger.Error("JsonParseError: " + errorArgs.ErrorContext.Error.Message);
                         errorArgs.ErrorContext.Handled = true;
                     };
                 options.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
             });

            services.AddSingleton(serviceProvider => DevServerConfig.ReadFromFile());
            services.AddSingleton<DevServer>();
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env, IServiceProvider serviceProvider)
        {

            app.UseResponseCompression();
            if (env.IsDevelopment())
                app.UseDeveloperExceptionPage();
            else
            {
                //app.UseHsts();
                app.UseExceptionHandler(new ExceptionHandlerOptions
                {
                    ExceptionHandler = context => context.Response.WriteAsync("Some error...")
                });
            }

            app.UseHttpsRedirection();
            app.UseMvc();
            app.Run(async (context) =>
            {
                await context.Response.WriteAsync("Hello World!");
            });

            //var tbConfig = TechBrain.DevServerConfig.ReadFromFile();
            //var devServer = new DevServer(tbConfig);
            //devServer.Start();

            try
            {
                var devServer = serviceProvider.GetService<DevServer>();
                devServer.Start(); //todo set logger
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                //todo Logger.Error("Start CommonCleaner: error.", ex);
            }
        }
    }
}
