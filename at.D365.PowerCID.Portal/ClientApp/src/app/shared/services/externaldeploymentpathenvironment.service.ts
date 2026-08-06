import { Injectable } from "@angular/core";
import ODataStore from "devextreme/data/odata/store";
import { ODataService } from "./odata.service";

@Injectable({
  providedIn: "root",
})
export class ExternalDeploymentPathEnvironmentService {
  constructor(private odataService: ODataService) {}

  public getStore(): ODataStore {
    return this.odataService.context["ExternalDeploymentPathEnvironments"];
  }

  public remove(externalDeploymentPathId: number, externalEnvironmentId: number) {
    return new Promise<void>((resolve, reject) => {
      this.getStore()
        .remove({ ExternalDeploymentPath: externalDeploymentPathId, ExternalEnvironment: externalEnvironmentId })
        .then(() => resolve())
        .catch(() => reject());
    });
  }
}
