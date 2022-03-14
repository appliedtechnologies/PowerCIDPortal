import { DeploymentPath } from "./deploymentpath.model";
import { Environment } from "./environment.model";

export interface DeploymentPathEnvironment {
    Id?: number;
    DeploymentPath?: number;
    Environment?: number;
    StepNumber?: number;
    DeploymentPathNavigation?: DeploymentPath;
    EnvironmentNavigation?: Environment;    
}