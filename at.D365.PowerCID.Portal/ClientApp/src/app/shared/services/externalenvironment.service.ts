import { HttpClient } from "@angular/common/http";
import { Injectable } from "@angular/core";
import ODataStore from "devextreme/data/odata/store";
import { AppConfig } from "../config/app.config";
import { ExternalEnvironment } from "../models/externalenvironment.model";
import { ODataService } from "./odata.service";

export interface RegistrableEnvironment {
  id: number;
  name: string;
  basicUrl: string;
  tenantId: number;
  tenantName: string;
  tenantMsId: string;
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
    return new Promise<void>((resolve, reject) => {
      this.getStore()
        .insert(externalEnvironment)
        .then(() => resolve())
        .catch((error: Error) => reject(error));
    });
  }

  public update(id: number, externalEnvironment: ExternalEnvironment): Promise<void> {
    return new Promise<void>((resolve, reject) => {
      this.getStore()
        .update(id, externalEnvironment)
        .then(() => resolve())
        .catch((error: Error) => reject(error));
    });
  }

  public setDeactivated(id: number, isDeactive: boolean): Promise<void> {
    return new Promise<void>((resolve, reject) => {
      this.getStore()
        .update(id, { IsDeactive: isDeactive })
        .then(() => resolve())
        .catch((error: Error) => reject(error));
    });
  }

  public remove(id: number): Promise<void> {
    return new Promise<void>((resolve, reject) => {
      this.getStore()
        .remove(id)
        .then(() => resolve())
        .catch((error: Error) => reject(error));
    });
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
