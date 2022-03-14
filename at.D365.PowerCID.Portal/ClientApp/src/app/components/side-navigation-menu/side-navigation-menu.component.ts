import { Component, NgModule, Output, Input, EventEmitter, ViewChild, ElementRef, AfterViewInit, OnDestroy } from '@angular/core';
import { DxTreeViewModule, DxTreeViewComponent } from 'devextreme-angular/ui/tree-view';
import { UserService } from "src/app/shared/services/user.service";

import * as events from 'devextreme/events';
import { navigation } from 'src/app/app-navigation';
import { AppConfig } from 'src/app/shared/config/app.config';

@Component({
  selector: 'app-side-navigation-menu',
  templateUrl: './side-navigation-menu.component.html',
  styleUrls: ['./side-navigation-menu.component.css']
})
export class SideNavigationMenuComponent implements AfterViewInit, OnDestroy {
  @ViewChild(DxTreeViewComponent, { static: true })
  menu: DxTreeViewComponent;

  @Output()
  selectedItemChanged = new EventEmitter<string>();

  @Output()
  openMenu = new EventEmitter<any>();

  private _selectedItem: String;
  @Input()
  set selectedItem(value: String) {
    this._selectedItem = value;
    if (!this.menu.instance) {
      return;
    }

    this.menu.instance.selectItem(value);
  }

  private _items;
  get items() {
    if (!this._items) {
      this._items = navigation.map((item) => {       
        if(item.path && !(/^\//.test(item.path))){ 
          item.path = `/${item.path}`;
        }
        if(item.id == "user"){
          item.visible = this.userService.isLogggedIn
        }
        if(item.id == "manager"){
          item.visible = this.userService.isLogggedIn && this.userService.currentUserRoles && this.userService.currentUserRoles.some(e => [AppConfig.settings.azure.roleNameAdmin, AppConfig.settings.azure.roleNameManager].includes(e))
        }
        if(item.id == "admin"){
          item.visible = this.userService.isLogggedIn && this.userService.currentUserRoles && this.userService.currentUserRoles.includes(AppConfig.settings.azure.roleNameAdmin)
        }       

        if(item.items){
          this._items = item.items.map((items) => {       
            if(items.id == "user"){
              items.visible = this.userService.isLogggedIn
            }
            if(items.id == "manager"){
              items.visible = this.userService.isLogggedIn && this.userService.currentUserRoles && this.userService.currentUserRoles.some(e => [AppConfig.settings.azure.roleNameAdmin, AppConfig.settings.azure.roleNameManager].includes(e))
            }
            if(item.id == "admin"){
              items.visible = this.userService.isLogggedIn && this.userService.currentUserRoles && this.userService.currentUserRoles.includes(AppConfig.settings.azure.roleNameAdmin)
            }                   
            });  
        }
        
         return { ...item, expanded: !this._compactMode }
        });  
    }
    return this._items;
  }

  private _compactMode = false;
  @Input()
  get compactMode() {
    return this._compactMode;
  }
  set compactMode(val) {
    this._compactMode = val;

    if (!this.menu.instance) {
      return;
    }

    if (val) {
      this.menu.instance.collapseAll();
    } else {
      this.menu.instance.expandItem(this._selectedItem);
    }
  }

  constructor(private elementRef: ElementRef, private userService: UserService) { }

  onItemClick(event) {
    this.selectedItemChanged.emit(event);
  }

  ngAfterViewInit() {
    events.on(this.elementRef.nativeElement, 'dxclick', (e) => {
      this.openMenu.next(e);
    });
  }

  ngOnDestroy() {
    events.off(this.elementRef.nativeElement, 'dxclick');
  }
}