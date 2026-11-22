import { Component, Input, OnChanges, ChangeDetectionStrategy } from "@angular/core";
import { Action } from "src/app/shared/models/action.model";
import { ActionService } from "src/app/shared/services/action.service";
import { DeploymentMode } from "src/app/shared/models/deployment-mode.model";

@Component({
    selector: "app-action-detail",
    templateUrl: "./action-detail.component.html",
    styleUrls: ["./action-detail.component.css"],
    changeDetection: ChangeDetectionStrategy.Eager,
    standalone: false
})
export class ActionDetailComponent implements OnChanges {
  @Input() actionId: number;
  public action: Action;

  public get deploymentModeName(): string {
    switch (this.action?.DeploymentMode) {
      case DeploymentMode.OneStepUpgrade:
        return "One-step upgrade";
      case DeploymentMode.TwoStepUpgrade:
        return "Two-step upgrade";
      case DeploymentMode.UpdateOnly:
        return "Update only";
      default:
        return "";
    }
  }

  constructor(
    public actionService: ActionService
  ) {}

  public ngOnChanges(): void {
      this.actionService.getStore().byKey(this.actionId, {
        expand: [
          "CreatedByNavigation",
          "SolutionNavigation",
          "TargetEnvironmentNavigation",
          "TypeNavigation",
          "ResultNavigation",
          "StatusNavigation",
        ]
      }).then((actions: Action[]) => {
        this.action = actions[0];
      })
  }
}
