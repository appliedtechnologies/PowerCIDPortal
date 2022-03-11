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
      roleNameAdmin: string;
      roleNameManager: string;
      roleNameUser: string;
    }
    version: string;
  }
  