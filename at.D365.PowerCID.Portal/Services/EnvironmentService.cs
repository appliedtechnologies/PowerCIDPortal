using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using at.D365.PowerCID.Portal.Data.Models;
using at.D365.PowerCID.Portal.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Client;
using Microsoft.Identity.Web;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Octokit.GraphQL;
using Action = at.D365.PowerCID.Portal.Data.Models.Action;
using Microsoft.Extensions.Logging;

namespace at.D365.PowerCID.Portal.Services
{
    public class EnvironmentService
    {
        private readonly ILogger logger;
        private readonly atPowerCIDContext dbContext;
        private readonly IConfiguration configuration;

        public EnvironmentService(ILogger<SolutionService> logger, atPowerCIDContext dbContext, IConfiguration configuration)
        {
            this.logger = logger;
            this.dbContext = dbContext;
            this.configuration = configuration;
        }

        internal void PublishAllCustomizations(string basicUrl)
        {
            logger.LogDebug($"Begin: EnvironmentService PublishAllCustomizations(basicUrl: {basicUrl})");

            using (var dataverseClient = new ServiceClient(new Uri(basicUrl), configuration["AzureAd:ClientId"], configuration["AzureAd:ClientSecret"], true))
            {
                var request = new PublishAllXmlAsyncRequest { RequestId = Guid.NewGuid() };
                dataverseClient.Execute(request);
            }

            logger.LogDebug($"End: EnvironmentService PublishAllCustomizations()");
        }
    }
}