import { Component, Input, OnInit, ViewChild, ChangeDetectionStrategy } from "@angular/core";
import { DxDataGridComponent } from "devextreme-angular";
import DataSource from "devextreme/data/data_source";
import ODataStore from "devextreme/data/odata/store";
import { ToolbarPreparingEvent } from "devextreme/ui/data_grid";
import { Action } from "src/app/shared/models/action.model";
import { ActionResult } from "src/app/shared/models/actionresult.model";
import { ActionStatus } from "src/app/shared/models/actionstatus.model";
import { ActionType } from "src/app/shared/models/actiontype.model";
import { Application } from "src/app/shared/models/application.model";
import { Environment } from "src/app/shared/models/environment.model";
import { Solution } from "src/app/shared/models/solution.model";
import { User } from "src/app/shared/models/user.model";
import { ActionService } from "src/app/shared/services/action.service";
import { ApplicationService } from "src/app/shared/services/application.service";
import { EnvironmentService } from "src/app/shared/services/environment.service";
import { ODataService } from "src/app/shared/services/odata.service";
import { SolutionService } from "src/app/shared/services/solution.service";
import { UserService } from "src/app/shared/services/user.service";

interface HistoryGridRowEvent {
  row: {
    data: Action;
  };
}

@Component({
    selector: "app-history",
    templateUrl: "./history.component.html",
    styleUrls: ["./history.component.css"],
    changeDetection: ChangeDetectionStrategy.Eager,
    standalone: false
})
export class HistoryComponent implements OnInit {
  @ViewChild(DxDataGridComponent, { static: false }) dataGrid: DxDataGridComponent;
  public dataSourceActions: DataSource;
  public dataStoreApplications: ODataStore;
  public dataStoreEnvironments: ODataStore;
  public dataStoreSolutions: ODataStore;
  public dataStoreUsers: ODataStore;
  public selectedActionId: number;
  public selectedSolution: Solution;
  public selectedApplication: Application;
  public isActionDetailPopupVisible: boolean;
  public isSolutionDetailPopupVisible: boolean;

  public headerFilterDataUser: unknown;
  public headerFilterDataSolution: unknown;
  public headerFilterDataApplication: unknown;
  public headerFilterDataEnvironment: unknown;
  public headerFilterDataActionResult: unknown;
  public headerFilterDataActionStatus: unknown;
  public headerFilterDataActionType: unknown;

  @Input() userId: number;

  constructor(
    private actionService: ActionService,
    private solutionService: SolutionService,
    private applicationService: ApplicationService,
    private environmentService: EnvironmentService,
    private userService: UserService,
    private odataService: ODataService
  ) {
    this.calculateDuration = this.calculateDuration.bind(this);
  }
  
  public ngOnInit(): void {  
    this.dataStoreApplications = this.applicationService.getStore();
    this.dataStoreEnvironments = this.environmentService.getStore();
    this.dataStoreSolutions = this.solutionService.getStore();
    this.dataStoreUsers = this.userService.getStore();

    this.headerFilterDataUser = {
      store: this.userService.getStore(),
      filter: ["IsDeactive", "=", false],
      map: (item: User) => {
          return {
              text: item.Firstname + " " + item.Lastname,
              value: item.Id,
              Id: item.Id
          }
      }
    };

    this.headerFilterDataSolution = {
      store: this.solutionService.getStore(),
      map: (item: Solution) => {
          return {
              text: item.Name,
              value: item.Id,
              Id: item.Id
          }
      }
    };

    this.headerFilterDataApplication = {
      store: this.applicationService.getStore(),
      filter: ["IsDeactive", "=", false],
      map: (item: Application) => {
          return {
              text: item.Name,
              value: item.Id,
              Id: item.Id
          }
      }
    };

    this.headerFilterDataEnvironment = {
      store: this.environmentService.getStore(),
      map: (item: Environment) => {
          return {
              text: item.Name,
              value: item.Id,
              Id: item.Id
          }
      }
    };

    this.headerFilterDataActionResult = {
      store: this.odataService.context["ActionResults"],
      map: (item: ActionResult) => {
          return {
              text: item.Result,
              value: item.Result,
              Id: item.Id
          }
      }
    };

    this.headerFilterDataActionStatus = {
      store: this.odataService.context["ActionStatus"],
      map: (item: ActionStatus) => {
          return {
              text: item.Status,
              value: item.Status,
              Id: item.Id
          }
      }
    };

    this.headerFilterDataActionType = {
      store: this.odataService.context["ActionTypes"],
      map: (item: ActionType) => {
          return {
              text: item.Type,
              value: item.Type,
              Id: item.Id
          }
      }
    };

    if(this.userId == undefined){
      this.dataSourceActions = new DataSource({
        store: this.actionService.getStore(),
        expand: [
          "SolutionNavigation($select=Application)",
          "TypeNavigation",
          "ResultNavigation",
          "StatusNavigation",
        ],
      });
    }
    else{
      this.dataSourceActions = new DataSource({
        store: this.actionService.getStore(),
        filter: ["CreatedBy","=",this.userId],
        expand: [
          "SolutionNavigation($select=Application)",
          "TypeNavigation",
          "ResultNavigation",
          "StatusNavigation",
        ],
      });
    }
  }

  public onClickActionInfo(rowAction: HistoryGridRowEvent): void {
    this.selectedActionId = rowAction.row.data.Id;
    this.isActionDetailPopupVisible = true;
  }

  public onClickSolutionInfo(rowSolution: HistoryGridRowEvent): void {
    this.solutionService
      .getStore()
      .load({
        expand: [
          "ApplicationNavigation",
          "CreatedByNavigation",
          "ModifiedByNavigation",
        ],
        filter: "Id eq " + rowSolution.row.data.Solution,
      })
      .then((d) => {
        this.selectedSolution = d[0];
        this.selectedApplication = d[0].ApplicationNavigation;
        this.isSolutionDetailPopupVisible = true;
      });
  }

  public onToolbarPreparingDataGrid(e: ToolbarPreparingEvent<Action, number>): void{
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
  }

  public onClickRefresh(): void {
    this.dataGrid.instance.refresh();
  }

  public calculateDuration(rowData: Action): string {
    return this.actionService.getDurationString(rowData);
  }

  public createdByFullName(user: User): string {
    return (
      user.Firstname +
      " " +
      user.Lastname
    );
  }
}
