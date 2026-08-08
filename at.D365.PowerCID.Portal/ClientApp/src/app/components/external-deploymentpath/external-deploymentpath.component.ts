import { AfterViewInit, ChangeDetectorRef, Component, ChangeDetectionStrategy, NgZone, ViewChild } from "@angular/core";
import { DxTreeViewComponent } from "devextreme-angular";
import { confirm } from "devextreme/ui/dialog";
import dxTreeView from "devextreme/ui/tree_view";
import { ExternalDeploymentPath } from "src/app/shared/models/externaldeploymentpath.model";
import { ExternalDeploymentPathEnvironment } from "src/app/shared/models/externaldeploymentpathenvironment.model";
import { ExternalEnvironment } from "src/app/shared/models/externalenvironment.model";
import { ExternalDeploymentPathService } from "src/app/shared/services/externaldeploymentpath.service";
import { ExternalDeploymentPathEnvironmentService } from "src/app/shared/services/externaldeploymentpathenvironment.service";
import { ExternalEnvironmentService } from "src/app/shared/services/externalenvironment.service";
import {
  LayoutParameter,
  LayoutService,
  NotificationType,
} from "src/app/shared/services/layout.service";

type ExternalDeploymentPathTreeItem = ExternalDeploymentPath & ExternalDeploymentPathEnvironment;

@Component({
  selector: "app-external-deploymentpath",
  templateUrl: "./external-deploymentpath.component.html",
  styleUrls: ["./external-deploymentpath.component.css"],
  changeDetection: ChangeDetectionStrategy.Eager,
  standalone: false,
})
export class ExternalDeploymentpathComponent implements AfterViewInit {
  @ViewChild("treeViewExternalDeploymentPath") public treeViewExternalDeploymentPath: DxTreeViewComponent;
  @ViewChild("treeViewExternalEnvironment") public treeViewExternalEnvironment: DxTreeViewComponent;

  public dataSourceExternalEnvironments: ExternalEnvironment[] = [];
  public externalDeploymentPaths: ExternalDeploymentPath[] = [];
  public externalEnvironments: ExternalEnvironment[] = [];
  public externalTenants: { MsId: string; Name: string }[] = [];
  public selectedTenantMsId: string;

  public isAddDeploymentPathVisible = false;
  public isRenameDeploymentPathVisible = false;
  public deploymentPathToEdit: ExternalDeploymentPath = {};
  public newDeploymentPath: ExternalDeploymentPath = {};

  private allExternalDeploymentPaths: ExternalDeploymentPath[] = [];
  private allExternalEnvironments: ExternalEnvironment[] = [];

  constructor(
    private externalDeploymentPathService: ExternalDeploymentPathService,
    private externalDeploymentPathEnvironmentService: ExternalDeploymentPathEnvironmentService,
    private externalEnvironmentService: ExternalEnvironmentService,
    private layoutService: LayoutService,
    private changeDetectorRef: ChangeDetectorRef,
    private ngZone: NgZone
  ) {
    this.onAdd = this.onAdd.bind(this);
    this.onReorder = this.onReorder.bind(this);
    this.onDragChange = this.onDragChange.bind(this);
    this.onDragEnd = this.onDragEnd.bind(this);
    this.onClickDeleteDeploymentPathOrStep = this.onClickDeleteDeploymentPathOrStep.bind(this);
    this.onClickOpenRenameDeploymentPath = this.onClickOpenRenameDeploymentPath.bind(this);
    this.displayName = this.displayName.bind(this);


  }

  public ngAfterViewInit(): void {
    void this.loadData();
  }

  public onValueChangedTenant(e): void {
    this.selectedTenantMsId = e.value;
    this.applyTenantFilter();
  }

  public onClickOpenAddDeploymentPath(): void {
    this.newDeploymentPath = {};
    this.isAddDeploymentPathVisible = true;
  }

  public onClickSaveDeploymentPath(): void {
    this.layoutService.change(LayoutParameter.ShowLoading, true);
    this.externalDeploymentPathService
      .add(this.newDeploymentPath)
      .then(() =>
        this.layoutService.notify({
          type: NotificationType.Success,
          message: "The new external deployment path has been created successfully.",
        })
      )
      .catch((error: Error) =>
        this.layoutService.notify({
          type: NotificationType.Error,
          message: error?.message
            ? `The external deployment path could not be created: ${error.message}`
            : "The external deployment path could not be created.",
        })
      )
      .finally(() => {
        this.isAddDeploymentPathVisible = false;
        this.newDeploymentPath = {};
        this.loadData();
        this.layoutService.change(LayoutParameter.ShowLoading, false);
      });
  }

  public onClickOpenRenameDeploymentPath(e: unknown, entry: ExternalDeploymentPathTreeItem): void {
    void e;
    this.deploymentPathToEdit = { Id: entry.Id, Name: entry.Name };
    this.isRenameDeploymentPathVisible = true;
  }

  public onClickSaveRenameDeploymentPath(): void {
    this.layoutService.change(LayoutParameter.ShowLoading, true);
    this.externalDeploymentPathService
      .update(this.deploymentPathToEdit.Id, { Name: this.deploymentPathToEdit.Name })
      .then(() =>
        this.layoutService.notify({
          type: NotificationType.Success,
          message: "The name change has been saved successfully.",
        })
      )
      .catch((error: Error) =>
        this.layoutService.notify({
          type: NotificationType.Error,
          message: error?.message
            ? `The name change could not be saved: ${error.message}`
            : "The name change could not be saved.",
        })
      )
      .finally(() => {
        this.isRenameDeploymentPathVisible = false;
        this.deploymentPathToEdit = {};
        this.loadData();
        this.layoutService.change(LayoutParameter.ShowLoading, false);
      });
  }

  public onClickDeleteDeploymentPathOrStep(e: unknown, entry: ExternalDeploymentPathTreeItem): void {
    void e;
    const isStep = entry.ExternalEnvironmentNavigation !== undefined;
    const message = isStep
      ? `Would you like to remove the "${this.displayName(entry.ExternalEnvironmentNavigation)}" environment from the external deployment path?`
      : `Would you like to delete the external deployment path "${entry.Name}"?`;
    const result = confirm(message, isStep ? "Remove Environment" : "Delete External Deployment Path");

    result.then((dialogResult) => {
      if (!dialogResult) return;

      this.layoutService.change(LayoutParameter.ShowLoading, true);
      const operation = isStep
        ? this.externalDeploymentPathEnvironmentService.remove(entry.ExternalDeploymentPath, entry.ExternalEnvironment)
        : this.externalDeploymentPathService.remove(entry.Id);

      operation
        .then(() =>
          this.layoutService.notify({
            type: NotificationType.Success,
            message: isStep
              ? "The external deployment path step was successfully removed."
              : "The external deployment path was successfully deleted.",
          })
        )
        .catch((error: Error) =>
          this.layoutService.notify({
            type: NotificationType.Error,
            message: error?.message ? error.message : "The operation could not be completed.",
          })
        )
        .finally(() => {
          this.loadData();
          this.layoutService.change(LayoutParameter.ShowLoading, false);
        });
    });
  }

  public onAdd(e): void {
    if (e.fromData !== "externalenvironment" || e.toData !== "externaldeploymentpath") return;

    const fromTreeView = this.getTreeView(e.fromData);
    const toTreeView = this.getTreeView(e.toData);
    const fromNode = this.findNode(fromTreeView, e.fromIndex);
    const toNode = this.findNode(toTreeView, this.calculateToIndex(e));

    if (!fromNode || !toNode) return;

    const targetPath = toNode.itemData.ExternalEnvironmentNavigation
      ? toNode.parent?.itemData
      : toNode.itemData;
    const environment = fromNode.itemData as ExternalEnvironment;
    const path = targetPath as ExternalDeploymentPath;

    if (!path?.Id || !environment?.Id) return;

    const steps = path.ExternalDeploymentPathEnvironments || [];
    if (steps.some((step) => step.ExternalEnvironment === environment.Id)) {
      return;
    }

    this.externalDeploymentPathEnvironmentService
      .getStore()
      .insert({
        ExternalDeploymentPath: path.Id,
        ExternalEnvironment: environment.Id,
        StepNumber: steps.length + 1,
      })
      .then(() => {
        this.loadData();
        this.layoutService.notify({
          type: NotificationType.Success,
          message: "Changes have been saved",
          displayTime: 1000,
        });
      })
      .catch((error: Error) =>
        this.layoutService.notify({
          type: NotificationType.Error,
          message: error?.message ? error.message : "The environment could not be added as a step.",
        })
      );
  }

  public onReorder(e): void {
    if (e.fromData !== "externaldeploymentpath" || e.toData !== "externaldeploymentpath") return;
    if (e.fromComponent !== e.toComponent) return;

    const treeView = this.getTreeView(e.fromData);
    const fromNode = this.findNode(treeView, e.fromIndex);
    const toNode = this.findNode(treeView, this.calculateToIndex(e));

    if (!fromNode?.itemData.ExternalEnvironmentNavigation || !toNode?.itemData.ExternalEnvironmentNavigation) return;
    if (fromNode.parent?.itemData.Id !== toNode.parent?.itemData.Id) {
      this.layoutService.notify({
        type: NotificationType.Error,
        message: "It is not possible to move environments between external deployment paths.",
      });
      return;
    }

    const step = fromNode.itemData as ExternalDeploymentPathEnvironment;
    const fromIndex = step.StepNumber;
    const toIndex = (toNode.itemData as ExternalDeploymentPathEnvironment).StepNumber;

    this.externalDeploymentPathEnvironmentService
      .getStore()
      .update(
        { ExternalDeploymentPath: step.ExternalDeploymentPath, ExternalEnvironment: step.ExternalEnvironment },
        { ToIndex: toIndex, FromIndex: fromIndex }
      )
      .then(() => {
        this.loadData();
        this.layoutService.notify({
          type: NotificationType.Success,
          message: "Changes have been saved",
          displayTime: 1000,
        });
      })
      .catch((error: Error) =>
        this.layoutService.notify({
          type: NotificationType.Error,
          message: error?.message ? error.message : "The environment order could not be saved.",
        })
      );
  }

  public onDragChange(e): void {
    if (e.fromData === "externaldeploymentpath" && e.toData === "externaldeploymentpath") return;
  }

  public onDragEnd(e): void {
    if (e.fromData === e.toData && e.fromIndex === e.toIndex) return;
    void e;
  }

  public displayName(externalEnvironment: ExternalEnvironment): string {
    if (!externalEnvironment) return "";
    const loadedEnvironment = this.allExternalEnvironments.find(
      (environment) => environment.Id === externalEnvironment.Id
    );
    return (
      externalEnvironment.Alias ||
      externalEnvironment.EnvironmentNavigation?.Name ||
      loadedEnvironment?.Alias ||
      loadedEnvironment?.EnvironmentNavigation?.Name ||
      ""
    );
  }

  private loadData(): Promise<void> {
    return Promise.all([
      this.externalDeploymentPathService.getStore().load({
        sort: [{ selector: "Name", desc: false }],
        expand: ["ExternalDeploymentPathEnvironments.ExternalEnvironmentNavigation"],
      }),
      this.externalEnvironmentService.getStore().load({
        filter: ["IsDeactive", "=", false],
        expand: ["EnvironmentNavigation.TenantNavigation"],
        sort: [{ selector: "Alias", desc: false }],
      }),
    ]).then(([paths, environments]) => {
      this.allExternalDeploymentPaths = paths as ExternalDeploymentPath[];
      this.allExternalEnvironments = environments as ExternalEnvironment[];
      this.externalTenants = this.allExternalEnvironments
        .reduce((tenants, environment) => {
          const tenant = this.getTenant(environment);
          const tenantMsId = this.normalizeMsId(tenant?.MsId);
          if (tenantMsId && !tenants.some((item) => item.MsId === tenantMsId)) {
            tenants.push({ MsId: tenantMsId, Name: tenant.Name });
          }
          return tenants;
        }, [] as { MsId: string; Name: string }[]);
      if (!this.selectedTenantMsId && this.externalTenants.length > 0) {
        this.selectedTenantMsId = this.externalTenants[0].MsId;
      }
      if (this.selectedTenantMsId && !this.externalTenants.some((tenant) => tenant.MsId === this.selectedTenantMsId)) {
        this.selectedTenantMsId = undefined;
      }
      this.applyTenantFilter();
      this.ngZone.run(() => {
        this.changeDetectorRef.detectChanges();
        this.treeViewExternalDeploymentPath?.instance.option("dataSource", this.externalDeploymentPaths);
        this.treeViewExternalEnvironment?.instance.option("dataSource", this.dataSourceExternalEnvironments);
      });
    });
  }

  private applyTenantFilter(): void {
    this.externalDeploymentPaths = this.allExternalDeploymentPaths.filter((path) => {
      const steps = path.ExternalDeploymentPathEnvironments || [];
      return steps.length === 0 || steps.some(
        (step) => this.getExternalEnvironmentTenantMsId(step.ExternalEnvironment) === this.selectedTenantMsId
      );
    });

    this.dataSourceExternalEnvironments = this.allExternalEnvironments.filter(
      (environment) => this.getExternalEnvironmentTenantMsId(environment.Id) === this.selectedTenantMsId
    );
  }

  private getExternalEnvironmentTenantMsId(externalEnvironmentId: number): string | undefined {
    const environment = this.allExternalEnvironments.find((item) => item.Id === externalEnvironmentId);
    return this.normalizeMsId(this.getTenant(environment)?.MsId);
  }

  private getTenant(externalEnvironment: ExternalEnvironment): { MsId?: string | { _value?: string }; Name?: string } | undefined {
    const environmentNavigation =
      externalEnvironment?.EnvironmentNavigation ??
      (externalEnvironment as ExternalEnvironment & { environmentNavigation?: ExternalEnvironment["EnvironmentNavigation"] })
        .environmentNavigation;
    return (
      environmentNavigation?.TenantNavigation ??
      (environmentNavigation as typeof environmentNavigation & {
        tenantNavigation?: { MsId?: string; Name?: string };
      })?.tenantNavigation
    );
  }

  private normalizeMsId(msId: string | { _value?: string } | undefined): string | undefined {
    return typeof msId === "string" ? msId : msId?._value;
  }

  private getTreeView(name: string): dxTreeView {
    return name === "externaldeploymentpath"
      ? this.treeViewExternalDeploymentPath.instance
      : this.treeViewExternalEnvironment.instance;
  }

  private findNode(treeView: dxTreeView, index: number): any {
    const nodeElement = treeView.element().querySelectorAll(".dx-treeview-node")[index];
    if (!nodeElement) return null;
    return this.findNodeById(treeView.getNodes(), nodeElement.getAttribute("data-item-id"));
  }

  private findNodeById(nodes: any[], id: string | null): any {
    for (const node of nodes) {
      if (node.itemData?.Id == id) return node;
      if (node.children) {
        const found = this.findNodeById(node.children, id);
        if (found) return found;
      }
    }
    return null;
  }

  private calculateToIndex(e): number {
    if (e.fromComponent !== e.toComponent || e.dropInsideItem) return e.toIndex;
    return e.fromIndex >= e.toIndex ? e.toIndex : e.toIndex + 1;
  }
}
