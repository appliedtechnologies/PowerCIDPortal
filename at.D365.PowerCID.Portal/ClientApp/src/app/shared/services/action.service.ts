import { HttpClient } from "@angular/common/http";
import { Injectable } from "@angular/core";
import ODataStore from "devextreme/data/odata/store";
import { AppConfig } from "../config/app.config";
import { TimeHelper } from "../helper/time.helper";
import { Action } from "../models/action.model";
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
          next: () => resolve(),
          error: (error) => reject(error),
        });
    });
  }

  public getDurationString(action: Action): string{
    if (action?.FinishTime != null && action?.StartTime != null) {
      const startTime = this.getTimeValue(action.StartTime);
      const finishTime = this.getTimeValue(action.FinishTime);
      if (!Number.isFinite(startTime) || !Number.isFinite(finishTime)) {
        return null;
      }

      return TimeHelper.millisecondsToString(finishTime - startTime);
    }

    return null;
  }

  private getTimeValue(value: Date | string): number {
    return value instanceof Date ? value.getTime() : Date.parse(value);
  }
}
