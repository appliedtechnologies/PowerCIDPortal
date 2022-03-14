import { Component, OnInit, ViewChild } from "@angular/core";
import DataSource from "devextreme/data/data_source";
import { UserService } from "src/app/shared/services/user.service";
import { Approle } from "../../shared/models/approle.model";
import {
  LayoutParameter,
  LayoutService,
  NotificationType,
} from "src/app/shared/services/layout.service";
import { AppConfig } from "src/app/shared/config/app.config";
import { User } from "src/app/shared/models/user.model";
import { EnvironmentService } from "src/app/shared/services/environment.service";
import { Environment } from "src/app/shared/models/environment.model";
import { environment } from "src/environments/environment";
import { UserEnvironmentService } from "src/app/shared/services/userenvironment.service";
import { UserEnvironment } from "src/app/shared/models/userenvironment.model";
import { DxListComponent, DxPopupComponent } from "devextreme-angular";

@Component({
  selector: "app-user",
  templateUrl: "./user.component.html",
  styleUrls: ["./user.component.css"],
})
export class UserComponent {
  @ViewChild(DxPopupComponent, { static: false })
  popupPermissionEnvironments: DxPopupComponent;
  dataSourceUsers: DataSource;
  dataSourceEnvironments: DataSource;
  isEditRoles: boolean;
  isEditPermissionsPopupVisible: boolean;
  selectedItemKeysPermissionEnvironments: number[];

  currentSelectedUser: User;
  isInitPermissionEnvironmentSelection: boolean;
  userRoles;
  appRoles;

  userAssignedAppRoles: Approle[] = [];
  userAssignedAppRolesBeforeChange: Approle[] = [];

  constructor(
    private userService: UserService,
    private layoutService: LayoutService,
    private environmentService: EnvironmentService,
    private userEnvironmentService: UserEnvironmentService
  ) {
    this.dataSourceUsers = new DataSource({
      store: this.userService.getStore(),
      expand: "TenantNavigation",
    });

    this.dataSourceEnvironments = new DataSource({
      store: this.environmentService.getStore(),
      paginate: false,
      sort: "Name",
    });

    this.onClickEditRoles = this.onClickEditRoles.bind(this);
    this.onClickEditPermissions = this.onClickEditPermissions.bind(this);
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
    this.dataSourceUsers.reload();
    this.dataSourceEnvironments.reload();
  }

  public onContentReadyPermissionEnvironmentList(e: any): void {
    this.popupPermissionEnvironments?.instance.repaint();
  }

  public onSelectionChangedPermissionEnvironments(e: any): void {
    if (!this.isInitPermissionEnvironmentSelection) {
      (e.addedItems as Array<Environment>).forEach((environment) => {
        this.userEnvironmentService
          .addEnvironmentPermission(this.currentSelectedUser.Id, environment.Id)
          .then(() => this.layoutService.notify({type: NotificationType.Success, message: "Changes have been saved", displayTime: 1000}));
      });
      (e.removedItems as Array<Environment>).forEach((environment) => {
        this.userEnvironmentService
          .removeEnvironmentPermission(
            this.currentSelectedUser.Id,
            environment.Id
          )
          .then(() => this.layoutService.notify({type: NotificationType.Success, message: "Changes have been saved", displayTime: 1000}));
      });
    } else this.isInitPermissionEnvironmentSelection = false;
  }

  public onClickEditPermissions(e: any): void {
    this.currentSelectedUser = e.row.data;

    this.layoutService.change(LayoutParameter.ShowLoading, true);
    this.userEnvironmentService
      .getStore()
      .load({
        filter: ["User", "=", this.currentSelectedUser.Id],
      })
      .then((userEnvironments: UserEnvironment[]) => {
        if (
          this.selectedItemKeysPermissionEnvironments === undefined &&
          userEnvironments.length > 0
        )
          this.isInitPermissionEnvironmentSelection = true;
        this.selectedItemKeysPermissionEnvironments = userEnvironments.map(
          (e) => e.Environment
        );
        this.isEditPermissionsPopupVisible = true;
        this.layoutService.change(LayoutParameter.ShowLoading, false);
      });
  }

  public onClickEditRoles(e: any) {
    this.userAssignedAppRoles = [];
    this.userAssignedAppRolesBeforeChange = [];
    this.currentSelectedUser = e.row.data;
    this.layoutService.change(LayoutParameter.ShowLoading, true);

    this.storeAppRoles(this.currentSelectedUser.Id).then(() =>
      this.storeUserAssignedRoles(this.currentSelectedUser.Id).then(() => {
        this.storeUserAssignedAppRoles(e.row.data.MsId._value);
        this.setIsAssigned();
        this.userAssignedAppRolesBeforeChange = JSON.parse(
          JSON.stringify(this.userAssignedAppRoles)
        );

        this.isEditRoles = true;
        this.layoutService.change(LayoutParameter.ShowLoading, false);
      })
    );
  }

  public onClickSaveRoles(): void {
    for (let index = 0; index < this.userAssignedAppRoles.length; index++) {
      if (
        this.userAssignedAppRoles[index].IsAssigned !=
        this.userAssignedAppRolesBeforeChange[index].IsAssigned
      ) {
        if (this.userAssignedAppRoles[index].IsAssigned) {
          //if changed to true http post
          this.userService
            .assignRole(
              this.currentSelectedUser.Id,
              this.userAssignedAppRoles[index].PrincipalId,
              this.userAssignedAppRoles[index].Id
            )
            .then(() => {
              this.layoutService.notify({
                type: NotificationType.Success,
                message: "Roles successfully changed",
              });
            })
            .catch(() => {
              this.layoutService.notify({
                type: NotificationType.Error,
                message: "An error occurred",
              });
            });
        } else {
          //else http delete
          this.userService
            .removeAssignedRole(
              this.currentSelectedUser.Id,
              this.userAssignedAppRoles[index].AppRoleAssignmentId
            )
            .then(() => {
              this.layoutService.notify({
                type: NotificationType.Success,
                message: "Roles successfully changed",
              });
            })
            .catch(() => {
              this.layoutService.notify({
                type: NotificationType.Error,
                message: "An error occurred",
              });
            });
        }
      }
    }
    this.isEditRoles = false;
  }

  private storeAppRoles(userId): Promise<void> {
    return new Promise<void>(
      (resolve, reject) =>
        (this.appRoles = this.userService.getAppRoles(userId).then((r) => {
          this.appRoles = r;
          resolve();
        }))
    );
  }

  private storeUserAssignedRoles(userId): Promise<void> {
    return new Promise<void>((resolve, reject) =>
      this.userService.getUserRoles(userId).then((r) => {
        this.userRoles = r;
        resolve();
      })
    );
  }

  private storeUserAssignedAppRoles(principalId): void {
    if (this.userAssignedAppRoles.length == 0) {
      this.appRoles.forEach((element) => {
        this.userAssignedAppRoles.push({
          Id: element.id,
          Name: element.displayName,
          IsAssigned: false,
          AppRoleAssignmentId: this.userRoles.filter(
            (ur) => element.id === ur.appRoleId
          )[0]?.id,
          PrincipalId: principalId,
        });
      });
    }
  }

  private setIsAssigned(): void {
    for (let index = 0; index < this.userRoles?.length; index++) {
      if (
        this.appRoles.filter((ar) => ar.id === this.userRoles[index].appRoleId)
          .length > 0
      ) {
        this.userAssignedAppRoles.filter(
          (r) => r.Id === this.userRoles[index].appRoleId
        )[0].IsAssigned = true;
      }
    }
  }
}
