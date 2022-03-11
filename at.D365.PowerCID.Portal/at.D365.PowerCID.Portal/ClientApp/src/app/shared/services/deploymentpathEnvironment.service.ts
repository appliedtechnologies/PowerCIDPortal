import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import ODataStore from 'devextreme/data/odata/store';
import { AppConfig } from '../config/app.config';
import { DeploymentPath } from '../models/deploymentpath.model';
import { ODataService } from './odata.service';

@Injectable({
  providedIn: 'root'
})
export class DeploymentPathEnvironmentService {

  constructor(private odataService: ODataService,
    private http: HttpClient) {}

  public getStore(): ODataStore {
    return this.odataService.context["DeploymentPathEnvironments"];
}


}
