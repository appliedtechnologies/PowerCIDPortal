import { HttpClient } from "@angular/common/http";
import { Injectable } from "@angular/core";
import ODataStore from "devextreme/data/odata/store";
import { ConnectionReferenceEnvironment } from "../models/connectionreferenceenvironment.model";
import { ODataService } from "./odata.service";

@Injectable({
  providedIn: "root",
})
export class ConnectionReferenceEnvironmentService {
  constructor(private odataService: ODataService, private http: HttpClient) {}

  public getStore(): ODataStore {
    return this.odataService.context["ConnectionReferenceEnvironments"];
  } 

  public add(connectionReferenceEnvironment: ConnectionReferenceEnvironment): Promise<ConnectionReferenceEnvironment>{
    return this.getStore().insert(connectionReferenceEnvironment);
  }

  public update(connectionReferenceId: number, environmentId: number, connectionReferenceEnvironment: ConnectionReferenceEnvironment){
      return this.getStore().update(
        {
          ConnectionReference: connectionReferenceId,
          Environment: environmentId
        },
        connectionReferenceEnvironment
      );
  }
}
