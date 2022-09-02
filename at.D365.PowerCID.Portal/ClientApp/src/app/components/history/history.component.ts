import { Component, Input, OnInit, ViewChild } from "@angular/core";
import { DxDataGridComponent } from "devextreme-angular";
import DataSource from "devextreme/data/data_source";
import ODataStore from "devextreme/data/odata/store";
import { TimeHelper } from "src/app/shared/helper/time.helper";
import { Action } from "src/app/shared/models/action.model";
import { Application } from "src/app/shared/models/application.model";
import { Solution } from "src/app/shared/models/solution.model";
import { User } from "src/app/shared/models/user.model";
import { ActionService } from "src/app/shared/services/action.service";
import { ApplicationService } from "src/app/shared/services/application.service";
import { EnvironmentService } from "src/app/shared/services/environment.service";
import { SolutionService } from "src/app/shared/services/solution.service";
import { UserService } from "src/app/shared/services/user.service";

@Component({
  selector: "app-history",
  templateUrl: "./history.component.html",
  styleUrls: ["./history.component.css"],
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

  @Input() userId: number;

  constructor(
    private actionService: ActionService,
    private solutionService: SolutionService,
    private applicationService: ApplicationService,
    private environmentService: EnvironmentService,
    private userService: UserService
  ) {
    this.calculateDuration = this.calculateDuration.bind(this);
  }
  
  public ngOnInit(): void {  
    this.dataStoreApplications = this.applicationService.getStore();
    this.dataStoreEnvironments = this.environmentService.getStore();
    this.dataStoreSolutions = this.solutionService.getStore();
    this.dataStoreUsers = this.userService.getStore();

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

  public onClickActionInfo(rowAction: any): void {
    this.selectedActionId = rowAction.row.data.Id;
    this.isActionDetailPopupVisible = true;
  }

  public onClickSolutionInfo(rowSolution: any): any {
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

  public onToolbarPreparingDataGrid(e: any): void{
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
