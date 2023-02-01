import { Component, ViewChild } from "@angular/core";
import { DxDataGridComponent } from "devextreme-angular";
import DataSource from "devextreme/data/data_source";
import { Column } from "devextreme/ui/data_grid";
import dxSelectBox from "devextreme/ui/select_box";
import { Application } from "src/app/shared/models/application.model";
import { Environment } from "src/app/shared/models/environment.model";
import { EnvironmentVariable } from "src/app/shared/models/environmentvariable.model";
import { ApplicationService } from "src/app/shared/services/application.service";
import { EnvironmentVariableService } from "src/app/shared/services/environmentvariable.service";
import {
  LayoutParameter,
  LayoutService,
  NotificationType,
} from "src/app/shared/services/layout.service";
import { SolutionService } from "src/app/shared/services/solution.service";

@Component({
  selector: 'app-environment-variable',
  templateUrl: './environment-variable.component.html',
  styleUrls: ['./environment-variable.component.css']
})
export class EnvironmentVariableComponent {
  @ViewChild(DxDataGridComponent, { static: false })
  dataGrid: DxDataGridComponent;
  public dataSource: DataSource;
  public selectedApplication: Application;
  public dataSourceApplications: DataSource;
  private applicationService: ApplicationService;
  public dataSourceSolutions: DataSource;
  public applicationSelectBoxInstance: dxSelectBox;
  public dataGridColumns: Column[] = [
    {
      caption: "Please select an application",
      name: "no_data",
    },
  ];

  constructor(
    private environmentVariableService: EnvironmentVariableService,
    private solutionService: SolutionService,
    private layoutService: LayoutService
  ) { 
    this.dataSource = new DataSource({
      store: this.environmentVariableService.getStore(),
      expand: [
        "CreatedByNavigation",
        "ModifiedByNavigation",
        "TenantNavigation",
      ],
    });
    this.dataSourceApplications = new DataSource({
      store: this.applicationService.getStore(),
      sort: [
        { selector: "OrdinalNumber", desc: false },
        { selector: "Name", desc: false },
      ],
    });
  }

  public ngOnInit(): void {
    let selectedApplicationIdFromLocalStorage =
      parseInt(
        localStorage.getItem("atPowerCIDPortal_SolutionOverview_SelectedId")
      ) || null;
    if (selectedApplicationIdFromLocalStorage != null)
      this.setSelectedApplicationId(selectedApplicationIdFromLocalStorage);
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
      },
    );
  }

  private onClickRefreshDataGrid(): void {
    this.dataGrid.instance.refresh();
  }

  private generateDataGridColumns(): void {
    this.environmentVariableService
      .getStore()
      .load()
      .then((environments: Environment[]) => {
        if (this.selectedApplication == null) {
          this.setSelectedApplicationId(null);
          return;
        }

        this.dataGridColumns = [
        ];
      });
  }

  private setSelectedApplicationId(id: number) {
    if (id != null) {
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

        this.generateDataGridColumns();
        this.dataSourceSolutions = new DataSource({
          store: this.solutionService.getStore(),
          filter: [["Application", "=", this.selectedApplication.Id]],
          expand: [
            "Actions.CreatedByNavigation",
            "Actions.TargetEnvironmentNavigation",
            "Actions.TypeNavigation",
            "Actions.StatusNavigation",
            "Actions.ResultNavigation",
            "Actions.SolutionNavigation",
            "Actions($orderby=StartTime desc)",
            "ApplicationNavigation",
            "ApplicationNavigation.DeploymentPaths",
            "ApplicationNavigation.ApplicationDeploymentPaths",
            "CreatedByNavigation",
            "ModifiedByNavigation",
          ],
        });
      });
    } else {
      localStorage.removeItem("atPowerCIDPortal_SolutionOverview_SelectedId");
    }
  }
}
