import { HttpClient } from "@angular/common/http";
import { Injectable } from "@angular/core";
import ODataStore from "devextreme/data/odata/store";
import { AppConfig } from "../config/app.config";
import { ODataService } from "./odata.service";
import { UserService } from "./user.service";

@Injectable({
  providedIn: "root",
})
export class TenantService {
  constructor(
    private odataService: ODataService,
    private userService: UserService,
    private http: HttpClient
  ) {}

  getStore(): ODataStore {
    return this.odataService.context["Tenants"];
  }

  public setGitHubInstallation(installationId: number): Promise<any> {
    return this.getStore()
      .update(this.userService.currentDbUserWithTenant.Tenant, {
        GitHubInstallationId: installationId,
      })
      .then(() => {
        this.userService.reloadUser();
      });
  }

  public setGitHubRepositoryName(repositoryName: string): Promise<any> {
    return this.getStore()
      .update(this.userService.currentDbUserWithTenant.Tenant, {
        GitHubRepositoryName: repositoryName,
      })
      .then(() => {
        this.userService.reloadUser();
      });
  }

  public setDisablePatchCreation(valueDisablePatchCreation: boolean): Promise<any> {
    return this.getStore()
      .update(this.userService.currentDbUserWithTenant.Tenant, {
        DisablePatchCreation: valueDisablePatchCreation,
      })
      .then(() => {
        this.userService.reloadUser();
      });
  }

  public getGitHubRepositories(): Promise<string[]> {
    return new Promise<string[]>((resolve, reject) => {
      let request = this.http
        .post(`${AppConfig.settings.api.url}/Tenants(${this.userService.currentDbUserWithTenant.Tenant})/GetGitHubRepositories`, {})
        .subscribe({
          next: (data) => resolve(data["value"]),
          error: () => reject(),
        });
    });
  }
}
