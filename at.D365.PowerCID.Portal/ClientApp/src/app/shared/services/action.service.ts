import { HttpClient } from "@angular/common/http";
import { Injectable } from "@angular/core";
import ODataStore from "devextreme/data/odata/store";
import { AppConfig } from "../config/app.config";
import { ODataService } from "./odata.service";

@Injectable({
  providedIn: "root",
})
export class ActionService {
  constructor(private odataService: ODataService, private http: HttpClient) {}

  public getStore(): ODataStore {
    return this.odataService.context["Actions"];
  }

  public cancelImport(actionId: number): Promise<void> {
    return new Promise<void>((resolve, reject) => {
      this.http
        .post(`${AppConfig.settings.api.url}/Actions(${actionId})/CancelImport`, {})
        .subscribe({
          next: (data) => resolve(),
          error: (error) => reject(error),
        });
    });
  }
}
