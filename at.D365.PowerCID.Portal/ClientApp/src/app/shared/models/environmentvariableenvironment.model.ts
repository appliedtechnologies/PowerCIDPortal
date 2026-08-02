import { Environment } from "./environment.model";
import { EnvironmentVariable } from "./environmentvariable.model";

export interface EnvironmentVariableEnvironment {
    EnvironmentVariable?: number;
    Environment?: number;
    Value?: string;
    EnvironmentVariableNavigation?: EnvironmentVariable;
    EnvironmentNavigation?: Environment;
}