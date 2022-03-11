import { Inject, Injectable } from "@angular/core";
import {
  HttpEvent,
  HttpInterceptor,
  HttpHandler,
  HttpRequest,
} from "@angular/common/http";

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
    req: HttpRequest<any>,
    next: HttpHandler
  ): Observable<HttpEvent<any>> {
      try{
        let url = new URL(req.url);
        if (url.origin.includes("dynamics.com"))
          this.addDataverseResource(url.origin);
      }
      finally{
        return next.handle(req);
      }
  }

  private addDataverseResource(url: string) {
    let resource: string = `${url}/api/data/v9.2/*`;

    if (!this.msalInterceptorConfig.protectedResourceMap.has(resource))
      this.msalInterceptorConfig.protectedResourceMap.set(resource, [
        `${url}//user_impersonation`,
      ]);
  }
}
