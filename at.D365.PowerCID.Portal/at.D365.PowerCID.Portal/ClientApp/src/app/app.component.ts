import { Component, OnInit } from "@angular/core";
import { NavigationEnd, Router } from "@angular/router";
import { filter } from "rxjs/operators";
import { AppConfig } from "./shared/config/app.config";
import { LayoutService } from "./shared/services/layout.service";
import notify from "devextreme/ui/notify";
import { UserService } from "./shared/services/user.service";
import { User } from "src/app/shared/models/user.model";

export class List {
  id: number;
  text: string;
  icon: string;
}

@Component({
  selector: "app-root",
  templateUrl: "./app.component.html",
  styleUrls: ["./app.component.css"],
})
export class AppComponent implements OnInit {
  public showLoading: boolean;
  public loadingMessage: string;
  public isDrawerOpen: boolean = true;
  public verion: string = AppConfig.settings.version;
  public selectedNavigationEntryRouerLink;
  public toolbarContent = [];
    
  ngOnInit(): void {
    this.toolbarContent = this.generateToolbarContent(false);
    this.userService.stateChanged$.subscribe(() => {
      this.toolbarContent = this.generateToolbarContent(this.userService.isLogggedIn);
    });    
  }

  public generateToolbarContent(showLogoutButton: boolean){  
    let content =  [      
      {
      widget: "dxButton",
      location: "before",
      options: {
        icon: "menu",
        onClick: () => (this.isDrawerOpen = !this.isDrawerOpen),
        hint: "Toggle menu",
        type: "default",
      },
    },
    {
      html: "<strong>Power CID Portal</strong>",
      location: "center",
    },
    {    
      widget: "dxButton",
      location: "after",
      options: {
        onClick: () => (this.logout()),
        icon: "hidepanel",
        visible: showLogoutButton,
        text: "Logout" ,
        hint: "Logout",
        type: "default"       
      }  
    },

  ];
    if(showLogoutButton){
      let welcomeUser = {
          html: "<div id='welcome-user'>Hello "+ this.userService.currentDbUserWithTenant?.Firstname + "!<div>",
          location: "after"
        }     
      content.splice(2, 0, welcomeUser)
    }
    return content;
  }
  
  async logout() {
    await this.userService.logout();
  }

  public navigationEntries: any[];

  constructor(private router: Router, private layoutService: LayoutService, private userService: UserService) {
    this.router.events
      .pipe(filter((event) => event instanceof NavigationEnd))
      .subscribe((event) => {
        this.selectedNavigationEntryRouerLink = [this.router.url.split(/[\#\?]+/)[0]];
      });

    this.setNavigationEntries();
    this.userService.stateChanged$.subscribe(() => {
      this.setNavigationEntries();
    });

    //get layout values from layout service
    this.layoutService.showLoading$.subscribe(
      (show) => (this.showLoading = show)
    );
    this.layoutService.loadingMessage$.subscribe(
      (message) => (this.loadingMessage = message)
    );

    //get notifications
    this.layoutService.notifications$.subscribe((notification) =>
      notification.message !== undefined
        ? notify(
            notification.message,
            notification.type,
            notification.displayTime
          )
        : notify(
            notification.options,
            notification.type,
            notification.displayTime
          )
    );
  }

  public onItemClickDrawerNavigation(e): void {
    this.router.navigate([e.itemData.routerLink]);
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
      }
    ];

    this.navigationEntries.forEach(e => {
      if(e.visible === undefined)
        e.visible = false;
    });
  }
}
