import { Component, ViewChild, ChangeDetectionStrategy } from "@angular/core";
import { DxDataGridComponent } from "devextreme-angular";
import DataSource from "devextreme/data/data_source";
import { confirm } from "devextreme/ui/dialog";
import { ExternalEnvironment } from "src/app/shared/models/externalenvironment.model";
import {
  ExternalEnvironmentService,
  RegistrableEnvironment,
} from "src/app/shared/services/externalenvironment.service";
import {
  LayoutParameter,
  LayoutService,
  NotificationType,
} from "src/app/shared/services/layout.service";

@Component({
    selector: "app-external-environment",
    templateUrl: "./external-environment.component.html",
    styleUrls: ["./external-environment.component.css"],
    changeDetection: ChangeDetectionStrategy.Eager,
    standalone: false
})
export class ExternalEnvironmentComponent {
  @ViewChild(DxDataGridComponent, { static: false }) dataGrid: DxDataGridComponent;

  public dataSourceExternalEnvironments: DataSource;
  public isRegisterPopupVisible = false;
  public registrableEnvironments: RegistrableEnvironment[] = [];
  public selectedRegistrableEnvironment: RegistrableEnvironment;
  public newAlias: string;

  constructor(
    private externalEnvironmentService: ExternalEnvironmentService,
    private layoutService: LayoutService
  ) {
    this.onClickDeactivate = this.onClickDeactivate.bind(this);
    this.onClickActivate = this.onClickActivate.bind(this);
    this.onClickDelete = this.onClickDelete.bind(this);
    this.isActive = this.isActive.bind(this);
    this.isDeactivated = this.isDeactivated.bind(this);

    this.dataSourceExternalEnvironments = new DataSource({
      store: this.externalEnvironmentService.getStore(),
      expand: ["EnvironmentNavigation.TenantNavigation"],
      sort: [{ selector: "Alias", desc: false }],
    });
  }

  public onToolbarPreparingDataGrid(e): void {
    const toolbarItems = e.toolbarOptions.items;

    toolbarItems.unshift({
      widget: "dxButton",
      options: {
        icon: "refresh",
        stylingMode: "contained",
        type: "success",
        hint: "Refresh",
        onClick: this.onClickRefresh.bind(this),
      },
      location: "after",
    });

    toolbarItems.unshift({
      widget: "dxButton",
      options: {
        icon: "add",
        text: "Register environment",
        stylingMode: "contained",
        type: "success",
        hint: "Register an existing environment of a foreign tenant for external delivery.",
        onClick: this.onClickOpenRegisterPopup.bind(this),
      },
      location: "after",
    });
  }

  public onClickRefresh(): void {
    this.dataGrid.instance.refresh();
  }

  public onClickOpenRegisterPopup(): void {
    this.newAlias = undefined;
    this.selectedRegistrableEnvironment = undefined;
    this.layoutService.change(LayoutParameter.ShowLoading, true);
    this.externalEnvironmentService
      .getRegistrableEnvironments()
      .then((environments) => {
        this.registrableEnvironments = environments;
        this.isRegisterPopupVisible = true;
      })
      .catch(() =>
        this.layoutService.notify({
          type: NotificationType.Error,
          message: "An error occurred while loading the registrable environments.",
        })
      )
      .finally(() => this.layoutService.change(LayoutParameter.ShowLoading, false));
  }

  public onValueChangedRegistrableEnvironment(e): void {
    this.selectedRegistrableEnvironment = e.value;
  }

  public onClickRegisterEnvironment(): void {
    if (!this.selectedRegistrableEnvironment) {
      this.layoutService.notify({
        type: NotificationType.Error,
        message: "Please select an environment to register.",
      });
      return;
    }

    this.layoutService.change(LayoutParameter.ShowLoading, true);
    this.externalEnvironmentService
      .add({ Environment: this.selectedRegistrableEnvironment.Id, Alias: this.newAlias })
      .then(() => {
        this.layoutService.notify({
          type: NotificationType.Success,
          message: "The environment was successfully registered for external delivery.",
        });
        this.isRegisterPopupVisible = false;
        this.dataGrid.instance.refresh();
      })
      .catch((error: Error) =>
        this.layoutService.notify({
          type: NotificationType.Error,
          message: error?.message
            ? `The environment could not be registered: ${error.message}`
            : "The environment could not be registered.",
        })
      )
      .finally(() => this.layoutService.change(LayoutParameter.ShowLoading, false));
  }

  public onClickDeactivate(e): void {
    const externalEnvironment = e.row.data as ExternalEnvironment;
    this.layoutService.change(LayoutParameter.ShowLoading, true);
    this.externalEnvironmentService
      .setDeactivated(externalEnvironment.Id, true)
      .then(() =>
        this.layoutService.notify({
          type: NotificationType.Success,
          message: "The environment was successfully deactivated.",
        })
      )
      .catch((error: Error) =>
        this.layoutService.notify({
          type: NotificationType.Error,
          message: error?.message ? `An error occurred: ${error.message}` : "An error occurred while deactivating the environment.",
        })
      )
      .finally(() => {
        this.layoutService.change(LayoutParameter.ShowLoading, false);
        this.dataGrid.instance.refresh();
      });
  }

  public onClickActivate(e): void {
    const externalEnvironment = e.row.data as ExternalEnvironment;
    this.layoutService.change(LayoutParameter.ShowLoading, true);
    this.externalEnvironmentService
      .setDeactivated(externalEnvironment.Id, false)
      .then(() =>
        this.layoutService.notify({
          type: NotificationType.Success,
          message: "The environment was successfully activated.",
        })
      )
      .catch(() =>
        this.layoutService.notify({
          type: NotificationType.Error,
          message: "An error occurred while activating the environment.",
        })
      )
      .finally(() => {
        this.layoutService.change(LayoutParameter.ShowLoading, false);
        this.dataGrid.instance.refresh();
      });
  }

  public onClickDelete(e): void {
    const externalEnvironment = e.row.data as ExternalEnvironment;
    const result = confirm(
      "Are you sure you want to remove this environment registration?<br /> This will NOT delete the environment itself.",
      "Confirm Removal"
    );
    result.then((dialogResult) => {
      if (dialogResult) {
        this.layoutService.change(LayoutParameter.ShowLoading, true);
        this.externalEnvironmentService
          .remove(externalEnvironment.Id)
          .then(() =>
            this.layoutService.notify({
              type: NotificationType.Success,
              message: "The environment registration was successfully removed.",
            })
          )
          .catch((error: Error) =>
            this.layoutService.notify({
              type: NotificationType.Error,
              message: error?.message
                ? `The registration could not be removed: ${error.message}`
                : "The registration could not be removed.",
            })
          )
          .finally(() => {
            this.layoutService.change(LayoutParameter.ShowLoading, false);
            this.dataGrid.instance.refresh();
          });
      }
    });
  }

  public isActive(e): boolean {
    return !e.row.data.IsDeactive;
  }

  public isDeactivated(e): boolean {
    return e.row.data.IsDeactive === true;
  }
}
