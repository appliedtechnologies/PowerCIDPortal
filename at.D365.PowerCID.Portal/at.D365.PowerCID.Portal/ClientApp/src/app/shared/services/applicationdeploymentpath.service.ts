import { Injectable } from "@angular/core";
import ODataStore from "devextreme/data/odata/store";
import { ODataService } from "./odata.service";

@Injectable({
  providedIn: "root",
})
export class ApplicationdeploymentpathService {
  constructor(private odataService: ODataService) {}

  public getStore(): ODataStore {
    return this.odataService.context["ApplicationDeploymentPaths"];
  }
}
