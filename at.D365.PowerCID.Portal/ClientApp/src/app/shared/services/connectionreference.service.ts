import { HttpClient } from "@angular/common/http";
import { Injectable } from "@angular/core";
import ODataStore from "devextreme/data/odata/store";
import { Observable } from "rxjs";
import { AppConfig } from "../config/app.config";
import { ConnectionReference } from "../models/connectionreference.model";
import { Environment } from "../models/environment.model";
import { Publisher } from "../models/publisher.model";
import { ODataService } from "./odata.service";

@Injectable({
  providedIn: "root",
})
export class ConnectionReferenceService {
  constructor(private odataService: ODataService, private http: HttpClient) {}

  public getStore(): ODataStore {
    return this.odataService.context["ConnectionReferences"];
  } 

  public getExistingForApplication(applicationId: number): Promise<ConnectionReference[]>{
    return this.getStore().load({
      filter: ["Application", "=", applicationId],
      expand: ["ConnectionReferenceEnvironments"]
    });
  }

  public getFromDataverseForApplication(applicationId: number): Promise<ConnectionReference[]> {
    return new Promise<ConnectionReference[]>((resolve, reject) => {
      this.http
        .post(
          `${AppConfig.settings.api.url}/ConnectionReferences/GetConnectionReferencesForApplication`,
          {
              "applicationId": applicationId
          }
        )
        .subscribe({
          next: (data) => resolve(data as ConnectionReference[]),
          error: (error) => reject(),
        });
    });
  }

  public add(connectionReference: ConnectionReference): Promise<ConnectionReference>{
    return this.getStore().insert(connectionReference);
  }
}
