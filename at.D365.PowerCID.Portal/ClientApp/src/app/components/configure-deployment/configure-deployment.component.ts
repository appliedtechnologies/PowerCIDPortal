import { Component, Input, OnInit, ChangeDetectionStrategy } from "@angular/core";
import { Application } from "src/app/shared/models/application.model";
import { Environment } from "src/app/shared/models/environment.model";
import { Solution } from "src/app/shared/models/solution.model";
import {
  DeploymentSetting,
  DeploymentSettingsStatus,
  SolutionDeploymentSettingsService,
} from "src/app/shared/services/solution-deployment-settings.service";
import { SolutionService } from "src/app/shared/services/solution.service";
import { LayoutParameter, LayoutService, NotificationType } from "src/app/shared/services/layout.service";
import { InitializedEvent, ValueChangedEvent } from "devextreme/ui/text_box";

@Component({
    selector: "app-configure-deployment",
    templateUrl: "./configure-deployment.component.html",
    styleUrls: ["./configure-deployment.component.css"],
    changeDetection: ChangeDetectionStrategy.Eager,
    standalone: false
})
export class ConfigureDeploymentComponent implements OnInit {
    @Input() environment: Environment;
    @Input() application: Application;
    @Input() solution: Solution;

    public solutions: Solution[] = [];
    public selectedSolution: Solution;
    public selectedSolutionId: number;
    public connectionReferencesFromDataverse: DeploymentSetting[] = [];
    public environmentVariablesFromDataverse: DeploymentSetting[] = [];
    public status: DeploymentSettingsStatus;
    private readonly settingEditors = new WeakMap<DeploymentSetting, InitializedEvent["component"]>();

    constructor(
      private deploymentSettingsService: SolutionDeploymentSettingsService,
      private solutionService: SolutionService,
      private layoutService: LayoutService
    ) {}

    public ngOnInit(): void {
        setTimeout(() => this.layoutService.change(LayoutParameter.ShowLoading, true));
        Promise.resolve(this.solutionService.getStore().load({
            filter: ["Application", "=", this.application.Id],
            sort: [{ selector: "CreatedOn", desc: true }]
        })).then((solutions: Solution[]) => {
            this.solutions = solutions;
            this.selectedSolution = this.solution || solutions[0];
            this.selectedSolutionId = this.selectedSolution?.Id;
            return this.loadSelectedSolution();
        }).catch(() => {
            this.layoutService.notify({
                type: NotificationType.Error,
                message: "The deployment settings could not be loaded."
            });
        }).finally(() => {
            setTimeout(() => this.layoutService.change(LayoutParameter.ShowLoading, false));
        });
    }

    public onSolutionChanged(solutionId: number): void {
        if (solutionId && solutionId !== this.selectedSolution?.Id) {
            const solution = this.solutions.find(item => item.Id === solutionId);
            if (!solution) {
                return;
            }

            this.selectedSolution = solution;
            this.loadSelectedSolution();
        }
    }

    public refreshManifest(): void {
        if (!this.canRefreshManifest()) {
            return;
        }

        this.layoutService.change(LayoutParameter.ShowLoading, true);
        this.deploymentSettingsService.refresh(this.selectedSolution.Id)
            .then(() => this.loadSelectedSolution())
            .finally(() => this.layoutService.change(LayoutParameter.ShowLoading, false));
    }

    public resetSetting(setting: DeploymentSetting): void {
        this.deploymentSettingsService.reset(
            this.selectedSolution.Id,
            setting.id,
            this.environment.Id
            ).then(() => {
                return this.deploymentSettingsService.get(
                    this.selectedSolution.Id,
                    this.environment.Id
                );
            }).then(response => {
                const refreshedSetting = response.settings.find(value => value.id === setting.id);
                if (!refreshedSetting) {
                    throw new Error("The reset deployment setting could not be reloaded.");
                }

                setting.value = refreshedSetting.value;
                this.settingEditors.get(setting)?.option("value", refreshedSetting.value?.value ?? "");
                return this.deploymentSettingsService.status(this.selectedSolution.Id, this.environment.Id);
            }).then(status => this.status = status).catch(() => {
            this.layoutService.notify({
                type: NotificationType.Error,
                message: "The deployment setting could not be reset."
            });
        });
    }

    public onValueChangedConnectionIdTextBox(e: ValueChangedEvent, setting: DeploymentSetting): void {
        if (e.value !== e.previousValue && e.event) {
            this.updateSetting(setting, e.value, true);
        }
    }

    public onValueChangedEnvVarValueTextBox(e: ValueChangedEvent, setting: DeploymentSetting): void {
        if (e.value !== e.previousValue && e.event) {
            this.updateSetting(setting, e.value, true);
        }
    }

    public onInitializedConnectionIdTextBox(e: InitializedEvent, setting: DeploymentSetting): void {
        this.initializeSettingEditor(e, setting);
    }

    public onInitializedEnvVarValueTextBox(e: InitializedEvent, setting: DeploymentSetting): void {
        this.initializeSettingEditor(e, setting);
    }

    public configuredCount(): string {
        return this.status ? `${this.status.configured}/${this.status.total} configured` : "";
    }

    public canRefreshManifest(): boolean {
        return !!this.selectedSolution?.Id &&
            this.selectedSolution.Id === this.solutions[0]?.Id;
    }

    private loadSelectedSolution(): Promise<void> {
        if (!this.selectedSolution?.Id) {
            return Promise.resolve();
        }
        return this.deploymentSettingsService.get(this.selectedSolution.Id, this.environment.Id)
            .then(response => {
                this.connectionReferencesFromDataverse = response.settings.filter(e => e.kind === "ConnectionReference");
                this.environmentVariablesFromDataverse = response.settings.filter(e => e.kind === "EnvironmentVariable");
                if (response.manifestStatus === "Draft" && response.settings.length === 0) {
                    this.status = {
                        manifestStatus: response.manifestStatus,
                        configurationStatus: "Unavailable",
                        total: 0,
                        configured: 0,
                        missing: 0,
                        inherited: 0,
                        missingSettings: []
                    };
                    return;
                }
                return this.deploymentSettingsService.status(this.selectedSolution.Id, this.environment.Id);
            })
            .then(status => {
                if (status) {
                    this.status = status;
                }
            });
    }

    private updateSetting(setting: DeploymentSetting, value: string, isConfigured: boolean): void {
        this.deploymentSettingsService.update(
            this.selectedSolution.Id,
            setting.id,
            this.environment.Id,
            value,
            isConfigured,
            setting.value?.rowVersion
        ).then(updated => {
            setting.value = {
                ...setting.value,
                ...updated,
                value
            };
            this.layoutService.notify({
                type: NotificationType.Success,
                message: "Changes have been saved",
                displayTime: 1000
            });
            return this.deploymentSettingsService.status(this.selectedSolution.Id, this.environment.Id);
        }).then(status => this.status = status).catch(() => {
            this.layoutService.notify({
                type: NotificationType.Error,
                message: "The deployment setting could not be saved. The latest settings will be reloaded."
            });
            return this.loadSelectedSolution().catch(() => {
                this.layoutService.notify({
                    type: NotificationType.Error,
                    message: "The latest settings could not be loaded. Reopen this view before editing again."
                });
            });
        });
    }

    private initializeSettingEditor(e: InitializedEvent, setting: DeploymentSetting): void {
        this.settingEditors.set(setting, e.component);
        if (setting.value?.isConfigured) {
            e.component.option("value", setting.value.value ?? "");
        }
    }

}
