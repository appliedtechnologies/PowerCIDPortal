export interface IAppConfig {
    logging: {
      console: boolean;
    }
    api: {
      url: string;
    },
    github:{
      installation_url: string;
    },
    azure:{
      applicationId: string;
      appRoleNames: {
        admin: string,
        manager: string,
        user: string,
        externalReleaseManager: string,
        externalDeployer: string
      },
      appRoleIds: {
        admin: string,
        manager: string,
        user: string,
        externalReleaseManager: string,
        externalDeployer: string
      }
    }
    version: string;
  }
  