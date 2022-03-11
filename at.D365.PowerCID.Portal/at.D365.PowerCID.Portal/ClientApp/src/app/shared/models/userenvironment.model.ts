import { User } from "oidc-client";
import { Environment } from "./environment.model";

export interface UserEnvironment {
    User?: number;
    Environment?: number;
    UserNavigation?: User;
    EnvironmentNavigation?: Environment;
}
  