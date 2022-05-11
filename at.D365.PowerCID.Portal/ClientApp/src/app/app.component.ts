import { Component, OnInit } from "@angular/core";
import { Router } from "@angular/router";
import { LayoutService } from "./shared/services/layout.service";
import notify from "devextreme/ui/notify";
import { UserService } from "./shared/services/user.service";
import { AppInfoService } from "./shared/services/app-info.service";
import Button from "devextreme/ui/button"

@Component({
  selector: "app-root",
  templateUrl: "./app.component.html",
  styleUrls: ["./app.component.css"],
})
export class AppComponent implements OnInit {

  showLoading: boolean;
  loadingMessage: string;
  isDrawerOpen: boolean = true;
  toolbarContent = [];
  isLoggedIn : boolean;
  userName : string;

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
 
  ngOnInit(): void {
    this.toolbarContent = this.generateToolbarContent(false);
    this.userName = this.getUserName();
    this.userService.stateChanged$.subscribe(() => {
      this.toolbarContent = this.generateToolbarContent(this.userService.isLogggedIn);
      this.userName = this.getUserName();
    });    
  }

  onContentReady(e){
    let buttonElement = document.querySelector(".dx-toolbar-menu-container .dx-dropdownmenu-button");
    let instance = Button.getInstance(buttonElement) as Button;  
    instance.option("type", "default");
    instance.option("visible", this.userService.isLogggedIn);
    instance.option("text", this.userName);
    instance.option("hint", "Accountmanager for " + this.userName);
  } 

  generateToolbarContent(isLoggedIn: boolean){  
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
          locateInMenu:"always",     
          options: {
            text: 'Profile',
            visible: isLoggedIn,     
            icon: "at-icon powercid-icon-benutzer",
            onClick: () => {
              this.router.navigate(['/profile']);
            }
          }
        },
        {    
          widget: "dxButton",
          locateInMenu:"always",
          options: {
            text: "Logout" ,
            visible: isLoggedIn, 
            icon: "hidepanel",    
            onClick: () => (this.logout()),    
          }  
        },
      ]
    return content;
  }

  getUserName(){
    let userName;
    if(this.userService.isLogggedIn && this.userService.currentDbUserWithTenant){  
      userName = this.userService.currentDbUserWithTenant.Firstname + " " + this.userService.currentDbUserWithTenant.Lastname;
    }
    return userName;
  }

  async logout() {
    await this.userService.logout();
  }
}