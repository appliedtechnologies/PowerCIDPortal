import { Environment } from "./environment.model";
import { ExternalDeploymentPath } from "./externaldeploymentpath.model";
import { ExternalDeploymentPathEnvironment } from "./externaldeploymentpathenvironment.model";
import { User } from "./user.model";

export interface ExternalEnvironment {
  Id?: number;
  Environment?: number;
  Alias?: string;
  IsDeactive?: boolean;
  CreatedBy?: number;
  CreatedOn?: string;
  ModifiedBy?: number;
  ModifiedOn?: string;
  EnvironmentNavigation?: Environment;
  CreatedByNavigation?: User;
  ModifiedByNavigation?: User;
  ExternalDeploymentPaths?: ExternalDeploymentPath[];
  ExternalDeploymentPathEnvironments?: ExternalDeploymentPathEnvironment[];
}
