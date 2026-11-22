import { AfterViewInit, ChangeDetectorRef, Component, ChangeDetectionStrategy, NgZone, ViewChild } from "@angular/core";
import { DxTreeViewComponent } from "devextreme-angular";
import { confirm } from "devextreme/ui/dialog";
import dxTreeView, { dxTreeViewNode } from "devextreme/ui/tree_view";
import { ExternalDeploymentPath } from "src/app/shared/models/externaldeploymentpath.model";
import { ExternalDeploymentPathEnvironment } from "src/app/shared/models/externaldeploymentpathenvironment.model";
import { ExternalEnvironment } from "src/app/shared/models/externalenvironment.model";
import { ExternalDeploymentPathService } from "src/app/shared/services/externaldeploymentpath.service";
import { ExternalDeploymentPathEnvironmentService } from "src/app/shared/services/externaldeploymentpathenvironment.service";
import { ExternalEnvironmentService } from "src/app/shared/services/externalenvironment.service";
import { ApplicationExternalDeploymentPathService } from "src/app/shared/services/applicationexternaldeploymentpath.service";
import {
  LayoutParameter,
  LayoutService,
  NotificationType,
} from "src/app/shared/services/layout.service";

type ExternalDeploymentPathTreeItem = ExternalDeploymentPath & ExternalDeploymentPathEnvironment;
type ExternalDeploymentPathTreeNode = dxTreeViewNode<ExternalDeploymentPathTreeItem, number>;

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
    private applicationExternalDeploymentPathService: ApplicationExternalDeploymentPathService,
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
    if (!isStep && (entry.ApplicationExternalDeploymentPaths?.length ?? 0) > 0) {
      const applicationName =
        entry.ApplicationExternalDeploymentPaths[0].ApplicationNavigation?.Name;
      this.layoutService.notify({
        type: NotificationType.Error,
        message: applicationName
          ? `The external deployment path is used by application '${applicationName}'.`
          : "The external deployment path cannot be deleted because it is assigned to an application.",
      });
      return;
    }
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

    const environment = fromNode.itemData as ExternalEnvironment;
    if (!environment?.Id) return;

    const isPathHeader = !toNode.itemData.ExternalEnvironmentNavigation;
    let path: ExternalDeploymentPath | undefined;
    let targetStepIndex: number;
    if (isPathHeader && !e.dropInsideItem) {
      const headerIndex = this.externalDeploymentPaths.findIndex(
        (deploymentPath) => deploymentPath.Id === toNode.itemData.Id
      );
      if (headerIndex < 1) {
        this.layoutService.notify({
          type: NotificationType.Error,
          message: "Drop the environment inside an external deployment path to add its first step.",
        });
        return;
      }

      path = this.externalDeploymentPaths[headerIndex - 1];
      targetStepIndex = path.ExternalDeploymentPathEnvironments?.length ?? 0;
    } else if (isPathHeader) {
      path = toNode.itemData as ExternalDeploymentPath;
      targetStepIndex = path.ExternalDeploymentPathEnvironments?.length ?? 0;
    } else {
      path = toNode.parent?.itemData as ExternalDeploymentPath | undefined;
      const steps = path?.ExternalDeploymentPathEnvironments ?? [];
      targetStepIndex = steps.findIndex(
        (step) => step.ExternalEnvironment === toNode.itemData.ExternalEnvironment
      );
    }

    if (!path?.Id || targetStepIndex < 0) {
      this.layoutService.notify({
        type: NotificationType.Error,
        message: "The destination position could not be determined.",
      });
      return;
    }

    const steps = path.ExternalDeploymentPathEnvironments || [];
    if (steps.some((step) => step.ExternalEnvironment === environment.Id)) {
      return;
    }

    const expectedOrder = steps.map((step) => step.ExternalEnvironment);
    this.externalDeploymentPathEnvironmentService
      .insertWithExpectedOrder(path.Id, environment.Id, targetStepIndex + 1, expectedOrder)
      .then(() => {
        return this.loadData().then(() =>
          this.layoutService.notify({
            type: NotificationType.Success,
            message: "Changes have been saved",
            displayTime: 1000,
          })
        );
      })
      .catch((error: Error) => {
        this.layoutService.notify({
          type: NotificationType.Error,
          message: error?.message ? error.message : "The environment could not be added as a step.",
        });
        return this.loadData().catch(() =>
          this.layoutService.notify({
            type: NotificationType.Error,
            message: "Could not reload the external deployment path environment order.",
          })
        );
      });
  }

  public onReorder(e): void {
    if (e.fromData !== "externaldeploymentpath" || e.toData !== "externaldeploymentpath") return;
    if (e.fromComponent !== e.toComponent) return;

    const treeView = this.getTreeView(e.fromData);
    const fromNode = this.findNode(treeView, e.fromIndex);
    const toNode = this.findNode(treeView, this.calculateToIndex(e));

    if (!fromNode?.itemData.ExternalEnvironmentNavigation) return;

    const step = fromNode.itemData as ExternalDeploymentPathEnvironment;
    const path = fromNode.parent?.itemData as ExternalDeploymentPath | undefined;
    const assignments = path?.ExternalDeploymentPathEnvironments ?? [];
    const fromIndex = assignments.findIndex(
      (assignment) => assignment.ExternalEnvironment === step.ExternalEnvironment
    ) + 1;
    const sourcePathIndex = this.externalDeploymentPaths.findIndex(
      (deploymentPath) => deploymentPath.Id === step.ExternalDeploymentPath
    );
    if (!path || fromIndex < 1 || sourcePathIndex < 0) {
      this.layoutService.notify({
        type: NotificationType.Error,
        message: "The environment order could not be determined. Reload the external deployment paths and try again.",
      });
      return;
    }

    let toIndex: number;
    if (toNode === null) {
      if (sourcePathIndex !== this.externalDeploymentPaths.length - 1) {
        this.notifyEnvironmentMoveAcrossPaths();
        return;
      }
      toIndex = assignments.length;
    } else {
      const targetStep = toNode.itemData as ExternalDeploymentPathTreeItem;
      const targetPathId = targetStep.ExternalDeploymentPath ??
        (toNode.parent === null && targetStep.ExternalEnvironmentNavigation === undefined
          ? targetStep.Id
          : undefined);
      const targetPathIndex = this.externalDeploymentPaths.findIndex(
        (deploymentPath) => deploymentPath.Id === targetPathId
      );
      const isEndOfCurrentPath =
        !e.dropInsideItem &&
        toNode.parent === null &&
        sourcePathIndex + 1 === targetPathIndex;
      const isPathHeader = targetStep.ExternalEnvironmentNavigation === undefined;

      if (isPathHeader && !e.dropInsideItem && targetPathId === step.ExternalDeploymentPath) {
        this.layoutService.notify({
          type: NotificationType.Error,
          message: "Drop the environment inside its external deployment path or immediately before the next path.",
        });
        return;
      }

      if (targetPathId !== step.ExternalDeploymentPath && !isEndOfCurrentPath) {
        this.notifyEnvironmentMoveAcrossPaths();
        return;
      }

      if (isEndOfCurrentPath || (isPathHeader && e.dropInsideItem)) {
        toIndex = assignments.length;
      } else {
        const targetIndex = assignments.findIndex(
          (assignment) => assignment.ExternalEnvironment === targetStep.ExternalEnvironment
        ) + 1;
        if (targetIndex < 1) {
          this.layoutService.notify({
            type: NotificationType.Error,
            message: "The destination environment could not be determined. Reload the external deployment paths and try again.",
          });
          return;
        }
        toIndex = targetIndex > fromIndex ? targetIndex - 1 : targetIndex;
      }
    }

    if (toIndex < 1) {
      this.layoutService.notify({
        type: NotificationType.Error,
        message: "The environment order could not be determined. Reload the external deployment paths and try again.",
      });
      return;
    }

    this.externalDeploymentPathEnvironmentService
      .moveWithExpectedOrder(
        step.ExternalDeploymentPath,
        step.ExternalEnvironment,
        fromIndex,
        toIndex,
        assignments.map((assignment) => assignment.ExternalEnvironment)
      )
      .then(() => {
        return this.loadData().then(() =>
          this.layoutService.notify({
            type: NotificationType.Success,
            message: "Changes have been saved",
            displayTime: 1000,
          })
        );
      })
      .catch((error: Error) => {
        this.layoutService.notify({
          type: NotificationType.Error,
          message: error?.message ? error.message : "The environment order could not be saved.",
        });
        return this.loadData().catch(() =>
          this.layoutService.notify({
            type: NotificationType.Error,
            message: "Could not reload the external deployment path environment order.",
          })
        );
      });
  }

  private notifyEnvironmentMoveAcrossPaths(): void {
    this.layoutService.notify({
      type: NotificationType.Error,
      message: "It is not possible to move environments between external deployment paths.",
    });
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
      this.applicationExternalDeploymentPathService.getStore().load({
        expand: ["ApplicationNavigation"],
      }),
      this.externalEnvironmentService.getStore().load({
        filter: ["IsDeactive", "=", false],
        expand: ["EnvironmentNavigation.TenantNavigation"],
        sort: [{ selector: "Alias", desc: false }],
      }),
    ]).then(([paths, assignments, environments]) => {
      this.allExternalDeploymentPaths = paths as ExternalDeploymentPath[];
      for (const path of this.allExternalDeploymentPaths) {
        path.ExternalDeploymentPathEnvironments = (path.ExternalDeploymentPathEnvironments ?? [])
          .sort((a, b) =>
            (a.StepNumber ?? Number.MAX_SAFE_INTEGER) - (b.StepNumber ?? Number.MAX_SAFE_INTEGER) ||
            (a.ExternalEnvironment ?? 0) - (b.ExternalEnvironment ?? 0)
          );
        path.ApplicationExternalDeploymentPaths = (assignments as {
          ExternalDeploymentPath?: number;
        }[]).filter((assignment) => assignment.ExternalDeploymentPath === path.Id);
      }
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

  private findNode(treeView: dxTreeView<ExternalDeploymentPathTreeItem, number>, index: number): ExternalDeploymentPathTreeNode | null {
    const nodeElement = treeView.element().querySelectorAll(".dx-treeview-node")[index];
    if (!nodeElement) return null;
    return this.findNodeById(treeView.getNodes(), nodeElement.getAttribute("data-item-id"));
  }

  private findNodeById(nodes: ExternalDeploymentPathTreeNode[], id: string | null): ExternalDeploymentPathTreeNode | null {
    for (const node of nodes) {
      if (node.itemData?.Id?.toString() === id) return node;
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
