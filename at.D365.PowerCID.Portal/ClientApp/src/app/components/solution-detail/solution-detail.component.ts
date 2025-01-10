import {
  Component,
  EventEmitter,
  Input,
  OnChanges,
  Output,
  ViewChild,
} from "@angular/core";
import { DxTextBoxComponent } from "devextreme-angular";
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
import Validator from "devextreme/ui/validator";

@Component({
  selector: "app-solution-detail",
  templateUrl: "./solution-detail.component.html",
  styleUrls: ["./solution-detail.component.css"],
})
export class SolutionDetailComponent implements OnChanges {
  @Input() solution: Patch | Upgrade;
  @Input() application: Application;
  @Input() isAddUpgrade: boolean;
  @Output() onSaveCompleted = new EventEmitter<void>();
  @Output() onRenamed = new EventEmitter<void>();

  @ViewChild("textBoxName") public textBoxName: DxTextBoxComponent;
  @ViewChild("textAreaDescription") public textAreaDescription: DxTextBoxComponent;

  public isUpgrade: boolean;
  public isAdd: boolean;
  public buttonOptionsSaveAdd: any;

  constructor(
    private patchService: PatchService,
    private upgradeService: UpgradeService,
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

  public onClickSaveSolution(e) {
    let validation = e.validationGroup.validate();
    if (validation.isValid) {
      this.layoutService.change(LayoutParameter.ShowLoading, true);
      this.solution.Application = this.application.Id;
      this.solution.Name = this.textBoxName.value;
      this.solution.Description = this.textAreaDescription.value;
      if (this.isAddUpgrade) {
        this.upgradeService
          .add(this.solution)
          .then(() => {
            this.layoutService.notify({
              type: NotificationType.Success,
              message: "Upgrade was successfully created.",
            });
          })
          .catch(() => {
            this.layoutService.notify({
              type: NotificationType.Error,
              message: "An error occurred while creating the upgrade.",
            });
          })
          .then(() => {
            this.layoutService.change(LayoutParameter.ShowLoading, false);
            this.onSaveCompleted.emit();
          });
      } else {
        this.patchService
          .add(this.solution)
          .then(() => {
            this.layoutService.notify({
              type: NotificationType.Success,
              message: "Patch was successfully created.",
            });
          })
          .catch((error: Error) => {
            this.layoutService.notify({
              type: NotificationType.Error,
              message: error.message ? `An error occurred while creating the patch: ${error.message}` : "An error occurred while creating the patch."
            });
          })
          .then(() => {
            this.layoutService.change(LayoutParameter.ShowLoading, false);
            this.onSaveCompleted.emit();
          });
      }
    }
  }

  public onFocusOutSolutionName(e: any): void {
    if(!this.isAdd)
    {
      let newValue: string = this.textBoxName.value;
      let validation = (Validator.getInstance(e.element) as Validator).validate();
      if(validation.isValid && newValue && newValue != this.solution.Name){
        if (this.isUpgrade) {
          this.renameUpgrade(this.solution.Id, e.component.option("value"));
        } else {
          this.renamePatch(this.solution.Id, e.component.option("value"));
        }
      }
    }
  }

  public onFocusOutSolutionDescription(e: any): void {
    if(!this.isAdd)
    {
      let newValue: string = this.textAreaDescription.value;
      let validation = (Validator.getInstance(e.element) as Validator).validate();
      if(validation.isValid && newValue && newValue != this.solution.Description){
        if (this.isUpgrade) {
          this.changeDescriptionUpgrade(this.solution.Id, e.component.option("value"));
        } else {
          this.changeDescriptionPatch(this.solution.Id, e.component.option("value"));
        }
      }
    }
  }

  public ngOnChanges(): void{
    if (this.solution != undefined) {
      this.isUpgrade = "ApplyManually" in this.solution;
      this.isAdd = false;
      if(this.solution.OverwriteUnmanagedCustomizations === null)
        this.solution.OverwriteUnmanagedCustomizations = true;
      if(this.solution.EnableWorkflows === null)
        this.solution.EnableWorkflows = true;
    } else {
      this.isAdd = true;
      this.solution = {
        OverwriteUnmanagedCustomizations: true,
        EnableWorkflows: true
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
      this.onRenamed.emit();
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
      this.onRenamed.emit();
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
      this.onRenamed.emit();
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
      this.onRenamed.emit();
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
