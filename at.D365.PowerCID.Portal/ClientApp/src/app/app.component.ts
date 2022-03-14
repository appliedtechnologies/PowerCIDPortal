import { Component, Input, OnInit, ViewChild } from "@angular/core";
import { NavigationEnd, Router } from "@angular/router";
import { filter } from "rxjs/operators";
import { AppConfig } from "./shared/config/app.config";
import { LayoutService } from "./shared/services/layout.service";
import notify from "devextreme/ui/notify";
import { UserService } from "./shared/services/user.service";
import { User } from "src/app/shared/models/user.model";
import { ScreenService } from "./shared/services/screen.service";
import { DxScrollViewComponent } from "devextreme-angular";
import { navigation } from "./app-navigation";
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
  @ViewChild(DxScrollViewComponent, { static: true }) scrollView: DxScrollViewComponent;
  selectedRoute = '';

  menuOpened: boolean;
  temporaryMenuOpened = false;

  @Input()
  title: string;

  menuMode = 'shrink';
  menuRevealMode = 'expand';
  minMenuSize = 0;
  shaderEnabled = false;

  public showLoading: boolean;
  public loadingMessage: string;
  public isDrawerOpen: boolean = true;
  public verion: string = AppConfig.settings.version;
  public selectedNavigationEntryRouerLink;
  public toolbarContent = [];
    
  ngOnInit(): void {
    this.menuOpened = this.screen.sizes['screen-large'];

    this.router.events.subscribe(val => {
      if (val instanceof NavigationEnd) {
        this.selectedRoute = val.urlAfterRedirects.split('?')[0];
      }
    });

    this.screen.changed.subscribe(() => this.updateDrawer());

    this.updateDrawer();

    this.toolbarContent = this.generateToolbarContent(false);
    this.userService.stateChanged$.subscribe(() => {
      this.toolbarContent = this.generateToolbarContent(this.userService.isLogggedIn);
    });    
  }

  updateDrawer() {
    const isXSmall = this.screen.sizes['screen-x-small'];
    const isLarge = this.screen.sizes['screen-large'];

    this.menuMode = isLarge ? 'shrink' : 'overlap';
    this.menuRevealMode = isXSmall ? 'slide' : 'expand';
    this.minMenuSize = isXSmall ? 0 : 60;
    this.shaderEnabled = !isLarge;
  }

  get hideMenuAfterNavigation() {
    return this.menuMode === 'overlap' || this.temporaryMenuOpened;
  }

  get showMenuAfterClick() {
    return !this.menuOpened;
  }

  navigationChanged(event) {
    const path = event.itemData.path;
    const pointerEvent = event.event;

    if (path && this.menuOpened) {
      if (event.node.selected) {
        pointerEvent.preventDefault();
      } else {
        this.router.navigate([path]);
        this.scrollView.instance.scrollTo(0);
      }

      if (this.hideMenuAfterNavigation) {
        this.temporaryMenuOpened = false;
        this.menuOpened = false;
        pointerEvent.stopPropagation();
      }
    } else {
      pointerEvent.preventDefault();
    }
  }

  navigationClick() {
    if (this.showMenuAfterClick) {
      this.temporaryMenuOpened = true;
      this.menuOpened = true;
    }
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



  constructor(private router: Router, private layoutService: LayoutService, private userService: UserService, private screen: ScreenService, public appInfo: AppInfoService ) {
    this.router.events
      .pipe(filter((event) => event instanceof NavigationEnd))
      .subscribe((event) => {
        this.selectedNavigationEntryRouerLink = [this.router.url.split(/[\#\?]+/)[0]];
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
}
