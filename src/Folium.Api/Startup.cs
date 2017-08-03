
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
using System;
using System.IO;
using Autofac;
using EventSaucing;
using EventSaucing.DependencyInjection.Autofac;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Folium.Api.Services;
using Autofac.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Serilog;
using Hangfire;
using Hangfire.Common;

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
				.WriteTo.RollingFile(Path.Combine(env.ContentRootPath, "logs", Configuration.GetValue<string>("Serilog:LogFilePattern")))
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
            
            // Add our Config object so it can be injected
            services.Configure<ConnectionStrings>(Configuration.GetSection("ConnectionStrings"));
            services.Configure<Configuration>(Configuration.GetSection("ApplicationConfiguration"));

            // Add application services.
            services.AddSingleton<IDbService, SqlDbService>();
            services.AddSingleton<ICourseService, CourseService>();
            services.AddSingleton<ISkillService, SkillService>();
            services.AddSingleton<ITaxonomyService, TaxonomyService>();
            services.AddSingleton<ISelfAssessmentService, SelfAssessmentService>();
            services.AddSingleton<IUserService, UserService>();
			services.AddSingleton<IEntryService, EntryService>();
			services.AddSingleton<IPlacementService, PlacementService>();
            services.AddSingleton<IEmailService, EmailService>();
            services.AddSingleton<IViewRenderService, ViewRenderService>();
            services.AddSingleton<IConfigurationRoot>(Configuration);

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
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory, IOptions<Configuration> applicationConfiguration) {
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();
	        loggerFactory.AddSerilog();

			if (env.IsDevelopment()) {
                app.UseDeveloperExceptionPage();
            } else {
                app.UseExceptionHandler();
            }

			app.UseCors(builder => builder
				.AllowAnyOrigin()
				.AllowAnyHeader()
				.AllowAnyMethod()
				.AllowCredentials());
                
            app.UseStaticFiles();
           
            app.UseIdentityServerAuthentication(
                new IdentityServerAuthenticationOptions {
                    Authority = applicationConfiguration.Value.OidcAuthority,
                    RequireHttpsMetadata = applicationConfiguration.Value.OidcRequireHttpsMetadata,
                    ApiName = "folium_app_api"
                });

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
        }
    }
}
