import { Component, ViewChild, ChangeDetectionStrategy } from "@angular/core";
import { DxDataGridComponent } from "devextreme-angular";
import DataSource from "devextreme/data/data_source";
import { Solution } from "src/app/shared/models/solution.model";
import { ExternalDeploymentPath } from "src/app/shared/models/externaldeploymentpath.model";
import { ExternalDeploymentPathEnvironment } from "src/app/shared/models/externaldeploymentpathenvironment.model";
import { ApplicationExternalDeploymentPath } from "src/app/shared/models/applicationexternaldeploymentpath.model";
import { SolutionService } from "src/app/shared/services/solution.service";
import { ApplicationExternalDeploymentPathService } from "src/app/shared/services/applicationexternaldeploymentpath.service";
import { ExternalDeploymentPathEnvironmentService } from "src/app/shared/services/externaldeploymentpathenvironment.service";
import {
  LayoutParameter,
  LayoutService,
  NotificationType,
} from "src/app/shared/services/layout.service";
import { Action } from "src/app/shared/models/action.model";
import { ODataService } from "src/app/shared/services/odata.service";

@Component({
    selector: "app-external-delivery",
    templateUrl: "./external-delivery.component.html",
    styleUrls: ["./external-delivery.component.css"],
    changeDetection: ChangeDetectionStrategy.Eager,
    standalone: false
})
export class ExternalDeliveryComponent {
  @ViewChild(DxDataGridComponent, { static: false }) dataGrid: DxDataGridComponent;

  public dataSourceReleasedSolutions: DataSource;

  public isDeployPopupVisible = false;
  public selectedSolution: Solution;
  public availableExternalDeploymentPaths: ExternalDeploymentPath[] = [];
  public selectedExternalDeploymentPath: ExternalDeploymentPath;
  public availableSteps: ExternalDeploymentPathEnvironment[] = [];
  public selectedStep: ExternalDeploymentPathEnvironment;

  public isActionDetailPopupVisible = false;
  public lastActionId: number;

  constructor(
    private odataService: ODataService,
    private solutionService: SolutionService,
    private applicationExternalDeploymentPathService: ApplicationExternalDeploymentPathService,
    private externalDeploymentPathEnvironmentService: ExternalDeploymentPathEnvironmentService,
    private layoutService: LayoutService
  ) {
    this.onClickDeploy = this.onClickDeploy.bind(this);
    this.stepDisplayName = this.stepDisplayName.bind(this);

    this.dataSourceReleasedSolutions = new DataSource({
      store: this.odataService.context["Solutions"],
      filter: ["IsReleasedExternally", "=", true],
      expand: ["ApplicationNavigation"],
      sort: [{ selector: "ModifiedOn", desc: true }],
    });
  }

  public onToolbarPreparingDataGrid(e): void {
    const toolbarItems = e.toolbarOptions.items;

    toolbarItems.unshift({
      widget: "dxButton",
      options: {
        icon: "refresh",
        stylingMode: "contained",
        type: "success",
        hint: "Refresh",
        onClick: () => this.dataGrid.instance.refresh(),
      },
      location: "after",
    });
  }

  public onClickDeploy(e): void {
    this.selectedSolution = e.row.data as Solution;
    this.selectedExternalDeploymentPath = undefined;
    this.selectedStep = undefined;
    this.availableSteps = [];

    this.layoutService.change(LayoutParameter.ShowLoading, true);
    this.applicationExternalDeploymentPathService
      .getStore()
      .load({
        filter: ["Application", "=", this.selectedSolution.Application],
        sort: [{ selector: "HierarchieNumber", desc: false }],
        expand: ["ExternalDeploymentPathNavigation"],
      })
      .then((applicationExternalDeploymentPaths: ApplicationExternalDeploymentPath[]) => {
        this.availableExternalDeploymentPaths = applicationExternalDeploymentPaths.map(
          (a) => a.ExternalDeploymentPathNavigation
        );
        this.isDeployPopupVisible = true;
      })
      .finally(() => this.layoutService.change(LayoutParameter.ShowLoading, false));
  }

  public onValueChangedExternalDeploymentPath(e): void {
    this.selectedExternalDeploymentPath = e.value;
    this.selectedStep = undefined;
    this.availableSteps = [];

    if (!this.selectedExternalDeploymentPath) return;

    this.externalDeploymentPathEnvironmentService
      .getStore()
      .load({
        filter: ["ExternalDeploymentPath", "=", this.selectedExternalDeploymentPath.Id],
        sort: [{ selector: "StepNumber", desc: false }],
        expand: ["ExternalEnvironmentNavigation.EnvironmentNavigation"],
      })
      .then((steps: ExternalDeploymentPathEnvironment[]) => {
        this.availableSteps = steps;
      });
  }

  public onValueChangedStep(e): void {
    this.selectedStep = e.value;
  }

  public onClickStartDeployment(): void {
    if (!this.selectedExternalDeploymentPath || !this.selectedStep) {
      this.layoutService.notify({
        type: NotificationType.Error,
        message: "Please select an external deployment path and an environment.",
      });
      return;
    }

    this.layoutService.change(LayoutParameter.ShowLoading, true);
    this.solutionService
      .externalImport(this.selectedSolution.Id, this.selectedStep.ExternalEnvironment, this.selectedExternalDeploymentPath.Id)
      .then((action: Action) => {
        this.isDeployPopupVisible = false;
        this.lastActionId = action.Id;
        this.isActionDetailPopupVisible = true;
        this.layoutService.notify({
          type: NotificationType.Success,
          message: "The deployment has been started.",
        });
      })
      .catch((error) => {
        this.layoutService.notify({
          type: NotificationType.Error,
          message: error?.error?.value
            ? `An error occurred while starting the deployment: ${error.error.value}`
            : "An error occurred while starting the deployment.",
        });
      })
      .finally(() => this.layoutService.change(LayoutParameter.ShowLoading, false));
  }

  public stepDisplayName(step: ExternalDeploymentPathEnvironment): string {
    if (!step) return "";
    return (
      step.ExternalEnvironmentNavigation?.Alias ||
      step.ExternalEnvironmentNavigation?.EnvironmentNavigation?.Name ||
      ""
    );
  }
}
