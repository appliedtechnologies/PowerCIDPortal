import { HttpClient } from "@angular/common/http";
import { Injectable } from "@angular/core";
import ODataStore from "devextreme/data/odata/store";
import { firstValueFrom } from "rxjs";
import { AppConfig } from "../config/app.config";
import { ODataService } from "./odata.service";

@Injectable({
  providedIn: "root",
})
export class ApplicationExternalDeploymentPathService {
  constructor(private odataService: ODataService, private http: HttpClient) {}

  public getStore(): ODataStore {
    return this.odataService.context["ApplicationExternalDeploymentPaths"];
  }

  public insertWithExpectedOrder(
    applicationId: number,
    externalDeploymentPathId: number,
    hierarchyNumber: number,
    expectedOrder: number[]
  ): Promise<void> {
    return firstValueFrom(this.http.post<void>(
      `${AppConfig.settings.api.url}/ApplicationExternalDeploymentPaths/InsertWithExpectedOrder`,
      { applicationId, externalDeploymentPathId, hierarchyNumber, expectedOrder }
    ));
  }

  public moveWithExpectedOrder(
    applicationId: number,
    externalDeploymentPathId: number,
    fromIndex: number,
    toIndex: number,
    expectedOrder: number[]
  ): Promise<void> {
    return firstValueFrom(this.http.post<void>(
      `${AppConfig.settings.api.url}/ApplicationExternalDeploymentPaths/MoveWithExpectedOrder`,
      { applicationId, externalDeploymentPathId, fromIndex, toIndex, expectedOrder }
    ));
  }
}
