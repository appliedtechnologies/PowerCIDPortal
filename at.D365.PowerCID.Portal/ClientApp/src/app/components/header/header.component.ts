import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { Router } from '@angular/router';
import { UserService } from 'src/app/shared/services/user.service';

@Component({
  selector: 'app-header',
  templateUrl: './header.component.html',
  styleUrls: ['./header.component.css']
})
export class HeaderComponent implements OnInit {
  @Output()
  menuToggle = new EventEmitter<boolean>();

  @Input()
  menuToggleEnabled = false;

  @Input()
  title: string;

  constructor(private userService: UserService, private router: Router) { }

  user = { email: '' };
  userObj : any;
  isLoggedIn = false;

  userMenuItems = [{
    text: 'Profile',
    icon: "at-icon powercid-icon-benutzer",
    onClick: () => {
      this.router.navigate(['/profile']);
    }
  },
  {
    text: 'Logout',
    icon: "hidepanel",
    onClick: () => {
      this.userService.logout();
    }
  }];

  private getUserInformation(){
    this.isLoggedIn = this.userService.isLogggedIn;
    if(this.isLoggedIn && this.userService.currentIdentityUser){
      this.userObj = this.userService.currentIdentityUser;    
    }
  }

  ngOnInit() {
    this.getUserInformation();
    this.user.email = this.userObj.name;
    this.userService.stateChanged$.subscribe(() => {
      this.getUserInformation();
      this.user.email = this.userObj.name;
    }); 
  }

  toggleMenu = () => {
    this.menuToggle.emit();
  }
}
