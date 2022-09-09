import { HttpClient } from "@angular/common/http";
import { Injectable } from "@angular/core";
import ODataStore from "devextreme/data/odata/store";
import { AppConfig } from "../config/app.config";
import { Action } from "../models/action.model";
import { ODataService } from "./odata.service";

@Injectable({
  providedIn: "root",
})
export class SolutionService {
  constructor(private odataService: ODataService, private http: HttpClient) {}

  public getStore(): ODataStore {
    return this.odataService.context["Solutions"];
  }

  public export(solutionId: number): Promise<Action> {
    return new Promise<Action>((resolve, reject) => {
      this.http
        .post(
          `${AppConfig.settings.api.url}/Solutions(${solutionId})/Export`,
          {}
        )
        .subscribe({
          next: (data) => resolve(data as Action),
          error: (error) => reject(),
        });
    });
  }

  public import(
    solutionId: number,
    targetEnvironmentId,
    deploymentPathId
  ): Promise<Action> {
    return new Promise<Action>((resolve, reject) => {
      this.http
        .post(`${AppConfig.settings.api.url}/Solutions(${solutionId})/Import`, {
          targetEnvironmentId: targetEnvironmentId,
          deploymentPathId: deploymentPathId,
        })
        .subscribe({
          next: (data) => resolve(data as Action),
          error: (error) => reject(error),
        });
    });
  }

  public applyUpgrade(solutionId: number, targetEnvironmentId: any): Promise<Action> {
    return new Promise<Action>((resolve, reject) => {
      this.http
        .post(`${AppConfig.settings.api.url}/Solutions(${solutionId})/ApplyUpgrade`, {
          targetEnvironmentId: targetEnvironmentId,
        })
        .subscribe({
          next: (data) => resolve(data as Action),
          error: (error) => reject(error),
        });
    });
  }

  public enableFlows(solutionId: number, targetEnvironmentId: any): Promise<Action> {
    return new Promise<Action>((resolve, reject) => {
      this.http
        .post(`${AppConfig.settings.api.url}/Solutions(${solutionId})/EnableFlows`, {
          targetEnvironmentId: targetEnvironmentId,
        })
        .subscribe({
          next: (data) => resolve(data as Action),
          error: (error) => reject(error),
        });
    });
  }

  public getSolutionAsBase64String(solutionId: number): Promise<string> {
    return new Promise<string>((resolve, reject) => {
      this.http
        .post(
          `${AppConfig.settings.api.url}/Solutions(${solutionId})/GetSolutionAsBase64String`,
          {}
        )
        .subscribe({
          next: (data) => resolve(data as string),
          error: (error) => reject(),
        });
    });
  }
}
