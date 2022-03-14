import { ConnectionReference } from "./connectionreference.model";
import { Environment } from "./environment.model";

export interface ConnectionReferenceEnvironment {
    ConnectionReference?: number;
    Environment?: number;
    ConnectionId?: string;
    ConnectionReferenceNavigation?: ConnectionReference;
    EnvironmentNavigation?: Environment;
}