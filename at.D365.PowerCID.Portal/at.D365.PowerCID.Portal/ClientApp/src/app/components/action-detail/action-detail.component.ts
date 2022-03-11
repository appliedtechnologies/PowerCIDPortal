import { Component, Input, OnChanges, OnInit, SimpleChanges } from "@angular/core";
import { Action } from "src/app/shared/models/action.model";
import { ActionService } from "src/app/shared/services/action.service";

@Component({
  selector: "app-action-detail",
  templateUrl: "./action-detail.component.html",
  styleUrls: ["./action-detail.component.css"],
})
export class ActionDetailComponent implements OnChanges {
  @Input() actionId: number;
  public action: Action;

  constructor(
    private actionService: ActionService
  ) {}

  public ngOnChanges(changes: SimpleChanges): void {
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
