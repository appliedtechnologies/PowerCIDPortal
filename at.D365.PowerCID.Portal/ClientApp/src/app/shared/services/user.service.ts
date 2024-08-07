import { HttpClient } from "@angular/common/http";
import { Injectable } from "@angular/core";
import { Router } from "@angular/router";
import { MsalBroadcastService, MsalService } from "@azure/msal-angular";
import {
  AccountEntity,
  AccountInfo,
  EventMessage,
  EventType,
  InteractionStatus,
  SilentRequest,
} from "@azure/msal-browser";
import { ThrottlingUtils } from "@azure/msal-common";
import ODataStore from "devextreme/data/odata/store";
import { firstValueFrom, forkJoin, from, merge, Observable, Subject } from "rxjs";
import { filter, map, switchMap, takeUntil } from "rxjs/operators";
import { isDebuggerStatement } from "typescript";
import { AppConfig } from "../config/app.config";
import { InitRedirctRequest } from "../config/auth-config";
import { User } from "../models/user.model";
import {
  LayoutParameter,
  LayoutService,
  NotificationType,
} from "./layout.service";
import { LogService } from "./log.service";
import { ODataService } from "./odata.service";
import { AppRoleAssignment } from "../models/approleassignment.model";
import { alert } from "devextreme/ui/dialog";

@Injectable()
export class UserService {
  public isLogggedIn: boolean = false;
  public currentUserRoles: string[];
  public currentIdentityUser: AccountInfo;
  public currentDbUserWithTenant: User;

  private isPortalLogginInProgess: boolean = false;

  //observables
  private readonly _destroying$ = new Subject<void>();
  private manualUpdate = new Subject<void>();
  public stateChanged$: Observable<void>;

  constructor(
    private authService: MsalService,
    private msalBroadcastService: MsalBroadcastService,
    private logService: LogService,
    private http: HttpClient,
    private odataService: ODataService,
    private layoutService: LayoutService
  ) {
    this.msalBroadcastService.inProgress$
      .pipe(
        filter(
          (status: InteractionStatus) => status === InteractionStatus.None
        ),
        takeUntil(this._destroying$)
      )
      .subscribe(() => {
        if (this.isMSALLoggedIn()) {
          if (this.isPortalLoggedIn()) {
            this.updateUserInformation().then(() =>
              this.logService.debug("MSAL interaction completed")
            );
          } else {
            this.doPortalLogin()
              .then(() => {
                this.logService.debug("login after MSAL interaction completed");
              })
              .catch((error) =>
                error
                  ? this.logService.debug(
                      "cannot start portal login after MSAL interaction, " +
                        error
                    )
                  : this.logService.error(
                      "cannot start portal login after MSAL interaction"
                    )
              );
          }
        }
      });

    let login$ = this.msalBroadcastService.msalSubject$.pipe(
      filter((msg: EventMessage) => msg.eventType === EventType.LOGIN_SUCCESS),
      takeUntil(this._destroying$),
      switchMap((data) => {
        return this.doPortalLogin()
          .then(() => {
            this.checkUpdatedOwnership();
            this.layoutService.notify({
              message: "Login completed successfully",
              type: NotificationType.Success,
            });
            this.manualUpdate.next(); //TODO müsste eigentlich überflüssig sein
            this.logService.debug("login after MSAL login completed");
          })
          .catch((error) =>
            error
              ? this.logService.debug(
                  "cannot start portal login after MSAL login, " + error
                )
              : this.logService.error(
                  "cannot start portal login after MSAL login"
                )
          );
      })
    );

    let logout$ = this.msalBroadcastService.msalSubject$.pipe(
      filter((msg: EventMessage) => msg.eventType === EventType.LOGOUT_SUCCESS),
      takeUntil(this._destroying$),
      map(() => {
        this.doPortalLogout();
        this.logService.debug("logout after MSAL logout completed");
      })
    );

    this.stateChanged$ = merge(
      login$,
      logout$,
      this.manualUpdate.asObservable()
    ).pipe(
      map(() => {
        this.logService.debug("user state may have changed");
      })
    );

    if (this.isMSALLoggedIn() && this.isPortalLoggedIn()) {
      this.updateUserInformation()
        .then(() => {
          this.checkUpdatedOwnership();
          this.manualUpdate.next();
          this.logService.debug("initialized user service");
        })
        .catch(() => {
          this.logService.error("get user information failed, logging out...");
          this.authService.logoutRedirect({
            account: this.currentIdentityUser,
            postLogoutRedirectUri: location.origin,
          });
        });
    }
  }

  public login(): Observable<void> {
    return this.authService.loginRedirect(InitRedirctRequest).pipe(
      map((result) => {
        this.logService.debug(result);
      })
    );
  }

  public async logout(): Promise<void> {
    await this.authService.logoutRedirect({
      account: this.currentIdentityUser
    });
  }

  public getStore(): ODataStore {
    return this.odataService.context["Users"];
  }

  public delete(id: number): Promise<void>{
    return this.getStore().remove(id);
  }

  public reloadUser(): Promise<void> {
    return new Promise<void>((resolve, reject) => {
      this.updateUserInformation(true)
        .then(() => {
          this.manualUpdate.next();
          resolve();
        })
        .catch(() => reject());
    });
  }

  private getDbUserWithTenant(): Promise<void> {
    return new Promise<void>((resolve, reject) => {
      if (!this.isLogggedIn) reject();

      this.odataService.context["Users"]
        .load({
          filter: `MsId eq ${this.currentIdentityUser.localAccountId}`,
          expand: "TenantNavigation",
        })
        .then((response) => {
          if (response[0] != null) {
            this.currentDbUserWithTenant = response[0];
            resolve();
          }
        });
    });
  }

  setupApplicationUsers() {
    return new Promise<any>((resolve, reject) => {
      this.http
        .post(`${AppConfig.settings.api.url}/Users/SetupApplicationUsers`, {})
        .subscribe({
          next: (data) => resolve(data as any),
          error: () => reject(),
        });
    });
  }

  public syncAdminRole(): Promise<void> {
    return new Promise<void>((resolve, reject) => {
      this.http
        .post(`${AppConfig.settings.api.url}/Users/SyncAdminRole`, {})
        .subscribe({
          next: (data) => resolve(),
          error: () => reject(),
        });
    });
  }

  public getUserRoles(userId: number): Promise<AppRoleAssignment[]> {
    return new Promise<AppRoleAssignment[]>((resolve, reject) => {
      this.http
        .post(`${AppConfig.settings.api.url}/Users(${userId})/GetUserRoles`, {})
        .subscribe({
          next: (data) => resolve(data["value"] as AppRoleAssignment[]),
          error: () => reject(),
        });
    });
  }

  public assignRole(userId: number, appRoleId: string): Promise<void> {
    return new Promise<void>((resolve, reject) => {
      this.http
        .post(`${AppConfig.settings.api.url}/Users(${userId})/AssignRole`, {
          appRoleId: appRoleId,
        })
        .subscribe({
          next: () => resolve(),
          error: () => reject(),
        });
    });
  }

  public removeAssignedRole(userId: number, roleAssignmentId: string): Promise<void> {
    return new Promise<void>((resolve, reject) => {
      this.http
        .post(
          `${AppConfig.settings.api.url}/Users(${userId})/RemoveAssignedRole`,
          {
            roleAssignmentId: roleAssignmentId,
          }
        )
        .subscribe({
          next: () => resolve(),
          error: () => reject(),
        });
    });
  }

  private updateUserInformation(forceDbReload: boolean = false, forceIdentityReload: boolean = false): Promise<void> {
    return new Promise<void>((resolve, reject) => {
      this.isLogggedIn = this.isMSALLoggedIn() && this.isPortalLoggedIn();
      this.manualUpdate.next();
      if (this.isLogggedIn) {
        if (this.currentIdentityUser == undefined || forceIdentityReload)
          this.currentIdentityUser =
            this.authService.instance.getAllAccounts()[0];
        if (this.currentUserRoles == undefined || forceIdentityReload)
          this.currentUserRoles =
            this.currentIdentityUser.idTokenClaims["roles"];
        if (this.currentDbUserWithTenant == undefined || forceDbReload)
          this.getDbUserWithTenant()
            .then(() => {
              resolve();
            })
            .catch(() => reject());
        else resolve();
      } else resolve();
    });
  }

  private callPortalLogin(): Promise<void> {
    return new Promise<void>((resolve, reject) => {
      let request = this.http
        .post(`${AppConfig.settings.api.url}/Users/Login`, {})
        .subscribe({
          next: () => resolve(),
          error: () => reject(),
        });
    });
  }

  private isMSALLoggedIn(): boolean {
    return this.authService.instance.getAllAccounts().length > 0;
  }

  private isPortalLoggedIn(): boolean {
    return JSON.parse(
      localStorage.getItem("atPowerCIDPortal_PortalLoginState")
    );
  }

  private doPortalLogin(): Promise<void> {
    return new Promise<void>((resolve, reject) => {
      if (!this.isPortalLogginInProgess) {
        this.isPortalLogginInProgess = true;
        if (this.isMSALLoggedIn()) {
          this.layoutService.change(LayoutParameter.ShowLoading, true);
          this.callPortalLogin()
            .then(() => {
              localStorage.setItem("atPowerCIDPortal_PortalLoginState", "true");
              this.updateUserInformation().then(() => {
                      this.isPortalLogginInProgess = false;
                  resolve();
              });
            })
            .finally(() =>
              this.layoutService.change(LayoutParameter.ShowLoading, false)
            );
        } else reject();
      } else reject("portal login already in progress");
    });
  }

  private doPortalLogout(): void {
    localStorage.removeItem("atPowerCIDPortal_PortalLoginState");
    this.currentUserRoles = undefined;
    this.currentIdentityUser = undefined;
    this.currentDbUserWithTenant = undefined;
  }

  private checkUpdatedOwnership(){
    if(this.currentDbUserWithTenant.RemoveAdminRole)
      alert("You are not the admin of this application. Ask an existing admin to synchronise the admin roles. In the meantime, you will be logged out - <a href ='https://github.com/appliedtechnologies/PowerCIDPortal/wiki/Setup-and-maintenance-of-a-tenant/#unilateral-synchronization-of-ownership-and-administrator-role' target='_blank'>more information.</a>", "Admin status has changed").then(() => {
        this.logout();
      });
    else if ((this.currentDbUserWithTenant.IsOwner && (!this.currentUserRoles || !this.currentUserRoles.includes("atPowerCID.Admin"))) || (!this.currentDbUserWithTenant.IsOwner && (this.currentUserRoles && this.currentUserRoles.includes("atPowerCID.Admin")))) {
      alert("Your status of being an admin of this application has changed. That's why you need to log in once again to reload your permissions - <a href ='https://github.com/appliedtechnologies/PowerCIDPortal/wiki/Setup-and-maintenance-of-a-tenant/#unilateral-synchronization-of-ownership-and-administrator-role' target='_blank'>more information.</a>", "Admin status has changed").then(() => {
        this.logout();
      }); 
    }
  }
}
