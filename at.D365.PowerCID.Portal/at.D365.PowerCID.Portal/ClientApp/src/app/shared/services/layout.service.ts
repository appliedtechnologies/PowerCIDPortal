import { Injectable } from "@angular/core";
import { Subject } from "rxjs";
import { custom } from "devextreme/ui/dialog";
import { LogService } from "./log.service";


@Injectable()
export class LayoutService {

  //subjects
  private showLoadingSource = new Subject<boolean>();
  private loadingMessageSource = new Subject<string>();
  private notificationsSource = new Subject<Notification>();

  //observables
  public showLoading$ = this.showLoadingSource.asObservable();
  public loadingMessage$ = this.loadingMessageSource.asObservable();
  public notifications$ = this.notificationsSource.asObservable();

  constructor(private logService: LogService) {
    //set default values
    this.change(LayoutParameter.ShowLoading, false);
    this.change(LayoutParameter.LoadingMessage, "Loading...");
  }

  public change(valueToChange: LayoutParameter, value: any): void {
    Promise.resolve(null).then(() => {
      switch (valueToChange) {
        case LayoutParameter.ShowLoading:
          this.showLoadingSource.next(value);
          break;
        case LayoutParameter.LoadingMessage:
          this.loadingMessageSource.next(value);
          if (value === false)
            this.change(LayoutParameter.LoadingMessage, "Loding...");
          break;
        default:
          this.logService.error("unknow layout parameter");
      }
    });
  }

  public notify(notification: Notification) {
    if (notification.displayTime === undefined)
      notification.displayTime = 5000; //default 5 secounds
    this.notificationsSource.next(notification)
  }

  public confirmUnsavedChanged(): Promise<boolean> {
    let dialog = custom({
      dragEnabled: false,
      messageHtml: "There are unsaved changes that will be lost if you continue. Are you sure you want this?",
      title: "Discard changes?",
      buttons: [{
        text: "yes",
        onClick: () => {
          return true;
        }
      },
      {
        text: "no",
        onClick: () => {
          return false;
        }
      }]
    });

    return dialog.show() as Promise<boolean>;
  }
}

export enum LayoutParameter {
  ShowLoading,
  LoadingMessage
}

export interface Notification {
  message?: string,
  type: NotificationType,
  displayTime?: number,
  options?: any
}

export enum NotificationType {
  Custom = 'custom',
  Error = 'error',
  Info = 'info',
  Success = 'success',
  Warning = 'warning'
}
