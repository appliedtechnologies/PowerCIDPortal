import { Injectable } from "@angular/core";
import ODataStore from "devextreme/data/odata/store";
import { ODataService } from "./odata.service";

@Injectable({
  providedIn: "root",
})
export class UserEnvironmentService {
  constructor(private odataService: ODataService) {}

  public getStore(): ODataStore {
    return this.odataService.context["UserEnvironments"];
  }

  public addEnvironmentPermission(userId: number, environmentId: number){
    return this.getStore().insert({
        User: userId,
        Environment: environmentId
    });
  }

  public removeEnvironmentPermission(userId: number, environmentId: number){
    return this.getStore().remove({
        User: userId,
        Environment: environmentId
    });
  }
}
