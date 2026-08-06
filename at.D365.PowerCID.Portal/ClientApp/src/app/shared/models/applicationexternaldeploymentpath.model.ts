import { Application } from "./application.model";
import { ExternalDeploymentPath } from "./externaldeploymentpath.model";

export interface ApplicationExternalDeploymentPath {
  Application?: number;
  ExternalDeploymentPath?: number;
  HierarchieNumber?: number;
  ApplicationNavigation?: Application;
  ExternalDeploymentPathNavigation?: ExternalDeploymentPath;
}
