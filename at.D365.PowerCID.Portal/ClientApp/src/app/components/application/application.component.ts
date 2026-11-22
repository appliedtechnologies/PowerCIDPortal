import { Component, ViewChild, ChangeDetectionStrategy } from "@angular/core";
import { DxDataGridComponent } from "devextreme-angular";
import DataSource from "devextreme/data/data_source";
import ODataStore from "devextreme/data/odata/store";
import { Application } from "src/app/shared/models/application.model";
import { DeploymentPath } from "src/app/shared/models/deploymentpath.model";
import { Environment } from "src/app/shared/models/environment.model";
import { Publisher } from "src/app/shared/models/publisher.model";
import { ApplicationService } from "src/app/shared/services/application.service";
import { DeploymentpathService } from "src/app/shared/services/deploymentpath.service";
import { EnvironmentService } from "src/app/shared/services/environment.service";
import {
  LayoutParameter,
  LayoutService,
  NotificationType,
} from "src/app/shared/services/layout.service";
import { PublisherService } from "src/app/shared/services/publisher.service";
import { ApplicationdeploymentpathService } from "src/app/shared/services/applicationdeploymentpath.service";
import { confirm } from 'devextreme/ui/dialog';
import { ExternalDeploymentPath } from "src/app/shared/models/externaldeploymentpath.model";
import { ExternalDeploymentPathService } from "src/app/shared/services/externaldeploymentpath.service";
import { ApplicationExternalDeploymentPathService } from "src/app/shared/services/applicationexternaldeploymentpath.service";
import { ExternalEnvironmentService } from "src/app/shared/services/externalenvironment.service";
import { UserService } from "src/app/shared/services/user.service";

@Component({
    selector: "app-application",
    templateUrl: "./application.component.html",
    styleUrls: ["./application.component.css"],
    changeDetection: ChangeDetectionStrategy.Eager,
    standalone: false
})
export class ApplicationComponent {
  @ViewChild(DxDataGridComponent, { static: false }) dataGrid: DxDataGridComponent;
  publisherSelectionDisabled = true;
  filteredPublishers: Publisher[] = [];
  dataSourceApplications: DataSource;
  dataSourceEnvironments: DataSource;
  dataStoreEnvironments: ODataStore;
  showDeactivatedApplications = false;
  isPullApplications = false;
  currentEnvironment: Environment;
  developmentEnvironments: Environment[];
  pulledApplications: string[] = [];
  currentApplication: string;
  applicationSelectionDisabled = true;
  isAssignDevelopmentPaths = false;
  deploymentPaths: DeploymentPath[];
  currentApplicationId: number;
  applicationDeploymentPaths;
  currentApplicationName: string;

  isAssignExternalDeploymentPaths = false;
  isExternalDeploymentPathMutationPending = false;
  isExternalDeploymentPathOrderReloadRequired = false;
  externalDeploymentPaths: ExternalDeploymentPath[] = [];
  applicationExternalDeploymentPaths: ExternalDeploymentPath[] = [];
  private assignedExternalDeploymentPathOrder: number[] = [];
  private assignedExternalDeploymentPathsLoadGeneration = 0;
  private externalEnvironmentTenantNames = new Map<number, string>();

  constructor(
    public userService: UserService,
    private applicationService: ApplicationService,
    private environmentService: EnvironmentService,
    private publisherService: PublisherService,
    private deploymentPathService: DeploymentpathService,
    private applicationDeploymentPathService: ApplicationdeploymentpathService,
    private externalDeploymentPathService: ExternalDeploymentPathService,
    private applicationExternalDeploymentPathService: ApplicationExternalDeploymentPathService,
    private externalEnvironmentService: ExternalEnvironmentService,
    private layoutService: LayoutService
  ) {
    this.onAdd = this.onAdd.bind(this);
    this.onRemove = this.onRemove.bind(this);
    this.onReorder = this.onReorder.bind(this);
    this.onAddExternal = this.onAddExternal.bind(this);
    this.onRemoveExternal = this.onRemoveExternal.bind(this);
    this.onReorderExternal = this.onReorderExternal.bind(this);
    this.onClickAssignDeploymentPaths = this.onClickAssignDeploymentPaths.bind(this);
    this.onClickAssignExternalDeploymentPaths = this.onClickAssignExternalDeploymentPaths.bind(this);
    this.onClickDisableApplication = this.onClickDisableApplication.bind(this);
    this.onClickActivateApplication = this.onClickActivateApplication.bind(this);
    this.onClickToggleDeactivatedApplications = this.onClickToggleDeactivatedApplications.bind(this);
    this.onClickOpenPage = this.onClickOpenPage.bind(this);
    this.onClickOpenMakerPortal = this.onClickOpenMakerPortal.bind(this);
    this.loadFilteredPublishers = this.loadFilteredPublishers.bind(this);
    this.dataStoreEnvironments = this.environmentService.getStore();

    this.dataSourceApplications = new DataSource({
      store: this.applicationService.getStore(),
      expand: [
        "CreatedByNavigation",
        "ModifiedByNavigation",
        "DevelopmentEnvironmentNavigation",
        "PublisherNavigation",
      ],
      filter: [ "IsDeactive", "=", false ]
    });
    this.dataSourceEnvironments = new DataSource({
      store: this.environmentService.getStore(),
      filter: [["IsDevelopmentEnvironment", "=", true], "and", ["IsDeactive", "=", false]],
    });
  }

  public onClickRefresh(): void {
    this.dataGrid.instance.refresh();
  }

  public onToolbarPreparingDataGrid(e): void {
    const toolbarItems = e.toolbarOptions.items;

    toolbarItems.unshift({
      widget: "dxButton",
      options: {
        icon: "refresh",
        stylingMode: "contained",
        type: "success",
        hint: "Refresh Applications",
        onClick: this.onClickRefresh.bind(this),
      },
      location: "after",
    });

    toolbarItems.unshift({
      widget: "dxButton",
      options: {
        icon: "filter",
        text: "Show deactivated",
        stylingMode: "contained",
        type: "normal",
        hint: "Show deactivated applications.",
        onClick: this.onClickToggleDeactivatedApplications,
      },
      location: "after",
    });

    toolbarItems.unshift({
      widget: "dxButton",
      options: {
        icon: "download",
        text: "Add existing Application",
        stylingMode: "contained",
        type: "success",
        hint: "Add an existing Application from Tenant and saves them in PowerCID Portal.",
        onClick: this.onClickPullApplications.bind(this),
      },
      location: "after",
    });

    toolbarItems.unshift({
      widget: "dxButton",
      options: {
        icon: "add",
        text: "Add new Application",
        stylingMode: "contained",
        type: "success",
        hint: "Add a new Application",
        onClick: this.onClickAddNewRow.bind(this),
      },
      location: "after",
    });

    toolbarItems.unshift({
      widget: "dxButton",
      options: {
        icon: "at-icon powercid-icon-sortierpfeile",
        text: "Transfer sorting to Ordinal Numbers",
        stylingMode: "contained",
        type: "success",
        hint: "The current sorting of the grid is permanently saved as changed Ordinal Numbers and thus made available in other places.",
        onClick: this.onClickSaveSorting.bind(this),
      },
      location: "after",
    });
  }
  onDragStart(e) {
    e.itemData = e.fromData[e.fromIndex];
  }

  onAdd(e) {
    if (this.applicationDeploymentPaths.some((x) => x.Id == e.itemData.Id)) {
      e.Cancel = true;
    } else {
      e.toData.splice(e.toIndex, 0, e.itemData);
      const itemDataId = e.itemData.Id;
      const toIndex = e.toIndex + 1;

      this.applicationDeploymentPathService.getStore().insert({
        Application: this.currentApplicationId,
        DeploymentPath: itemDataId,
        HierarchieNumber: toIndex,
      }).then(() => this.layoutService.notify({type: NotificationType.Success, message: "Changes have been saved", displayTime: 1000}))
        .catch(() => {
          this.layoutService.notify({type: NotificationType.Error, message: "Could not save the deployment path order."});
          return this.loadApplicationDeploymentPaths().catch(() =>
            this.layoutService.notify({type: NotificationType.Error, message: "Could not reload the deployment path order."})
          );
        });
    }
  }

  onRemove(e) {
    e.fromData.splice(e.fromIndex, 1);
    const itemDataId = e.itemData.Id;

    this.applicationDeploymentPathService.getStore().remove({
      Application: this.currentApplicationId,
      DeploymentPath: itemDataId,
    }).then(() => this.layoutService.notify({type: NotificationType.Success, message: "Changes have been saved", displayTime: 1000}))
      .catch(() => {
        this.layoutService.notify({type: NotificationType.Error, message: "Could not save the deployment path order."});
        return this.loadApplicationDeploymentPaths().catch(() =>
          this.layoutService.notify({type: NotificationType.Error, message: "Could not reload the deployment path order."})
        );
      });
  }

  onReorder(e) {
    const itemDataId = e.itemData.Id;
    const fromIndex = e.fromIndex + 1;
    const toIndex = e.toIndex + 1;

    this.applicationDeploymentPathService
      .getStore()
      .update(
        {
          Application: this.currentApplicationId,
          DeploymentPath: itemDataId,
        },
        {
          ToIndex: toIndex,
          FromIndex: fromIndex,
          ExpectedOrder: this.applicationDeploymentPaths.map((path) => path.Id),
        }
      )
      .then(() => {
        e.toData.splice(e.fromIndex, 1);
        e.toData.splice(e.toIndex, 0, e.itemData);
        this.layoutService.notify({type: NotificationType.Success, message: "Changes have been saved", displayTime: 1000});
      })
      .catch(() => {
        this.layoutService.notify({type: NotificationType.Error, message: "The deployment path order changed or could not be saved. The latest order is being loaded."});
        return this.loadApplicationDeploymentPaths().catch(() =>
          this.layoutService.notify({type: NotificationType.Error, message: "Could not reload the deployment path order."})
        );
      });
  }

  onClickAssignDeploymentPaths(e) {
    this.currentApplicationName = e.row.data.Name;
    this.currentApplicationId = e.row.data.Id;
    this.deploymentPathService
      .getStore()
      .load()
      .then((d) => {
        this.deploymentPaths = d;
      });

    this.loadApplicationDeploymentPaths().catch(() =>
      this.layoutService.notify({type: NotificationType.Error, message: "Could not load the application's deployment paths."})
    );

    this.isAssignDevelopmentPaths = true;
  }

  onAddExternal(e) {
    if (this.isExternalDeploymentPathMutationPending || this.isExternalDeploymentPathOrderReloadRequired) {
      e.cancel = true;
      return;
    }

    if (this.applicationExternalDeploymentPaths.some((x) => x.Id == e.itemData.Id)) {
      e.cancel = true;
    } else {
      const applicationId = this.currentApplicationId;
      const expectedOrder = [...this.assignedExternalDeploymentPathOrder];
      this.isExternalDeploymentPathMutationPending = true;
      e.toData.splice(e.toIndex, 0, e.itemData);
      const itemDataId = e.itemData.Id;
      const toIndex = e.toIndex + 1;

      this.applicationExternalDeploymentPathService
        .insertWithExpectedOrder(applicationId, itemDataId, toIndex, expectedOrder)
        .then(() => this.loadAssignedExternalDeploymentPaths(applicationId)
        .then(() => this.layoutService.notify({
          type: NotificationType.Success,
          message: "Changes have been saved",
          displayTime: 1000,
        }))
        .catch(() => this.layoutService.notify({
          type: NotificationType.Error,
          message: "The external deployment path was saved, but the latest order could not be loaded.",
        })))
        .catch((error: Error) => {
          this.layoutService.notify({
            type: NotificationType.Error,
            message: error?.message || "Could not save the external deployment path order.",
          });
          return this.loadAssignedExternalDeploymentPaths(applicationId).catch(() =>
            this.layoutService.notify({
              type: NotificationType.Error,
              message: "Could not reload the external deployment path order.",
            })
          );
        })
        .finally(() => {
          this.isExternalDeploymentPathMutationPending = false;
        });
    }
  }

  onRemoveExternal(e) {
    if (this.isExternalDeploymentPathMutationPending || this.isExternalDeploymentPathOrderReloadRequired) return;

    const applicationId = this.currentApplicationId;
    this.isExternalDeploymentPathMutationPending = true;
    e.fromData.splice(e.fromIndex, 1);
    const itemDataId = e.itemData.Id;

    this.applicationExternalDeploymentPathService.getStore().remove({
      Application: applicationId,
      ExternalDeploymentPath: itemDataId,
    }).then(() => this.loadAssignedExternalDeploymentPaths(applicationId)
      .then(() => this.layoutService.notify({
        type: NotificationType.Success,
        message: "Changes have been saved",
        displayTime: 1000,
      }))
      .catch(() => this.layoutService.notify({
        type: NotificationType.Error,
        message: "The external deployment path was removed, but the latest order could not be loaded.",
      })))
      .catch((error: Error) => {
        this.layoutService.notify({
          type: NotificationType.Error,
          message: error?.message || "Could not remove the external deployment path.",
        });
        return this.loadAssignedExternalDeploymentPaths(applicationId).catch(() =>
          this.layoutService.notify({
            type: NotificationType.Error,
            message: "Could not reload the external deployment path order.",
          })
        );
      })
      .finally(() => {
        this.isExternalDeploymentPathMutationPending = false;
      });
  }

  onReorderExternal(e) {
    if (this.isExternalDeploymentPathMutationPending || this.isExternalDeploymentPathOrderReloadRequired) return;

    const applicationId = this.currentApplicationId;
    this.isExternalDeploymentPathMutationPending = true;
    const itemDataId = e.itemData.Id;
    const fromIndex = e.fromIndex + 1;
    const toIndex = e.toIndex + 1;

    this.applicationExternalDeploymentPathService
      .moveWithExpectedOrder(
        applicationId,
        itemDataId,
        fromIndex,
        toIndex,
        [...this.assignedExternalDeploymentPathOrder]
      )
      .then(() => this.loadAssignedExternalDeploymentPaths(applicationId)
        .then(() => this.layoutService.notify({
          type: NotificationType.Success,
          message: "Changes have been saved",
          displayTime: 1000,
        }))
        .catch(() => this.layoutService.notify({
          type: NotificationType.Error,
          message: "The external deployment path order was saved, but the latest order could not be loaded.",
        })))
      .catch((error: Error) => {
        this.layoutService.notify({
          type: NotificationType.Error,
          message: error?.message || "Could not save the external deployment path order.",
        });
        return this.loadAssignedExternalDeploymentPaths(applicationId).catch(() =>
          this.layoutService.notify({
            type: NotificationType.Error,
            message: "Could not reload the external deployment path order.",
          })
        );
      })
      .finally(() => {
        this.isExternalDeploymentPathMutationPending = false;
      });
  }

  private loadApplicationDeploymentPaths(): Promise<void> {
    return this.applicationService
      .getStore()
      .load({
        filter: "Id eq " + this.currentApplicationId,
        expand: ["DeploymentPaths", "ApplicationDeploymentPaths"],
        select: "DeploymentPaths",
      })
      .then((applications) => {
        const application = applications[0] as Application | undefined;
        this.applicationDeploymentPaths = application
          ? this.applicationService.sortAfterHierarchieAndStepNumber(application, false)
          : [];
      });
  }

  onClickAssignExternalDeploymentPaths(e) {
    this.currentApplicationName = e.row.data.Name;
    this.currentApplicationId = e.row.data.Id;
    this.isExternalDeploymentPathOrderReloadRequired = true;
    Promise.all([
      this.externalEnvironmentService.getStore().load({
        expand: ["EnvironmentNavigation.TenantNavigation"],
      }),
      this.loadAssignedExternalDeploymentPaths(this.currentApplicationId),
    ]).then(([environments]) => {
      this.externalEnvironmentTenantNames = new Map(
        (environments as { Id: number; EnvironmentNavigation?: { TenantNavigation?: { Name?: string } } }[]).map((environment) => [
          environment.Id,
          environment.EnvironmentNavigation?.TenantNavigation?.Name,
        ])
      );
    }).catch((error: Error) => {
      this.layoutService.notify({
        type: NotificationType.Error,
        message: error?.message || "Could not load external deployment paths.",
      });
    });

    this.isAssignExternalDeploymentPaths = true;
  }

  onReloadExternalDeploymentPaths(): void {
    if (this.isExternalDeploymentPathMutationPending) return;

    this.loadAssignedExternalDeploymentPaths(this.currentApplicationId).catch((error: Error) =>
      this.layoutService.notify({
        type: NotificationType.Error,
        message: error?.message || "Could not reload the external deployment path order.",
      })
    );
  }

  private loadAssignedExternalDeploymentPaths(applicationId: number): Promise<void> {
    if (applicationId !== this.currentApplicationId) {
      return Promise.resolve();
    }

    const requestGeneration = ++this.assignedExternalDeploymentPathsLoadGeneration;
    return Promise.all([
      this.externalDeploymentPathService.getStore().load({
        expand: ["ExternalDeploymentPathEnvironments.ExternalEnvironmentNavigation"],
      }),
      this.applicationService.getStore().load({
        filter: "Id eq " + applicationId,
        expand: ["ApplicationExternalDeploymentPaths.ExternalDeploymentPathNavigation"],
      }),
    ]).then(([paths, applications]) => {
        if (applicationId !== this.currentApplicationId ||
            requestGeneration !== this.assignedExternalDeploymentPathsLoadGeneration)
          return;

        this.externalDeploymentPaths = paths as ExternalDeploymentPath[];
        for (const path of this.externalDeploymentPaths) {
          path.ExternalDeploymentPathEnvironments = (path.ExternalDeploymentPathEnvironments ?? [])
            .sort((a, b) =>
              (a.StepNumber ?? Number.MAX_SAFE_INTEGER) - (b.StepNumber ?? Number.MAX_SAFE_INTEGER) ||
              (a.ExternalEnvironment ?? 0) - (b.ExternalEnvironment ?? 0)
            );
        }

        const application = applications[0] as Application | undefined;
        const pathsById = new Map(
          this.externalDeploymentPaths
            .filter((path): path is ExternalDeploymentPath & { Id: number } => typeof path.Id === "number")
            .map((path) => [path.Id, path])
        );

        this.applicationExternalDeploymentPaths = (application?.ApplicationExternalDeploymentPaths ?? [])
          .map((assignment) => ({
            path: pathsById.get(assignment.ExternalDeploymentPath) ?? assignment.ExternalDeploymentPathNavigation,
            hierarchyNumber: assignment.HierarchieNumber,
          }))
          .filter((assignment) => assignment.path !== undefined)
          .sort((a, b) => {
            const hierarchyA = Number.isFinite(a.hierarchyNumber)
              ? a.hierarchyNumber
              : Number.MAX_SAFE_INTEGER;
            const hierarchyB = Number.isFinite(b.hierarchyNumber)
              ? b.hierarchyNumber
              : Number.MAX_SAFE_INTEGER;
            return hierarchyA - hierarchyB || (a.path.Id ?? 0) - (b.path.Id ?? 0);
          })
          .map((assignment) => assignment.path);
        this.assignedExternalDeploymentPathOrder = this.applicationExternalDeploymentPaths
          .map((path) => path.Id)
          .filter((id): id is number => typeof id === "number");
        this.isExternalDeploymentPathOrderReloadRequired = false;
      })
      .catch((error) => {
        if (applicationId !== this.currentApplicationId ||
            requestGeneration !== this.assignedExternalDeploymentPathsLoadGeneration)
          return;

        this.isExternalDeploymentPathOrderReloadRequired = true;
        throw error;
      });
  }

  externalDeploymentPathDisplayName(path: ExternalDeploymentPath): string {
    if (!path) return "";
    const environmentId = path.ExternalDeploymentPathEnvironments?.[0]?.ExternalEnvironment;
    const tenantName = this.externalEnvironmentTenantNames.get(environmentId);
    return tenantName
      ? `${path.Name} (${tenantName})`
      : path.Name || "";
  }

  onClickDisableApplication(e) {
    const application = e.row.data as Application;
    const result = confirm("Are you sure you want to disable this application?<br /> This will NOT delete the application in any environment.", "Confirm Deactivation");
    result.then((dialogResult) => {
      if (dialogResult) {
        this.layoutService.change(LayoutParameter.ShowLoading, true);
        this.applicationService.setDeactivated(application.Id, true)
          .then(() => {
            this.layoutService.notify({
              type: NotificationType.Success,
              message: "Application was successfully disabled.",
            });
          })
          .catch(() => {
            this.layoutService.notify({
              type: NotificationType.Error,
              message: "An error occurred while disabling the application.",
            });
          })
          .then(() => {
            this.layoutService.change(LayoutParameter.ShowLoading, false);
            this.dataGrid.instance.refresh();
          });
      }
    });
  }

  onClickActivateApplication(e): void {
    const application = e.row.data as Application;
    this.layoutService.change(LayoutParameter.ShowLoading, true);
    this.applicationService
      .setDeactivated(application.Id, false)
      .then(() => {
        this.layoutService.notify({
          type: NotificationType.Success,
          message: "Application was successfully activated.",
        });
      })
      .catch(() => {
        this.layoutService.notify({
          type: NotificationType.Error,
          message: "An error occurred while activating the application.",
        });
      })
      .finally(() => {
        this.layoutService.change(LayoutParameter.ShowLoading, false);
        this.dataGrid.instance.refresh();
      });
  }

  onClickToggleDeactivatedApplications(e): void {
    this.showDeactivatedApplications = !this.showDeactivatedApplications;
    this.dataSourceApplications.filter(["IsDeactive", "=", this.showDeactivatedApplications]);
    this.dataSourceApplications.reload();
    e.component.option(
      "text",
      this.showDeactivatedApplications ? "Show active" : "Show deactivated"
    );
    e.component.option(
      "hint",
      this.showDeactivatedApplications
        ? "Show active applications."
        : "Show deactivated applications."
    );
  }

  public isActiveApplication(e): boolean {
    return !e.row.data.IsDeactive;
  }

  public isDeactivatedApplication(e): boolean {
    return e.row.data.IsDeactive === true;
  }

  onClickAddNewRow() {
    this.dataGrid.instance.addRow();
  }

  onClickPullApplications(): void {
    this.environmentService
      .getStore()
      .load({ filter: [["IsDevelopmentEnvironment", "=", true], "and", ["IsDeactive", "=", false]] })
      .then((e) => (this.developmentEnvironments = e));
    this.isPullApplications = true;
  }

  onValueChangedEnvironments(e) {
    this.currentEnvironment = e.value;
    this.applicationService
      .callPullApplications(e.value)
      .then((a) => (this.pulledApplications = a["value"]));
    this.applicationSelectionDisabled = false;
  }

  onValueChangedApplications(e) {
    this.currentApplication = e.value;
  }

  onClickSaveApplication() {
    this.applicationService
      .saveApplication(this.currentApplication, this.currentEnvironment)
      .then(() => {
        this.layoutService.notify({
          type: NotificationType.Success,
          message: "Application was successfully saved",
        });
        this.isPullApplications = false;
        this.dataGrid.instance.refresh();
      })
      .catch((e) => {
        const message =
          this.currentApplication == null
            ? "Select an application"
            : e.error.value;

        this.layoutService.notify({
          type: NotificationType.Error,
          message: message,
        });
      })
      .finally(() => {
        this.layoutService.change(LayoutParameter.ShowLoading, false);
      });
  }

  onInitNewRowApplication(e) {
    this.publisherSelectionDisabled = true;
    this.filteredPublishers = [];
    e.data.ForceManagedDeployment = false;
  }

  onValueChangedEnvironment(event, cellInfo) {
    cellInfo.setValue(event.value.Id);

    this.loadFilteredPublishers(event.value.Id).then(
      () => (this.publisherSelectionDisabled = false)
    );
  }

  onValueChangedPublishers(event, cellInfo) {
    cellInfo.setValue(event.value);
  }

  loadFilteredPublishers(environmentId: number): Promise<void> {
    return new Promise<void>((resolve, reject) => {
      const publishers: Publisher[] = [];
      if (environmentId !== undefined) {
        this.environmentService
          .getDataversePublishers(environmentId)
          .then((data) => {
            data
              .filter((d) => d.isreadonly == false)
              .forEach((publisher) => {
                publishers.push({
                  Name: publisher.friendlyname,
                  MsId: publisher.publisherid,
                });
              });
            this.filteredPublishers = publishers;
            resolve();
          });
      } else {
        reject();
      }
    });
  }

  onRowInserted(e) {
    void e;
    this.layoutService.notify({
      type: NotificationType.Success,
      message: "Created solution in dataverse successfully.",
    });
  }

  createdByFullName(rowData) {
    if (rowData.CreatedByNavigation == undefined) {
      return "";
    }
    return (
      rowData.CreatedByNavigation.Firstname +
      " " +
      rowData.CreatedByNavigation.Lastname
    );
  }

  modifiedByFullName(rowData) {
    if (rowData.ModifiedByNavigation == undefined) {
      return "";
    }
    return (
      rowData.ModifiedByNavigation.Firstname +
      " " +
      rowData.ModifiedByNavigation.Lastname
    );
  }

  onEditorPreparing(e): void {
    const columnCaptionNames: string[] = [
      "Solution Unique Name",
      "Development Environment",
    ];
    for (const columnCaptionName of columnCaptionNames) {
      if (e.caption == columnCaptionName && e.parentType == "dataRow") {
        e.editorOptions.disabled = !e.row.isNewRow;
      }
    }
    if (e.parentType === "dataRow" && e.caption === "Publisher") {
      if (e.row.isNewRow) {
        e.editorOptions.disabled =
          typeof e.row.data.DevelopmentEnvironment !== "number";
      } else {
        e.editorOptions.disabled = true;
      }
    }
  }

  ordinalNumberSortValue(rowData: Application) {
    if (rowData.OrdinalNumber == null) return Infinity;
    else return rowData.OrdinalNumber;
  }

  onClickOpenPage(e) {
    if (e.row.data.MsId == null) {
      this.layoutService.notify({
        type: NotificationType.Info,
        message: "App Id is missing. Make sure to add an app id if available.",
      });
    } else {
      const url =
        e.row.data.DevelopmentEnvironmentNavigation.BasicUrl +
        "main.aspx?appid=" +
        e.row.data.MsId;
      window.open(url, "_blank");
    }
  }

  public onClickOpenMakerPortal(e): void {
    this.layoutService.change(LayoutParameter.ShowLoading, true);
    this.applicationService.getMakerPortalUrl(e.row.data.Id)
      .then((url: string) => window.open(url, "_blank"))
      .finally(() => this.layoutService.change(LayoutParameter.ShowLoading, false));
  }

  private onClickSaveSorting(): void {
    const result = confirm("Are you sure you want to save the current sorting of the grid permanently as Ordinal Numbers?<br /> Existing Ordinal Numbers will be overwritten. Hidden rows (by filtering) are not included.", "Overwrite Ordinal Numbers");
    result.then((dialogResult) => {
      if (dialogResult) {
        const loadOptions = this.dataGrid.instance.getDataSource().loadOptions();
        const filterExpression = this.dataGrid.instance.getCombinedFilter(true);

        this.dataGrid.instance.getDataSource().store().load({filter: filterExpression, sort: loadOptions?.sort}).then((rows: Application[]) => {
          const updates = [];

          rows.forEach((row, index) => { 
            updates.push(this.applicationService.update(row.Id, {OrdinalNumber: index}));
          });

          Promise.all(updates).then(() => {
            this.dataGrid.instance.refresh();
            this.layoutService.notify({"message": "The current sorting of the grid was successfully saved as Ordinal Numbers.", "type": NotificationType.Success});
          })
          .catch(() => {
            this.layoutService.notify({"message": "An error occurred while saving the current sorting of the grid as Ordinal Numbers.", "type": NotificationType.Error});
          });
        });
      }
    });
  }
}
