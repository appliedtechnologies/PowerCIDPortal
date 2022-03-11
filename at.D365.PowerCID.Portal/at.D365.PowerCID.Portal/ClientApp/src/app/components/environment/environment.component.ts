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

@Component({
  selector: "app-environment",
  templateUrl: "./environment.component.html",
  styleUrls: ["./environment.component.css"],
})
export class EnvironmentComponent {
  @ViewChild(DxDataGridComponent, { static: false })
  dataGrid: DxDataGridComponent;
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
        hint: "Refresh the table.",
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
}
