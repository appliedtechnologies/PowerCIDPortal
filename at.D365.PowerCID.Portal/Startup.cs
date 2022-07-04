using at.D365.PowerCID.Portal.Data.Models;
using at.D365.PowerCID.Portal.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.SpaServices.AngularCli;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.OData;
using Microsoft.AspNetCore.OData.Extensions;
using Microsoft.OData.Edm;
using Microsoft.OData.ModelBuilder;
using Microsoft.Identity.Web;

namespace at.D365.PowerCID.Portal
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        private static IEdmModel GetEdmModel()
        {
            ODataConventionModelBuilder builder = new ODataConventionModelBuilder();
            builder.EntitySet<Data.Models.Action>("Actions");
            builder.EntitySet<ActionType>("ActionTypes");
            builder.EntitySet<ActionStatus>("ActionStatus");
            builder.EntitySet<ActionResult>("ActionResults");
            builder.EntitySet<Patch>("Patches");
            builder.EntitySet<Upgrade>("Upgrades");
            builder.EntitySet<User>("Users");
            builder.EntitySet<Tenant>("Tenants");
            builder.EntitySet<Data.Models.Environment>("Environments");
            builder.EntitySet<Application>("Applications");
            builder.EntitySet<Solution>("Solutions");
            builder.EntitySet<Publisher>("Publishers");
            builder.EntitySet<DeploymentPath>("DeploymentPaths");
            builder.EntitySet<ConnectionReference>("ConnectionReferences");
            builder.EntitySet<EnvironmentVariable>("EnvironmentVariables");
            builder.EntitySet<AsyncJob>("AsyncJobs");

            builder.EntitySet<EnvironmentVariableEnvironment>("EnvironmentVariableEnvironments").EntityType.HasKey(e => new { e.EnvironmentVariable, e.Environment });
            builder.EntitySet<ConnectionReferenceEnvironment>("ConnectionReferenceEnvironments").EntityType.HasKey(e => new { e.ConnectionReference, e.Environment });
            builder.EntitySet<DeploymentPathEnvironment>("DeploymentPathEnvironments").EntityType.HasKey(e => new { e.DeploymentPath, e.Environment }).Property(e => e.Id);
            builder.EntitySet<ApplicationDeploymentPath>("ApplicationDeploymentPaths").EntityType.HasKey(ad => new { ad.Application, ad.DeploymentPath });
            builder.EntitySet<UserEnvironment>("UserEnvironments").EntityType.HasKey(e => new { e.User, e.Environment });

            builder.EntityType<User>().Collection.Action("Login");

            builder.EntityType<Application>().Action("GetMakerPortalUrl");

            //Get Environment Variables
            var getEnvironmentVariablesForApplication = builder.EntityType<EnvironmentVariable>().Collection.Action("GetEnvironmentVariablesForApplication");
            getEnvironmentVariablesForApplication.Parameter<int>("applicationId");

            //Get Connection References
            var getConnectionReferencesForApplication = builder.EntityType<ConnectionReference>().Collection.Action("GetConnectionReferencesForApplication");
            getConnectionReferencesForApplication.Parameter<int>("applicationId");

            //Get Status Connection Reference Environment
            var getStatusConnectionReferenceEnvironment = builder.EntityType<Application>().Action("GetDeploymentSettingsStatus");
            getStatusConnectionReferenceEnvironment.Parameter<int>("environmentId");

            // Get Roles
            builder.EntityType<User>().Action("GetUserRoles");
            builder.EntityType<User>().Action("GetAppRoles");

            builder.EntityType<User>().Collection.Action("SetupApplicationUsers");

            //Assign / remove roles
            builder.EntityType<User>().Action("RemoveAssignedRole").Parameter<string>("roleAssignmentId");
            var assignRoles = builder.EntityType<User>().Action("AssignRole");
            assignRoles.Parameter<string>("principalId");
            assignRoles.Parameter<string>("appRoleId");

            builder.EntityType<Environment>().Collection.Action("PullExisting");
            builder.EntityType<Environment>().Action("GetDataversePublishers");

            builder.EntityType<Application>().Collection.Action("PullExisting").Parameter<Environment>("environment");
            var addApplication = builder.EntityType<Application>().Collection.Action("SaveApplication");
            addApplication.Parameter<string>("applicationUniqueName");
            addApplication.Parameter<Environment>("environment");

            builder.EntityType<Solution>().Action("Export").ReturnsFromEntitySet<Action>("Actions");
            var import = builder.EntityType<Solution>().Action("Import").ReturnsFromEntitySet<Action>("Actions");
            import.Parameter<int>("targetEnvironmentId");
            import.Parameter<int>("deploymentPathId");

            builder.EntityType<Solution>().Action("GetSolutionAsBase64String");


            builder.EntityType<Tenant>().Action("GetGitHubRepositories");

            return builder.GetEdmModel();
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddHttpContextAccessor();
            services.AddMicrosoftIdentityWebApiAuthentication(Configuration, "AzureAd")
                .EnableTokenAcquisitionToCallDownstreamApi()
                .AddDownstreamWebApi("AzureManagementApi", Configuration.GetSection("DownstreamApis:AzureManagementApi"))
                .AddDownstreamWebApi("DataverseApi", Configuration.GetSection("DownstreamApis:DataverseApi"))
                .AddDownstreamWebApi("GraphApi", Configuration.GetSection("DownstreamApis:GraphApi"))
                .AddInMemoryTokenCaches();
            // In production, the Angular files will be served from this directory
            services.AddSpaStaticFiles(configuration =>
            {
                configuration.RootPath = "ClientApp/dist";
            });
            services.AddDbContext<atPowerCIDContext>(options =>
                options.UseLazyLoadingProxies().UseSqlServer(Configuration.GetConnectionString("atPowerCIDPortal")));

            services.AddControllers().AddOData(options => options.AddRouteComponents("odata", GetEdmModel()).Select().Count().Filter().OrderBy().Expand().SetMaxTop(100));

            //custom services
            services.AddScoped<GitHubService>();
            services.AddScoped<SolutionService>();
            services.AddScoped<ConnectionReferenceService>();
            services.AddScoped<EnvironmentVariableService>();

            services.AddHostedService<AsyncJobService>();
            services.AddHostedService<ActionService>();

            //logger
            services.AddApplicationInsightsTelemetry();

        }


        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, atPowerCIDContext dbContext)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                dbContext.Database.MigrateAsync();

                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            if (!env.IsDevelopment())
            {
                app.UseSpaStaticFiles();
            }

            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });

            app.UseSpa(spa =>
            {
                // To learn more about options for serving an Angular SPA from ASP.NET Core,
                // see https://go.microsoft.com/fwlink/?linkid=864501

                spa.Options.SourcePath = "ClientApp";

                if (env.IsDevelopment())
                {
                    spa.UseProxyToSpaDevelopmentServer(System.Environment.GetEnvironmentVariable("ASPNETCORE_ANGULAR_URL"));
                }
            });
        }
    }
}
