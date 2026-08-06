import { Component, ViewChild, ChangeDetectionStrategy } from "@angular/core";
import { DxDataGridComponent } from "devextreme-angular";
import DataSource from "devextreme/data/data_source";
import { confirm } from "devextreme/ui/dialog";
import { ExternalDeploymentPath } from "src/app/shared/models/externaldeploymentpath.model";
import { ExternalEnvironment } from "src/app/shared/models/externalenvironment.model";
import { ExternalDeploymentPathService } from "src/app/shared/services/externaldeploymentpath.service";
import { ExternalDeploymentPathEnvironmentService } from "src/app/shared/services/externaldeploymentpathenvironment.service";
import { ExternalEnvironmentService } from "src/app/shared/services/externalenvironment.service";
import {
  LayoutParameter,
  LayoutService,
  NotificationType,
} from "src/app/shared/services/layout.service";

@Component({
    selector: "app-external-deploymentpath",
    templateUrl: "./external-deploymentpath.component.html",
    styleUrls: ["./external-deploymentpath.component.css"],
    changeDetection: ChangeDetectionStrategy.Eager,
    standalone: false
})
export class ExternalDeploymentpathComponent {
  @ViewChild(DxDataGridComponent, { static: false }) dataGrid: DxDataGridComponent;

  public dataSourceExternalDeploymentPaths: DataSource;
  public isAddPopupVisible = false;
  public newExternalDeploymentPath: ExternalDeploymentPath = {};
  public isRenamePopupVisible = false;
  public externalDeploymentPathToEdit: ExternalDeploymentPath = {};

  public isManageStepsPopupVisible = false;
  public currentExternalDeploymentPathId: number;
  public currentExternalDeploymentPathName: string;
  public assignedSteps: ExternalEnvironment[] = [];
  public availableExternalEnvironments: ExternalEnvironment[] = [];

  constructor(
    private externalDeploymentPathService: ExternalDeploymentPathService,
    private externalDeploymentPathEnvironmentService: ExternalDeploymentPathEnvironmentService,
    private externalEnvironmentService: ExternalEnvironmentService,
    private layoutService: LayoutService
  ) {
    this.onDragStart = this.onDragStart.bind(this);
    this.onAddStep = this.onAddStep.bind(this);
    this.onRemoveStep = this.onRemoveStep.bind(this);
    this.onReorderStep = this.onReorderStep.bind(this);
    this.onClickManageSteps = this.onClickManageSteps.bind(this);
    this.onClickOpenRename = this.onClickOpenRename.bind(this);
    this.onClickDelete = this.onClickDelete.bind(this);
    this.displayName = this.displayName.bind(this);

    this.dataSourceExternalDeploymentPaths = new DataSource({
      store: this.externalDeploymentPathService.getStore(),
      sort: [{ selector: "Name", desc: false }],
      expand: ["CreatedByNavigation"],
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
        onClick: this.onClickRefresh.bind(this),
      },
      location: "after",
    });

    toolbarItems.unshift({
      widget: "dxButton",
      options: {
        icon: "add",
        text: "Add external deployment path",
        stylingMode: "contained",
        type: "success",
        onClick: this.onClickOpenAdd.bind(this),
      },
      location: "after",
    });
  }

  public onClickRefresh(): void {
    this.dataGrid.instance.refresh();
  }

  public onClickOpenAdd(): void {
    this.newExternalDeploymentPath = {};
    this.isAddPopupVisible = true;
  }

  public onClickSaveAdd(): void {
    this.layoutService.change(LayoutParameter.ShowLoading, true);
    this.externalDeploymentPathService
      .add(this.newExternalDeploymentPath)
      .then(() =>
        this.layoutService.notify({
          type: NotificationType.Success,
          message: "The external deployment path has been created successfully.",
        })
      )
      .catch((error: Error) =>
        this.layoutService.notify({
          type: NotificationType.Error,
          message: error?.message
            ? `The external deployment path could not be created: ${error.message}`
            : "The external deployment path could not be created.",
        })
      )
      .finally(() => {
        this.isAddPopupVisible = false;
        this.newExternalDeploymentPath = {};
        this.layoutService.change(LayoutParameter.ShowLoading, false);
        this.dataGrid.instance.refresh();
      });
  }

  public onClickOpenRename(e): void {
    this.externalDeploymentPathToEdit = { Id: e.row.data.Id, Name: e.row.data.Name };
    this.isRenamePopupVisible = true;
  }

  public onClickSaveRename(): void {
    this.layoutService.change(LayoutParameter.ShowLoading, true);
    this.externalDeploymentPathService
      .update(this.externalDeploymentPathToEdit.Id, { Name: this.externalDeploymentPathToEdit.Name })
      .then(() =>
        this.layoutService.notify({
          type: NotificationType.Success,
          message: "The name change has been saved successfully.",
        })
      )
      .catch((error: Error) =>
        this.layoutService.notify({
          type: NotificationType.Error,
          message: error?.message ? `The name change could not be saved: ${error.message}` : "The name change could not be saved.",
        })
      )
      .finally(() => {
        this.isRenamePopupVisible = false;
        this.externalDeploymentPathToEdit = {};
        this.layoutService.change(LayoutParameter.ShowLoading, false);
        this.dataGrid.instance.refresh();
      });
  }

  public onClickDelete(e): void {
    const externalDeploymentPath = e.row.data as ExternalDeploymentPath;
    const result = confirm(
      `Would you like to delete the external deployment path "${externalDeploymentPath.Name}"?`,
      "Delete External Deployment Path"
    );
    result.then((dialogResult) => {
      if (dialogResult) {
        this.layoutService.change(LayoutParameter.ShowLoading, true);
        this.externalDeploymentPathService
          .remove(externalDeploymentPath.Id)
          .then(() =>
            this.layoutService.notify({
              type: NotificationType.Success,
              message: "The external deployment path was successfully deleted.",
            })
          )
          .catch((error: Error) =>
            this.layoutService.notify({
              type: NotificationType.Error,
              message: error?.message
                ? `The external deployment path could not be deleted: ${error.message}`
                : "The external deployment path could not be deleted.",
            })
          )
          .finally(() => {
            this.layoutService.change(LayoutParameter.ShowLoading, false);
            this.dataGrid.instance.refresh();
          });
      }
    });
  }

  public onClickManageSteps(e): void {
    this.currentExternalDeploymentPathId = e.row.data.Id;
    this.currentExternalDeploymentPathName = e.row.data.Name;

    this.layoutService.change(LayoutParameter.ShowLoading, true);
    this.loadSteps().finally(() => {
      this.isManageStepsPopupVisible = true;
      this.layoutService.change(LayoutParameter.ShowLoading, false);
    });
  }

  public onDragStart(e): void {
    e.itemData = e.fromData[e.fromIndex];
  }

  public onAddStep(e): void {
    if (this.assignedSteps.some((x) => x.Id == e.itemData.Id)) {
      e.cancel = true;
      return;
    }
    e.toData.splice(e.toIndex, 0, e.itemData);
    const externalEnvironmentId = e.itemData.Id;
    const stepNumber = e.toIndex + 1;

    this.externalDeploymentPathEnvironmentService
      .getStore()
      .insert({
        ExternalDeploymentPath: this.currentExternalDeploymentPathId,
        ExternalEnvironment: externalEnvironmentId,
        StepNumber: stepNumber,
      })
      .then(() => {
        this.layoutService.notify({ type: NotificationType.Success, message: "Changes have been saved", displayTime: 1000 });
        this.refreshAvailableExternalEnvironments();
      })
      .catch((error: Error) => {
        e.toData.splice(e.toIndex, 1);
        this.layoutService.notify({
          type: NotificationType.Error,
          message: error?.message ? error.message : "The environment could not be added as a step.",
        });
      });
  }

  public onRemoveStep(e): void {
    e.fromData.splice(e.fromIndex, 1);
    const externalEnvironmentId = e.itemData.Id;

    this.externalDeploymentPathEnvironmentService
      .remove(this.currentExternalDeploymentPathId, externalEnvironmentId)
      .then(() => {
        this.layoutService.notify({ type: NotificationType.Success, message: "Changes have been saved", displayTime: 1000 });
        this.refreshAvailableExternalEnvironments();
      });
  }

  public onReorderStep(e): void {
    const externalEnvironmentId = e.itemData.Id;
    const fromIndex = e.fromIndex + 1;
    const toIndex = e.toIndex + 1;

    this.externalDeploymentPathEnvironmentService
      .getStore()
      .update(
        { ExternalDeploymentPath: this.currentExternalDeploymentPathId, ExternalEnvironment: externalEnvironmentId },
        { ToIndex: toIndex, FromIndex: fromIndex }
      )
      .then(() => {
        e.toData.splice(e.fromIndex, 1);
        e.toData.splice(e.toIndex, 0, e.itemData);
        this.layoutService.notify({ type: NotificationType.Success, message: "Changes have been saved", displayTime: 1000 });
      })
      .catch((error: Error) => {
        this.layoutService.notify({ type: NotificationType.Error, message: error.message });
      });
  }

  public displayName(externalEnvironment: ExternalEnvironment): string {
    if (!externalEnvironment) return "";
    return externalEnvironment.Alias || externalEnvironment.EnvironmentNavigation?.Name || "";
  }

  private loadSteps(): Promise<void> {
    const promiseSteps = this.externalDeploymentPathEnvironmentService
      .getStore()
      .load({
        filter: ["ExternalDeploymentPath", "=", this.currentExternalDeploymentPathId],
        sort: [{ selector: "StepNumber", desc: false }],
        expand: ["ExternalEnvironmentNavigation.EnvironmentNavigation.TenantNavigation"],
      });

    const promiseAllEnvironments = this.externalEnvironmentService
      .getStore()
      .load({
        filter: ["IsDeactive", "=", false],
        expand: ["EnvironmentNavigation.TenantNavigation"],
        sort: [{ selector: "Alias", desc: false }],
      });

    return Promise.all([promiseSteps, promiseAllEnvironments]).then(([steps, allEnvironments]) => {
      this.assignedSteps = steps.map((s) => s["ExternalEnvironmentNavigation"] as ExternalEnvironment);
      this.setAvailableExternalEnvironments(allEnvironments as ExternalEnvironment[]);
    });
  }

  private refreshAvailableExternalEnvironments(): void {
    this.externalEnvironmentService
      .getStore()
      .load({
        filter: ["IsDeactive", "=", false],
        expand: ["EnvironmentNavigation.TenantNavigation"],
        sort: [{ selector: "Alias", desc: false }],
      })
      .then((allEnvironments: ExternalEnvironment[]) => this.setAvailableExternalEnvironments(allEnvironments));
  }

  private setAvailableExternalEnvironments(allEnvironments: ExternalEnvironment[]): void {
    const assignedIds = this.assignedSteps.map((s) => s.Id);
    const tenantMsId = this.assignedSteps[0]?.EnvironmentNavigation?.TenantNavigation?.MsId;
    this.availableExternalEnvironments = allEnvironments.filter(
      (e) => !assignedIds.includes(e.Id) && (!tenantMsId || e.EnvironmentNavigation?.TenantNavigation?.MsId === tenantMsId)
    );
  }
}
