import { Injectable } from "@angular/core";
import ODataStore from "devextreme/data/odata/store";
import { ODataService } from "./odata.service";

@Injectable({
  providedIn: "root",
})
export class UserExternalTenantService {
  constructor(private odataService: ODataService) {}

  public getStore(): ODataStore {
    return this.odataService.context["UserExternalTenants"];
  }

  public addTenantPermission(userId: number, tenantId: number) {
    return this.getStore().insert({
      User: userId,
      Tenant: tenantId,
    });
  }

  public removeTenantPermission(userId: number, tenantId: number) {
    return this.getStore().remove({
      User: userId,
      Tenant: tenantId,
    });
  }
}
