import { Component, OnDestroy, OnInit, ViewChild } from "@angular/core";
import {
  DxButtonComponent,
  DxDataGridComponent,
  DxSelectBoxComponent,
  DxSelectBoxModule,
} from "devextreme-angular";
import DataSource from "devextreme/data/data_source";
import { Column } from "devextreme/ui/data_grid";
import dxSelectBox from "devextreme/ui/select_box";
import { ApplicationService } from "src/app/shared/services/application.service";
import { PatchService } from "src/app/shared/services/patch.service";
import { SolutionService } from "src/app/shared/services/solution.service";
import { UpgradeService } from "src/app/shared/services/upgrade.service";
import { UserService } from "src/app/shared/services/user.service";
import { Patch } from "src/app/shared/models/patch.model";
import { Application } from "src/app/shared/models/application.model";
import { Environment } from "src/app/shared/models/environment.model";
import { EnvironmentService } from "src/app/shared/services/environment.service";
import { Action } from "src/app/shared/models/action.model";
import { ActionResult } from "src/app/shared/models/actionresult.model";
import { ActionStatus } from "src/app/shared/models/actionstatus.model";
import { Solution } from "src/app/shared/models/solution.model";
import dxButton from "devextreme/ui/button";
import { ActionService } from "src/app/shared/services/action.service";
import {
  LayoutParameter,
  LayoutService,
  NotificationType,
} from "src/app/shared/services/layout.service";
import { DeploymentPath } from "src/app/shared/models/deploymentpath.model";
import { confirm } from 'devextreme/ui/dialog';

@Component({
  selector: "app-solutions-overview",
  templateUrl: "./solutions-overview.component.html",
  styleUrls: ["./solutions-overview.component.css"],
})
export class SolutionsOverviewComponent implements OnInit, OnDestroy {
  @ViewChild(DxDataGridComponent, { static: false })
  dataGrid: DxDataGridComponent;
  public applicationSelectBoxInstance: dxSelectBox;
  public autoRefreshHintButtonInstance: dxButton;
  public autoRefreshCancelButtonInstance: dxButton;
  public addPatchButtonInstance: dxButton;
  public addUpgradeButtonInstance: dxButton;
  public dataSourceSolutions: DataSource;
  public dataSourceApplications: DataSource;
  public dataGridColumns: Column[] = [
    {
      caption: "Please select an application",
      name: "no_data",
    },
  ];
  public autoRefreshInterval: number;
  public actionId: number;
  public isActionDetailPopupVisible: boolean;
  public detailSolution: Solution;
  public isSolutionDetailPopupVisible: boolean;
  public isAddUpgrade: boolean;
  public solutionDetailPopupTitle: string;
  public selectedApplication: Application;
  public canImport: boolean;
  public previousEnvironmentName: string;
  public environments: Environment[];
  public isConfigureDeploymentPopupVisible: boolean;
  public configureDeploymentEnvironment: Environment;

  private refreshAfterPopupClose: boolean = false;

  constructor(
    public userService: UserService,
    private applicationService: ApplicationService,
    private solutionService: SolutionService,
    private environmentService: EnvironmentService,
    private actionService: ActionService,
    private layoutService: LayoutService,
    private patchService: PatchService
  ) {
    this.dataSourceApplications = new DataSource({
      store: this.applicationService.getStore(),
      sort: [
        { selector: "OrdinalNumber", desc: false },
        { selector: "Name", desc: false },
      ],
      filter: [ "IsDeactive", "=", false ]
    });
    this.environmentService
      .getStore()
      .load()
      .then((d: Environment[]) => (this.environments = d));
  }

  public ngOnInit(): void {
    let selectedApplicationIdFromLocalStorage =
      parseInt(
        localStorage.getItem("atPowerCIDPortal_SolutionOverview_SelectedId")
      ) || null;
    if (selectedApplicationIdFromLocalStorage != null)
      this.setSelectedApplicationId(selectedApplicationIdFromLocalStorage);
  }

  public ngOnDestroy(): void {
    this.cancelAutoRefresh();
  }

  public onToolbarPreparingSolutionsGrid(e): void {
    let toolbarItems = e.toolbarOptions.items;

    toolbarItems.unshift(
      {
        location: "after",
        widget: "dxButton",
        options: {
          icon: "assets/animations/loading.gif",
          text: "auto-refresh is active",
          stylingMode: "text",
          disabled: true,
          visible: this.autoRefreshInterval != undefined,
          onInitialized: (args: any) => {
            this.autoRefreshHintButtonInstance = args.component;
          },
        },
      },
      {
        location: "after",
        widget: "dxButton",
        options: {
          icon: "clear",
          stylingMode: "contained",
          type: "success",
          onClick: this.onClickCancelAutoRefresh.bind(this),
          visible: this.autoRefreshInterval != undefined,
          onInitialized: (args: any) => {
            this.autoRefreshCancelButtonInstance = args.component;
          },
        },
      },
      {
        widget: "dxButton",
        options: {
          icon: "refresh",
          stylingMode: "contained",
          type: "success",
          hint: "Refresh the table.",
          onClick: this.onClickRefreshSolutionsGrid.bind(this),
        },
        location: "after",
      }
    );

    toolbarItems.push(
      {
        widget: "dxSelectBox",
        options: {
          placeholder: "Select Application",
          value: this.selectedApplication?.Id,
          hint: "Select an application from which solutions should be displayed.",
          displayExpr: "Name",
          onInitialized: (args: any) => {
            this.applicationSelectBoxInstance = args.component;
          },
          onValueChanged: (e) => {
            this.setSelectedApplicationId(e.value);
          },
          valueExpr: "Id",
          width: "300",
          dataSource: this.dataSourceApplications,
        },
        location: "before",
      }
    );

    if(!this.userService.currentDbUserWithTenant.TenantNavigation.DisablePatchCreation){
      toolbarItems.push(
        {
          location: "before",
          widget: "dxButton",
          options: {
            text: "Add Patch",
            icon: "add",
            stylingMode: "contained",
            type: "success",
            disabled: this.selectedApplication == null ? true : false,
            onClick: this.onClickAddPatch.bind(this),
            onInitialized: (args: any) => {
              this.addPatchButtonInstance = args.component;
            },
          },
        }
      );
    }

    toolbarItems.push(
      {
        location: "before",
        widget: "dxButton",
        options: {
          text: "Add Upgrade",
          icon: "add",
          stylingMode: "contained",
          type: "success",
          disabled: this.selectedApplication == null ? true : false,
          onClick: this.onClickAddUpgrade.bind(this),
          onInitialized: (args: any) => {
            this.addUpgradeButtonInstance = args.component;
          },
        },
      }
    );
  }

  public onClickConfigureDeployment(environment: Environment) {
    this.configureDeploymentEnvironment = environment;
    this.isConfigureDeploymentPopupVisible = true;
  }

  public onClickSolutionInfo(cellInfo): void {
    this.detailSolution = cellInfo.data;
    this.solutionDetailPopupTitle = `Details: ${this.detailSolution.Name}`;
    this.isSolutionDetailPopupVisible = true;
  }

  public onClickActionInfo(lastAction: Action): void {
    this.actionId = lastAction.Id;
    this.isActionDetailPopupVisible = true;
  }

  public onClickDownloadSolution(cellInfo, lastAction: Action) {
    var solutionType =
      lastAction.SolutionNavigation["ApplyManually"] === undefined
        ? "Patch"
        : "Upgrade";

    var solutionVersion = lastAction.SolutionNavigation.Version;

    let fileName = `${this.selectedApplication.SolutionUniqueName}_${solutionType}_${solutionVersion
      .split(".")
      .join("-")}.zip`;

    this.solutionService
      .getSolutionAsBase64String(lastAction.Solution)
      .then((base64String) =>
        this.downloadBase64File("text/plain", base64String["value"], fileName)
      );
  }

  downloadBase64File(contentType, base64Data, fileName) {
    const linkSource = `data:${contentType};base64,${base64Data}`;
    const downloadLink = document.createElement("a");
    downloadLink.href = linkSource;
    downloadLink.download = fileName;
    downloadLink.click();
  }

  public onClickDeploySolution(cellInfo, exportOnly: boolean = false, applyUpgradeOnly: boolean = false, enableFlowsOnly: boolean = false): void {
    let targetEnvironmentId = cellInfo.column.name.split(",")[1];

    if (exportOnly) {
      this.executeExport(cellInfo, exportOnly);
    } else if(applyUpgradeOnly) {
      this.layoutService.change(LayoutParameter.ShowLoading, true);
      this.startApplyUpgrade(cellInfo.data.Id, targetEnvironmentId)
        .then(() => {
          this.layoutService.change(LayoutParameter.ShowLoading, false);
        });
    } else if(enableFlowsOnly){
      this.layoutService.change(LayoutParameter.ShowLoading, true);
      this.startEnableFlows(cellInfo.data.Id, targetEnvironmentId)
        .then(() => {
          this.layoutService.change(LayoutParameter.ShowLoading, false);
        });
    } else {
      let deploymentPathId = cellInfo.column.name.split(",")[0];
      this.layoutService.change(LayoutParameter.ShowLoading, true);
      this.applicationService.getDeploymentSettingsStatus(this.selectedApplication.Id, targetEnvironmentId).then((status) => {
        if (status == 0) {
          let confirmResult = confirm("Import without completed Deployment Settings (e.g. Connection References)?", "Incomplete Deployment Settings");
          this.layoutService.change(LayoutParameter.ShowLoading, false);
          confirmResult.then((result) => {
            this.layoutService.change(LayoutParameter.ShowLoading, true);
            if (result == true) {
              this.startImport(cellInfo.data.Id, targetEnvironmentId, deploymentPathId)
                .then(() => {
                  this.layoutService.change(LayoutParameter.ShowLoading, false);
                });
            }
            else {
              this.layoutService.change(LayoutParameter.ShowLoading, false);
            }
          });
        }
        else {
          this.startImport(cellInfo.data.Id, targetEnvironmentId, deploymentPathId)
            .then(() => {
              this.layoutService.change(LayoutParameter.ShowLoading, false);
            });
        }
      });
    }
  }

  public executeExport(cellInfo, exportOnly: boolean) {
    this.layoutService.change(LayoutParameter.ShowLoading, true);
    this.solutionService
      .export(cellInfo.data.Id)
      .then((action) => {
        this.layoutService.notify({
          type: NotificationType.Success,
          message: "Export started...",
        });
        this.startAutoRefresh(action);
      })
      .catch(() => {
        this.layoutService.notify({
          type: NotificationType.Error,
          message: "An error occurred while starting an export.",
        });
      })
      .then(() => {
        this.layoutService.change(LayoutParameter.ShowLoading, false);
      });
  }

  public onClickAddPatch(): void {
    this.solutionDetailPopupTitle = "Add Patch";
    this.isSolutionDetailPopupVisible = true;
  }

  public onClickAddUpgrade(): void {
    this.solutionDetailPopupTitle = "Add Upgrade";
    this.isAddUpgrade = true;
    this.isSolutionDetailPopupVisible = true;
  }

  public onClickCancelAutoRefresh(): void {
    this.cancelAutoRefresh();
    this.dataGrid.instance.repaint();
  }

  public onHiddenSolutionDetailPopup(e): void {
    if(this.refreshAfterPopupClose)
      this.dataGrid.instance.refresh();

    this.resetSolutionDetailPopup();
  }

  public onSaveCompletedSolutionDetail(e): void {
    this.isSolutionDetailPopupVisible = false;
    this.resetSolutionDetailPopup();
    this.dataGrid.instance.refresh();
  }

  public onClickStartAutoRefresh(lastAction: Action): void {
    this.startAutoRefresh(lastAction);
  }

  public onClickCancelImport(lastAction: Action): void {
    let result = confirm("Are you sure you want to cancel this import?<br /> This will not affect any operations already in progress in the environment.", "Confirm Cancellation");
    result.then((dialogResult) => {
      if (dialogResult) {
        this.cancelAutoRefresh();
        this.layoutService.change(LayoutParameter.ShowLoading, true);
        this.actionService.cancelImport(lastAction.Id).then(() => {
          this.dataGrid.instance.refresh();
          this.layoutService.notify({ type: NotificationType.Success, message: "Import was canceled." });
        }).catch(() => {
          this.layoutService.notify({ type: NotificationType.Error, message: "Import could not be canceled." });
          this.startAutoRefresh(lastAction);
        }).finally(() =>{
          this.layoutService.change(LayoutParameter.ShowLoading, false);
        });
      }
    });
  }

  onClickDeletePatch(patch: Patch) {
    let result = confirm("Are you sure you want to delete this patch?<br /> This will also delete the patch in the development environment.", "Confirm Deletion");
    result.then((dialogResult) => {
      if (dialogResult) {
        this.layoutService.change(LayoutParameter.ShowLoading, true);
        this.patchService
              .delete(patch.Id)
              .then(() => {
                this.layoutService.notify({
                  type: NotificationType.Success,
                  message: "Patch was successfully deleted.",
                });
              })
              .catch(() => {
                this.layoutService.notify({
                  type: NotificationType.Error,
                  message: "An error occurred while deleting the patch.",
                });
              })
              .then(() => {
                this.layoutService.change(LayoutParameter.ShowLoading, false);
                this.dataGrid.instance.refresh();
              });
      }
    });
  }

  public getLastActionForEnvironment(cellInfo): Action {
    let allActionOfSolution: Action[] = cellInfo.data.Actions;
    let targetEnvironmentId =
      cellInfo.column.name.split(",").length > 1
        ? cellInfo.column.name.split(",")[1]
        : cellInfo.column.name;

    return allActionOfSolution.find(
      (e) => e.TargetEnvironment == targetEnvironmentId
    );
  }

  public canDeployToEnv(cellInfo): boolean {
    this.canImport = true;
    if (cellInfo.column.name.split(",").length > 1) {
      let deploymentPathId = cellInfo.column.name.split(",")[0];
      let environmentId = cellInfo.column.name.split(",")[1];

      let deploymentPath = this.selectedApplication.DeploymentPaths.find(
        (x) => x.Id == deploymentPathId
      );

      let stepNumber = deploymentPath?.DeploymentPathEnvironments.find(
        (x) =>
          x.DeploymentPath == deploymentPathId && x.Environment == environmentId
      ).StepNumber;

      let previousEnvironmentId;

      if (stepNumber > 1) {
        previousEnvironmentId = deploymentPath?.DeploymentPathEnvironments.find(
          (x) => x.StepNumber == stepNumber - 1
        ).Environment;

        this.previousEnvironmentName = this.environments.find(
          (x) => x.Id == previousEnvironmentId
        ).Name;

        this.canImport = cellInfo.data.Actions.some(
          (x) => x.TargetEnvironment == previousEnvironmentId && x.Result == 1
        );
      }
    }
    return this.canImport;
  }

  public onClickOpenMakerPortal(cellInfo: any): void {
    window.open(cellInfo.data.UrlMakerportal, "_blank")
  }

  public onRenamedSolutionDetail(e: any): void{
    this.refreshAfterPopupClose = true;
  }

  private startImport(solutionId: number, targetEnvironmentId, deploymentPathId): Promise<void> {
    return this.solutionService
      .import(solutionId, targetEnvironmentId, deploymentPathId)
      .then((action) => {
        this.layoutService.notify({
          type: NotificationType.Success,
          message: "Import started...",
        });
        this.startAutoRefresh(action);
      })
      .catch((error) => {
        this.layoutService.notify({
          type: NotificationType.Error,
          message: error.error.value
            ? `An error occurred while starting an import: ${error.error.value}`
            : "An error occurred while starting an import.",
        });
      });
  }

  private startApplyUpgrade(solutionId: number, targetEnvironmentId): Promise<void> {
    return this.solutionService
      .applyUpgrade(solutionId, targetEnvironmentId)
      .then((action) => {
        this.layoutService.notify({
          type: NotificationType.Success,
          message: "Apply upgrade started...",
        });
        this.startAutoRefresh(action);
      })
      .catch((error) => {
        this.layoutService.notify({
          type: NotificationType.Error,
          message: error.error.value
            ? `An error occurred while starting an apply upgrade: ${error.error.value}`
            : "An error occurred while starting an apply upgrade.",
        });
      });
  }

  private startEnableFlows(solutionId: number, targetEnvironmentId): Promise<void> {
    return this.solutionService
      .enableFlows(solutionId, targetEnvironmentId)
      .then((action) => {
        this.layoutService.notify({
          type: NotificationType.Success,
          message: "Enable flows started...",
        });
        this.startAutoRefresh(action);
      })
      .catch((error) => {
        this.layoutService.notify({
          type: NotificationType.Error,
          message: error.error.value
            ? `An error occurred while starting enable flows: ${error.error.value}`
            : "An error occurred while starting enable flows.",
        });
      });
  }

  private startAutoRefresh(action: Action) {
    let buffer = 1;
    this.autoRefreshHintButtonInstance.option("visible", true);
    this.autoRefreshCancelButtonInstance.option("visible", true);
    this.autoRefreshInterval = window.setInterval(() => {
      this.dataGrid.instance.refresh().then(() => {
        let refreshedLastAction: Action = (
          this.dataGrid.instance.getDataSource().items() as Solution[]
        ).find((e) => e.Id == action.Solution).Actions[0];

        if (refreshedLastAction.Status == 3) buffer--;
        if (refreshedLastAction.Status == 3 && buffer <= 0) {
          this.cancelAutoRefresh();
        }
      });
    }, 2000);
  }

  private resetSolutionDetailPopup(): void {
    this.detailSolution = undefined;
    this.isAddUpgrade = false;
    this.refreshAfterPopupClose = false;
  }

  private cancelAutoRefresh(): void {
    window.clearInterval(this.autoRefreshInterval);
    this.autoRefreshInterval = null;
    this.autoRefreshHintButtonInstance.option("visible", false);
    this.autoRefreshCancelButtonInstance.option("visible", false);
  }

  private generateDataGridColumns(): void {
    if (this.selectedApplication == null) {
      this.setSelectedApplicationId(null);
      return;
    }
    let developmentEnvironment: Environment =
      this.selectedApplication.DevelopmentEnvironmentNavigation;

    let sortedDeploymentPaths: DeploymentPath[] =
      this.applicationService.sortAfterHierarchieAndStepNumber(
        this.selectedApplication,
        true
      );

    this.dataGridColumns = [
      {
        caption: "Type",
        name: "Type",
        width: 90,
        allowSorting: true,
        allowReordering: false,
        cellTemplate: "typeCellTemplate",
        calculateCellValue: (rowData: any) => {
          if (rowData["ApplyManually"] === undefined) return "Patch";
          else return "Upgrade";
        },
        visibleIndex: 0,
      },
      {
        caption: "Version",
        name: "Version",
        dataField: "Version",
        allowSorting: true,
        allowReordering: false,
        sortOrder: "desc",
        sortingMethod: (a, b) =>
          a
            .replace(/\d+/g, (n: any) => +n + 100000)
            .localeCompare(b.replace(/\d+/g, (n: any) => +n + 100000)),
        cellTemplate: "versionCellTemplate",
        visibleIndex: 1,
      },
      {
        caption: "Name",
        name: "Name",
        dataField: "Name",
        allowSorting: true,
        allowReordering: false,
        cellTemplate: "nameCellTemplate",
        allowFiltering: true,
        visibleIndex: 2,
      },
      {
        caption: "Development Environment",
        name: "devEnvironment",
        allowReordering: false,
        columns: [
          //dev environment
          {
            cellTemplate: "devEnvironmentCellTemplate",
            caption: developmentEnvironment.Name,
            name: developmentEnvironment.Id.toString(),
            allowSorting: false,
            allowReordering: false,
            allowFiltering: false,
          },
        ],
      },
      //other environments
      ...sortedDeploymentPaths.map((d) => {
        return {
          caption: d.Name,
          allowReordering: false,
          name: d.Id.toString() + d.Name,

          columns: [
            ...d.Environments.map((e) => {
              return {
                headerCellTemplate: "environmentHeaderCellTemplate",
                cellTemplate: "environmentCellTemplate",
                caption: e.Name,
                name: d.Id.toString() + "," + e.Id.toString(),
                environmentData: e,
                allowSorting: false,
                allowFiltering: false,
              };
            }),
          ],
        };
      }),
    ];
  }

  private onClickRefreshSolutionsGrid(): void {
    this.dataGrid.instance.refresh();
  }

  private setSelectedApplicationId(id: number) {
    if (id != null && this.selectedApplication?.Id != id) {
      localStorage.setItem(
        "atPowerCIDPortal_SolutionOverview_SelectedId",
        id.toString()
      );

      this.applicationService.getApplicationById(id).then((application) => {
        this.selectedApplication = application;
        this.applicationSelectBoxInstance.option(
          "value",
          this.selectedApplication.Id
        );

        this.addPatchButtonInstance?.option("disabled", false);
        this.addUpgradeButtonInstance?.option("disabled", false);

        this.generateDataGridColumns();
        this.dataSourceSolutions = new DataSource({
          store: this.solutionService.getStore(),
          filter: [["Application", "=", this.selectedApplication.Id]],
          expand: [
            "Actions.TargetEnvironmentNavigation($select=DeployUnmanaged, ConnectionsOwner)",
            "Actions.TypeNavigation",
            "Actions.StatusNavigation",
            "Actions.ResultNavigation",
            "Actions($orderby=StartTime desc;$select=Type, Status, Result, TargetEnvironment)",
            "CreatedByNavigation($select=Firstname, Lastname)",
            "ModifiedByNavigation($select=Firstname, Lastname)",
          ]
        });
      });
    } else if (id == null){
      localStorage.removeItem("atPowerCIDPortal_SolutionOverview_SelectedId");
      this.addPatchButtonInstance?.option("disabled", true);
      this.addUpgradeButtonInstance?.option("disabled", true);
    }
  }
}
