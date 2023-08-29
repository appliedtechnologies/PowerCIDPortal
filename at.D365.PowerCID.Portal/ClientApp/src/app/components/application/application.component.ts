import { Component, ViewChild } from "@angular/core";
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
import { ApplicationDeploymentPath } from "src/app/shared/models/applicationdeploymentpath.model";
import { from } from "rxjs";
import { confirm } from 'devextreme/ui/dialog';

@Component({
  selector: "app-application",
  templateUrl: "./application.component.html",
  styleUrls: ["./application.component.css"],
})
export class ApplicationComponent {
  @ViewChild(DxDataGridComponent, { static: false }) dataGrid: DxDataGridComponent;
  publisherSelectionDisabled = true;
  filteredPublishers: Publisher[] = [];
  dataSourceApplications: DataSource;
  dataSourceEnvironments: DataSource;
  dataStoreEnvironments: ODataStore;
  isPullApplications: boolean = false;
  currentEnvironment: Environment;
  developmentEnvironments: Environment[];
  pulledApplications: string[] = [];
  currentApplication: string;
  applicationSelectionDisabled = true;
  isAssignDevelopmentPaths: boolean = false;
  deploymentPaths: DeploymentPath[];
  currentApplicationId: number;
  applicationDeploymentPaths;
  currentApplicationName: any;

  constructor(
    private applicationService: ApplicationService,
    private environmentService: EnvironmentService,
    private publisherService: PublisherService,
    private deploymentPathService: DeploymentpathService,
    private applicationDeploymentPathService: ApplicationdeploymentpathService,
    private layoutService: LayoutService
  ) {
    this.onAdd = this.onAdd.bind(this);
    this.onRemove = this.onRemove.bind(this);
    this.onReorder = this.onReorder.bind(this);
    this.onClickAssignDeploymentPaths = this.onClickAssignDeploymentPaths.bind(this);
    this.onClickDisableApplication = this.onClickDisableApplication.bind(this);
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
      filter: "IsDevelopmentEnvironment eq true",
    });
  }

  public onClickRefresh(): void {
    this.dataGrid.instance.refresh();
  }

  public onToolbarPreparingDataGrid(e): void {
    let toolbarItems = e.toolbarOptions.items;

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
      let itemDataId = e.itemData.Id;
      let toIndex = e.toIndex + 1;

      this.applicationDeploymentPathService.getStore().insert({
        Application: this.currentApplicationId,
        DeploymentPath: itemDataId,
        HierarchieNumber: toIndex,
      }).then(() => this.layoutService.notify({type: NotificationType.Success, message: "Changes have been saved", displayTime: 1000}));
    }
  }

  onRemove(e) {
    e.fromData.splice(e.fromIndex, 1);
    let itemDataId = e.itemData.Id;
    let fromIndex = e.fromIndex + 1;

    this.applicationDeploymentPathService.getStore().remove({
      Application: this.currentApplicationId,
      DeploymentPath: itemDataId,
    }).then(() => this.layoutService.notify({type: NotificationType.Success, message: "Changes have been saved", displayTime: 1000}));
  }

  onReorder(e) {
    let itemDataId = e.itemData.Id;
    let fromIndex = e.fromIndex + 1;
    let toIndex = e.toIndex + 1;

    this.applicationDeploymentPathService
      .getStore()
      .update(
        {
          Application: this.currentApplicationId,
          DeploymentPath: itemDataId,
        },
        { ToIndex: toIndex, FromIndex: fromIndex }
      )
      .then(() => {
        e.toData.splice(e.fromIndex, 1);
        e.toData.splice(e.toIndex, 0, e.itemData);
        this.layoutService.notify({type: NotificationType.Success, message: "Changes have been saved", displayTime: 1000});
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

    this.applicationService
      .getStore()
      .load({
        filter: "Id eq " + this.currentApplicationId,
        expand: ["DeploymentPaths", "ApplicationDeploymentPaths"],
        select: "DeploymentPaths",
      })
      .then((ad) => {
        this.applicationDeploymentPaths = ad[0];
        this.applicationDeploymentPaths =
          this.applicationService.sortAfterHierarchieAndStepNumber(
            this.applicationDeploymentPaths,
            false
          );
      });

    this.isAssignDevelopmentPaths = true;
  }

  onClickDisableApplication(e) {
    let result = confirm("Are you sure you want to disable this application?<br /> This will NOT delete the application in any environment.", "Confirm Deactivation");
    result.then((dialogResult) => {
      if (dialogResult) {
        this.layoutService.change(LayoutParameter.ShowLoading, true);
        let applicationId: number = e.row.data.Id;
        this.applicationService.delete(applicationId)
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

  onClickAddNewRow() {
    this.dataGrid.instance.addRow();
  }

  onClickPullApplications(): void {
    this.environmentService
      .getStore()
      .load({ filter: "IsDevelopmentEnvironment eq true" })
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
        let message =
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
      let publishers: Publisher[] = [];
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
    var columnCaptionNames: String[] = [
      "Solution Unique Name",
      "Development Environment",
    ];
    for (let index = 0; index < columnCaptionNames.length; index++) {
      if (e.caption == columnCaptionNames[index] && e.parentType == "dataRow") {
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
      let url =
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
    let result = confirm("Are you sure you want to save the current sorting of the grid permanently as Ordinal Numbers?<br /> Existing Ordinal Numbers will be overwritten. Hidden rows (by filtering) are not included.", "Overwrite Ordinal Numbers");
    result.then((dialogResult) => {
      if (dialogResult) {
        var updates = [];
        this.dataGrid.instance.getVisibleRows().forEach((row, index) => { 
          updates.push(this.applicationService.update(row.data.Id, {OrdinalNumber: index}));
        });
        Promise.all(updates).then(() => {
          this.dataGrid.instance.refresh();
          this.layoutService.notify({"message": "The current sorting of the grid was successfully saved as Ordinal Numbers.", "type": NotificationType.Success});
        })
        .catch(() => {
          this.layoutService.notify({"message": "An error occurred while saving the current sorting of the grid as Ordinal Numbers.", "type": NotificationType.Error});
        });
      }
    });
  }
}
