import { Component, OnInit } from "@angular/core";
import { ActivatedRoute, Router } from "@angular/router";
import { Key } from "selenium-webdriver";
import { AppConfig } from "src/app/shared/config/app.config";
import {
  LayoutParameter,
  LayoutService,
  NotificationType,
} from "src/app/shared/services/layout.service";
import { TenantService } from "src/app/shared/services/tenant.service";
import { UserService } from "src/app/shared/services/user.service";

@Component({
  selector: "app-settings",
  templateUrl: "./settings.component.html",
  styleUrls: ["./settings.component.css"],
})
export class SettingsComponent implements OnInit {
  public isGitHubConnected;
  public dataSourceGitHubRepositories;
  public valueSelectBoxRepositoryName: string;
  public valueDisablePatchCreation: boolean;
  public isSetupApplicationUser: boolean = false;
  public environmentMessages;
  public appId: string;

  constructor(
    private route: ActivatedRoute,
    private userService: UserService,
    private tenatService: TenantService,
    private layoutService: LayoutService,
    private router: Router
  ) {
    this.appId = AppConfig.settings.azure.applicationId;
  }

  public ngOnInit(): void {
    if (this.userService.currentDbUserWithTenant)
      this.refreshCurrentSettings();
      
    this.userService.stateChanged$.subscribe(() => {
      this.refreshCurrentSettings();

      this.route.queryParams.subscribe((params) => {
        if (
          !this.isGitHubConnected &&
          params["installation_id"] != undefined &&
          (params["setup_action"] == "install" || params["setup_action"] == "update")
        ) {
          this.layoutService.change(LayoutParameter.ShowLoading, true);
          this.tenatService
            .setGitHubInstallation(params["installation_id"])
            .then(() => {
              this.layoutService.notify({
                type: NotificationType.Success,
                message: "You have successfully created a connection to GitHub.",
              });
            })
            .catch(() => {
              this.layoutService.notify({
                type: NotificationType.Error,
                message:
                  "An error occurred while creating a connection to GitHub.",
              });
            })
            .then(() => {
              this.router.navigate(["/settings"]);
              this.layoutService.change(LayoutParameter.ShowLoading, false);
            });
        }
      });
    });    
  }

  public onClickSetupApplicationUsers(e) {
    this.layoutService.change(LayoutParameter.ShowLoading, true);
    this.userService.setupApplicationUsers().then((d) => {
      this.environmentMessages = d.value;
      this.isSetupApplicationUser = true;
      this.layoutService.change(LayoutParameter.ShowLoading, false);
    });
  }

  public onClickConnectGitHub(e): void {
    window.location.href = AppConfig.settings.github.installation_url;
  }

  public onClickSaveRepositoryName(e): void {
    this.layoutService.change(LayoutParameter.ShowLoading, true);
    this.tenatService
      .setGitHubRepositoryName(this.valueSelectBoxRepositoryName)
      .then(() => {
        this.layoutService.notify({
          type: NotificationType.Success,
          message: "You have successfully saved the repository.",
        });
      })
      .catch(() => {
        this.layoutService.notify({
          type: NotificationType.Error,
          message: "An error occurred while saving the repository.",
        });
      })
      .then(() => {
        this.layoutService.change(LayoutParameter.ShowLoading, false);
      });
  }

  public onValueChangedDisablePatchCreation(e): void {
    if(this.userService.currentDbUserWithTenant.TenantNavigation.DisablePatchCreation == this.valueDisablePatchCreation)
      return;
      
    this.layoutService.change(LayoutParameter.ShowLoading, true);
    this.tenatService
      .setDisablePatchCreation(this.valueDisablePatchCreation)
      .then(() => {
        this.layoutService.notify({
          type: NotificationType.Success,
          message: "You have successfully saved the disablement of patches state.",
        });
      })
      .catch(() => {
        this.layoutService.notify({
          type: NotificationType.Error,
          message: "An error occurred while saving the disablement of patches state.",
        });
      })
      .then(() => {
        this.layoutService.change(LayoutParameter.ShowLoading, false);
      });
  }

  private refreshCurrentSettings() {
    this.valueDisablePatchCreation = this.userService.currentDbUserWithTenant.TenantNavigation.DisablePatchCreation;
    this.refreshGitHubConnectionDisplay();
  }

  private refreshGitHubConnectionDisplay() {
    this.isGitHubConnected =
      this.userService.currentDbUserWithTenant.TenantNavigation
        .GitHubInstallationId != 0;

    if (this.isGitHubConnected) {
      this.layoutService.change(LayoutParameter.ShowLoading, true);
      this.tenatService.getGitHubRepositories().then((repositories) => {
        this.dataSourceGitHubRepositories = repositories;
        this.layoutService.change(LayoutParameter.ShowLoading, false);
      });
      this.valueSelectBoxRepositoryName =
        this.userService.currentDbUserWithTenant.TenantNavigation.GitHubRepositoryName;
    }
  }
}
