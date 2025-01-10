import { Action } from "./action.model";
import { Application } from "./application.model";
import { User } from "./user.model";

export interface Solution {
  Id?: number;
  MsId?: string;
  Name?: string;
  Description?: string;
  Application?: number;
  Version?: string;
  UrlMakerportal?: string;
  UniqueName?: string;
  CreatedBy?: number;
  CreatedOn?: string;
  ModifiedBy?: number;
  ModifiedOn?: string;
  ApplicationNavigation?: Application;
  CreatedByNavigation?: User;
	ModifiedByNavigation?: User;
	Actions?: Action[];
	OverwriteUnmanagedCustomizations?: boolean;
	EnableWorkflows?: boolean;
}
