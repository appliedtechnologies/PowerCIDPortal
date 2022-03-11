import { Injectable } from "@angular/core";
import ODataStore from "devextreme/data/odata/store";
import { Patch } from "../models/patch.model";
import { ODataService } from "./odata.service";

@Injectable({
  providedIn: "root",
})
export class PatchService {
  constructor(private odataService: ODataService) {}

  getStore(): ODataStore {
    return this.odataService.context["Patches"];
  }

  getStoreById(Id: number): Patch {
    let patch: Patch;
    this.odataService.context["Patches"]
      .load({
        expand: [
          "CreatedByNavigation",
          "ModifiedByNavigation",
          "ApplicationNavigation",
        ],
        filter: "Id eq " + Id,
      })
      .then((p) => (patch = p));
    return patch;
  }
}
