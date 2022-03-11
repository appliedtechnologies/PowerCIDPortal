import { Application } from "./application.model";
import { Environment } from "./environment.model";
import { EnvironmentVariableEnvironment } from "./environmentvariableenvironment.model";
import { User } from "./user.model";

export interface EnvironmentVariable {
    Id?: number;
    Application?: number;
    MsId?: string;
    LogicalName?: string;
    DisplayName?: string;
    CreatedBy?: number;
    CreatedOn?: string;
    ModifiedBy?: number;
    ModifiedOn?: string;
    CreatedByNavigation?: User;
    ModifiedByNavigation?: User;
    ApplicationNavigation?: Application;
    Environments?: Environment[];
    EnvironmentVariableEnvironments?: EnvironmentVariableEnvironment[];
}