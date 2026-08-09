import { ExternalDeploymentPath } from "./externaldeploymentpath.model";
import { ExternalEnvironment } from "./externalenvironment.model";

export interface ExternalDeploymentPathEnvironment {
  Id?: number;
  ExternalDeploymentPath?: number;
  ExternalEnvironment?: number;
  StepNumber?: number;
  ExternalDeploymentPathNavigation?: ExternalDeploymentPath;
  ExternalEnvironmentNavigation?: ExternalEnvironment;
}
