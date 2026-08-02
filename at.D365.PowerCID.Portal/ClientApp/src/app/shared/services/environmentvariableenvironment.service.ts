import { HttpClient } from "@angular/common/http";
import { Injectable } from "@angular/core";
import ODataStore from "devextreme/data/odata/store";
import { EnvironmentVariableEnvironment } from "../models/environmentvariableenvironment.model";
import { ODataService } from "./odata.service";

@Injectable({
  providedIn: "root",
})
export class EnvironmentVariableEnvironmentService {
  constructor(private odataService: ODataService, private http: HttpClient) {}

  public getStore(): ODataStore {
    return this.odataService.context["EnvironmentVariableEnvironments"];
  } 

  public add(environmentVariableEnvironment: EnvironmentVariableEnvironment): Promise<EnvironmentVariableEnvironment>{
    return this.getStore().insert(environmentVariableEnvironment);
  }

  public update(environmentVariableId: number, environmentId: number, environmentVariableEnvironment: EnvironmentVariableEnvironment){  
    return this.getStore().update(
        {
            EnvironmentVariable: environmentVariableId,
            Environment: environmentId
        },
        environmentVariableEnvironment
      );
  }
}
