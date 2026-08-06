import { Injectable } from "@angular/core";
import ODataStore from "devextreme/data/odata/store";
import { ExternalDeploymentPath } from "../models/externaldeploymentpath.model";
import { ODataService } from "./odata.service";

@Injectable({
  providedIn: "root",
})
export class ExternalDeploymentPathService {
  constructor(private odataService: ODataService) {}

  public getStore(): ODataStore {
    return this.odataService.context["ExternalDeploymentPaths"];
  }

  public update(id: number, externalDeploymentPath: ExternalDeploymentPath) {
    return new Promise<void>((resolve, reject) => {
      this.getStore()
        .update(id, externalDeploymentPath)
        .then(() => resolve())
        .catch((error: Error) => reject(error));
    });
  }

  public add(externalDeploymentPath: ExternalDeploymentPath) {
    return new Promise<void>((resolve, reject) => {
      this.getStore()
        .insert(externalDeploymentPath)
        .then(() => resolve())
        .catch((error: Error) => reject(error));
    });
  }

  public remove(id: number) {
    return new Promise<void>((resolve, reject) => {
      this.getStore()
        .remove(id)
        .then(() => resolve())
        .catch((error) => reject(error));
    });
  }
}
