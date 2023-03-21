import { BrowserModule } from "@angular/platform-browser";
import { APP_INITIALIZER, NgModule } from "@angular/core";
import { FormsModule } from "@angular/forms";
import {
  HttpClient,
  HttpClientModule,
  HTTP_INTERCEPTORS,
} from "@angular/common/http";
import {
  DxDataGridModule,
  DxDrawerModule,
  DxListModule,
  DxLoadPanelModule,
  DxToolbarModule,
  DxSelectBoxModule,
  DxFormModule,
  DxLookupModule,
  DxPopupModule,
  DxTextBoxModule,
  DxButtonModule,
  DxTreeViewModule,
  DxSortableModule,
  DxTextAreaModule,
  DxScrollViewModule,
  DxContextMenuModule,
  DxResponsiveBoxModule 
} from "devextreme-angular";

import {
  MsalModule,
  MsalInterceptor,
  MsalRedirectComponent,
  MsalService,
  MsalGuard,
  MsalBroadcastService,
  MSAL_INSTANCE,
  MSAL_GUARD_CONFIG,
  MSAL_INTERCEPTOR_CONFIG,
} from "@azure/msal-angular";

import { AppComponent } from "./app.component";
import { HomeComponent } from "./components/home/home.component";
import { ProfileComponent } from "./components/profile/profile.component";
import {
  MSALGuardConfigFactory,
  MSALInstanceFactory,
  MSALInterceptorConfigFactory,
} from "./shared/config/auth-config";
import { AppRoutingModule } from "./app-routing.module";
import { UserService } from "./shared/services/user.service";
import { LogService } from "./shared/services/log.service";
import { ActionService } from "./shared/services/action.service";
import { ApplicationService } from "./shared/services/application.service";
import { EnvironmentService } from "./shared/services/environment.service";
import { PatchService } from "./shared/services/patch.service";
import { TenantService } from "./shared/services/tenant.service";
import { UpgradeService } from "./shared/services/upgrade.service";
import { AppConfig } from "./shared/config/app.config";
import { ODataService } from "./shared/services/odata.service";

import devextremeAjax from "devextreme/core/utils/ajax";
import { sendRequestFactory } from "./shared/helper/ng-http-client-helper";
import { EnvironmentComponent } from "./components/environment/environment.component";
import { ApplicationComponent } from "./components/application/application.component";
import { LayoutService } from "./shared/services/layout.service";
import { SolutionsOverviewComponent } from "./components/solutions-overview/solutions-overview.component";
import { SolutionService } from "./shared/services/solution.service";
import { ProtectedResourcesInterceptor } from "./shared/interceptors/protected-resources.interceptor";
import { PublisherService } from "./shared/services/publisher.service";
import { DeploymentpathComponent } from "./components/deploymentpath/deploymentpath.component";
import { DxTreeListModule } from "devextreme-angular";
import { DeploymentpathService } from "./shared/services/deploymentpath.service";
import { ActionDetailComponent } from "./components/action-detail/action-detail.component";
import { SolutionDetailComponent } from "./components/solution-detail/solution-detail.component";
import { SettingsComponent } from "./components/settings/settings.component";
import { HistoryComponent } from "./components/history/history.component";
import { UserComponent } from "./components/user/user.component";
import { ApplicationdeploymentpathService } from "./shared/services/applicationdeploymentpath.service";
import { RoleGuard } from "./shared/guards/role.guard";
import { Router } from "@angular/router";
import { LogPipe } from "./shared/pipes/log.pipe";
import { ConfigureDeploymentComponent } from "./components/configure-deployment/configure-deployment.component";
import { ConnectionReferenceEnvironmentService } from "./shared/services/connectionreferenceenvironment.service";
import { ConnectionReferenceService } from "./shared/services/connectionreference.service";
import { EnvironmentVariableService } from "./shared/services/environmentvariable.service";
import { SideNavigationMenuComponent } from './components/side-navigation-menu/side-navigation-menu.component';
import { FullNamePipe } from "./shared/pipes/full-name.pipe";
import { IsPatchPipe } from "./shared/pipes/is-patch.pipe";
import { IsPatchDeletablePipe } from "./shared/pipes/is-patch-deletable.pipe";

export function initializeAppConfig(appConfig: AppConfig, router: Router) {
  return () => appConfig.load();
}

@NgModule({
  declarations: [
    AppComponent,
    HomeComponent,
    ProfileComponent,
    EnvironmentComponent,
    ApplicationComponent,
    SolutionsOverviewComponent,
    DeploymentpathComponent,
    ActionDetailComponent,
    SolutionDetailComponent,
    HistoryComponent,
    SettingsComponent,
    UserComponent,
    ConfigureDeploymentComponent,  
    LogPipe, 
    SideNavigationMenuComponent, 
    FullNamePipe,
    IsPatchPipe,
    IsPatchDeletablePipe
  ],
  imports: [
    BrowserModule.withServerTransition({ appId: "ng-cli-universal" }),
    HttpClientModule,
    FormsModule,
    AppRoutingModule,
    MsalModule,
    DxDataGridModule,
    DxDrawerModule,
    DxListModule,
    DxToolbarModule,
    DxLoadPanelModule,
    DxSelectBoxModule,
    DxTreeListModule,
    DxSortableModule,
    DxTreeViewModule,
    DxButtonModule,
    DxSelectBoxModule,
    DxFormModule,
    DxLookupModule,
    DxPopupModule,
    DxTextBoxModule,
    DxTreeViewModule,
    DxSortableModule,
    DxTextBoxModule,
    DxTextAreaModule,
    DxScrollViewModule,
    DxContextMenuModule,
    DxResponsiveBoxModule 
  ],
  providers: [
    AppConfig,
    LogService,
    {
      provide: APP_INITIALIZER,
      useFactory: initializeAppConfig,
      deps: [AppConfig, Router],
      multi: true,
    },
    {
      provide: MSAL_INSTANCE,
      useFactory: MSALInstanceFactory,
    },
    {
      provide: MSAL_GUARD_CONFIG,
      useFactory: MSALGuardConfigFactory,
    },
    {
      provide: MSAL_INTERCEPTOR_CONFIG,
      useFactory: MSALInterceptorConfigFactory,
    },
    {
      provide: HTTP_INTERCEPTORS,
      useClass: ProtectedResourcesInterceptor,
      multi: true,
    },
    {
      provide: HTTP_INTERCEPTORS,
      useClass: MsalInterceptor,
      multi: true,
    },
    MsalService,
    MsalGuard,
    MsalBroadcastService,
    RoleGuard,
    ODataService,
    UserService,
    LayoutService,
    ActionService,
    ApplicationService,
    EnvironmentService,
    PatchService,
    TenantService,
    UpgradeService,
    SolutionService,
    PublisherService,
    DeploymentpathService,
    ApplicationdeploymentpathService,
    ConnectionReferenceEnvironmentService,
    ConnectionReferenceService,
    EnvironmentVariableService,
  ],
  bootstrap: [AppComponent, MsalRedirectComponent],
})
export class AppModule {
  constructor(httpClient: HttpClient) {
    devextremeAjax.inject({ sendRequest: sendRequestFactory(httpClient) });
  }
}
