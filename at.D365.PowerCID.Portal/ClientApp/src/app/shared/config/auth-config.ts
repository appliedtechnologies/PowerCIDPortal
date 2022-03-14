import {
  MsalGuardConfiguration,
  MsalInterceptorConfiguration,
} from "@azure/msal-angular";
import {
  BrowserCacheLocation,
  EndSessionRequest,
  InteractionType,
  IPublicClientApplication,
  LogLevel,
  PublicClientApplication,
  RedirectRequest,
} from "@azure/msal-browser";

import { AppConfig } from "src/app/shared/config/app.config";

const isIE =
  window.navigator.userAgent.indexOf("MSIE ") > -1 ||
  window.navigator.userAgent.indexOf("Trident/") > -1;

export function loggerCallback(logLevel: LogLevel, message: string) {
  console.log(message);
}

export function MSALInstanceFactory(): IPublicClientApplication {
  return new PublicClientApplication({
    auth: {
      //howto create azure app registration: https://docs.microsoft.com/en-us/azure/active-directory/develop/scenario-spa-app-registration
      clientId: AppConfig.settings.azure.applicationId, // This is your client ID
      authority: "https://login.microsoftonline.com/organizations", // This is your tenant ID
      redirectUri: `${location.origin}/profile`, // This is your redirect URI,
      postLogoutRedirectUri: location.origin,
    },
    cache: {
      cacheLocation: BrowserCacheLocation.LocalStorage,
      storeAuthStateInCookie: isIE, // set to true for IE 11
    },
    system: {
      loggerOptions: {
        loggerCallback,
        logLevel: LogLevel.Info,
        piiLoggingEnabled: false,
      },
    },
  });
}

export function MSALInterceptorConfigFactory(): MsalInterceptorConfiguration {
  const protectedResourceMap = new Map<string, Array<string>>();
  protectedResourceMap.set("https://graph.microsoft.com/v1.0/*", ["user.read"]);
  protectedResourceMap.set(
    "https://management.azure.com/providers/Microsoft.ProcessSimple/environments?api-version=2016-11-01",
    ["https://management.azure.com//user_impersonation"]
  );
  protectedResourceMap.set(location.origin + "/odata/*", [
    `api://${AppConfig.settings.azure.applicationId}/access_as_user`,
  ]);

  return {
    interactionType: InteractionType.Redirect,
    protectedResourceMap,
  };
}

export function MSALGuardConfigFactory(): MsalGuardConfiguration {
  return {
    interactionType: InteractionType.Redirect,
    authRequest: InitRedirctRequest,
  };
}

export const InitRedirctRequest: RedirectRequest = {
  scopes: ["https://graph.microsoft.com/.default"]
};
