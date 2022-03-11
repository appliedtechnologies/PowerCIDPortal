import { HttpClient } from "@angular/common/http";
import { Component, OnInit } from "@angular/core";
import { MsalService, MsalBroadcastService } from "@azure/msal-angular";
import {
  EventMessage,
  EventType,
  InteractionStatus,
} from "@azure/msal-browser";
import { Subject } from "rxjs";
import { filter, takeUntil } from "rxjs/operators";
import { User } from "src/app/shared/models/user.model";
import { ODataService } from "src/app/shared/services/odata.service";
import { UserService } from "src/app/shared/services/user.service";
import { UserEnvironmentService } from "src/app/shared/services/userenvironment.service";
import { UserEnvironment } from "src/app/shared/models/userenvironment.model";
import { DxListComponent, DxPopupComponent } from "devextreme-angular";

@Component({
  selector: "app-profile",
  templateUrl: "./profile.component.html",
  styleUrls: ["./profile.component.css"]
})
export class ProfileComponent implements OnInit {
  isLoggedIn = false;
  user: User;
  roles: string;
  allowedEnv: string;

  constructor(
    private userService: UserService,
    private userEnvironmentService: UserEnvironmentService
  ) {}

  ngOnInit(): void {
    this.getUserInformation();
    this.userService.stateChanged$.subscribe(() => {
      this.getUserInformation();
    });
  }

  private getUserInformation(){
    this.isLoggedIn = this.userService.isLogggedIn;
    if(this.isLoggedIn && this.userService.currentDbUserWithTenant){
      this.user = this.userService.currentDbUserWithTenant;
      this.roles = this.userService.currentUserRoles?.join(", ");
      this.getAllowedEnvironments();     
    }
  }

  private getAllowedEnvironments(){
    this.userEnvironmentService
      .getStore()
      .load({
        filter: ["User", "=", this.user.Id],
        expand: ["EnvironmentNavigation"]
      })
      .then((userEnvironments: UserEnvironment[]) => {
        if (userEnvironments.length > 0){
          this.allowedEnv = userEnvironments.map((e) => e.EnvironmentNavigation.Name).join(", ");
        }
        else{
          this.allowedEnv = "none"
        };
      });
  }

  login() {
    this.userService.login();
  }

  async logout() {
    await this.userService.logout();
  }
}
