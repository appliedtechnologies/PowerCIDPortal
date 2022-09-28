import { Injectable } from "@angular/core";
import { DxExtendedPromise } from "devextreme/core/utils/deferred";
import ODataStore from "devextreme/data/odata/store";
import { Upgrade } from "../models/upgrade.model";
import { ODataService } from "./odata.service";

@Injectable({
  providedIn: "root",
})
export class UpgradeService {
  constructor(private odataService: ODataService) {}

  public getStore(): ODataStore {
    return this.odataService.context["Upgrades"];
  }

  public add(upgrade: Upgrade): Promise<void>{
    return this.getStore().insert(upgrade);
  }

  public update(id: number, upgrade: Upgrade): Promise<void>{
    return this.getStore().update(id, upgrade);
  }
}
