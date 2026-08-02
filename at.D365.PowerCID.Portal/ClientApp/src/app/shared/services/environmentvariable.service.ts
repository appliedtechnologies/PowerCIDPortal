import { HttpClient } from "@angular/common/http";
import { Injectable } from "@angular/core";
import ODataStore from "devextreme/data/odata/store";
import { AppConfig } from "../config/app.config";
import { EnvironmentVariable } from "../models/environmentvariable.model";
import { ODataService } from "./odata.service";

@Injectable({
  providedIn: "root",
})
export class EnvironmentVariableService {
  constructor(private odataService: ODataService, private http: HttpClient) {}

  public getStore(): ODataStore {
    return this.odataService.context["EnvironmentVariables"];
  } 

  public getExistingForApplication(applicationId: number): Promise<EnvironmentVariable[]>{
    return this.getStore().load({
      filter: ["Application", "=", applicationId],
      expand: ["EnvironmentVariableEnvironments"]
    });
  }

  public getFromDataverseForApplication(applicationId: number): Promise<EnvironmentVariable[]> {
    return new Promise<EnvironmentVariable[]>((resolve, reject) => {
      this.http
        .post(
          `${AppConfig.settings.api.url}/EnvironmentVariables/GetEnvironmentVariablesForApplication`,
          {
              "applicationId": applicationId
          }
        )
        .subscribe({
          next: (data) => resolve(data as EnvironmentVariable[]),
          error: () => reject(),
        });
    });
  }

  public add(environmentVariable: EnvironmentVariable): Promise<EnvironmentVariable>{
    return this.getStore().insert(environmentVariable);
  }
}
