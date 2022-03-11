import { HttpClient } from "@angular/common/http";
import { Injectable } from "@angular/core";
import ODataStore from "devextreme/data/odata/store";
import { AppConfig } from "../config/app.config";
import { ODataService } from "./odata.service";
import { UserService } from "./user.service";

@Injectable({
  providedIn: "root",
})
export class EnvironmentService {
  constructor(
    private odataService: ODataService,
    private http: HttpClient,
    private userService: UserService
  ) {
    this.callPullEnvironments = this.callPullEnvironments.bind(this);
  }

  public getStore(): ODataStore {
    return this.odataService.context["Environments"];
  }

  public callPullEnvironments(): Promise<void> {
    return new Promise<void>((resolve, reject) => {
      let request = this.http
        .post(`${AppConfig.settings.api.url}/Environments/PullExisting`, {})
        .subscribe({
          next: () => resolve(),
          error: () => reject(),
        });
    });
  }

  public getDataversePublishers(environmentId: number): Promise<any>{
    return new Promise<any>((resolve, reject) => {
      let request = this.http
        .post(`${AppConfig.settings.api.url}/Environments(${environmentId})/GetDataversePublishers`, {})
        .subscribe({
          next: (data) => resolve(data),
          error: () => reject(),
        });
    });  }
}
