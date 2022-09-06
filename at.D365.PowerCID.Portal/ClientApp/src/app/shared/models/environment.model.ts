import { User } from "./user.model";
import { Tenant } from "./tenant.model";

export interface Environment {
  Id?: number;
  OrdinalNumber?: number | null;
  Name?: string;
  BasicUrl?: string;
  ConnectionsOwner?: string;
  IsDevelopmentEnvironment?: boolean;
  MsId?: string;
  Tenant?: number;
  CreatedBy?: number;
  CreatedOn?: string;
  ModifiedBy?: number;
  ModifiedOn?: string;
  CreatedByNavigation?: User;
  ModifiedByNavigation?: User;
  TenantNavigation?: Tenant;
}
