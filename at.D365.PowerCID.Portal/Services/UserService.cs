using System;
using System.Linq;
using System.Threading.Tasks;
using at.D365.PowerCID.Portal.Data.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace at.D365.PowerCID.Portal.Services
{
    public class UserService
    {
        private readonly ILogger logger;
        private readonly IConfiguration configuration;

        public UserService(ILogger<UserService> logger, IConfiguration configuration)
        {
            this.logger = logger;
            this.configuration = configuration;
        }

        public async Task<Guid> GetSystemUserId(string domainName, string basicUrl){
            logger.LogDebug($"Begin: UserService GetSystemUserId(domainName: {domainName}, basicUrl: {basicUrl})");

            using(var dataverseClient = new ServiceClient(new Uri(basicUrl), configuration["AzureAd:ClientId"], configuration["AzureAd:ClientSecret"], true)){
                var query = new QueryExpression("systemuser");
                query.Criteria.AddCondition("domainname", ConditionOperator.Equal, domainName);

                EntityCollection response = await dataverseClient.RetrieveMultipleAsync(query);

                if(response.Entities.Count == 0)
                    throw new Exception($"Can not find systemuser with domain name {domainName} in environment with url {basicUrl}");

                var systemUserId =  response.Entities.First().Id;

                logger.LogDebug($"End: UserService GetSystemUserId(return {systemUserId})");

                return systemUserId;
            }
        }
    }
}