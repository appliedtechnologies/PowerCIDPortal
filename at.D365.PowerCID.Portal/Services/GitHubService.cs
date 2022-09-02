using System;
using System.Threading.Tasks;
using at.D365.PowerCID.Portal.Data.Models;
using Microsoft.Extensions.Configuration;
using Octokit;
using Octokit.GraphQL;
using ProductHeaderValue = Octokit.ProductHeaderValue;
using Microsoft.Extensions.Logging;

namespace at.D365.PowerCID.Portal.Services
{
    public class GitHubService
    {

        private readonly int appId;
        private readonly string privateKey;
        private readonly string userAgend;
        private readonly string version;
        private readonly ILogger logger;

        public GitHubService(IConfiguration config, ILogger<GitHubService> logger)
        {
            this.appId = config.GetValue<int>("GitHubApp:Id");
            this.privateKey = config["GitHubApp:PrivateKey"];
            this.userAgend = config["GitHubApp:UserAgend"];
            this.version = GetType().Assembly.GetName().Version.ToString();
            this.logger = logger;
        }

        public GitHubClient GetAppClient()
        {
            logger.LogDebug("Begin: GitHubService GetAppClient()");

            var appJwt = this.GetAppJwt();

            var appClient = new GitHubClient(new ProductHeaderValue(userAgend))
            {
                Credentials = new Credentials(appJwt, AuthenticationType.Bearer)
            };
            logger.LogDebug("End: GitHubService GetAppClient()");

            return appClient;
        }

        public async Task<Tuple<Installation, GitHubClient>> GetInstallationWithClient(int installationId)
        {
            logger.LogDebug($"Begin: GitHubService GetInstallationWithClient(installationId: {installationId})");

            var appClient = this.GetAppClient();
            var installation = await appClient.GitHubApps.GetInstallationForCurrent(installationId);
            var installationTokenResponse = await appClient.GitHubApps.CreateInstallationToken(installationId);

            var installationClient = new GitHubClient(new ProductHeaderValue($"{userAgend}-Installation{installationId}"))
            {
                Credentials = new Credentials(installationTokenResponse.Token)
            };
            logger.LogDebug($"End: GitHubService GetInstallationWithClient(installationId: {installationId})");

            return new Tuple<Installation, GitHubClient>(installation, installationClient);
        }

        public async Task<Octokit.GraphQL.Connection> GetGraphQLConnetion(int installationId)
        {
            logger.LogDebug($"Begin: GitHubService GetGraphQLConnetion(installationId: {installationId})");

            var appClient = this.GetAppClient();
            var installation = await appClient.GitHubApps.GetInstallationForCurrent(installationId);
            var installationTokenResponse = await appClient.GitHubApps.CreateInstallationToken(installationId);

            var productInformation = new Octokit.GraphQL.ProductHeaderValue($"{userAgend}-Installation{installationId}", this.version);
            var connection = new Octokit.GraphQL.Connection(productInformation, installationTokenResponse.Token);

            logger.LogDebug($"End: GitHubService GetGraphQLConnetion(installationId: {installationId})");

            return connection;
        }

        public async Task<byte[]> GetSolutionFileAsByteArray(Tenant tenant, Solution solution)
        {
            logger.LogDebug($"Begin: GitHubService GetSolutionFileAsByteArray(tenant GitHubInstallationId: {tenant.GitHubInstallationId}, solution Name: {solution.Name})");

            (var installation, var installationClient) = await this.GetInstallationWithClient(tenant.GitHubInstallationId);

            string path = $"applications/{solution.ApplicationNavigation.Id}_{solution.ApplicationNavigation.SolutionUniqueName}/{solution.Version}/{solution.Name}_managed.zip";
            string[] gitHubRepositoryName = tenant.GitHubRepositoryName.Split('/');
            string repositoryName = gitHubRepositoryName[1];
            string owner = gitHubRepositoryName[0];

            var connection = await this.GetGraphQLConnetion(tenant.GitHubInstallationId);
            var query = new Query()
                .RepositoryOwner(owner)
                .Repository(repositoryName)
                .Object($"HEAD:{path}")
                .Select(x => new
                {
                    x.Oid
                })
                .Compile();
            var reuslt = await connection.Run(query);

            var solutionZipFileBase64 = (await installationClient.Git.Blob.Get(owner, repositoryName, reuslt.Oid)).Content;

            logger.LogDebug($"End: GitHubService GetSolutionFileAsByteArray(tenant GitHubInstallationId: {tenant.GitHubInstallationId}, solution Name: {solution.Name})");

            return Convert.FromBase64String(solutionZipFileBase64);
        }

        public async Task<string> GetSolutionFileAsBase64String(Tenant tenant, Solution solution)
        {
            logger.LogDebug($"Begin: GitHubService  GetSolutionFileAsBase64String()");

            var solutionZipFile = await this.GetSolutionFileAsByteArray(tenant, solution);

            logger.LogDebug($"End: GitHubService  GetSolutionFileAsBase64String()");

            return Convert.ToBase64String(solutionZipFile);
        }

        public async Task SaveSolutionFile(AsyncJob asyncJob, string exportSolutionFile, Tenant tenant)
        {
            logger.LogDebug($"Begin: GitHubService SaveSolutionFile(asyncJob IsManaged: {asyncJob.IsManaged} exportSolutionFile: {exportSolutionFile}, tenant GitHubInstallationId: {tenant.GitHubInstallationId})");

            (var installation, var installationClient) = await this.GetInstallationWithClient(tenant.GitHubInstallationId);
            (var owner, var repositoryName) = this.SplitOwnerAndRepositoryName(tenant.GitHubRepositoryName);

            string managed = asyncJob.IsManaged == true ? "managed" : "unmanaged";

            // 1. Get the SHA of the latest commit of the main branch.
            var headMasterRef = "heads/main";
            var masterReference = installationClient.Git.Reference.Get(owner, repositoryName, headMasterRef).Result; // Get reference of master branch
            var latestCommit = installationClient.Git.Commit.Get(owner, repositoryName,
            masterReference.Object.Sha).Result; // Get the lastet commit of this branch
            var nt = new NewTree { BaseTree = latestCommit.Tree.Sha };

            //2. Create the blob(s) corresponding to your file(s)
            var textBlob = new NewBlob { Encoding = EncodingType.Base64, Content = exportSolutionFile };
            var textBlobRef = installationClient.Git.Blob.Create(owner, repositoryName, textBlob);

            // 3. Create a new tree with:
            nt.Tree.Add(new NewTreeItem { Path = $"applications/{asyncJob.ActionNavigation.SolutionNavigation.ApplicationNavigation.Id}_{asyncJob.ActionNavigation.SolutionNavigation.ApplicationNavigation.SolutionUniqueName}/{asyncJob.ActionNavigation.SolutionNavigation.Version}/{asyncJob.ActionNavigation.SolutionNavigation.Name}_{managed}.zip", Mode = FileMode.File, Type = TreeType.Blob, Sha = textBlobRef.Result.Sha });
            var newTree = installationClient.Git.Tree.Create(owner, repositoryName, nt).Result;

            // 4. Create the commit with the SHAs of the tree and the reference of master branchS
            // Create Commit
            var newCommit = new NewCommit($"Commit {managed} Export for ActionId {asyncJob.Action} and AsyncJob MsId {asyncJob.JobId}", newTree.Sha, masterReference.Object.Sha);
            var commit = installationClient.Git.Commit.Create(owner, repositoryName, newCommit).Result;

            // 5. Update the reference of master branch with the SHA of the commit
            // Update HEAD with the commit
            await installationClient.Git.Reference.Update(owner, repositoryName, headMasterRef, new ReferenceUpdate(commit.Sha));

            logger.LogDebug($"End: GitHubService SaveSolutionFile(asyncJob IsManaged: {asyncJob.IsManaged} exportSolutionFile: {exportSolutionFile}, tenant GitHubInstallationId: {tenant.GitHubInstallationId})");
        }

        private Tuple<string, string> SplitOwnerAndRepositoryName(string ownerAndRepositoryName)
        {
            logger.LogDebug("Begin: GitHubService SplitOwnerAndRepositoryName(ownerAndRepositoryName: {ownerAndRepositoryName})");

            string[] gitHubRepositoryName = ownerAndRepositoryName.Split('/');
            string owner = gitHubRepositoryName[0];
            string repositoryName = gitHubRepositoryName[1];

            logger.LogDebug("End: GitHubService SplitOwnerAndRepositoryName(ownerAndRepositoryName: {ownerAndRepositoryName})");

            return new Tuple<string, string>(owner, repositoryName);
        }

        private string GetAppJwt()
        {
            logger.LogDebug("Begin: GitHubService GetAppJwt())");

            var generator = new GitHubJwt.GitHubJwtFactory(
                new GitHubJwt.StringPrivateKeySource(this.privateKey),
                new GitHubJwt.GitHubJwtFactoryOptions
                {
                    AppIntegrationId = this.appId,
                    ExpirationSeconds = 600
                }
            );

            var jwtToken = generator.CreateEncodedJwtToken();

            logger.LogDebug("End: GitHubService GetAppJwt())");

            return jwtToken;
        }
    }
}