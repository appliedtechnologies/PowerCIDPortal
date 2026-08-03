import { Injectable } from "@angular/core";
import ODataStore from "devextreme/data/odata/store";
import { Application } from "../models/application.model";
import { ODataService } from "./odata.service";
import { AppConfig } from "../config/app.config";
import { HttpClient } from "@angular/common/http";
import { Environment } from "../models/environment.model";
import { DeploymentPath } from "../models/deploymentpath.model";
import { DeploymentPathEnvironment } from "../models/deploymentpathenvironment.model";

@Injectable({
  providedIn: "root",
})
export class ApplicationService {
  constructor(private odataService: ODataService, private http: HttpClient) {}

  public getStore(): ODataStore {
    return this.odataService.context["Applications"];
  }

  public getApplicationById(id: number): Promise<Application> {
    return new Promise<Application>((resolve, reject) => {
      this.getStore()
        .byKey(id, {
          expand: [
            "DevelopmentEnvironmentNavigation",
            "DeploymentPaths",
            "ApplicationDeploymentPaths",
            "DeploymentPaths.Environments",
            "DeploymentPaths.DeploymentPathEnvironments",
          ],
        })
        .then((data) => resolve(data[0] as Application))
        .catch(() => reject());
    });
  }

  public getDeploymentSettingsStatus(applicationId: number, environmentId: number): Promise<number> {
    //status 0=incomplete configuration;1=complete configuration 
    return new Promise<number>((resolve, reject) => {
      this.http
        .post(`${AppConfig.settings.api.url}/Applications(${applicationId})/GetDeploymentSettingsStatus`, {
          environmentId: environmentId,
        })
        .subscribe({
          next: (data) => resolve(data['value'] as number),
          error: () => reject(),
        });
    });
  }

  public getMakerPortalUrl(applicationId: number): Promise<string> {
    return new Promise<string>((resolve, reject) => {
      this.http
        .post(`${AppConfig.settings.api.url}/Applications(${applicationId})/GetMakerPortalUrl`, {})
        .subscribe({
          next: (data) => resolve(data['value'] as string),
          error: () => reject(),
        });
    });
  }

  public callPullApplications(environment: Environment): Promise<string[]> {
    return new Promise<string[]>((resolve, reject) => {
      this.http
        .post(`${AppConfig.settings.api.url}/Applications/PullExisting`, {
          environment: environment,
        })
        .subscribe({
          next: (data) => resolve(data as string[]),
          error: () => reject(),
        });
    });
  }

  public sortAfterHierarchieAndStepNumber(
    application: Application,
    sortStepNumber: boolean
  ) {
    const deploymentPaths: DeploymentPath[] = [];
    for (let i = 0; i < application?.ApplicationDeploymentPaths.length; i++) {
      const hierarchieNumber =
        application.ApplicationDeploymentPaths[i].HierarchieNumber;
      deploymentPaths[hierarchieNumber - 1] = application.DeploymentPaths[i];
    }
    if (sortStepNumber == true) {
      for (const deploymentPath of deploymentPaths) {
        deploymentPath.Environments = deploymentPath.Environments.sort((a: Environment, b: Environment) => {
          const stepA: DeploymentPathEnvironment = deploymentPath.DeploymentPathEnvironments.find(e => e.Environment == a.Id);
          const stepB: DeploymentPathEnvironment = deploymentPath.DeploymentPathEnvironments.find(e => e.Environment == b.Id);
          return (stepA.StepNumber > stepB.StepNumber) ? 1 : -1;
        });
      }
    }

    return deploymentPaths;
  }

  public saveApplication(
    applicationUniqueName: string,
    environment: Environment
  ): Promise<void> {
    return new Promise<void>((resolve, reject) => {
      this.http
        .post(`${AppConfig.settings.api.url}/Applications/SaveApplication`, {
          applicationUniqueName: applicationUniqueName,
          environment: environment,
        })
        .subscribe({
          next: () => resolve(),
          error: (e) => reject(e),
        });
    });
  }

  public delete(id: number): Promise<void>{
    return this.getStore().remove(id);
  }

  public setDeactivated(id: number, isDeactive: boolean): Promise<void> {
    return Promise.resolve(this.getStore().update(id, { IsDeactive: isDeactive }))
      .then(() => undefined);
  }

  public update(id: number, environment: Environment){
    return this.getStore().update(id, environment);
  }
}
