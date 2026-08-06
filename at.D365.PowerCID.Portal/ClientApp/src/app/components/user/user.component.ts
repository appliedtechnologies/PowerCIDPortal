import { Component, ViewChild, ChangeDetectionStrategy } from "@angular/core";
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
import { UserEnvironmentService } from "src/app/shared/services/userenvironment.service";
import { UserEnvironment } from "src/app/shared/models/userenvironment.model";
import { DxDataGridComponent, DxPopupComponent } from "devextreme-angular";
import { AppRoleAssignment } from "src/app/shared/models/approleassignment.model";
import { confirm } from 'devextreme/ui/dialog';
import { ToolbarPreparingEvent } from "devextreme/ui/data_grid";
import { ContentReadyEvent, SelectionChangedEvent } from "devextreme/ui/list";
import { ValueChangedEvent } from "devextreme/ui/check_box";
import { IAppConfig } from "src/app/shared/config/app-config.model";
import { ExternalEnvironmentService } from "src/app/shared/services/externalenvironment.service";
import { UserExternalTenantService } from "src/app/shared/services/userexternaltenant.service";
import { ExternalEnvironment } from "src/app/shared/models/externalenvironment.model";
import { UserExternalTenant } from "src/app/shared/models/userexternaltenant.model";

interface UserGridRowEvent {
  row: {
    data: User;
  };
}

interface ExternalTenantOption {
  Id: number;
  Name: string;
}

@Component({
    selector: "app-user",
    templateUrl: "./user.component.html",
    styleUrls: ["./user.component.css"],
    changeDetection: ChangeDetectionStrategy.Eager,
    standalone: false
})
export class UserComponent {
  @ViewChild(DxPopupComponent, { static: false }) popupPermissionEnvironments: DxPopupComponent;
  @ViewChild(DxDataGridComponent, { static: false }) dataGrid: DxDataGridComponent;

  public appRoleNames: IAppConfig["azure"]["appRoleNames"] = AppConfig.settings.azure.appRoleNames;
  public dataSourceUsers: DataSource;
  public dataSourceEnvironments: DataSource;
  public isEditRolesVisible: boolean;
  public isEditPermissionsPopupVisible: boolean;
  public selectedItemKeysPermissionEnvironments: number[];
  public currentSelectedUser: User;
  public currentSelectedUserRoles: AppRoleAssignment[];

  public isEditExternalPermissionsPopupVisible: boolean;
  public selectedItemKeysPermissionExternalTenants: number[];
  public dataSourceExternalTenants: ExternalTenantOption[] = [];

  private isInitPermissionEnvironmentSelection: boolean;
  private isInitPermissionExternalTenantSelection: boolean;

  constructor(
    public userService: UserService,
    private layoutService: LayoutService,
    private environmentService: EnvironmentService,
    private userEnvironmentService: UserEnvironmentService,
    private externalEnvironmentService: ExternalEnvironmentService,
    private userExternalTenantService: UserExternalTenantService
  ) {
    this.dataSourceUsers = new DataSource({
      store: this.userService.getStore(),
      filter: ["IsDeactive", "=", false],
      expand: "TenantNavigation",
    });

    this.dataSourceEnvironments = new DataSource({
      store: this.environmentService.getStore(),
      filter: ["IsDeactive", "=", false],
      paginate: false,
      sort: "Name",
    });

    this.onClickEditRoles = this.onClickEditRoles.bind(this);
    this.onClickEditPermissions = this.onClickEditPermissions.bind(this);
    this.onClickEditExternalPermissions = this.onClickEditExternalPermissions.bind(this);
    this.onClickDeactivateUser = this.onClickDeactivateUser.bind(this);
  }
  
  public onToolbarPreparingDataGrid(e: ToolbarPreparingEvent<User, number>): void{
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

  public onContentReadyPermissionEnvironmentList(e: ContentReadyEvent<Environment, number>): void {
    void e;
    this.popupPermissionEnvironments?.instance.repaint();
  }

  public onClickDeactivateUser(e: UserGridRowEvent){
    const result = confirm("Are you sure you want to disable this user?<br /> This will NOT delete potential ownership in Azure Entra ID.", "Confirm Deactivation");
    result.then((dialogResult) => {
      if (dialogResult) {
        this.layoutService.change(LayoutParameter.ShowLoading, true);
        this.userService.delete(e.row.data.Id)
          .then(() => {
            this.layoutService.notify({
              type: NotificationType.Success,
              message: "User was successfully disabled.",
            });
          })
          .catch(() => {
            this.layoutService.notify({
              type: NotificationType.Error,
              message: "An error occurred while disabling the user.",
            });
          })
          .then(() => {
            this.layoutService.change(LayoutParameter.ShowLoading, false);
            this.dataGrid.instance.refresh();
          });
      }
    });
  }

  public onSelectionChangedPermissionEnvironments(e: SelectionChangedEvent<Environment, number>): void {
    if (!this.isInitPermissionEnvironmentSelection) {
      (e.addedItems as Environment[]).forEach((environment) => {
        this.userEnvironmentService
          .addEnvironmentPermission(this.currentSelectedUser.Id, environment.Id)
          .then(() => this.layoutService.notify({type: NotificationType.Success, message: "Changes have been saved", displayTime: 1000}));
      });
      (e.removedItems as Environment[]).forEach((environment) => {
        this.userEnvironmentService
          .removeEnvironmentPermission(
            this.currentSelectedUser.Id,
            environment.Id
          )
          .then(() => this.layoutService.notify({type: NotificationType.Success, message: "Changes have been saved", displayTime: 1000}));
      });
    } else this.isInitPermissionEnvironmentSelection = false;
  }

  public onClickEditPermissions(e: UserGridRowEvent): void {
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

  public onContentReadyPermissionExternalTenantList(e: ContentReadyEvent<ExternalTenantOption, number>): void {
    void e;
  }

  public onSelectionChangedPermissionExternalTenants(e: SelectionChangedEvent<ExternalTenantOption, number>): void {
    if (!this.isInitPermissionExternalTenantSelection) {
      (e.addedItems as ExternalTenantOption[]).forEach((tenant) => {
        this.userExternalTenantService
          .addTenantPermission(this.currentSelectedUser.Id, tenant.Id)
          .then(() => this.layoutService.notify({type: NotificationType.Success, message: "Changes have been saved", displayTime: 1000}));
      });
      (e.removedItems as ExternalTenantOption[]).forEach((tenant) => {
        this.userExternalTenantService
          .removeTenantPermission(this.currentSelectedUser.Id, tenant.Id)
          .then(() => this.layoutService.notify({type: NotificationType.Success, message: "Changes have been saved", displayTime: 1000}));
      });
    } else this.isInitPermissionExternalTenantSelection = false;
  }

  public onClickEditExternalPermissions(e: UserGridRowEvent): void {
    this.currentSelectedUser = e.row.data;

    this.layoutService.change(LayoutParameter.ShowLoading, true);

    const promiseExternalTenants = this.externalEnvironmentService
      .getStore()
      .load({ expand: ["EnvironmentNavigation.TenantNavigation"] })
      .then((externalEnvironments: ExternalEnvironment[]) => {
        const tenantsById = new Map<number, ExternalTenantOption>();
        externalEnvironments.forEach((externalEnvironment) => {
          const tenant = externalEnvironment.EnvironmentNavigation?.TenantNavigation;
          if (tenant && !tenantsById.has(tenant.Id))
            tenantsById.set(tenant.Id, { Id: tenant.Id, Name: tenant.Name });
        });
        this.dataSourceExternalTenants = Array.from(tenantsById.values());
      });

    const promiseUserExternalTenants = this.userExternalTenantService
      .getStore()
      .load({ filter: ["User", "=", this.currentSelectedUser.Id] })
      .then((userExternalTenants: UserExternalTenant[]) => {
        if (
          this.selectedItemKeysPermissionExternalTenants === undefined &&
          userExternalTenants.length > 0
        )
          this.isInitPermissionExternalTenantSelection = true;
        this.selectedItemKeysPermissionExternalTenants = userExternalTenants.map(
          (e) => e.Tenant
        );
      });

    Promise.all([promiseExternalTenants, promiseUserExternalTenants]).finally(() => {
      this.isEditExternalPermissionsPopupVisible = true;
      this.layoutService.change(LayoutParameter.ShowLoading, false);
    });
  }

  public onClickEditRoles(e: UserGridRowEvent) {
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

  public onClickSyncAdminRole(): void {
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

  public onValueChangedRoleAssignment(e: ValueChangedEvent, roleNameKey: string | number | symbol): void {
    this.layoutService.change(LayoutParameter.ShowLoading, true);
    const appRoleIds = AppConfig.settings.azure.appRoleIds as Record<string, string>;
    const appRoleId: string = appRoleIds[String(roleNameKey)];
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
