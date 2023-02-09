
/** 
 * Copyright 2017 The Royal Veterinary College, jbullock AT rvc.ac.uk
 * 
 * This file is part of Folium.
 * 
 * Folium is free software: you can redistribute it and/or modify 
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * Folium is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with Folium.  If not, see <http://www.gnu.org/licenses/>.
*/

using Autofac;
using Autofac.Extensions.DependencyInjection;
using EventSaucing;
using EventSaucing.DependencyInjection.Autofac;
using Folium.Api.Extensions;
using Folium.Api.FileHandlers;
using Folium.Api.Infrastructure;
using Folium.Api.Services;
using Hangfire;
using Hangfire.SqlServer;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Serilog;
using System;
using System.IO;
using tusdotnet;
using tusdotnet.Models;
using tusdotnet.Stores;

namespace Folium.Api {
    public class Startup {
        public Startup(IHostingEnvironment env) {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

            if (env.IsDevelopment()) {
                // For more details on using the user secret store see https://go.microsoft.com/fwlink/?LinkID=532709
                builder.AddUserSecrets<Startup>();
            }
            builder.AddEnvironmentVariables();
            Configuration = builder.Build();
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(Configuration)
                .CreateLogger();
        }

        public IConfigurationRoot Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public IServiceProvider ConfigureServices(IServiceCollection services) {
            // Add framework services.
            services.AddMvc().AddJsonOptions(options => {
                options.SerializerSettings.DateTimeZoneHandling = DateTimeZoneHandling.Utc; // Use UTC datetimes.
            });

            // Add functionality to inject IOptions<T>
            services.AddOptions();

            services.AddCors();
            
            // Add our Config object so it can be injected
            services.Configure<ConnectionStrings>(Configuration.GetSection("ConnectionStrings"));

            var sectionName = "ApplicationConfiguration";
            var applicationConfiguration = new Configuration();
            Configuration.GetSection(sectionName).Bind(applicationConfiguration);
            services.Configure<Configuration>(Configuration.GetSection(sectionName));

            // Add application services.
            services.AddSingleton<IDbService, SqlDbService>();
            services.AddSingleton<ICourseService, CourseService>();
            services.AddSingleton<ISkillService, SkillService>();
            services.AddSingleton<ITaxonomyService, TaxonomyService>();
            services.AddSingleton<ISelfAssessmentService, SelfAssessmentService>();
            services.AddSingleton<IUserService, UserService>();
            services.AddSingleton<ITutorGroupService, TutorGroupService>();
            services.AddSingleton<IEntryService, EntryService>();
            services.AddSingleton<IPlacementService, PlacementService>();
            services.AddSingleton<IReportService, ReportService>();
            services.AddTransient<IEmailService, EmailService>();
            services.AddSingleton<IViewRenderService, ViewRenderService>();
            services.AddSingleton<IMessagingService, MessagingService>();
            services.AddSingleton<ITusEventHandler, TusEventHandler>();
            services.AddSingleton<IFileService, FileService>();
            services.AddSingleton<IConfigurationRoot>(Configuration);
            services.AddSingleton<IFileHandler, DefaultFileHandler>();
            services.AddSingleton<IFileHandler, EntryFileHandler>();
            services.AddSingleton<IFileHandler, EntryImageThumbnailFileHandler>();
            services.AddSingleton<IFileHandler, TinyMceFileHandler>();
            services.AddSingleton<IFileHandler, EntryAudioVideoFileHandler>();


            services.AddAuthentication(options =>
            {
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(options => {
                options.Authority = applicationConfiguration.OidcAuthority;
                options.Audience = "folium_app_api";
                options.RequireHttpsMetadata = applicationConfiguration.OidcRequireHttpsMetadata;
            });

            // https://docs.microsoft.com/en-us/dotnet/architecture/microservices/implement-resilient-applications/use-httpclientfactory-to-implement-resilient-http-requests 
            services.AddHttpClient<ICoconutService, CoconutService>();

            var connectionString = Configuration.GetConnectionString("SqlConnectionString");

            // Add hangfire.
            services.AddHangfire(x => x.UseSqlServerStorage(connectionString));

            var builder = new ContainerBuilder();
            builder.RegisterEventSaucingModules(new EventSaucingConfiguration {
                ConnectionString = connectionString
            });
            builder.Populate(services);
            var container = builder.Build();

            container.StartEventSaucing();

            // Initialise the collaborator options.
            container.Resolve<IUserService>().RefreshCollaboratorOptionsAsync();

            return container.Resolve<IServiceProvider>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory, IOptions<Configuration> applicationConfiguration, ITusEventHandler tusEventHandler) {
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();
            loggerFactory.AddSerilog();

            if (env.IsDevelopment()) {
                app.UseDeveloperExceptionPage();
            }
            else {
                app.UseExceptionHandler();
            }

            app.UseCors(builder => builder
                .AllowAnyOrigin()
                .AllowAnyHeader()
                .AllowAnyMethod()
                .WithExposedHeaders(tusdotnet.Helpers.CorsHelper.GetExposedHeaders()));

            app.UseStaticFiles();

            app.UseAuthentication();

            app.UseMvc(routes => {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });

            var options = new BackgroundJobServerOptions {
                Queues = new[] { "email" }
            };

            app.UseHangfireServer(options);
            app.UseHangfireDashboard();

            // Path to where to store the file attachments.
            var path = Path.Combine(env.ContentRootPath, applicationConfiguration.Value.TusFiles);
            app.UseTus(httpContext => new DefaultTusConfiguration {
                Store = new TusDiskStore(path, deletePartialFilesOnConcat: true),
                // On what url should we listen for uploads?
                UrlPath = applicationConfiguration.Value.TusUrlPath,
                Events = tusEventHandler.Events,
            });
            app.UseFileStore();
        }

    }
}
