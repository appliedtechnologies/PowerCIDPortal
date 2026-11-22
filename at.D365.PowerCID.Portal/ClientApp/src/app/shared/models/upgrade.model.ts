import { Solution } from "./solution.model";
import { DeploymentMode } from "./deployment-mode.model";

export interface Upgrade extends Solution {
  DefaultDeploymentMode?: DeploymentMode;
}
