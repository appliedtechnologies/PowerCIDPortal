import { HttpClient } from "@angular/common/http";
import { Injectable } from "@angular/core";
import { Observable } from "rxjs";
import { Environment } from "../models/environment.model";
import { Publisher } from "../models/publisher.model";
import { ODataService } from "./odata.service";

@Injectable({
  providedIn: "root",
})
export class PublisherService {
  constructor(private odataService: ODataService, private http: HttpClient) {}

  getStore() {
    return this.odataService.context["Publishers"];
  }
}
