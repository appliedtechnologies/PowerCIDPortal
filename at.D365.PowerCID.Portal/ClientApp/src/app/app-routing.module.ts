import { NgModule } from "@angular/core";
import { Routes, RouterModule } from "@angular/router";
import { MsalGuard } from "@azure/msal-angular";
import { ApplicationComponent } from "./components/application/application.component";
import { DeploymentpathComponent } from "./components/deploymentpath/deploymentpath.component";
import { EnvironmentComponent } from "./components/environment/environment.component";
import { HomeComponent } from "./components/home/home.component";
import { SolutionDetailComponent } from "./components/solution-detail/solution-detail.component";
import { ProfileComponent } from "./components/profile/profile.component";
import { SolutionsOverviewComponent } from "./components/solutions-overview/solutions-overview.component";
import { HistoryComponent } from "./components/history/history.component";
import { SettingsComponent } from "./components/settings/settings.component";
import { UserComponent } from "./components/user/user.component";
import { RoleGuard } from "./shared/guards/role.guard";
import { AppConfig } from "./shared/config/app.config";

const routes: Routes = [
  { path: "", component: HomeComponent, pathMatch: "full" },
  { 
    path: "profile", 
    component: ProfileComponent,
  },
  { 
    path: "deploymentpaths", 
    component: DeploymentpathComponent, 
    canActivate: [MsalGuard, RoleGuard], 
    data: {
      expectedRoles: ["atPowerCID.Admin", "atPowerCID.Manager"],
    }, 
  },
  {
    path: "environments",
    component: EnvironmentComponent,
    canActivate: [MsalGuard, RoleGuard],
    data: {
      expectedRoles: ["atPowerCID.Admin", "atPowerCID.Manager"],
    },
  },
  {
    path: "applications",
    component: ApplicationComponent,
    canActivate: [MsalGuard, RoleGuard],
    data: {
      expectedRoles: ["atPowerCID.Admin", "atPowerCID.Manager"],
    },
  },
  {
    path: "solutions",
    component: SolutionsOverviewComponent,
    canActivate: [MsalGuard, RoleGuard],
    data: {
      expectedRoles: [
        "atPowerCID.Admin",
        "atPowerCID.Manager",
        "atPowerCID.User",
      ],
    },
  },
  {
    path: "settings",
    component: SettingsComponent,
    canActivate: [MsalGuard, RoleGuard],
    data: {
      expectedRoles: ["atPowerCID.Admin"],
    },
  },
  {
    path: "history",
    component: HistoryComponent,
    canActivate: [MsalGuard, RoleGuard],
    data: {
      expectedRoles: ["atPowerCID.Admin", "atPowerCID.Manager"],
    },
  },
  {
    path: "users",
    component: UserComponent,
    canActivate: [MsalGuard, RoleGuard],
    data: {
      expectedRoles: ["atPowerCID.Admin"],
    },
  },
  
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule],
})
export class AppRoutingModule {}
