import { Tenant } from "./tenant.model";
import { User } from "./user.model";

export interface UserExternalTenant {
  User?: number;
  Tenant?: number;
  UserNavigation?: User;
  TenantNavigation?: Tenant;
}
