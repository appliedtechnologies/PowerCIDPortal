import { Input } from "@angular/core";
import { Component, OnInit } from "@angular/core";
import DevExpress from "devextreme";
import { Button } from "protractor";
import { UserService } from "src/app/shared/services/user.service";

@Component({
  selector: "app-home",
  templateUrl: "./home.component.html",
  styleUrls: ["./home.component.css"]
})
export class HomeComponent implements OnInit {
  isLoggedIn: boolean;

  constructor(
    private userService: UserService,
  ) {}

  ngOnInit(): void {
    this.checkLogInStatus(this.userService.isLogggedIn)
    this.userService.stateChanged$.subscribe(() => {
      this.checkLogInStatus(this.userService.isLogggedIn)
    }); 
  }

  checkLogInStatus(loginStatus: boolean){
    this.isLoggedIn = loginStatus
  }

  login() {
    this.userService.login();
  }
}
