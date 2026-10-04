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
    public connectionReferencesFromDataverse: DeploymentSetting[] = [];
    public environmentVariablesFromDataverse: DeploymentSetting[] = [];
    public status: DeploymentSettingsStatus;

    constructor(
      private deploymentSettingsService: SolutionDeploymentSettingsService,
      private solutionService: SolutionService,
      private layoutService: LayoutService
    ) {}

    public ngOnInit(): void {
        this.layoutService.change(LayoutParameter.ShowLoading, true);
        this.solutionService.getStore().load({
            filter: ["Application", "=", this.application.Id],
            sort: [{ selector: "CreatedOn", desc: true }]
        }).then((solutions: Solution[]) => {
            this.solutions = solutions;
            this.selectedSolution = this.solution || solutions[0];
            return this.loadSelectedSolution();
        }).finally(() => {
            this.layoutService.change(LayoutParameter.ShowLoading, false);
        });
    }

    public onSolutionChanged(solution: Solution): void {
        if (solution && solution.Id !== this.selectedSolution?.Id) {
            this.selectedSolution = solution;
            this.loadSelectedSolution();
        }
    }

    public refreshManifest(): void {
        if (!this.selectedSolution?.Id) {
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
            setting.Id,
            this.environment.Id
        ).then(value => {
            setting.Value = value;
            return this.deploymentSettingsService.status(this.selectedSolution.Id, this.environment.Id);
        }).then(status => this.status = status);
    }

    public onValueChangedConnectionIdTextBox(e: ValueChangedEvent, setting: DeploymentSetting): void {
        if (e.value !== e.previousValue) {
            this.updateSetting(setting, e.value, true);
        }
    }

    public onValueChangedEnvVarValueTextBox(e: ValueChangedEvent, setting: DeploymentSetting): void {
        if (e.value !== e.previousValue) {
            this.updateSetting(setting, e.value, true);
        }
    }

    public onInitializedConnectionIdTextBox(e: InitializedEvent, setting: DeploymentSetting): void {
        this.setInitialValue(e, setting);
    }

    public onInitializedEnvVarValueTextBox(e: InitializedEvent, setting: DeploymentSetting): void {
        this.setInitialValue(e, setting);
    }

    public configuredCount(): string {
        return this.status ? `${this.status.Configured}/${this.status.Total} configured` : "";
    }

    private loadSelectedSolution(): Promise<void> {
        if (!this.selectedSolution?.Id) {
            return Promise.resolve();
        }
        return this.deploymentSettingsService.get(this.selectedSolution.Id, this.environment.Id)
            .then(response => {
                this.connectionReferencesFromDataverse = response.settings.filter(e => e.Kind === "ConnectionReference");
                this.environmentVariablesFromDataverse = response.settings.filter(e => e.Kind === "EnvironmentVariable");
                return this.deploymentSettingsService.status(this.selectedSolution.Id, this.environment.Id);
            })
            .then(status => {
                this.status = status;
            });
    }

    private updateSetting(setting: DeploymentSetting, value: string, isConfigured: boolean): void {
        this.deploymentSettingsService.update(
            this.selectedSolution.Id,
            setting.Id,
            this.environment.Id,
            value,
            isConfigured,
            setting.Value?.RowVersion
        ).then(updated => {
            setting.Value = updated;
            this.layoutService.notify({
                type: NotificationType.Success,
                message: "Changes have been saved",
                displayTime: 1000
            });
            return this.deploymentSettingsService.status(this.selectedSolution.Id, this.environment.Id);
        }).then(status => this.status = status);
    }

    private setInitialValue(e: InitializedEvent, setting: DeploymentSetting): void {
        if (setting.Value?.IsConfigured) {
            e.component.option("value", setting.Value.Value ?? "");
        }
    }
}
