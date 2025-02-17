using auth.Service;
using AutoMapper;
using LoanPledgingTool.Helpers;
using LoanPledgingTool.Models;
using LoanPledgingTool.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SpaServices.ReactDevelopmentServer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NF.Platform.Infrastructure.Logging;
using System;
using System.Linq;
using System.Text;

namespace LoanPledgingTool
{
	public class Startup
	{
		public Startup(IConfiguration configuration)
		{
			Configuration = configuration;
		}

		public IConfiguration Configuration { get; }

		// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
		public void Configure(IApplicationBuilder app, IHostingEnvironment env)
		{
			app.UseExceptionHandler(a => a.Run(async context =>
			{
				var exceptionFeature = context.Features.Get<IExceptionHandlerPathFeature>();
				var exception = exceptionFeature.Error;

                string message;
                if (exception.GetType() == typeof(AggregateException))
                    message = ((AggregateException)exception).Flatten().Message;
                else
                    message = exception.Message;

				Logger.Instance.LogError(0, exception, $"{exceptionFeature.Error} error: {message} and stack trace: {exceptionFeature.Error.StackTrace}");
				var result = JsonConvert.SerializeObject(
					new
					{
						Message = message
                    });
				context.Response.ContentType = "application/json";
				await context.Response.WriteAsync(result);
			}));

			app.UseHealthChecks("/api/ping", new HealthCheckOptions()
			{
				ResponseWriter = (HttpContext context, HealthReport result) =>
				{
					var json = new JObject(
						new JProperty("status", result.Status.ToString()),
						new JProperty("name", env.ApplicationName),
						new JProperty("buildConfiguration", env.IsEnvironment("Local") ? "DEBUG" : "RELEASE"),
						new JProperty("runtimeEnvironment", env.EnvironmentName),
						new JProperty("results", new JObject(result.Entries.Select(pair =>
						new JProperty(pair.Key, new JObject(
							new JProperty("status", pair.Value.Status.ToString()),
							new JProperty("description", pair.Value.Description),
							new JProperty("data", new JObject(pair.Value.Data.Select(
								p => new JProperty(p.Key, p.Value))))))))));

					return context.Response.WriteAsync(json.ToString(Formatting.Indented));
				}
			});

			if (!env.IsDevelopment())
			{
				// The default HSTS value is 30 days. You may want to change this for production
				// scenarios, see https://aka.ms/aspnetcore-hsts.
				app.UseHsts();
			}

			app.UseHttpsRedirection();
			app.UseStaticFiles();
			app.UseSpaStaticFiles();
			app.UseSession();
			app.UseCookiePolicy();
			app.UseAuthentication();
			app.UseMvc(routes =>
			{
				routes.MapRoute(
					name: "default",
					template: "{controller}/{action=Index}/{id?}");
			});

			app.UseSpa(spa =>
			{
				spa.Options.SourcePath = "ClientApp";

				if (env.IsEnvironment("Local"))
				{
					spa.UseReactDevelopmentServer(npmScript: "start");
				}
			});
		}

		// This method gets called by the runtime. Use this method to add services to the container.
		public void ConfigureServices(IServiceCollection services)
		{
			services.AddDistributedMemoryCache();

			services.AddSession(options =>
			{
				// Set a short timeout for easy testing.
				options.IdleTimeout = TimeSpan.FromSeconds(5);
				options.Cookie.HttpOnly = true;
				// Make the session cookie essential
				options.Cookie.IsEssential = true;
			});
			services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
			services.AddHealthChecks().AddDbContextCheck<ApolloContext>();
			var apolloConnectionString = @"Data Source=(localdb)\ProjectsV13;Initial Catalog=Apollo;
										Integrated Security=True;Connect Timeout=30;Encrypt=False;
										TrustServerCertificate=False;ApplicationIntent=ReadWrite;MultiSubnetFailover=False";
			services.AddDbContext<ApolloContext>(options => options.UseSqlServer(apolloConnectionString));
			services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
			// In production, the React files will be served from this directory
			services.AddSpaStaticFiles(configuration =>
			{
				configuration.RootPath = "ClientApp/build";
			});

			var appSettingSection = Configuration.GetSection("AppSettings");
			services.Configure<AppSettings>(appSettingSection);
			var appSettings = appSettingSection.Get<AppSettings>();

			var key = Encoding.ASCII.GetBytes(appSettings.Secret);
			services.AddAuthentication(x =>
			{
				x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
				x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
			})
			.AddJwtBearer(x =>
			{
				x.RequireHttpsMetadata = false;
				x.SaveToken = true;
				x.TokenValidationParameters = new TokenValidationParameters
				{
					ValidateIssuerSigningKey = true,
					IssuerSigningKey = new SymmetricSecurityKey(key),
					ValidateIssuer = false,
					ValidateAudience = false,
				};
			});

			services.AddScoped<IUserService, UserService>();
			services.AddScoped<IPledgingService, PledgingService>();
			services.AddScoped<IReportService, ReportService>();
		}
	}
}