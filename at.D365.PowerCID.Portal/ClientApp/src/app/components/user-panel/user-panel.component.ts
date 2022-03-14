import { Component, Input, OnInit } from '@angular/core';

@Component({
  selector: 'app-user-panel',
  templateUrl: './user-panel.component.html',
  styleUrls: ['./user-panel.component.css']
})
export class UserPanelComponent implements OnInit {
  @Input()
  menuItems: any;

  @Input()
  menuMode: string;

  @Input()
  user: { email: string };

  constructor() { }

  ngOnInit(): void {
  }

}
