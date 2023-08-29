import { HttpClient, HttpErrorResponse } from "@angular/common/http";
import { Injectable } from "@angular/core";
import { MsalService } from "@azure/msal-angular";
import ODataStore from "devextreme/data/odata/store";
import { AppConfig } from "../config/app.config";
import { ODataService } from "./odata.service";
import { UserService } from "./user.service";
import { Environment } from "../models/environment.model";

@Injectable({
  providedIn: "root",
})
export class EnvironmentService {
  constructor(
    private odataService: ODataService,
    private http: HttpClient,
    private userService: UserService,
    private authService: MsalService,
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
          error: (e: HttpErrorResponse) => {
            if(e.status == 403){
              this.authService.acquireTokenRedirect({
                scopes: [`api://${AppConfig.settings.azure.applicationId}/access_as_user`],
                extraScopesToConsent: ["https://management.azure.com//user_impersonation"]
              });
            }
          },
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
    });  
  }

  public update(id: number, environment: Environment){
    return this.getStore().update(id, environment);
  }
}
