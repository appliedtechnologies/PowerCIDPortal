import { Injectable } from "@angular/core";
import { Router } from "@angular/router";
import { LayoutService, NotificationType } from "../services/layout.service";
import { UserService } from "../services/user.service";

/**
 * Restricts routes to the tenant that cross-tenant delivery is enabled for (see UserService.isCrossTenantDeliveryEnabled,
 * backed by the server-side "CrossTenantDelivery:AllowedTenantId" configuration).
 */
@Injectable()
export class CrossTenantGuard {

    constructor(private userService: UserService, private router: Router, private layoutService: LayoutService) {}

    canActivate(): boolean {
      if (!this.userService.isCrossTenantDeliveryEnabled) {
        this.router.navigate(['/']);
        this.layoutService.notify({type: NotificationType.Error, message: "Cross-tenant delivery is not enabled for your tenant."});
        return false;
      }

      return true;
    }
  }
