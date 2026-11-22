import { HttpClient } from "@angular/common/http";
import { Injectable } from "@angular/core";
import ODataStore from "devextreme/data/odata/store";
import { firstValueFrom } from "rxjs";
import { AppConfig } from "../config/app.config";
import { ODataService } from "./odata.service";

@Injectable({
  providedIn: "root",
})
export class ExternalDeploymentPathEnvironmentService {
  constructor(private odataService: ODataService, private http: HttpClient) {}

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

  public insertWithExpectedOrder(
    externalDeploymentPathId: number,
    externalEnvironmentId: number,
    stepNumber: number,
    expectedOrder: number[]
  ): Promise<void> {
    return firstValueFrom(this.http.post<void>(
      `${AppConfig.settings.api.url}/ExternalDeploymentPathEnvironments/InsertWithExpectedOrder`,
      { externalDeploymentPathId, externalEnvironmentId, stepNumber, expectedOrder }
    ));
  }

  public moveWithExpectedOrder(
    externalDeploymentPathId: number,
    externalEnvironmentId: number,
    fromIndex: number,
    toIndex: number,
    expectedOrder: number[]
  ): Promise<void> {
    return firstValueFrom(this.http.post<void>(
      `${AppConfig.settings.api.url}/ExternalDeploymentPathEnvironments/MoveWithExpectedOrder`,
      { externalDeploymentPathId, externalEnvironmentId, fromIndex, toIndex, expectedOrder }
    ));
  }
}
