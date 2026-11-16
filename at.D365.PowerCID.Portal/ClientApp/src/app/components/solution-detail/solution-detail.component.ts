import {
  Component,
  EventEmitter,
  Input,
  OnChanges,
  Output,
  ViewChild,
  ChangeDetectionStrategy
} from "@angular/core";
import { DxTextAreaComponent, DxTextBoxComponent } from "devextreme-angular";
import { Application } from "src/app/shared/models/application.model";
import { Patch } from "src/app/shared/models/patch.model";
import { Upgrade } from "src/app/shared/models/upgrade.model";
import {
  LayoutParameter,
  LayoutService,
  NotificationType,
} from "src/app/shared/services/layout.service";
import { PatchService } from "src/app/shared/services/patch.service";
import { UpgradeService } from "src/app/shared/services/upgrade.service";
import { SolutionService } from "src/app/shared/services/solution.service";
import { UserService } from "src/app/shared/services/user.service";
import { AppConfig } from "src/app/shared/config/app.config";
import Validator from "devextreme/ui/validator";
import { FocusOutEvent } from "devextreme/ui/text_box";
import { ValueChangedEvent } from "devextreme/ui/check_box";

@Component({
    selector: "app-solution-detail",
    templateUrl: "./solution-detail.component.html",
    styleUrls: ["./solution-detail.component.css"],
    changeDetection: ChangeDetectionStrategy.Eager,
    standalone: false
})
export class SolutionDetailComponent implements OnChanges {
  @Input() solution: Patch | Upgrade;
  @Input() application: Application;
  @Input() isAddUpgrade: boolean;
  @Output() saveCompleted = new EventEmitter<void>();
  @Output() renamed = new EventEmitter<void>();

  @ViewChild("textBoxName") public textBoxName: DxTextBoxComponent;
  @ViewChild("textAreaDescription") public textAreaDescription: DxTextBoxComponent;
  @ViewChild("textAreaDeploymentNotes") public textAreaDeploymentNotes: DxTextAreaComponent;

  public isUpgrade: boolean;
  public isAdd: boolean;
  public buttonOptionsSaveAdd: Record<string, unknown>;
  public isReleasedExternally: boolean;

  constructor(
    private patchService: PatchService,
    private upgradeService: UpgradeService,
    private solutionService: SolutionService,
    public userService: UserService,
    private layoutService: LayoutService
  ) {
    this.buttonOptionsSaveAdd = {
      icon: 'save',
      text: 'Save',
      type: 'success',
      onClick: this.onClickSaveSolution.bind(this),
      width: '100%'
    };
  }

  public get canManageExternalRelease(): boolean {
    return (
      this.userService.isCrossTenantDeliveryEnabled &&
      this.userService.currentUserRoles &&
      this.userService.currentUserRoles.some((e) =>
        [
          AppConfig.settings.azure.appRoleNames.admin,
          AppConfig.settings.azure.appRoleNames.externalReleaseManager,
        ].includes(e)
      )
    );
  }

  public onValueChangedIsReleasedExternally(e: ValueChangedEvent): void {
    if (e.value === e.previousValue) return;

    this.layoutService.change(LayoutParameter.ShowLoading, true);
    this.solutionService
      .setExternalRelease(this.solution.Id, e.value)
      .then(() => {
        this.solution.IsReleasedExternally = e.value;
        this.layoutService.notify({
          type: NotificationType.Success,
          message: e.value
            ? "The solution version was successfully released for external deployment."
            : "The solution version's external release was successfully revoked.",
        });
      })
      .catch((error) => {
        this.isReleasedExternally = !e.value;
        const backendMessage =
          typeof error?.error === "string"
            ? error.error
            : error?.error?.error?.message || error?.error?.message;
        this.layoutService.notify({
          type: NotificationType.Error,
          message: backendMessage
            ? `External release could not be changed: ${backendMessage}`
            : "An error occurred while changing the external release.",
        });
      })
      .finally(() => {
        this.layoutService.change(LayoutParameter.ShowLoading, false);
      });
  }


  public async onClickSaveSolution(e): Promise<void> {
    const validation = e.validationGroup.validate();
    if (!validation.isValid) {
      return;
    }

    this.layoutService.change(LayoutParameter.ShowLoading, true);
    try {
      this.solution.Application = this.application.Id;
      this.solution.Name = this.textBoxName.value;
      this.solution.Description = this.textAreaDescription.value;
      this.solution.DeploymentNotes = this.textAreaDeploymentNotes.value || null;
      if (this.isAddUpgrade) {
        await this.upgradeService.add(this.solution);
        this.layoutService.notify({
          type: NotificationType.Success,
          message: "Upgrade was successfully created.",
        });
      } else {
        await this.patchService.add(this.solution);
        this.layoutService.notify({
          type: NotificationType.Success,
          message: "Patch was successfully created.",
        });
      }
    } catch (error) {
      const message = error?.message;
      this.layoutService.notify({
        type: NotificationType.Error,
        message: this.isAddUpgrade
          ? message
            ? `An error occurred while creating the Upgrade: ${message}`
            : "An error occurred while creating the Upgrade."
          : message
            ? `An error occurred while creating the Patch: ${message}`
            : "An error occurred while creating the Patch.",
      });
    } finally {
      this.layoutService.change(LayoutParameter.ShowLoading, false);
      this.saveCompleted.emit();
    }
  }

  public onFocusOutSolutionName(e: FocusOutEvent): void {
    if(!this.isAdd)
    {
      const newValue: string = this.textBoxName.value;
      const validation = (Validator.getInstance(e.element) as Validator).validate();
      if(validation.isValid && newValue && newValue != this.solution.Name){
        if (this.isUpgrade) {
          this.renameUpgrade(this.solution.Id, e.component.option("value"));
        } else {
          this.renamePatch(this.solution.Id, e.component.option("value"));
        }
      }
    }
  }

  public onFocusOutSolutionDescription(e: FocusOutEvent): void {
    if(!this.isAdd)
    {
      const newValue: string = this.textAreaDescription.value;
      const validation = (Validator.getInstance(e.element) as Validator).validate();
      if(validation.isValid && newValue && newValue != this.solution.Description){
        if (this.isUpgrade) {
          this.changeDescriptionUpgrade(this.solution.Id, e.component.option("value"));
        } else {
          this.changeDescriptionPatch(this.solution.Id, e.component.option("value"));
        }
      }
    }
  }

  public async onFocusOutDeploymentNotes(): Promise<void> {
    if (this.isAdd) {
      return;
    }

    const newValue: string = this.textAreaDeploymentNotes.value ?? "";
    if (newValue === (this.solution.DeploymentNotes ?? "")) {
      return;
    }

    const deploymentNotes = newValue || null;
    this.layoutService.change(LayoutParameter.ShowLoading, true);
    try {
      if (this.isUpgrade) {
        await this.upgradeService.update(this.solution.Id, { DeploymentNotes: deploymentNotes });
      } else {
        await this.patchService.update(this.solution.Id, { DeploymentNotes: deploymentNotes });
      }
      this.solution.DeploymentNotes = deploymentNotes;
      this.layoutService.notify({
        type: NotificationType.Success,
        message: "Deployment notes were successfully changed.",
      });
      this.renamed.emit();
    } catch (error) {
      this.layoutService.notify({
        type: NotificationType.Error,
        message: error?.message
          ? `An error occurred while changing deployment notes: ${error.message}`
          : "An error occurred while changing deployment notes.",
      });
    } finally {
      this.layoutService.change(LayoutParameter.ShowLoading, false);
    }
  }

  public ngOnChanges(): void{
    if (this.solution != undefined) {
      this.isUpgrade = "ApplyManually" in this.solution;
      this.isAdd = false;
      if(this.solution.OverwriteUnmanagedCustomizations === null)
        this.solution.OverwriteUnmanagedCustomizations = false;
      if(this.solution.EnableWorkflows === null)
        this.solution.EnableWorkflows = true;
      this.isReleasedExternally = this.solution.IsReleasedExternally === true;
    } else {
      this.isAdd = true;
      this.solution = {
        OverwriteUnmanagedCustomizations: false,
        EnableWorkflows: true,
        DeploymentNotes: null
      };
    }
  }

  private renamePatch(id: number, newName: string): void{
    this.patchService
    .update(id, { Name: newName })
    .then(() => {
      this.layoutService.notify({
        type: NotificationType.Success,
        message: "The Patch was successfully renamed.",
      });
      this.renamed.emit();
    })
    .catch((error) => {
      if(error != null)
        this.layoutService.notify({
          type: NotificationType.Error,
          message: `An error occurred while renaming the Patch: ${error.message}`,
        });
      else
        this.layoutService.notify({
          type: NotificationType.Error,
          message: "An error occurred while renaming the Patch.",
        });
      })
    .then(() => {
      this.layoutService.change(LayoutParameter.ShowLoading, false);
    });
  }

  private renameUpgrade(id: number, newName: string): void{
    this.upgradeService
    .update(id, { Name: newName })
    .then(() => {
      this.layoutService.notify({
        type: NotificationType.Success,
        message: "The Upgrade was successfully renamed.",
      });
      this.renamed.emit();
    })
    .catch((error) => {
      if(error != null)
        this.layoutService.notify({
          type: NotificationType.Error,
          message: `An error occurred while renaming the Upgrade: ${error.message}`,
        });
      else
        this.layoutService.notify({
          type: NotificationType.Error,
          message: "An error occurred while renaming the Upgrade.",
        });
    })
    .then(() => {
      this.layoutService.change(LayoutParameter.ShowLoading, false);
    });
  }

  private changeDescriptionPatch(id: number, newDescirption: string): void{
    this.patchService
    .update(id, { Description: newDescirption })
    .then(() => {
      this.layoutService.notify({
        type: NotificationType.Success,
        message: "Description was successfully changed.",
      });
      this.renamed.emit();
    })
    .catch((error) => {
      if(error != null)
        this.layoutService.notify({
          type: NotificationType.Error,
          message: `An error occurred while renaming the description: ${error.message}`,
        });
      else
        this.layoutService.notify({
          type: NotificationType.Error,
          message: "An error occurred while renaming the description.",
        });
      })
    .then(() => {
      this.layoutService.change(LayoutParameter.ShowLoading, false);
    });
  }

  private changeDescriptionUpgrade(id: number, newDescription: string): void{
    this.upgradeService
    .update(id, { Description: newDescription })
    .then(() => {
      this.layoutService.notify({
        type: NotificationType.Success,
        message: "Description was successfully renamed.",
      });
      this.renamed.emit();
    })
    .catch((error) => {
      if(error != null)
        this.layoutService.notify({
          type: NotificationType.Error,
          message: `An error occurred while renaming the description: ${error.message}`,
        });
      else
        this.layoutService.notify({
          type: NotificationType.Error,
          message: "An error occurred while renaming the description.",
        });
    })
    .then(() => {
      this.layoutService.change(LayoutParameter.ShowLoading, false);
    });
  }
}
