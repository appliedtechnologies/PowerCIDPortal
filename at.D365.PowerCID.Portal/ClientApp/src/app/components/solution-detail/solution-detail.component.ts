import {
  Component,
  EventEmitter,
  Input,
  OnChanges,
  Output,
} from "@angular/core";
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
      if (this.isAddUpgrade) {
        this.upgradeService
          .getStore()
          .insert(this.solution)
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
          .getStore()
          .insert(this.solution)
          .then(() => {
            this.layoutService.notify({
              type: NotificationType.Success,
              message: "Patch was successfully created.",
            });
          })
          .catch(() => {
            this.layoutService.notify({
              type: NotificationType.Error,
              message: "An error occurred while creating the patch.",
            });
          })
          .then(() => {
            this.layoutService.change(LayoutParameter.ShowLoading, false);
            this.onSaveCompleted.emit();
          });
      }
    }
  }

  ngOnChanges() {
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
}
