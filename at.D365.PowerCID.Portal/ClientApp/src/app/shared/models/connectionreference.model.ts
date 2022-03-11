import { Application } from "./application.model";
import { ConnectionReferenceEnvironment } from "./connectionreferenceenvironment.model";
import { Environment } from "./environment.model";
import { User } from "./user.model";

export interface ConnectionReference {
    Id: number;
    Application: number;
    MsId: string;
    LogicalName: string;
    ConnectorId: string;
    DisplayName: string;
    CreatedBy: number;
    CreatedOn: string;
    ModifiedBy: number;
    ModifiedOn: string;
    CreatedByNavigation: User;
    ModifiedByNavigation: User;
    ApplicationNavigation: Application;
    Environments: Environment[];
    ConnectionReferenceEnvironments: ConnectionReferenceEnvironment[];
}