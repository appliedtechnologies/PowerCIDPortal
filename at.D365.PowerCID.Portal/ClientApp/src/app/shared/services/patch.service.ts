import { Injectable } from "@angular/core";
import { DxExtendedPromise } from "devextreme/core/utils/deferred";
import ODataStore from "devextreme/data/odata/store";
import { Patch } from "../models/patch.model";
import { ODataService } from "./odata.service";

@Injectable({
  providedIn: "root",
})
export class PatchService {
  constructor(private odataService: ODataService) {}

  public getStore(): ODataStore {
    return this.odataService.context["Patches"];
  }

  public getStoreById(id: number): Patch {
    let patch: Patch;
    this.odataService.context["Patches"]
      .load({
        expand: [
          "CreatedByNavigation",
          "ModifiedByNavigation",
          "ApplicationNavigation",
        ],
        filter: "Id eq " + id,
      })
      .then((p) => (patch = p));
    return patch;
  }

  public add(patch: Patch): Promise<void>{
    return this.getStore().insert(patch);
  }

  public update(id: number, patch: Patch): Promise<void>{
    return this.getStore().update(id, patch);
  }

  public delete(id: number): Promise<void>{
    return this.getStore().remove(id);
  }
}
