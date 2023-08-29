import { HttpClient } from "@angular/common/http";
import { Injectable } from "@angular/core";
import ODataStore from "devextreme/data/odata/store";
import { Subject } from "rxjs";
import { AppConfig } from "../config/app.config";
import { DeploymentPath } from "../models/deploymentpath.model";
import { DeploymentPathEnvironment } from "../models/deploymentpathenvironment.model";
import { ODataService } from "./odata.service";

@Injectable({
  providedIn: "root",
})
export class DeploymentpathService {
  constructor(private odataService: ODataService, private http: HttpClient) {}

  public getStore(): ODataStore {
    return this.odataService.context["DeploymentPaths"];
  }

  public update(id: number, deploymentPath: DeploymentPath){
    return new Promise<void>((resolve, reject) => {
      this.getStore()
        .update(id, deploymentPath)
        .then(() => resolve())
        .catch((error: Error) => reject(error));
    });
  }

  public add(deploymentPath: DeploymentPath){
    return new Promise<void>((resolve, reject) => {
      this.getStore()
        .insert(deploymentPath)
        .then(() => resolve())
        .catch((error: Error) => reject(error));
    });
  }

  public remove(id: number){
    return new Promise<void>((resolve, reject) => {
      this.getStore()
        .remove(id)
        .then(() => resolve())
        .catch((error) => reject(error));
    });
  }
}
