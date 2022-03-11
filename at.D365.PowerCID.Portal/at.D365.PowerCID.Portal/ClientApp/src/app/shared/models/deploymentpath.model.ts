import { Application } from "./application.model";
import { ApplicationDeploymentPath } from "./applicationdeploymentpath.model";
import { DeploymentPathEnvironment } from "./deploymentpathenvironment.model";
import { Environment } from "./environment.model";
import { User } from "./user.model";

export interface DeploymentPath {
  Id?: number;
  Name?: string;
  CreatedBy?: number;
  CreatedOn?: string;
  ModifiedBy?: number;
  ModifiedOn?: string;
  CreatedByNavigation?: User;
  ModifiedByNavigation?: User;
  Environments?: Environment[];
  DeploymentPathEnvironments?: DeploymentPathEnvironment[];
  Applications?: Application;
  ApplicationDeploymentPaths?: ApplicationDeploymentPath[];
}
