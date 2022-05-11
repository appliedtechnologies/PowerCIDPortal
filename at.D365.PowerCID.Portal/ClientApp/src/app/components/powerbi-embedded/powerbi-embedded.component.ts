import { Component, ViewChild, ElementRef, OnInit, HostListener } from '@angular/core'; 
import { MsalService } from '@azure/msal-angular';
import { ResponseMode } from '@azure/msal-common';
import { IReportEmbedConfiguration, models, Page, Report, service, VisualDescriptor } from 'powerbi-client';
import { PowerBIReportEmbedComponent } from 'powerbi-client-angular';  
import { UserService } from 'src/app/shared/services/user.service';

@Component({
	selector: "app-powerbi-embedded",
	templateUrl: "./powerbi-embedded.component.html",
	styleUrls: ["./powerbi-embedded.component.css"],
  })
  
	export class PowerBIEmbeddedComponent implements OnInit{
		ngOnInit(): void {
			this.authService.acquireTokenSilent({
				scopes:["https://analysis.windows.net/powerbi/api/Report.Read.All"],
				account: this.userService.currentIdentityUser


			}).subscribe({
				next:(response) => this.reportConfig = {
					type: 'report',
					embedUrl: "https://app.powerbi.com/reportEmbed?reportId=50bda0a0-6257-4bf8-a183-3110b9f14277",
					tokenType: models.TokenType.Aad,
					accessToken: response.accessToken,					
					settings: {
						layoutType: models.LayoutType.Custom,
						customLayout: {
							displayOption: models.DisplayOption.FitToPage
						}
					},
				  }
			})
		}  
	  
		constructor(private authService:MsalService, private userService:UserService){}

		reportConfig: IReportEmbedConfiguration = {	type: 'report'}
	}  