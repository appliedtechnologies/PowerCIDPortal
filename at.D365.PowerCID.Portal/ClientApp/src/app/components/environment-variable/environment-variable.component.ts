import { Component, ViewChild } from "@angular/core";
import { DxDataGridComponent } from "devextreme-angular";
import DataSource from "devextreme/data/data_source";
import { EnvironmentVariableService } from "src/app/shared/services/environmentvariable.service";
import { EnvironmentVariableEnvironmentService } from "src/app/shared/services/environmentvariableenvironment.service";
import { LayoutParameter, LayoutService, NotificationType } from "src/app/shared/services/layout.service";

@Component({
  selector: 'app-environment-variable',
  templateUrl: './environment-variable.component.html',
  styleUrls: ['./environment-variable.component.css']
})
export class EnvironmentVariableComponent {
  @ViewChild(DxDataGridComponent, { static: false })
  dataGrid: DxDataGridComponent;
  public dataSource: DataSource;
  
  constructor(
    private environmentVariableService: EnvironmentVariableService,
    private layoutService: LayoutService
    ) {
      this.dataSource = new DataSource({
        store: this.environmentVariableService.getStore(),
        expand: [
          "CreatedByNavigation",
          "ModifiedByNavigation",
          "ApplicationNavigation"
        ],
      });
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
    this.environmentVariableService
      .getExistingForApplication(32)
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
