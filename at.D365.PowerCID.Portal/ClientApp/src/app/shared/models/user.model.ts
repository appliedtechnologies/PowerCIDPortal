import { Tenant } from "./tenant.model";

export interface User {
  Id?: number;
  Firstname?: string;
  Lastname?: string;
  Email?: string;
  MsId?: string;
  Tenant?: number;
  IsOwner?: boolean;
  RemoveAdminRole?: boolean;
  IsDeactive?: boolean;
  
  TenantNavigation?: Tenant;
}
