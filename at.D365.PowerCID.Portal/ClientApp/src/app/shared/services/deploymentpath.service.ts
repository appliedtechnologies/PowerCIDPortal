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
  //public DeploymentPath: DeploymentPath;
  private manualUpdate = new Subject<void>();
  constructor(private odataService: ODataService, private http: HttpClient) {}

  public getStore(): ODataStore {
    return this.odataService.context["DeploymentPaths"];
  }

  updateDeploymentPathEnvironments(id: number, deploymentpath: DeploymentPath) {
    return new Promise<void>((resolve, reject) => {
      this.http
        .patch(
          `${AppConfig.settings.api.url}/DeploymentPaths(${id})`,
          deploymentpath
        )
        .subscribe({
          next: () => resolve(),
          error: () => reject(),
        });
    });
  }
}
