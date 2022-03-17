import { Component, NgModule, Output, Input, EventEmitter, ViewChild, ElementRef, AfterViewInit, OnDestroy } from '@angular/core';
import { DxTreeViewModule, DxTreeViewComponent } from 'devextreme-angular/ui/tree-view';
import { UserService } from "src/app/shared/services/user.service";

import * as events from 'devextreme/events';
import { AppConfig } from 'src/app/shared/config/app.config';
import { NavigationEnd, Router } from '@angular/router';
import { filter } from 'rxjs/operators';

@Component({
  selector: 'app-side-navigation-menu',
  templateUrl: './side-navigation-menu.component.html',
  styleUrls: ['./side-navigation-menu.component.css']
})
export class SideNavigationMenuComponent {
  @ViewChild(DxTreeViewComponent, { static: true })
  menu: DxTreeViewComponent;

  public navigationEntries: NavigationEntry[];

  private _selectedItem: String;
  @Input()
  set selectedItem(value: String) {
    this._selectedItem = value;
    if (!this.menu.instance) {
      return;
    }

    this.menu.instance.selectItem(value);
  }

  constructor(private router: Router,private elementRef: ElementRef, private userService: UserService) {
    this.router.events
      .pipe(filter((event) => event instanceof NavigationEnd))
      .subscribe((event) => {
        this.selectedItem = this.router.url.split(/[\#\?]+/)[0];
      });

    this.setNavigationEntries();
    this.userService.stateChanged$.subscribe(() => {
      this.setNavigationEntries();
    });
  }

  private setNavigationEntries(): void{
    this.navigationEntries = [
      {
        text: "Home",
        icon: "home",
        routerLink: "/",
        visible: true
      },
      {
        text: "Profile",
        icon: "at-icon powercid-icon-benutzer",
        routerLink: "/profile",
        visible: this.userService.isLogggedIn
      },
      {
        text: "Solutions Overview",
        icon: "at-icon powercid-icon-code-file",
        routerLink: "/solutions",
        visible: this.userService.isLogggedIn && this.userService.currentUserRoles && this.userService.currentUserRoles.some(e => [AppConfig.settings.azure.roleNameAdmin, AppConfig.settings.azure.roleNameManager, AppConfig.settings.azure.roleNameUser].includes(e))
      },
      {
        text: "Deployment History",
        icon: "at-icon powercid-icon-vergangenheit",
        routerLink: "/history",
        visible: this.userService.isLogggedIn && this.userService.currentUserRoles && this.userService.currentUserRoles.some(e => [AppConfig.settings.azure.roleNameAdmin, AppConfig.settings.azure.roleNameManager].includes(e))
      },
      {
        text: "App Settings",
        icon: "at-icon powercid-icon-einstellungen",
        expanded: true,
        visible: this.userService.isLogggedIn && this.userService.currentUserRoles && this.userService.currentUserRoles.some(e => [AppConfig.settings.azure.roleNameAdmin, AppConfig.settings.azure.roleNameManager].includes(e)),
        items: [
        {
          text: "Applications",
          icon: "at-icon powercid-icon-web-design",
          routerLink: "/applications",
          visible: this.userService.isLogggedIn && this.userService.currentUserRoles && this.userService.currentUserRoles.some(e => [AppConfig.settings.azure.roleNameAdmin, AppConfig.settings.azure.roleNameManager].includes(e))
        },
        {
          text: "Environments",
          icon: "at-icon powercid-icon-unit",
          routerLink: "/environments",
          visible: this.userService.isLogggedIn && this.userService.currentUserRoles && this.userService.currentUserRoles.some(e => [AppConfig.settings.azure.roleNameAdmin, AppConfig.settings.azure.roleNameManager].includes(e))
        },
        {
          text: "Deployment Paths",
          icon: "at-icon powercid-icon-route",
          routerLink: "/deploymentpaths",
          visible: this.userService.isLogggedIn && this.userService.currentUserRoles && this.userService.currentUserRoles.some(e => [AppConfig.settings.azure.roleNameAdmin, AppConfig.settings.azure.roleNameManager].includes(e))
        },
      {
        text: "Users",
        icon: "at-icon powercid-icon-conference-hintergrund-ausgew-hlte",
        routerLink: "/users",
        visible: this.userService.isLogggedIn && this.userService.currentUserRoles && this.userService.currentUserRoles.includes(AppConfig.settings.azure.roleNameAdmin)
      },
      {
        text: "Settings",
        icon: "at-icon powercid-icon-einstellungen",
        routerLink: "/settings",
        visible: this.userService.isLogggedIn && this.userService.currentUserRoles && this.userService.currentUserRoles.includes(AppConfig.settings.azure.roleNameAdmin)
      }]}
    ];

    this.navigationEntries.forEach(e => {
      if(e.visible === undefined)
        e.visible = false;
    });
  }

  public onItemClickNavigation(e): void {
    this.router.navigate([e.itemData.routerLink]);   
  }
}


export class NavigationEntry {
  text: string;

  visible: boolean;

  icon: string;

  routerLink?: string;

  expanded?: boolean;

  items?: NavigationEntry[]; 
}