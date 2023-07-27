import { Component, OnInit, ViewChild } from "@angular/core";
import DataSource from "devextreme/data/data_source";
import { UserService } from "src/app/shared/services/user.service";
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
import { AppRoleAssignment } from "src/app/shared/models/approleassignment.model";

@Component({
  selector: "app-user",
  templateUrl: "./user.component.html",
  styleUrls: ["./user.component.css"],
})
export class UserComponent {
  @ViewChild(DxPopupComponent, { static: false }) popupPermissionEnvironments: DxPopupComponent;

  public appRoleNames: any = AppConfig.settings.azure.appRoleNames;
  public dataSourceUsers: DataSource;
  public dataSourceEnvironments: DataSource;
  public isEditRolesVisible: boolean;
  public isEditPermissionsPopupVisible: boolean;
  public selectedItemKeysPermissionEnvironments: number[];
  public currentSelectedUser: User;
  public currentSelectedUserRoles: AppRoleAssignment[];

  private isInitPermissionEnvironmentSelection: boolean;

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

    toolbarItems.unshift({
      widget: "dxButton",
      options: {
        icon: "download",
        stylingMode: "contained",
        type: "success",
        hint: "Sync Admin Role",
        text: "Sync Admin Role",
        onClick: this.onClickSyncAdminRole.bind(this),
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
    this.layoutService.change(LayoutParameter.ShowLoading, true);
    this.currentSelectedUser = e.row.data;

    this.userService.getUserRoles(this.currentSelectedUser.Id).then((appRoleAssignments) => {
      this.currentSelectedUserRoles = appRoleAssignments;

      this.isEditRolesVisible = true;
      this.layoutService.change(LayoutParameter.ShowLoading, false);
    }).catch(() => {
      this.layoutService.change(LayoutParameter.ShowLoading, false);
      this.layoutService.notify({
        type: NotificationType.Error,
        message: "An error occurred while querying the existing role assignment",
      });
    });;
  }

  public onClickSyncAdminRole(e: any): void {
    this.layoutService.change(LayoutParameter.ShowLoading, true);
    this.userService
      .syncAdminRole()
      .then(() => {
        this.layoutService.notify({
          type: NotificationType.Success,
          message: "Admin role was successfully synchronised",
        });
      })
      .catch(() => {
        this.layoutService.notify({
          type: NotificationType.Error,
          message: "An error occurred during synchronisation of admin role",
        });
      })
      .finally(() => {
        this.layoutService.change(LayoutParameter.ShowLoading, false);
      });
  }

  public onValueChangedRoleAssignment(e: any, roleNameKey: any): void {
    this.layoutService.change(LayoutParameter.ShowLoading, true);
    let appRoleId: string = AppConfig.settings.azure.appRoleIds[roleNameKey];
    if (e.value) {
      this.userService
        .assignRole(this.currentSelectedUser.Id, appRoleId)
        .then(() => {
          this.userService.getUserRoles(this.currentSelectedUser.Id).then((appRoleAssignments) => {
            this.currentSelectedUserRoles = appRoleAssignments;
            this.layoutService.notify({
              type: NotificationType.Success,
              message: "Role was assigned successfully",
            });
          });
        })
        .catch(() => {
          this.isEditRolesVisible = false;
          this.layoutService.notify({
            type: NotificationType.Error,
            message: "An error occurred during role assignment",
          });
        })
        .finally(() => {
          this.layoutService.change(LayoutParameter.ShowLoading, false);
        });
    } else {
      this.userService
        .removeAssignedRole(this.currentSelectedUser.Id, this.currentSelectedUserRoles.find(e => e.AppRoleId == appRoleId).Id)
        .then(() => {
          this.layoutService.notify({
            type: NotificationType.Success,
            message: "Role was successfully withdrawn",
          });
        })
        .catch(() => {
          this.isEditRolesVisible = false;
          this.layoutService.notify({
            type: NotificationType.Error,
            message: "An error occurred during role withdrawment",
          });
        })
        .finally(() => {
          this.layoutService.change(LayoutParameter.ShowLoading, false);
        });
    }
  }
}
