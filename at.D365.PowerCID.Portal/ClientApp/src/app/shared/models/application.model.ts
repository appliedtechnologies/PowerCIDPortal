import { User } from "./user.model";
import { Environment } from "./environment.model";
import { Publisher } from "./publisher.model";
import { DeploymentPath } from "./deploymentpath.model";
import { ApplicationDeploymentPath } from "./applicationdeploymentpath.model";
import { Solution } from "./solution.model";

export interface Application {
  Id?: number;
  OrdinalNumber?: number | null;
  Name?: string;
  MsId?: string;
  SolutionUniqueName?: string;
  DevelopmentEnvironment?: number;
  InternalDescription?: string;
  Publisher?: number;
  CreatedBy?: number;
  CreatedOn?: string;
  ModifiedBy?: number;
  ModifiedOn?: string;
  DevelopmentEnvironmentNavigation?: Environment;
  PublisherNavigation?: Publisher;
  CreatedByNavigation?: User;
  ModifiedByNavigation?: User;
  DeploymentPaths?: DeploymentPath[];
  ApplicationDeploymentPaths?: ApplicationDeploymentPath[];
  Solutions? : Solution[];
  IsDeactive?: boolean;
  ForceManagedDeployment? : boolean;
  AfterDeploymentInformation?: string;
}
