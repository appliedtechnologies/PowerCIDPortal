import { Inject, Injectable } from "@angular/core";
import { HttpEvent, HttpInterceptor, HttpHandler, HttpRequest } from "@angular/common/http";

import { Observable } from "rxjs";
import {
  MsalInterceptorConfiguration,
  MSAL_INTERCEPTOR_CONFIG,
} from "@azure/msal-angular";

@Injectable()
export class ProtectedResourcesInterceptor implements HttpInterceptor {
  constructor(
    @Inject(MSAL_INTERCEPTOR_CONFIG)
    private msalInterceptorConfig: MsalInterceptorConfiguration
  ) {}

  public intercept(
    req: HttpRequest<unknown>,
    next: HttpHandler
  ): Observable<HttpEvent<unknown>> {
      if(req.url.includes("dynamics.com")){
        try {
          const url = new URL(req.url);
          if (url.origin.includes("dynamics.com")) {
            this.addDataverseResource(url.origin);
          }
        } catch {
          // ignore invalid URLs and continue request handling
        }
      }
      return next.handle(req);
  }

  private addDataverseResource(url: string) {
    const resource = `${url}/api/data/v9.2/*`;

    if (!this.msalInterceptorConfig.protectedResourceMap.has(resource))
      this.msalInterceptorConfig.protectedResourceMap.set(resource, [
        `${url}//user_impersonation`,
      ]);
  }
}
