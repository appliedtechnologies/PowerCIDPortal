import { HttpClient } from "@angular/common/http";
import { Injectable } from "@angular/core";
import { AppConfig } from "../config/app.config";
import { firstValueFrom } from "rxjs";

export interface DeploymentSettingValue {
  Value?: string;
  IsConfigured: boolean;
  IsInherited: boolean;
  InheritedFromSolutionId?: number;
  RowVersion?: string;
}

export interface DeploymentSetting {
  Id: number;
  Kind: string;
  MsId: string;
  LogicalName: string;
  DisplayName: string;
  ConnectorId?: string;
  EnvironmentVariableType?: string;
  DefaultValue?: string;
  IsRequired: boolean;
  Value?: DeploymentSettingValue;
}

export interface DeploymentSettingsResponse {
  manifestStatus: string;
  manifestHash?: string;
  settings: DeploymentSetting[];
}

export interface DeploymentSettingsStatus {
  ManifestStatus: string;
  ConfigurationStatus: string;
  Total: number;
  Configured: number;
  Missing: number;
  Inherited: number;
  MissingSettings: DeploymentSetting[];
}

@Injectable({ providedIn: "root" })
export class SolutionDeploymentSettingsService {
  constructor(private http: HttpClient) {}

  public get(solutionId: number, environmentId: number): Promise<DeploymentSettingsResponse> {
    return firstValueFrom(this.http.get<DeploymentSettingsResponse>(
      `${AppConfig.settings.api.url}/Solutions(${solutionId})/DeploymentSettings?environmentId=${environmentId}`
    ));
  }

  public status(solutionId: number, environmentId: number): Promise<DeploymentSettingsStatus> {
    return firstValueFrom(this.http.post<DeploymentSettingsStatus>(
      `${AppConfig.settings.api.url}/Solutions(${solutionId})/GetDeploymentSettingsStatus`,
      { environmentId }
    ));
  }

  public refresh(solutionId: number): Promise<unknown> {
    return firstValueFrom(this.http.post(
      `${AppConfig.settings.api.url}/Solutions(${solutionId})/RefreshDeploymentSettings`,
      {}
    ));
  }

  public update(
    solutionId: number,
    settingId: number,
    environmentId: number,
    value: string,
    isConfigured: boolean,
    rowVersion?: string
  ): Promise<DeploymentSettingValue> {
    return firstValueFrom(this.http.patch<DeploymentSettingValue>(
      `${AppConfig.settings.api.url}/Solutions(${solutionId})/DeploymentSettings/${settingId}`,
      { EnvironmentId: environmentId, Value: value, IsConfigured: isConfigured, RowVersion: rowVersion }
    ));
  }

  public reset(solutionId: number, settingId: number, environmentId: number): Promise<DeploymentSettingValue> {
    return firstValueFrom(this.http.post<DeploymentSettingValue>(
      `${AppConfig.settings.api.url}/Solutions(${solutionId})/DeploymentSettings/${settingId}/Reset`,
      { EnvironmentId: environmentId }
    ));
  }
}
