import { Component, OnInit, ChangeDetectionStrategy } from "@angular/core";
import { User } from "src/app/shared/models/user.model";
import { UserService } from "src/app/shared/services/user.service";
import { UserEnvironmentService } from "src/app/shared/services/userenvironment.service";
import { UserEnvironment } from "src/app/shared/models/userenvironment.model";
import { LayoutService, NotificationType } from "src/app/shared/services/layout.service";

@Component({
    selector: "app-profile",
    templateUrl: "./profile.component.html",
    styleUrls: ["./profile.component.css"],
    changeDetection: ChangeDetectionStrategy.Eager,
    standalone: false
})
export class ProfileComponent implements OnInit {
  isLoggedIn = false;
  user: User;
  roles: string;
  allowedEnv: string;

  constructor(
    private userService: UserService,
    private userEnvironmentService: UserEnvironmentService,
    private layoutService: LayoutService
  ) {}

  ngOnInit(): void {
    this.getUserInformation();
    this.userService.stateChanged$.subscribe(() => {
      this.getUserInformation();
    });
  }

  public onClickResetGridSettings(e): void {
    void e;
    localStorage.removeItem("atPowerCIDPortal_Environments_GridState");
    localStorage.removeItem("atPowerCIDPortal_Applications_GridState");
    localStorage.removeItem("atPowerCIDPortal_Users_GridState");
    localStorage.removeItem("atPowerCIDPortal_History_GridState");
    this.layoutService.notify({ message: "The grid settings have been successfully reset.", type: NotificationType.Success });
  }

  private getUserInformation(){
    this.isLoggedIn = this.userService.isLogggedIn;
    if(this.isLoggedIn && this.userService.currentDbUserWithTenant){
      this.user = this.userService.currentDbUserWithTenant;
      this.roles = this.userService.currentUserRoles?.join(", ");
      if(this.roles)
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
