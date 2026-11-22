import { AfterViewInit, ChangeDetectorRef, Component, Input, OnChanges, OnDestroy, OnInit, SimpleChanges, ViewChild, ChangeDetectionStrategy } from "@angular/core";
import { DxDataGridComponent, DxSelectBoxComponent } from "devextreme-angular";
import DataSource from "devextreme/data/data_source";
import { Column } from "devextreme/ui/data_grid";
import dxSelectBox from "devextreme/ui/select_box";
import { ApplicationService } from "src/app/shared/services/application.service";
import { PatchService } from "src/app/shared/services/patch.service";
import { DeploymentNote, DeploymentOptions, SolutionService } from "src/app/shared/services/solution.service";
import { UserService } from "src/app/shared/services/user.service";
import { Patch } from "src/app/shared/models/patch.model";
import { Application } from "src/app/shared/models/application.model";
import { Environment } from "src/app/shared/models/environment.model";
import { EnvironmentService } from "src/app/shared/services/environment.service";
import { Action } from "src/app/shared/models/action.model";
import { Solution } from "src/app/shared/models/solution.model";
import { Upgrade } from "src/app/shared/models/upgrade.model";
import dxButton from "devextreme/ui/button";
import { ActionService } from "src/app/shared/services/action.service";
import {
  LayoutParameter,
  LayoutService,
  NotificationType,
} from "src/app/shared/services/layout.service";
import { DeploymentPath } from "src/app/shared/models/deploymentpath.model";
import { alert, confirm } from 'devextreme/ui/dialog';
import { InitializedEvent as ButtonInitializedEvent } from "devextreme/ui/button";
import { InitializedEvent as SelectBoxInitializedEvent } from "devextreme/ui/select_box";
import { ExternalEnvironmentService } from "src/app/shared/services/externalenvironment.service";
import { ExternalDeploymentPathEnvironmentService } from "src/app/shared/services/externaldeploymentpathenvironment.service";
import { ExternalDeploymentPath } from "src/app/shared/models/externaldeploymentpath.model";
import { ExternalDeploymentPathEnvironment } from "src/app/shared/models/externaldeploymentpathenvironment.model";
import { ExternalEnvironment } from "src/app/shared/models/externalenvironment.model";
import { ExternalDeploymentPathService } from "src/app/shared/services/externaldeploymentpath.service";
import { TenantService } from "src/app/shared/services/tenant.service";
import {
  DeploymentSettingsStatus,
  SolutionDeploymentSettingsService,
} from "src/app/shared/services/solution-deployment-settings.service";
import { UpgradeService } from "src/app/shared/services/upgrade.service";
import { isNewerUpgrade } from "./solution-version.helper";
import { DeploymentMode } from "src/app/shared/models/deployment-mode.model";

type SolutionRowData = Solution & {
  DefaultDeploymentMode?: DeploymentMode;
};

const ungroupedApplicationText = "Ungrouped";
const externalTenantStorageKey = "atPowerCIDPortal_ExternalDelivery_Tenant";
const externalGroupStorageKey = "atPowerCIDPortal_ExternalDelivery_Group";
const externalApplicationStorageKey = "atPowerCIDPortal_ExternalDelivery_Application";

interface ApplicationGroupOption {
  value: string | null;
  text: string;
}

interface SolutionCellInfo {
  data: SolutionRowData;
  column: {
    name: string;
    environmentData?: Environment;
  };
}

interface PendingDeployment {
  solution: SolutionRowData;
  targetEnvironmentId: number;
  targetEnvironmentName?: string;
  targetIsUnmanaged?: boolean;
  deploymentPathId?: number;
  externalEnvironmentId?: number;
  externalDeploymentPathId?: number;
  closeSelectionPopup?: boolean;
}

@Component({
    selector: "app-solutions-overview",
    templateUrl: "./solutions-overview.component.html",
    styleUrls: ["./solutions-overview.component.css"],
    changeDetection: ChangeDetectionStrategy.Eager,
    standalone: false
})
export class SolutionsOverviewComponent implements AfterViewInit, OnChanges, OnInit, OnDestroy {
  @Input() public externalMode = false;
  @ViewChild(DxDataGridComponent, { static: false })
  dataGrid: DxDataGridComponent;
  @ViewChild("externalSolutionsGrid", { static: false })
  externalSolutionsGrid: DxDataGridComponent;
  @ViewChild("externalGroupSelectBox", { static: false })
  externalGroupSelectBox: DxSelectBoxComponent;
  @ViewChild("externalApplicationSelectBox", { static: false })
  externalApplicationSelectBox: DxSelectBoxComponent;
  public applicationSelectBoxInstance: dxSelectBox;
  public autoRefreshHintButtonInstance: dxButton;
  public autoRefreshCancelButtonInstance: dxButton;
  public addPatchButtonInstance: dxButton;
  public addUpgradeButtonInstance: dxButton;
  public dataSourceSolutions: DataSource;
  public showSolutionsGrid = true;
  public applicationGroups: ApplicationGroupOption[] = [];
  public selectedApplicationGroup: string | null = null;
  public groupSelectBoxInstance: dxSelectBox;
  public selectionToolbarItems: unknown[] = [];
  public dataGridColumns: Column[] = [
    {
      caption: "Please select an application",
      name: "no_data",
    },
  ];
  public autoRefreshInterval: number;
  public actionId: number;
  public isActionDetailPopupVisible: boolean;
  public detailSolution: Solution;
  public isSolutionDetailPopupVisible: boolean;
  public isAddUpgrade: boolean;
  public solutionDetailPopupTitle: string;
  public selectedApplication: Application;
  public canImport: boolean;
  public previousEnvironmentName: string;
  public environments: Environment[];
  public isConfigureDeploymentPopupVisible: boolean;
  public configureDeploymentEnvironment: Environment;
  public externalTenants: { Id: number; Name: string; MsId: string }[] = [];
  public externalSelectionToolbarItems: unknown[] = [];
  public selectedExternalTenantId: number;
  public externalDataGridColumns: Column[] = [];
  public externalEnvironments: ExternalEnvironment[] = [];
  public isExternalDeployPopupVisible = false;
  public isDeploymentDialogVisible = false;
  public pendingDeployment: PendingDeployment;
  public selectedDeploymentMode = DeploymentMode.OneStepUpgrade;
  public overwriteUnmanagedCustomizations = true;
  public enableWorkflows = true;
  public availableDeploymentModeOptions: {
    value: DeploymentMode;
    text: string;
    description: string;
  }[] = [];
  public readonly deploymentModeOptions = [
    {
      value: DeploymentMode.OneStepUpgrade,
      text: "One-step upgrade",
      description:
        "Imports the solution and applies its upgrade in the same operation. Components removed from the new version are removed from the target.",
    },
    {
      value: DeploymentMode.TwoStepUpgrade,
      text: "Two-step upgrade",
      description:
        "Imports as a holding solution first. Start a separate Apply upgrade action later to promote it. Requires an existing managed solution and is not available for patches.",
    },
    {
      value: DeploymentMode.UpdateOnly,
      text: "Update only",
      description:
        "Imports updates without applying an upgrade. Components removed from the new version are retained in the target.",
    },
  ];
  public deploymentSettingsStatus: DeploymentSettingsStatus;
  public deploymentSettingsStatusError: string;
  public isDeploymentSettingsStatusLoading = false;
  private deploymentSettingsStatusRequestId = 0;
  public selectedExternalDeploymentPath: ExternalDeploymentPath;
  public selectedSolutionForExternalDeploy: Solution;
  public availableExternalDeploymentPaths: ExternalDeploymentPath[] = [];
  public availableExternalSteps: ExternalDeploymentPathEnvironment[] = [];
  public selectedExternalStep: ExternalDeploymentPathEnvironment;
  public externalApplications: Application[] = [];
  public externalApplicationOptions: Application[] = [];
  private externalDeploymentPaths: ExternalDeploymentPath[] = [];
  private externalDeliveryDataLoaded = false;
  private restoringExternalSelections = false;
  private latestUpgrade: Upgrade | null = null;
  private latestUpgradeLoadedForApplicationId: number | null = null;
  private latestUpgradeLoadRequestId = 0;

  private refreshAfterPopupClose = false;

  constructor(
    public userService: UserService,
    private applicationService: ApplicationService,
    private solutionService: SolutionService,
    private environmentService: EnvironmentService,
    private actionService: ActionService,
    private layoutService: LayoutService,
    private patchService: PatchService,
    private externalEnvironmentService: ExternalEnvironmentService,
    private externalDeploymentPathEnvironmentService: ExternalDeploymentPathEnvironmentService,
    private externalDeploymentPathService: ExternalDeploymentPathService,
    private tenantService: TenantService,
    private deploymentSettingsService: SolutionDeploymentSettingsService,
    private upgradeService: UpgradeService,
    private changeDetectorRef: ChangeDetectorRef
  ) {
    this.selectionToolbarItems = this.createSelectionToolbarItems();
    this.userService.stateChanged$.subscribe(() => {
      this.selectionToolbarItems = this.createSelectionToolbarItems();
    });
    if (!this.externalMode) {
      this.applicationService.getStore().load({
        filter: ["IsDeactive", "=", false],
        select: ["Group"],
        sort: "Group",
      }).then((applications: Application[]) => {
        this.applicationGroups = [
          { value: null, text: ungroupedApplicationText },
          ...[...new Set(
            applications.map((application) => application.Group).filter((group): group is string => !!group)
          )].map((group) => ({ value: group, text: group })),
        ];
        this.groupSelectBoxInstance?.option("items", this.applicationGroups);
      });
    }
    this.environmentService
      .getStore()
      .load()
      .then((d: Environment[]) => (this.environments = d));
  }

  public ngOnChanges(changes: SimpleChanges): void {
    this.initializeExternalDeliveryData(changes.externalMode?.currentValue === true);
  }

  public ngAfterViewInit(): void {
    this.initializeExternalDeliveryData(this.externalMode);
  }

  private initializeExternalDeliveryData(isExternalMode: boolean): void {
    if (isExternalMode && !this.externalDeliveryDataLoaded) {
      this.externalDeliveryDataLoaded = true;
      this.loadExternalDeliveryData();
    }
  }

  public ngOnInit(): void {
    if (this.externalMode) {
      return;
    }
    const selectedApplicationIdFromLocalStorage =
      parseInt(
        localStorage.getItem("atPowerCIDPortal_SolutionOverview_SelectedId")
      ) || null;
    if (selectedApplicationIdFromLocalStorage != null)
      this.setSelectedApplicationId(selectedApplicationIdFromLocalStorage);
  }

  private loadExternalDeliveryData(): void {
    Promise.all([
      this.externalEnvironmentService.getStore().load({
        filter: ["IsDeactive", "=", false],
        expand: ["EnvironmentNavigation.TenantNavigation"],
      }),
      this.applicationService.getStore().load({
        filter: ["IsDeactive", "=", false],
        expand: [
          "Solutions($filter=IsReleasedExternally eq true)",
          "ApplicationExternalDeploymentPaths.ExternalDeploymentPathNavigation",
        ],
      }),
      this.externalDeploymentPathService.getStore().load({
        expand: ["ExternalDeploymentPathEnvironments.ExternalEnvironmentNavigation"],
      }),
      this.tenantService.getStore().load(),
    ]).then(([environments, applications, paths, tenants]) => {
      this.externalEnvironments = environments as ExternalEnvironment[];
      const tenantById = new Map(
        (tenants as { Id: number; Name: string; MsId: string }[]).map((tenant) => [tenant.Id, tenant])
      );
      const externalTenantsById = new Map<number, { Id: number; Name: string; MsId: string }>();
      this.externalEnvironments.forEach((externalEnvironment) => {
        const environment = externalEnvironment.EnvironmentNavigation;
        const tenant = environment?.TenantNavigation;
        const tenantId = tenant?.Id ?? environment?.Tenant;
        if (tenantId === undefined || externalTenantsById.has(tenantId)) {
          return;
        }
        externalTenantsById.set(
          tenantId,
          tenantById.get(tenantId) ?? {
            Id: tenantId,
            Name: tenant?.Name ?? `Tenant ${tenantId}`,
            MsId: tenant?.MsId,
          }
        );
      });
      this.externalTenants = [...externalTenantsById.values()];
      this.externalApplications = (applications as Application[]).filter((a) =>
        a.Solutions?.some((s) => s.IsReleasedExternally)
      );
      this.externalApplicationOptions = this.getExternalApplications();
      this.applicationGroups = [
        { value: null, text: ungroupedApplicationText },
        ...[...new Set(this.externalApplications.map((a) => a.Group).filter((g): g is string => !!g))]
          .map((group) => ({ value: group, text: group })),
      ];
      this.externalDeploymentPaths = paths as ExternalDeploymentPath[];
      this.restoringExternalSelections = true;
      this.restoreExternalSelections();
      this.externalSelectionToolbarItems = this.createExternalSelectionToolbarItems();
      this.changeDetectorRef.detectChanges();
      setTimeout(() => {
        this.restoringExternalSelections = false;
      });
    });
  }

  public onExternalTenantChanged(tenantId: number): void {
    this.resetExternalGridPage();
    this.selectedExternalTenantId = tenantId;
    if (tenantId === undefined || tenantId === null) {
      localStorage.removeItem(externalTenantStorageKey);
    } else {
      localStorage.setItem(externalTenantStorageKey, String(tenantId));
    }
    this.externalApplicationOptions = this.getExternalApplications();
    this.externalDataGridColumns = this.selectedApplication
      ? this.createExternalDataGridColumns()
      : [];
    this.showSolutionsGrid = this.selectedApplication !== undefined;
    this.externalSelectionToolbarItems = this.createExternalSelectionToolbarItems();
  }

  public onExternalApplicationChanged(applicationId: number): void {
    if (this.restoringExternalSelections && (applicationId === undefined || applicationId === null)) {
      return;
    }
    this.resetExternalGridPage();
    this.selectedApplication = this.externalApplications.find((a) => a.Id === applicationId);
    if (applicationId !== undefined && applicationId !== null) {
      void this.loadLatestUpgrade(applicationId);
    }
    if (applicationId === undefined || applicationId === null) {
      localStorage.removeItem(externalApplicationStorageKey);
    } else {
      localStorage.setItem(externalApplicationStorageKey, String(applicationId));
    }
    this.externalDataGridColumns = this.createExternalDataGridColumns();
    this.dataSourceSolutions = new DataSource({
      store: this.solutionService.getStore(),
      filter: [["Application", "=", applicationId], "and", ["IsReleasedExternally", "=", true]],
      sort: [{ selector: "CreatedOn", desc: true }],
      expand: [
        "Actions.TargetEnvironmentNavigation($select=DeployUnmanaged, ConnectionsOwner)",
        "Actions.TypeNavigation",
        "Actions.StatusNavigation",
        "Actions.ResultNavigation",
        "Actions($orderby=StartTime desc;$select=Id, Type, Status, Result, TargetEnvironment, Solution, IsExternalDelivery, DeploymentMode)",
        "ApplicationNavigation",
      ],
    });
    this.showSolutionsGrid = true;
    this.externalSelectionToolbarItems = this.createExternalSelectionToolbarItems();
  }

  public onExternalGroupChanged(group: string | null): void {
    if (this.restoringExternalSelections) {
      return;
    }
    this.resetExternalGridPage();
    this.selectedApplicationGroup = group;
    if (group === null) {
      localStorage.removeItem(externalGroupStorageKey);
    } else {
      localStorage.setItem(externalGroupStorageKey, group);
    }
    this.selectedApplication = undefined;
    localStorage.removeItem(externalApplicationStorageKey);
    this.externalDataGridColumns = [];
    this.applicationSelectBoxInstance?.option("value", null);
    this.externalApplicationSelectBox?.instance.option("value", null);
    this.externalApplicationOptions = this.getExternalApplications();
    this.externalSelectionToolbarItems = this.createExternalSelectionToolbarItems();
  }

  public getExternalApplications(): Application[] {
    return this.externalApplications.filter((a) =>
      this.selectedApplicationGroup === null ||
      (a.Group ?? null) === this.selectedApplicationGroup
    );
  }

  private resetExternalGridPage(): void {
    if (this.externalSolutionsGrid) {
      this.externalSolutionsGrid.instance.pageIndex(0);
    } else {
      this.dataSourceSolutions?.pageIndex(0);
    }
  }

  private restoreExternalSelections(): void {
    const storedTenantId = Number(localStorage.getItem(externalTenantStorageKey));
    if (this.externalTenants.some((tenant) => tenant.Id === storedTenantId)) {
      this.selectedExternalTenantId = storedTenantId;
    } else {
      localStorage.removeItem(externalTenantStorageKey);
    }

    const storedGroup = localStorage.getItem(externalGroupStorageKey);
    if (
      storedGroup !== null &&
      this.applicationGroups.some((group) => group.value === storedGroup)
    ) {
      this.selectedApplicationGroup = storedGroup;
    } else {
      this.selectedApplicationGroup = null;
      if (storedGroup !== null) {
        localStorage.removeItem(externalGroupStorageKey);
      }
    }

    this.externalApplicationOptions = this.getExternalApplications();
    const storedApplicationId = Number(localStorage.getItem(externalApplicationStorageKey));
    const application = this.externalApplicationOptions.find(
      (candidate) => candidate.Id === storedApplicationId
    );
    if (application) {
      this.onExternalApplicationChanged(application.Id);
    } else {
      this.selectedApplication = undefined;
      localStorage.removeItem(externalApplicationStorageKey);
    }
  }

  private createExternalSelectionToolbarItems(): unknown[] {
    return [
      {
        widget: "dxSelectBox",
        location: "before",
        options: {
          placeholder: "Select external tenant",
          hint: "Select an external tenant.",
          items: this.externalTenants,
          displayExpr: "Name",
          valueExpr: "Id",
          value: this.selectedExternalTenantId,
          width: "240",
          onValueChanged: (e) => this.onExternalTenantChanged(e.value),
        },
      },
      {
        widget: "dxSelectBox",
        location: "before",
        options: {
          placeholder: "Select Group",
          hint: "Select an application group.",
          items: this.applicationGroups,
          displayExpr: "text",
          valueExpr: "value",
          value: this.selectedApplicationGroup,
          width: "220",
          onValueChanged: (e) => this.onExternalGroupChanged(e.value),
        },
      },
      {
        widget: "dxSelectBox",
        location: "before",
        options: {
          placeholder: "Select Application",
          hint: "Select an externally released application.",
          items: this.externalApplicationOptions,
          displayExpr: "Name",
          valueExpr: "Id",
          value: this.selectedApplication?.Id,
          disabled: !this.selectedExternalTenantId,
          width: "300",
          onValueChanged: (e) => this.onExternalApplicationChanged(e.value),
        },
      },
      {
        widget: "dxButton",
        location: "after",
        options: {
          icon: "assets/animations/loading.gif",
          text: "auto-refresh is active",
          stylingMode: "text",
          disabled: true,
          visible: false,
          onInitialized: (args: ButtonInitializedEvent) => {
            this.autoRefreshHintButtonInstance = args.component;
          },
        },
      },
      {
        widget: "dxButton",
        location: "after",
        options: {
          icon: "clear",
          stylingMode: "contained",
          type: "success",
          visible: false,
          onClick: this.onClickCancelAutoRefresh.bind(this),
          onInitialized: (args: ButtonInitializedEvent) => {
            this.autoRefreshCancelButtonInstance = args.component;
          },
        },
      },
      {
        widget: "dxButton",
        location: "after",
        options: {
          icon: "refresh",
          stylingMode: "contained",
          type: "success",
          hint: "Refresh the table.",
          onClick: this.onClickRefreshSolutionsGrid.bind(this),
        },
      },
    ];
  }

  private createExternalDataGridColumns(): Column[] {
    const columns: Column[] = [
      {
        caption: "Type",
        name: "Type",
        width: 90,
        allowReordering: false,
        cellTemplate: "externalTypeCellTemplate",
        calculateCellValue: (rowData: SolutionRowData) =>
         rowData.DefaultDeploymentMode === undefined ? "Patch" : "Upgrade",
        visibleIndex: 0,
      },
      {
        caption: "Version",
        name: "Version",
        dataField: "Version",
        allowReordering: false,
        cellTemplate: "externalVersionCellTemplate",
        visibleIndex: 1,
      },
      {
        caption: "Name",
        name: "Name",
        dataField: "Name",
        allowReordering: false,
        cellTemplate: "externalNameCellTemplate",
        visibleIndex: 2,
      },
      {
        dataField: "CreatedOn",
        sortOrder: "desc",
        visible: false,
      },
    ];
    const assignedPaths = (this.selectedApplication?.ApplicationExternalDeploymentPaths ?? [])
      .map((assignment) => ({
        path: this.externalDeploymentPaths.find((path) => path.Id === assignment.ExternalDeploymentPath) ??
          assignment.ExternalDeploymentPathNavigation,
        hierarchyNumber: assignment.HierarchieNumber,
      }))
      .filter((assignment) => assignment.path !== undefined)
      .sort((a, b) => {
        const hierarchyA = Number.isFinite(a.hierarchyNumber)
          ? a.hierarchyNumber
          : Number.MAX_SAFE_INTEGER;
        const hierarchyB = Number.isFinite(b.hierarchyNumber)
          ? b.hierarchyNumber
          : Number.MAX_SAFE_INTEGER;
        return hierarchyA - hierarchyB || (a.path.Id ?? 0) - (b.path.Id ?? 0);
      })
      .map((assignment) => assignment.path);
    const paths = assignedPaths.filter((path) =>
      !this.selectedExternalTenantId ||
      this.getExternalDeploymentPathTenantId(path) === this.selectedExternalTenantId
    );
    for (const path of paths) {
      const steps = path.ExternalDeploymentPathEnvironments ?? [];
      columns.push({
        caption: path.Name,
        name: `external-path-${path.Id}`,
        columns: steps.sort((a, b) => a.StepNumber - b.StepNumber).map((step) => {
          const environment = this.externalEnvironments.find((e) => e.Id === step.ExternalEnvironment) ?? step.ExternalEnvironmentNavigation;
          return {
            caption: environment?.Alias || environment?.EnvironmentNavigation?.Name,
            name: `${path.Id},${environment?.Id}`,
            environmentData: environment,
            headerCellTemplate: "externalEnvironmentHeaderCellTemplate",
            cellTemplate: "externalEnvironmentCellTemplate",
            allowSorting: false,
            allowFiltering: false,
          };
        }),
      });
    }
    return columns;
  }

  private getExternalDeploymentPathTenantId(path: ExternalDeploymentPath): number | undefined {
    const tenantIds = (path.ExternalDeploymentPathEnvironments ?? [])
      .map((step) => {
        const externalEnvironment = this.externalEnvironments.find((environment) =>
          environment.Id === step.ExternalEnvironment
        ) ?? step.ExternalEnvironmentNavigation;
        const environment = externalEnvironment?.EnvironmentNavigation;
        return environment?.TenantNavigation?.Id ?? environment?.Tenant;
      })
      .filter((tenantId): tenantId is number => tenantId !== undefined);
    return tenantIds[0] ?? path.Tenant;
  }

  public stepDisplayName = (step: ExternalDeploymentPathEnvironment): string => {
    const externalEnvironment =
      this.externalEnvironments.find((environment) => environment.Id === step.ExternalEnvironment) ??
      step.ExternalEnvironmentNavigation;
    return externalEnvironment?.Alias ||
      externalEnvironment?.EnvironmentNavigation?.Name ||
      `Step ${step.StepNumber ?? ""}`.trim();
  };

  public ngOnDestroy(): void {
    this.cancelAutoRefresh();
  }

  public onClickConfigureDeployment(environment: Environment) {
    this.configureDeploymentEnvironment = environment;
    this.isConfigureDeploymentPopupVisible = true;
  }

  public onClickSolutionInfo(cellInfo): void {
    this.detailSolution = cellInfo.data;
    this.solutionDetailPopupTitle = `Details: ${this.detailSolution.Name}`;
    this.isSolutionDetailPopupVisible = true;
  }

  public onClickActionInfo(lastAction: Action): void {
    this.actionId = lastAction.Id;
    this.isActionDetailPopupVisible = true;
  }

  public onClickDownloadSolution(e: Event, cellInfo: SolutionCellInfo, lastAction: Action) {
    const unmanaged = (e as MouseEvent | KeyboardEvent).ctrlKey === true;

    this.layoutService.notify({ message: "Downloading solution...", type: NotificationType.Info });

    const solutionType = "DefaultDeploymentMode" in cellInfo.data
      ? "Upgrade"
      : "Patch";

    const solutionVersion = cellInfo.data.Version;

    const fileName = `${this.selectedApplication.SolutionUniqueName}_${solutionType}_${solutionVersion
      .split(".")
      .join("-")}${unmanaged ? "_unmanaged" : "_managed"}.zip`;

    this.solutionService
      .getSolutionAsBase64String(lastAction.Solution, unmanaged)
      .then((base64String) =>
        this.downloadBase64File("text/plain", base64String["value"], fileName)
      );
  }

  downloadBase64File(contentType, base64Data, fileName) {
    const linkSource = `data:${contentType};base64,${base64Data}`;
    const downloadLink = document.createElement("a");
    downloadLink.href = linkSource;
    downloadLink.download = fileName;
    downloadLink.click();
  }

  public onClickDeploySolution(cellInfo, exportOnly = false, applyUpgradeOnly = false, enableFlowsOnly = false): void {
    const targetEnvironmentId = Number(cellInfo.column.name.split(",")[1]);

    if (exportOnly) {
      this.executeExport(cellInfo);
    } else if(applyUpgradeOnly) {
      this.layoutService.change(LayoutParameter.ShowLoading, true);
      this.startApplyUpgrade(cellInfo.data.Id, targetEnvironmentId)
        .then(() => {
          this.layoutService.change(LayoutParameter.ShowLoading, false);
        });
    } else if(enableFlowsOnly){
      this.layoutService.change(LayoutParameter.ShowLoading, true);
      this.startEnableFlows(cellInfo.data.Id, targetEnvironmentId)
        .then(() => {
          this.layoutService.change(LayoutParameter.ShowLoading, false);
        });
    } else {
      this.openDeploymentDialog({
        solution: cellInfo.data,
        targetEnvironmentId,
        targetEnvironmentName: cellInfo.column.environmentData?.Name,
        targetIsUnmanaged: cellInfo.column.environmentData?.DeployUnmanaged === true &&
          this.selectedApplication?.ForceManagedDeployment !== true,
        deploymentPathId: Number(cellInfo.column.name.split(",")[0]),
      });
    }
  }

  private openDeploymentDialog(deployment: PendingDeployment): void {
    this.pendingDeployment = deployment;
    const isUpgrade = "DefaultDeploymentMode" in deployment.solution;
    const canUseTwoStep = isUpgrade && deployment.targetIsUnmanaged !== true;
    this.availableDeploymentModeOptions = this.deploymentModeOptions.filter(
      (option) => option.value !== DeploymentMode.TwoStepUpgrade || canUseTwoStep
    );
    this.selectedDeploymentMode =
      deployment.solution.DefaultDeploymentMode ?? DeploymentMode.OneStepUpgrade;
    if (!this.availableDeploymentModeOptions.some(
      (option) => option.value === this.selectedDeploymentMode
    )) {
      this.selectedDeploymentMode = DeploymentMode.OneStepUpgrade;
    }
    this.overwriteUnmanagedCustomizations =
      deployment.solution.OverwriteUnmanagedCustomizations ?? true;
    this.enableWorkflows = deployment.solution.EnableWorkflows ?? true;
    this.deploymentSettingsStatus = undefined;
    this.deploymentSettingsStatusError = undefined;
    this.isDeploymentDialogVisible = true;
    void this.refreshDeploymentSettingsStatus();
  }

  public onCancelDeploymentDialog(): void {
    this.deploymentSettingsStatusRequestId++;
    this.pendingDeployment = undefined;
    this.deploymentSettingsStatus = undefined;
    this.deploymentSettingsStatusError = undefined;
    this.isDeploymentSettingsStatusLoading = false;
  }

  public get canConfirmDeployment(): boolean {
    return this.pendingDeployment != null &&
      !this.isDeploymentSettingsStatusLoading &&
      !this.deploymentSettingsStatusError &&
      this.deploymentSettingsStatus?.manifestStatus === "Ready";
  }

  public getMissingDeploymentSettingNames(status: DeploymentSettingsStatus): string {
    return status.missingSettings.map((setting) => setting.logicalName).join(", ");
  }

  public async refreshDeploymentSettingsStatus(): Promise<void> {
    const deployment = this.pendingDeployment;
    if (!deployment) {
      return;
    }

    const requestId = ++this.deploymentSettingsStatusRequestId;
    this.isDeploymentSettingsStatusLoading = true;
    this.deploymentSettingsStatus = undefined;
    this.deploymentSettingsStatusError = undefined;

    try {
      const status = await this.deploymentSettingsService.status(
        deployment.solution.Id,
        deployment.targetEnvironmentId
      );
      if (requestId === this.deploymentSettingsStatusRequestId &&
          this.pendingDeployment === deployment) {
        this.deploymentSettingsStatus = status;
      }
    } catch (error) {
      if (requestId === this.deploymentSettingsStatusRequestId &&
          this.pendingDeployment === deployment) {
        this.deploymentSettingsStatusError = this.getDeploymentErrorMessage(
          error,
          "Deployment settings could not be checked. The deployment was not started."
        );
      }
    } finally {
      if (requestId === this.deploymentSettingsStatusRequestId) {
        this.isDeploymentSettingsStatusLoading = false;
      }
    }
  }

  public async onConfirmDeployment(): Promise<void> {
    const deployment = this.pendingDeployment;
    if (!deployment) {
      this.layoutService.notify({
        type: NotificationType.Error,
        message: "No deployment is selected.",
      });
      return;
    }

    await this.refreshDeploymentSettingsStatus();
    if (this.pendingDeployment !== deployment || !this.canConfirmDeployment) {
      return;
    }

    const shouldContinue = await this.confirmDeploymentNotes(
      deployment.solution,
      deployment.targetEnvironmentId
    );
    if (!shouldContinue || this.pendingDeployment !== deployment) {
      return;
    }

    const options: DeploymentOptions = {
      deploymentMode: this.selectedDeploymentMode,
      overwriteUnmanagedCustomizations: this.overwriteUnmanagedCustomizations,
      enableWorkflows: this.enableWorkflows,
    };

    this.isDeploymentDialogVisible = false;
    this.layoutService.change(LayoutParameter.ShowLoading, true);
    try {
      if (deployment.externalEnvironmentId !== undefined &&
          deployment.externalDeploymentPathId !== undefined) {
        if (deployment.closeSelectionPopup) {
          this.isExternalDeployPopupVisible = false;
        }
        await this.startExternalDeployment(
          deployment.solution,
          deployment.externalEnvironmentId,
          deployment.externalDeploymentPathId,
          options
        );
      } else {
        await this.startImport(
          deployment.solution,
          deployment.targetEnvironmentId,
          deployment.deploymentPathId,
          options
        );
      }
    } catch (error) {
      this.layoutService.notify({
        type: NotificationType.Error,
        message: this.getDeploymentErrorMessage(
          error,
          "An error occurred while starting the deployment."
        ),
      });
    } finally {
      this.pendingDeployment = undefined;
      this.layoutService.change(LayoutParameter.ShowLoading, false);
    }
  }

  public executeExport(cellInfo) {
    this.layoutService.change(LayoutParameter.ShowLoading, true);
    this.solutionService
      .export(cellInfo.data.Id)
      .then((action) => {
        this.layoutService.notify({
          type: NotificationType.Success,
          message: "Export started...",
        });
        this.startAutoRefresh(action);
      })
      .catch((error) => {
        this.layoutService.notify({
          type: NotificationType.Error,
          message: this.getDeploymentErrorMessage(
            error,
            "An error occurred while starting an export."
          ),
        });
      })
      .then(() => {
        this.layoutService.change(LayoutParameter.ShowLoading, false);
      });
  }

  public onClickAddPatch(): void {
    this.solutionDetailPopupTitle = "Add Patch";
    this.isSolutionDetailPopupVisible = true;
  }

  public onClickAddUpgrade(): void {
    this.solutionDetailPopupTitle = "Add Upgrade";
    this.isAddUpgrade = true;
    this.isSolutionDetailPopupVisible = true;
  }

  public onClickCancelAutoRefresh(): void {
    this.cancelAutoRefresh();
    this.dataGrid.instance.repaint();
  }

  public onHiddenSolutionDetailPopup(e): void {
    void e;
    if(this.refreshAfterPopupClose)
      this.dataGrid.instance.refresh();

    this.resetSolutionDetailPopup();
  }

  public onSaveCompletedSolutionDetail(e): void {
    void e;
    this.isSolutionDetailPopupVisible = false;
    this.resetSolutionDetailPopup();
    if (this.selectedApplication?.Id != null) {
      void this.loadLatestUpgrade(this.selectedApplication.Id);
    }
    this.dataGrid.instance.refresh();
  }

  public onClickStartAutoRefresh(lastAction: Action): void {
    this.startAutoRefresh(lastAction);
  }

  public onClickCancelImport(lastAction: Action): void {
    const result = confirm("Are you sure you want to cancel this import?<br /> This will not affect any operations already in progress in the environment.", "Confirm Cancellation");
    result.then((dialogResult) => {
      if (dialogResult) {
        this.cancelAutoRefresh();
        this.layoutService.change(LayoutParameter.ShowLoading, true);
        this.actionService.cancelImport(lastAction.Id).then(() => {
          this.dataGrid.instance.refresh();
          this.layoutService.notify({ type: NotificationType.Success, message: "Import was canceled." });
        }).catch(() => {
          this.layoutService.notify({ type: NotificationType.Error, message: "Import could not be canceled." });
          this.startAutoRefresh(lastAction);
        }).finally(() =>{
          this.layoutService.change(LayoutParameter.ShowLoading, false);
        });
      }
    });
  }

  onClickDeletePatch(patch: Patch) {
    const result = confirm("Are you sure you want to delete this patch?<br /> This will also delete the patch in the development environment.", "Confirm Deletion");
    result.then((dialogResult) => {
      if (dialogResult) {
        this.layoutService.change(LayoutParameter.ShowLoading, true);
        this.patchService
              .delete(patch.Id)
              .then(() => {
                this.layoutService.notify({
                  type: NotificationType.Success,
                  message: "Patch was successfully deleted.",
                });
              })
              .catch(() => {
                this.layoutService.notify({
                  type: NotificationType.Error,
                  message: "An error occurred while deleting the patch.",
                });
              })
              .then(() => {
                this.layoutService.change(LayoutParameter.ShowLoading, false);
                this.dataGrid.instance.refresh();
              });
      }
    });
  }

  public getLastActionForEnvironment(cellInfo): Action {
    const allActionOfSolution: Action[] = cellInfo.data.Actions ?? [];
    const targetEnvironmentId =
      cellInfo.column.name.split(",").length > 1
        ? cellInfo.column.name.split(",")[1]
        : cellInfo.column.name;

    return this.getLatestAction(
      allActionOfSolution,
      (action) =>
        action.IsExternalDelivery !== true &&
        action.TargetEnvironment == targetEnvironmentId
    );
  }

  public hasSuccessfulImport(solution: Solution): boolean {
    return solution.Actions?.some((action) => action.Type === 2 && action.Result === 1) ?? false;
  }

  public canRenewExport(solution: Solution): boolean {
    if (this.hasSuccessfulImport(solution)) {
      return false;
    }

    if (solution.Application == null || this.latestUpgradeLoadedForApplicationId !== solution.Application) {
      return false;
    }

    return !this.latestUpgrade || !isNewerUpgrade(this.latestUpgrade, solution);
  }

  public getLastExternalAction(cellInfo): Action {
    const [, environmentIdValue] = cellInfo.column.name.split(",");
    const environmentId = Number(environmentIdValue);
    const targetEnvironmentId = this.externalEnvironments.find((e) => e.Id === environmentId)?.Environment;
    return this.getLatestAction(
      cellInfo.data.Actions ?? [],
      (action) =>
        action.IsExternalDelivery === true &&
        action.TargetEnvironment === targetEnvironmentId
    );
  }

  private getLatestAction(actions: Action[], matches: (action: Action) => boolean): Action {
    return actions
      .filter(matches)
      .sort((left, right) =>
        Date.parse(right.CreatedOn ?? "") - Date.parse(left.CreatedOn ?? "") ||
        (right.Id ?? 0) - (left.Id ?? 0)
      )[0];
  }

  public canDeployToExternalEnvironment(cellInfo): boolean {
    const predecessor = this.getExternalDeploymentPredecessor(cellInfo);
    if (!predecessor.required) {
      return true;
    }

    const previousEnvironment = predecessor.step &&
      (this.externalEnvironments.find((environment) =>
        environment.Id === predecessor.step.ExternalEnvironment
      ) ?? predecessor.step.ExternalEnvironmentNavigation);
    if (!previousEnvironment?.Environment) {
      return false;
    }

    return this.hasSuccessfulDeploymentOnEnvironment(
      cellInfo.data,
      previousEnvironment.Environment,
      true
    );
  }

  private hasSuccessfulDeploymentOnEnvironment(
    solution: SolutionRowData,
    environmentId: number,
    isExternalDelivery: boolean
  ): boolean {
    const actions: Action[] = solution.Actions ?? [];
    const matchesEnvironmentAndCategory = (action: Action): boolean =>
      action.TargetEnvironment === environmentId &&
      action.IsExternalDelivery === isExternalDelivery;
    const successfulImports = actions.filter((action) =>
      matchesEnvironmentAndCategory(action) &&
      action.Type === 2 &&
      action.Result === 1
    );

    return successfulImports.some((action) => {
      const deploymentMode = action.DeploymentMode ?? solution.DefaultDeploymentMode;
      if (deploymentMode !== DeploymentMode.TwoStepUpgrade) {
        return true;
      }

      return actions.some((applyAction) =>
        matchesEnvironmentAndCategory(applyAction) &&
        applyAction.Type === 3 &&
        applyAction.Result === 1
      );
    });
  }

  public getPreviousExternalEnvironmentName(cellInfo): string {
    const predecessor = this.getExternalDeploymentPredecessor(cellInfo);
    return predecessor.step
      ? this.stepDisplayName(predecessor.step)
      : "previous environment";
  }

  private getExternalDeploymentPredecessor(
    cellInfo
  ): { required: boolean; step?: ExternalDeploymentPathEnvironment } {
    const [pathIdValue, environmentIdValue] = cellInfo.column.name.split(",");
    const pathId = Number(pathIdValue);
    const environmentId = Number(environmentIdValue);
    const path = this.externalDeploymentPaths.find((deploymentPath) =>
      deploymentPath.Id === pathId
    );
    const steps = path?.ExternalDeploymentPathEnvironments ?? [];
    const currentStep = steps.find((step) => step.ExternalEnvironment === environmentId);
    const stepNumber = currentStep?.StepNumber;

    if (stepNumber === undefined || stepNumber <= 1) {
      return { required: false };
    }

    return {
      required: true,
      step: steps.find((step) => step.StepNumber === stepNumber - 1),
    };
  }

  public onClickExternalDeploy(cellInfo): void {
    if (!this.canDeployToExternalEnvironment(cellInfo)) {
      return;
    }

    const pathId = Number(cellInfo.column.name.split(",")[0]);
    const externalEnvironmentId = Number(cellInfo.column.name.split(",")[1]);
    const externalEnvironment = this.externalEnvironments.find((environment) => environment.Id === externalEnvironmentId);
    if (!externalEnvironment?.Environment) {
      this.layoutService.notify({
        type: NotificationType.Error,
        message: "The external environment could not be found.",
      });
      return;
    }

    this.openDeploymentDialog({
      solution: cellInfo.data,
      targetEnvironmentId: externalEnvironment.Environment,
      targetEnvironmentName: externalEnvironment.EnvironmentNavigation?.Name,
      targetIsUnmanaged: externalEnvironment.EnvironmentNavigation?.DeployUnmanaged === true &&
        cellInfo.data.ApplicationNavigation?.ForceManagedDeployment !== true,
      externalEnvironmentId,
      externalDeploymentPathId: pathId,
    });
  }

  private startExternalDeployment(
    solution: Solution,
    externalEnvironmentId: number,
    pathId: number,
    options: DeploymentOptions
  ): Promise<void> {
    const targetEnvironmentId = this.externalEnvironments.find(
      (environment) => environment.Id === externalEnvironmentId
    )?.Environment;
    if (targetEnvironmentId == null) {
      this.layoutService.notify({
        type: NotificationType.Error,
        message: "The external environment could not be found.",
      });
      return Promise.resolve();
    }

    return this.solutionService
      .externalDeploy(solution.Id, externalEnvironmentId, pathId, options)
      .then((action) => {
        this.dataSourceSolutions?.reload();
        this.layoutService.notify({ type: NotificationType.Success, message: "The deployment has been started." });
        this.startAutoRefresh(action);
      })
      .catch((error) => {
        this.layoutService.notify({
          type: NotificationType.Error,
          message: this.getDeploymentErrorMessage(error),
        });
      });
  }

  public onClickConfigureExternalDeployment(environment: ExternalEnvironment): void {
    this.configureDeploymentEnvironment = environment?.EnvironmentNavigation;
    this.isConfigureDeploymentPopupVisible = true;
  }

  public onClickStartExternalDeployment(): void {
    if (!this.selectedExternalStep || !this.selectedExternalDeploymentPath) {
      this.layoutService.notify({ type: NotificationType.Error, message: "Please select an external deployment path and environment." });
      return;
    }

    const externalEnvironmentId = this.selectedExternalStep.ExternalEnvironment;
    const targetEnvironmentId = this.externalEnvironments.find(
      (environment) => environment.Id === externalEnvironmentId
    )?.Environment;
    if (targetEnvironmentId == null) {
      this.layoutService.notify({ type: NotificationType.Error, message: "The external environment could not be found." });
      return;
    }

    this.openDeploymentDialog({
      solution: this.selectedSolutionForExternalDeploy as SolutionRowData,
      targetEnvironmentId,
      targetEnvironmentName: this.externalEnvironments.find(
        (environment) => environment.Id === externalEnvironmentId
      )?.EnvironmentNavigation?.Name,
      targetIsUnmanaged: this.externalEnvironments.find(
        (environment) => environment.Id === externalEnvironmentId
      )?.EnvironmentNavigation?.DeployUnmanaged === true &&
        this.selectedSolutionForExternalDeploy.ApplicationNavigation?.ForceManagedDeployment !== true,
      externalEnvironmentId,
      externalDeploymentPathId: this.selectedExternalDeploymentPath.Id,
      closeSelectionPopup: true,
    });
    this.isExternalDeployPopupVisible = false;
  }

  public canApplyTwoStepUpgrade(cellInfo, action: Action): boolean {
    const solution = cellInfo.data as SolutionRowData;
    return this.isTwoStepUpgradeAction(solution, action) &&
      action.Type === 2 &&
      action.Status === 3 &&
      action.Result === 1;
  }

  public canRetryApplyUpgrade(cellInfo, action: Action): boolean {
    return this.isTwoStepUpgradeAction(cellInfo.data, action) &&
      action.Type === 3 &&
      action.Status === 3 &&
      action.Result !== 1;
  }

  private isTwoStepUpgradeAction(solution: SolutionRowData, action: Action): boolean {
    const isUpgrade = "DefaultDeploymentMode" in solution;
    const isTwoStepUpgrade = action.DeploymentMode === DeploymentMode.TwoStepUpgrade ||
      (action.DeploymentMode == null &&
        solution.DefaultDeploymentMode === DeploymentMode.TwoStepUpgrade);
    return isUpgrade && isTwoStepUpgrade;
  }

  public onClickApplyUpgrade(cellInfo, lastAction: Action): void {
    this.layoutService.change(LayoutParameter.ShowLoading, true);
    const request = this.externalMode
      ? this.solutionService.externalApplyUpgrade(
          cellInfo.data.Id,
          Number(cellInfo.column.name.split(",")[1]),
          Number(cellInfo.column.name.split(",")[0])
        )
      : this.solutionService.applyUpgrade(
          cellInfo.data.Id,
          lastAction.TargetEnvironment ?? Number(cellInfo.column.name.split(",")[1])
        );

    request
      .then((action) => {
        this.layoutService.notify({
          type: NotificationType.Success,
          message: "Apply upgrade started...",
        });
        this.startAutoRefresh(action);
      })
      .catch((error) => {
        this.layoutService.notify({
          type: NotificationType.Error,
          message: this.getDeploymentErrorMessage(
            error,
            "An error occurred while starting an apply upgrade."
          ),
        });
      })
      .finally(() => this.layoutService.change(LayoutParameter.ShowLoading, false));
  }

  public onExternalDeploymentPathChanged(path: ExternalDeploymentPath): void {
    this.selectedExternalDeploymentPath = path;
    this.selectedExternalStep = undefined;
    this.availableExternalSteps = path?.ExternalDeploymentPathEnvironments ?? [];
  }

  private getDeploymentErrorMessage(error: unknown, fallbackMessage = "An error occurred while starting the deployment."): string {
    if (typeof error === "string") {
      return error;
    }

    if (!error || typeof error !== "object") {
      return fallbackMessage;
    }

    const response = error as { error?: unknown; message?: unknown };
    const responseError = response.error;
    if (typeof responseError === "string") {
      return responseError;
    }

    if (responseError && typeof responseError === "object") {
      const body = responseError as {
        value?: unknown;
        message?: unknown;
        error?: { message?: unknown } | string;
      };

      if (typeof body.value === "string") {
        return body.value;
      }
      if (typeof body.message === "string") {
        return body.message;
      }
      if (typeof body.error === "string") {
        return body.error;
      }
      if (body.error && typeof body.error === "object" && typeof body.error.message === "string") {
        return body.error.message;
      }
    }

    return typeof response.message === "string" ? response.message : fallbackMessage;
  }

  public canDeployToEnv(cellInfo): boolean {
    this.canImport = true;
    if (cellInfo.column.name.split(",").length > 1) {
      const deploymentPathId = cellInfo.column.name.split(",")[0];
      const environmentId = cellInfo.column.name.split(",")[1];

      const deploymentPath = this.selectedApplication.DeploymentPaths.find(
        (x) => x.Id == deploymentPathId
      );

      const stepNumber = deploymentPath?.DeploymentPathEnvironments.find(
        (x) =>
          x.DeploymentPath == deploymentPathId && x.Environment == environmentId
      ).StepNumber;

      let previousEnvironmentId;

      if (stepNumber > 1) {
        previousEnvironmentId = deploymentPath?.DeploymentPathEnvironments.find(
          (x) => x.StepNumber == stepNumber - 1
        ).Environment;

        this.previousEnvironmentName = this.environments.find(
          (x) => x.Id == previousEnvironmentId
        ).Name;

        this.canImport = this.hasSuccessfulDeploymentOnEnvironment(
          cellInfo.data,
          Number(previousEnvironmentId),
          false
        );
      }
    }
    return this.canImport;
  }

  public onClickOpenMakerPortal(cellInfo: SolutionCellInfo): void {
    window.open(cellInfo.data.UrlMakerportal, "_blank")
  }

  public onRenamedSolutionDetail(e: unknown): void{
    void e;
    this.refreshAfterPopupClose = true;
  }

  private startImport(
    solution: Solution,
    targetEnvironmentId: number,
    deploymentPathId: number,
    options: DeploymentOptions
  ): Promise<void> {
    return this.solutionService
      .deploy(solution.Id, targetEnvironmentId, deploymentPathId, options)
      .then((action) => {
        this.layoutService.notify({
          type: NotificationType.Success,
          message: "Import started...",
        });
        this.startAutoRefresh(action);
      })
      .catch((error) => {
        this.layoutService.notify({
          type: NotificationType.Error,
          message: this.getDeploymentErrorMessage(
            error,
            "An error occurred while starting an import."
          ),
        });
      });
  }

  private async confirmDeploymentNotes(solution: Solution, targetEnvironmentId: number): Promise<boolean> {
    let notes: DeploymentNote[];
    try {
      notes = await this.solutionService.getDeploymentNotes(solution.Id, targetEnvironmentId);
    } catch (error) {
      this.layoutService.notify({
        type: NotificationType.Error,
        message: this.getDeploymentErrorMessage(
          error,
          "Deployment notes could not be loaded. The deployment was not started."
        ),
      });
      return false;
    }

    if (notes.length === 0) {
      return true;
    }

    this.layoutService.change(LayoutParameter.ShowLoading, false);
    const shouldContinue = await confirm(
      this.formatDeploymentNotes(solution, notes),
      "Deployment Notes"
    );
    if (shouldContinue) {
      this.layoutService.change(LayoutParameter.ShowLoading, true);
    }
    return shouldContinue;
  }

  private formatDeploymentNotes(solution: Solution, notes: DeploymentNote[]): string {
    const escapeHtml = (value: string): string =>
      value
        .replace(/&/g, "&amp;")
        .replace(/</g, "&lt;")
        .replace(/>/g, "&gt;")
        .replace(/"/g, "&quot;")
        .replace(/'/g, "&#39;");
    const noteSections = notes.map((note) =>
      `<strong>${escapeHtml(note.solutionName)} ${escapeHtml(note.solutionVersion)}</strong><br>` +
      escapeHtml(note.deploymentNotes).replace(/\r\n|\r|\n/g, "<br>")
    ).join("<br><br>");

    return `Please review the deployment notes for ${escapeHtml(solution.Name ?? "")} ${escapeHtml(solution.Version ?? "")} and all versions not yet successfully imported to this environment:<br><br>${noteSections}<br><br>Continue with the deployment?`;
  }

  private startApplyUpgrade(solutionId: number, targetEnvironmentId): Promise<void> {
    return this.solutionService
      .applyUpgrade(solutionId, targetEnvironmentId)
      .then((action) => {
        this.layoutService.notify({
          type: NotificationType.Success,
          message: "Apply upgrade started...",
        });
        this.startAutoRefresh(action);
      })
      .catch((error) => {
        this.layoutService.notify({
          type: NotificationType.Error,
          message: this.getDeploymentErrorMessage(
            error,
            "An error occurred while starting an apply upgrade."
          ),
        });
      });
  }

  private startEnableFlows(solutionId: number, targetEnvironmentId): Promise<void> {
    return this.solutionService
      .enableFlows(solutionId, targetEnvironmentId)
      .then((action) => {
        this.layoutService.notify({
          type: NotificationType.Success,
          message: "Enable flows started...",
        });
        this.startAutoRefresh(action);
      })
      .catch((error) => {
        this.layoutService.notify({
          type: NotificationType.Error,
          message: this.getDeploymentErrorMessage(
            error,
            "An error occurred while starting enable flows."
          ),
        });
      });
  }

  private startAutoRefresh(action: Action) {
    let buffer = 1;
    this.cancelAutoRefresh();
    this.autoRefreshHintButtonInstance?.option("visible", true);
    this.autoRefreshCancelButtonInstance?.option("visible", true);
    this.autoRefreshInterval = window.setInterval(() => {
      this.dataGrid.instance.refresh().then(() => {
        const refreshedSolution = (
          this.dataGrid.instance.getDataSource().items() as Solution[]
        ).find((solution) => solution.Id === action.Solution);
        const refreshedLastAction = refreshedSolution?.Actions?.find(
          (refreshedAction) => refreshedAction.Id === action.Id
        );

        if (!refreshedLastAction) {
          return;
        }

        if (refreshedLastAction.Status == 3) buffer--;
        if (refreshedLastAction.Status == 3 && buffer <= 0) {
          if(refreshedLastAction.Type == 2 && refreshedLastAction.Result == 1 && this.selectedApplication.AfterDeploymentInformation)
            alert(this.selectedApplication.AfterDeploymentInformation, "After Deployment Information");
          this.cancelAutoRefresh();
        }
      });
    }, 2000);
  }

  private resetSolutionDetailPopup(): void {
    this.detailSolution = undefined;
    this.isAddUpgrade = false;
    this.refreshAfterPopupClose = false;
  }

  private cancelAutoRefresh(): void {
    window.clearInterval(this.autoRefreshInterval);
    this.autoRefreshInterval = null;
    this.autoRefreshHintButtonInstance?.option("visible", false);
    this.autoRefreshCancelButtonInstance?.option("visible", false);
  }

  private generateDataGridColumns(): void {
    if (this.selectedApplication == null) {
      this.setSelectedApplicationId(null);
      return;
    }
    const developmentEnvironment: Environment =
      this.selectedApplication.DevelopmentEnvironmentNavigation;

    const sortedDeploymentPaths: DeploymentPath[] =
      this.applicationService.sortAfterHierarchieAndStepNumber(
        this.selectedApplication,
        true
      );

    this.dataGridColumns = [
      {
        caption: "Type",
        name: "Type",
        width: 90,
        allowReordering: false,
        cellTemplate: "typeCellTemplate",
        calculateCellValue: (rowData: SolutionRowData) => {
         if (rowData.DefaultDeploymentMode === undefined) return "Patch";
          else return "Upgrade";
        },
        visibleIndex: 0,
      },
      {
        caption: "Version",
        name: "Version",
        dataField: "Version",
        allowReordering: false,
        cellTemplate: "versionCellTemplate",
        visibleIndex: 1,
      },
      {
        caption: "Name",
        name: "Name",
        dataField: "Name",
        allowReordering: false,
        cellTemplate: "nameCellTemplate",
        allowFiltering: true,
        visibleIndex: 2,
      },
      {
        dataField: "CreatedOn",
        sortOrder: "desc",
        visible: false,
      },
      {
        caption: "Development Environment",
        name: "devEnvironment",
        allowReordering: false,
        columns: [
          //dev environment
          {
            cellTemplate: "devEnvironmentCellTemplate",
            caption: developmentEnvironment.Name,
            name: developmentEnvironment.Id.toString(),
            allowSorting: false,
            allowReordering: false,
            allowFiltering: false,
          },
        ],
      },
      //other environments
      ...sortedDeploymentPaths.map((d) => {
        return {
          caption: d.Name,
          allowReordering: false,
          name: d.Id.toString() + d.Name,

          columns: [
            ...d.Environments.map((e) => {
              return {
                headerCellTemplate: "environmentHeaderCellTemplate",
                cellTemplate: "environmentCellTemplate",
                caption: e.Name,
                name: d.Id.toString() + "," + e.Id.toString(),
                environmentData: e,
                allowSorting: false,
                allowFiltering: false,
              };
            }),
          ],
        };
      }),
    ];
  }

  private onClickRefreshSolutionsGrid(): void {
    if (!this.dataGrid?.instance) {
      return;
    }

    if (this.selectedApplication?.Id != null) {
      void this.loadLatestUpgrade(this.selectedApplication.Id);
    }
    this.dataGrid.instance.refresh();
  }

  private async loadLatestUpgrade(applicationId: number): Promise<void> {
    const requestId = ++this.latestUpgradeLoadRequestId;
    this.latestUpgradeLoadedForApplicationId = null;
    this.latestUpgrade = null;

    try {
      const upgrades = await this.upgradeService.getStore().load({
        filter: ["Application", "=", applicationId],
        select: ["Id", "Application", "CreatedOn"],
        sort: [
          { selector: "CreatedOn", desc: true },
          { selector: "Id", desc: true },
        ],
        take: 1,
      }) as Upgrade[];

      if (requestId !== this.latestUpgradeLoadRequestId) {
        return;
      }

      this.latestUpgrade = upgrades[0] ?? null;
      this.latestUpgradeLoadedForApplicationId = applicationId;
    } catch {
      if (requestId !== this.latestUpgradeLoadRequestId) {
        return;
      }

      this.layoutService.notify({
        type: NotificationType.Error,
        message: "Could not check for newer upgrades. Export renewal remains hidden until the upgrade list is refreshed.",
      });
    }
  }

  private setSelectedApplicationId(id: number) {
    if (id != null && this.selectedApplication?.Id != id) {
      localStorage.setItem(
        "atPowerCIDPortal_SolutionOverview_SelectedId",
        id.toString()
      );

      this.applicationService.getApplicationById(id).then((application) => {
        this.selectedApplication = application;
        void this.loadLatestUpgrade(id);
        this.selectedApplicationGroup = this.selectedApplication.Group ?? null;
        this.applicationSelectBoxInstance?.option(
          "dataSource",
          this.createApplicationDataSource(this.selectedApplicationGroup)
        );
        this.groupSelectBoxInstance?.option(
          "value",
          this.selectedApplicationGroup
        );
        this.applicationSelectBoxInstance?.option(
          "value",
          this.selectedApplication.Id
        );

        this.addPatchButtonInstance?.option("disabled", false);
        this.addUpgradeButtonInstance?.option("disabled", false);

        this.generateDataGridColumns();
        this.dataSourceSolutions = new DataSource({
          store: this.solutionService.getStore(),
          filter: [["Application", "=", this.selectedApplication.Id]],
          expand: [
            "Actions.TargetEnvironmentNavigation($select=DeployUnmanaged, ConnectionsOwner)",
            "Actions.TypeNavigation",
            "Actions.StatusNavigation",
            "Actions.ResultNavigation",
            "Actions($orderby=StartTime desc;$select=Id, Type, Status, Result, TargetEnvironment, Solution, IsExternalDelivery, DeploymentMode)",
            "CreatedByNavigation($select=Firstname, Lastname)",
            "ModifiedByNavigation($select=Firstname, Lastname)",
          ],
        });
      });
    } else if (id == null){
      localStorage.removeItem("atPowerCIDPortal_SolutionOverview_SelectedId");
      this.clearSolutionsGrid();
      this.addPatchButtonInstance?.option("disabled", true);
      this.addUpgradeButtonInstance?.option("disabled", true);
    }
  }

  private createApplicationDataSource(group: string | null): DataSource {
      const filter = group == null
        ? [
          ["IsDeactive", "=", false],
          "and",
          [["Group", "=", null], "or", ["Group", "=", ""]],
        ]
        : [["IsDeactive", "=", false], "and", ["Group", "=", group]];

      return new DataSource({
        store: this.applicationService.getStore(),
        sort: [
          { selector: "OrdinalNumber", desc: false },
          { selector: "Name", desc: false },
        ],
        filter,
      });
    }

    private createSelectionToolbarItems(): unknown[] {
      const items: unknown[] = [
        {
          widget: "dxSelectBox",
          location: "before",
          options: {
            placeholder: "Select Group",
            hint: "Select an application group.",
            items: this.applicationGroups,
            displayExpr: "text",
            valueExpr: "value",
            value: this.selectedApplicationGroup,
            width: "220",
            onInitialized: (args: SelectBoxInitializedEvent) => {
              this.groupSelectBoxInstance = args.component;
            },
            onValueChanged: (e) => {
              const selectedApplicationGroup = this.selectedApplication?.Group ?? null;
              if (this.selectedApplication != null && e.value === selectedApplicationGroup) {
                return;
              }
              this.selectedApplicationGroup = e.value;
              this.clearSolutionsGrid();
              this.addPatchButtonInstance?.option("disabled", true);
              this.addUpgradeButtonInstance?.option("disabled", true);
              this.applicationSelectBoxInstance?.option("value", null);
              this.applicationSelectBoxInstance?.option(
                "dataSource",
                this.createApplicationDataSource(e.value)
              );
            },
          },
        },
        {
          widget: "dxSelectBox",
          location: "before",
          options: {
            placeholder: "Select Application",
            value: this.selectedApplication?.Id,
            hint: "Select an application from which solutions should be displayed.",
            displayExpr: "Name",
            onInitialized: (args: SelectBoxInitializedEvent) => {
              this.applicationSelectBoxInstance = args.component;
            },
            onValueChanged: (e) => {
              this.setSelectedApplicationId(e.value);
            },
            valueExpr: "Id",
            width: "300",
            dataSource: this.createApplicationDataSource(this.selectedApplicationGroup),
          },
        },
      ];

      if (this.userService.currentDbUserWithTenant?.TenantNavigation.DisablePatchCreation === false) {
        items.push({
          location: "before",
          widget: "dxButton",
          options: {
            text: "Add Patch",
            icon: "add",
            stylingMode: "contained",
            type: "success",
            disabled: true,
            onClick: this.onClickAddPatch.bind(this),
            onInitialized: (args: ButtonInitializedEvent) => {
              this.addPatchButtonInstance = args.component;
            },
          },
        });
      }

      items.push(
        {
          location: "before",
          widget: "dxButton",
          options: {
            text: "Add Upgrade",
            icon: "add",
            stylingMode: "contained",
            type: "success",
            disabled: true,
            onClick: this.onClickAddUpgrade.bind(this),
            onInitialized: (args: ButtonInitializedEvent) => {
              this.addUpgradeButtonInstance = args.component;
            },
          },
        },
        {
          location: "after",
          widget: "dxButton",
          options: {
            icon: "assets/animations/loading.gif",
            text: "auto-refresh is active",
            stylingMode: "text",
            disabled: true,
            visible: false,
            onInitialized: (args: ButtonInitializedEvent) => {
              this.autoRefreshHintButtonInstance = args.component;
            },
          },
        },
        {
          location: "after",
          widget: "dxButton",
          options: {
            icon: "clear",
            stylingMode: "contained",
            type: "success",
            visible: false,
            onClick: this.onClickCancelAutoRefresh.bind(this),
            onInitialized: (args: ButtonInitializedEvent) => {
              this.autoRefreshCancelButtonInstance = args.component;
            },
          },
        },
        {
          location: "after",
          widget: "dxButton",
          options: {
            icon: "refresh",
            stylingMode: "contained",
            type: "success",
            hint: "Refresh the table.",
            onClick: this.onClickRefreshSolutionsGrid.bind(this),
          },
        }
      );

      return items;
    }

    private clearSolutionsGrid(): void {
    this.selectedApplication = null;
    this.cancelAutoRefresh();
    this.showSolutionsGrid = false;
    this.dataSourceSolutions = new DataSource({
      store: this.solutionService.getStore(),
      filter: ["Id", "=", -1],
    });
    this.dataGridColumns = [
      {
        caption: "Please select an application",
        name: "no_data",
      },
    ];
    setTimeout(() => {
      this.showSolutionsGrid = true;
    });
  }
}
