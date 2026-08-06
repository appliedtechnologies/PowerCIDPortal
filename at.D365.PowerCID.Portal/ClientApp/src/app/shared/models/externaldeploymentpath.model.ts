import { Application } from "./application.model";
import { ApplicationExternalDeploymentPath } from "./applicationexternaldeploymentpath.model";
import { ExternalDeploymentPathEnvironment } from "./externaldeploymentpathenvironment.model";
import { ExternalEnvironment } from "./externalenvironment.model";
import { Tenant } from "./tenant.model";
import { User } from "./user.model";

export interface ExternalDeploymentPath {
  Id?: number;
  Name?: string;
  Tenant?: number;
  CreatedBy?: number;
  CreatedOn?: string;
  ModifiedBy?: number;
  ModifiedOn?: string;
  CreatedByNavigation?: User;
  ModifiedByNavigation?: User;
  TenantNavigation?: Tenant;
  ExternalEnvironments?: ExternalEnvironment[];
  ExternalDeploymentPathEnvironments?: ExternalDeploymentPathEnvironment[];
  Applications?: Application[];
  ApplicationExternalDeploymentPaths?: ApplicationExternalDeploymentPath[];
}
