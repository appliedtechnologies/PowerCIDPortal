import { Injectable } from "@angular/core";
import { ActivatedRouteSnapshot, Router } from "@angular/router";
import { LayoutService, NotificationType } from "../services/layout.service";
import { UserService } from "../services/user.service";
@Injectable()
export class RoleGuard  {

    constructor(private userService: UserService, private router: Router, private layoutService: LayoutService) {}
    
    canActivate(route: ActivatedRouteSnapshot): Promise<boolean> {
      const expectedRoles: string[] = route.data.expectedRoles;

      return this.userService.ensureUserInformationLoaded().then(() => {
        if (!this.userService.currentUserRoles || !this.userService.currentUserRoles.some(e => expectedRoles.includes(e))) {
          this.router.navigate(['/']);
          this.layoutService.notify({type: NotificationType.Error, message: `You do not have a role to access this page. Expected one of these roles: ${expectedRoles.join(", ")}`});
          return false;
        }

        return true;
      });
    }
  }