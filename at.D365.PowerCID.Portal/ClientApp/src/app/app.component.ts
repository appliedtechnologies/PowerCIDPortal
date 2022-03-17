import { Component, Input, OnInit, Output, ViewChild } from "@angular/core";
import { NavigationEnd, Router } from "@angular/router";
import { filter } from "rxjs/operators";
import { AppConfig } from "./shared/config/app.config";
import { LayoutService } from "./shared/services/layout.service";
import notify from "devextreme/ui/notify";
import { UserService } from "./shared/services/user.service";
import { User } from "src/app/shared/models/user.model";
import { DxScrollViewComponent, DxTreeViewComponent } from "devextreme-angular";
import { AppInfoService } from "./shared/services/app-info.service";

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
  @ViewChild(DxTreeViewComponent, { static: false }) menu: DxTreeViewComponent;

  public  selectedRoute = '';
  public showLoading: boolean;
  public loadingMessage: string;
  public isDrawerOpen: boolean = true;
  public verion: string = AppConfig.settings.version;
  public selectedNavigationEntryRouterLink;
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

  constructor(private router: Router, private layoutService: LayoutService, private userService: UserService, public appInfo: AppInfoService ) {
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
}