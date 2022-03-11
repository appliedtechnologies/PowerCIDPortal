import { environment } from "../../../environments/environment";
import { HttpBackend, HttpClient } from "@angular/common/http";
import { Injectable } from "@angular/core";
import { IAppConfig } from "./app-config.model";

@Injectable()
export class AppConfig {
  static settings: IAppConfig;

  constructor(private httpBackend: HttpBackend) { }

  load() {
    const jsonFile = `assets/config/config.${environment.name}.json`;
    return new Promise<void>((resolve, reject) => {
      new HttpClient(this.httpBackend).get(jsonFile).toPromise().then((response: IAppConfig) => {
        AppConfig.settings = <IAppConfig>response;
        resolve();
      }).catch((response: any) => {
        reject(`Could not load file '${jsonFile}': ${JSON.stringify(response)}`);
      });
    });
  }
}
