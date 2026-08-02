import { HttpClient } from "@angular/common/http";
import { Injectable } from "@angular/core";
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
