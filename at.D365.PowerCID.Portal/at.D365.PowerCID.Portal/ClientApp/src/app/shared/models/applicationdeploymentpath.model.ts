import { Application } from "./application.model";
import { DeploymentPath } from "./deploymentpath.model";

export interface ApplicationDeploymentPath {
  Application?: number;
  DeploymentPath?: number;
  HierarchieNumber?: number;
  ApplicationNavigation?: Application;
  DeploymentPathNavigation?: DeploymentPath;
}
