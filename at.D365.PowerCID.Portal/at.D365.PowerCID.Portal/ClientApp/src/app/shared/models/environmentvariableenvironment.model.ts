import { Environment } from "./environment.model";
import { EnvironmentVariable } from "./environmentvariable.model";
import { User } from "./user.model";

export interface EnvironmentVariableEnvironment {
    EnvironmentVariable?: number;
    Environment?: number;
    Value?: string;
    EnvironmentVariableNavigation?: EnvironmentVariable;
    EnvironmentNavigation?: Environment;
}