import { HttpClient } from "@angular/common/http";
import { Injectable } from "@angular/core";
import ODataStore from "devextreme/data/odata/store";
import { AppConfig } from "../config/app.config";
import { ExternalEnvironment } from "../models/externalenvironment.model";
import { ODataService } from "./odata.service";

export interface RegistrableEnvironment {
  Id: number;
  Name: string;
  BasicUrl: string;
  TenantId: number;
  TenantName: string;
  TenantMsId: string;
}

@Injectable({
  providedIn: "root",
})
export class ExternalEnvironmentService {
  constructor(private odataService: ODataService, private http: HttpClient) {}

  public getStore(): ODataStore {
    return this.odataService.context["ExternalEnvironments"];
  }

  public add(externalEnvironment: ExternalEnvironment): Promise<void> {
    return this.getStore().insert(externalEnvironment).then(() => undefined);
  }

  public update(id: number, externalEnvironment: ExternalEnvironment): Promise<void> {
    return this.getStore().update(id, externalEnvironment).then(() => undefined);
  }

  public setDeactivated(id: number, isDeactive: boolean): Promise<void> {
    return this.getStore().update(id, { IsDeactive: isDeactive }).then(() => undefined);
  }

  public remove(id: number): Promise<void> {
    return this.getStore().remove(id).then(() => undefined);
  }

  public getRegistrableEnvironments(): Promise<RegistrableEnvironment[]> {
    return new Promise<RegistrableEnvironment[]>((resolve, reject) => {
      this.http
        .post(`${AppConfig.settings.api.url}/ExternalEnvironments/GetRegistrableEnvironments`, {})
        .subscribe({
          next: (data) => resolve(data as RegistrableEnvironment[]),
          error: () => reject(),
        });
    });
  }
}
