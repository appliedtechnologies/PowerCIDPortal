import { User } from "./user.model";
import { ActionResult } from "./actionresult.model";
import { ActionStatus } from "./actionstatus.model";
import { ActionType } from "./actiontype.model";
import { Environment } from "./environment.model";
import { Solution } from "./solution.model";

export interface Action {
  Id?: number;
  Name?: string;
  TargetEnvironment?: number;
  Type?: number;
  Status?: number;
  Result?: number;
  StartTime?: string;
  FinishTime?: string;
  Solution?: number;
  ErrorMessage?: string;
  CreatedBy?: number;
  CreatedOn?: string;
  CreatedByNavigation?: User;
  SolutionNavigation?: Solution;
  ResultNavigation?: ActionResult;
  StatusNavigation?: ActionStatus;
  TargetEnvironmentNavigation?: Environment;
  TypeNavigation?: ActionType;
  AsyncOperationId?: string;
  JobId?: string;
}
