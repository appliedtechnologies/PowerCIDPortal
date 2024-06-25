import { Component, ViewChild } from "@angular/core";
import { DxDataGridComponent } from "devextreme-angular";
import DataSource from "devextreme/data/data_source";
import { Environment } from "src/app/shared/models/environment.model";
import { EnvironmentService } from "src/app/shared/services/environment.service";
import {
  LayoutParameter,
  LayoutService,
  NotificationType,
} from "src/app/shared/services/layout.service";
import { UserService } from "src/app/shared/services/user.service";
import { confirm } from 'devextreme/ui/dialog';

@Component({
  selector: "app-environment",
  templateUrl: "./environment.component.html",
  styleUrls: ["./environment.component.css"],
})
export class EnvironmentComponent {
  @ViewChild(DxDataGridComponent, { static: false }) dataGrid: DxDataGridComponent;
  public dataSource: DataSource;

  constructor(
    private environmentService: EnvironmentService,
    private userService: UserService,
    private layoutService: LayoutService
  ) {
    this.dataSource = new DataSource({
      store: this.environmentService.getStore(),
      expand: [
        "CreatedByNavigation",
        "ModifiedByNavigation",
        "TenantNavigation",
      ],
    });
  }

  public modifiedByFullName(rowData): string {
    return (
      rowData.ModifiedByNavigation.Firstname +
      " " +
      rowData.ModifiedByNavigation.Lastname
    );
  }

  public ordinalNumberSortValue(rowData: Environment) {
    if (rowData.OrdinalNumber == null) return Infinity;
    else return rowData.OrdinalNumber;
  }

  public onToolbarPreparingDataGrid(e): void {
    let toolbarItems = e.toolbarOptions.items;

    toolbarItems.unshift({
      widget: "dxButton",
      options: {
        icon: "refresh",
        stylingMode: "contained",
        type: "success",
        hint: "Refresh the grid.",
        onClick: this.onClickRefreshDataGrid.bind(this),
      },
      location: "after",
    });

    toolbarItems.unshift({
      widget: "dxButton",
      options: {
        icon: "download",
        text: "Pull Environments",
        stylingMode: "contained",
        type: "success",
        hint: "Pull the existing environments from tenant and saves them in PowerCID Portal.",
        onClick: this.onClickPullEnvironments.bind(this),
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

  private onClickPullEnvironments(): void {
    this.layoutService.change(LayoutParameter.ShowLoading, true);
    this.environmentService
      .callPullEnvironments()
      .then(() => {
        this.layoutService.notify({
          type: NotificationType.Success,
          message:
            "Existing environments were successfully pulled from tenant.",
        });
        this.dataGrid.instance.refresh();
      })
      .finally(() => {
        this.layoutService.change(LayoutParameter.ShowLoading, false);
      });
  }

  private onClickRefreshDataGrid(): void {
    this.dataGrid.instance.refresh();
  }

  private onClickSaveSorting(): void {
    let result = confirm("Are you sure you want to save the current sorting of the grid permanently as Ordinal Numbers?<br /> Existing Ordinal Numbers will be overwritten. Hidden rows (by filtering) are not included.", "Overwrite Ordinal Numbers");
    result.then((dialogResult) => {
      if (dialogResult) {
        var loadOptions = this.dataGrid.instance.getDataSource().loadOptions();
        var filterExpression = this.dataGrid.instance.getCombinedFilter(true);

        this.dataGrid.instance.getDataSource().store().load({filter: filterExpression, sort: loadOptions?.sort}).then((rows: any) => {
          var updates = [];

          rows.forEach((row, index) => { 
            updates.push(this.environmentService.update(row.Id, {OrdinalNumber: index}));
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
