using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Octokit;

namespace at.D365.PowerCID.Portal.Services
{
    public class GitHubService{

        private readonly int appId;
        private readonly string privateKey;
        private readonly string userAgend;
        private readonly string version;

        public GitHubService(IConfiguration config)
        {
            this.appId = config.GetValue<int>("GitHubApp:Id");
            this.privateKey = config["GitHubApp:PrivateKey"];
            this.userAgend = config["GitHubApp:UserAgend"];
            this.version = GetType().Assembly.GetName().Version.ToString();
        }

        public GitHubClient GetAppClient(){
            var appJwt = this.GetAppJwt();

            var appClient = new GitHubClient(new ProductHeaderValue(userAgend)){
                Credentials = new Credentials(appJwt, AuthenticationType.Bearer)
            };

            return appClient;
        }

        public async Task<Tuple<Installation, GitHubClient>> GetInstallationWithClient(int installationId){
            var appClient = this.GetAppClient();
            var installation = await appClient.GitHubApps.GetInstallationForCurrent(installationId);
            var installationTokenResponse = await appClient.GitHubApps.CreateInstallationToken(installationId);

            var installationClient = new GitHubClient(new ProductHeaderValue($"{userAgend}-Installation{installationId}")){
                Credentials = new Credentials(installationTokenResponse.Token)
            };        

            return new Tuple<Installation, GitHubClient>(installation,installationClient);
        }

        public async Task<Octokit.GraphQL.Connection> GetGraphQLConnetion(int installationId){
            var appClient = this.GetAppClient();
            var installation = await appClient.GitHubApps.GetInstallationForCurrent(installationId);
            var installationTokenResponse = await appClient.GitHubApps.CreateInstallationToken(installationId);

            var productInformation = new Octokit.GraphQL.ProductHeaderValue($"{userAgend}-Installation{installationId}", this.version);
            var connection = new Octokit.GraphQL.Connection(productInformation, installationTokenResponse.Token);      

            return connection;
        }

        private string GetAppJwt(){
            var generator = new GitHubJwt.GitHubJwtFactory(
                new GitHubJwt.StringPrivateKeySource(this.privateKey),
                new GitHubJwt.GitHubJwtFactoryOptions{
                    AppIntegrationId = this.appId,
                    ExpirationSeconds = 600
                }
            );

            var jwtToken = generator.CreateEncodedJwtToken();
            return jwtToken;
        }
    }
}