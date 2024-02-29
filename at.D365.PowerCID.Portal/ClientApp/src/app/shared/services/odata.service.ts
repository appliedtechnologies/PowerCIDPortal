import { Injectable } from "@angular/core";
import { LogService } from "./log.service";
import { AppConfig } from "../config/app.config";
import { Router } from "@angular/router";
import { UserService } from "./user.service";
import ODataContext from "devextreme/data/odata/context";

@Injectable()
export class ODataService {
  context: ODataContext;
  constructor(private router: Router, private logService: LogService) {
    this.context = new ODataContext({
      beforeSend: (e) => {},
      url: AppConfig.settings.api.url,
      version: 4,
      errorHandler: (error) => {
        this.logService.error(
          error.errorDetails,
          "error occurred while odata request"
        );
      },
      entities: {
        Actions: {
          key: "Id",
          keyType: "Int32",
        },
        ActionStatus: {
          key: "Id",
          keyType: "Int32",
        },
        ActionResults: {
          key: "Id",
          keyType: "Int32",
        },
        Applications: {
          key: "Id",
          keyType: "Int32",
        },
        Environments: {
          key: "Id",
          keyType: "Int32",
        },
        Patches: {
          key: "Id",
          keyType: "Int32",
        },
        Tenants: {
          key: "Id",
          keyType: "Int32",
        },
        Upgrades: {
          key: "Id",
          keyType: "Int32",
        },
        Users: {
          key: "Id",
          keyType: "Int32",
          fieldTypes: {
            MsId: "Guid"
          }
        },
        Solutions: {
          key: "Id",
          keyType: "Int32",
        },
        DeploymentPaths: {
          key: "Id",
          keyType: "Int32",
        },
        DeploymentPathEnvironments: {
          key: ["DeploymentPath", "Environment"],
          keyType: {
            DeploymentPath: "Int32",
            Environment: "Int32",
          },
        },
        ApplicationDeploymentPaths: {
          key: ["Application", "DeploymentPath"],
          keyType: {
            Application: "Int32",
            DeploymentPath: "Int32",
          },
        },
        UserEnvironments: {
          key: ["User", "Environment"],
          keyType: {
            User: "Int32",
            Environment: "Int32",
          },
        },
        ConnectionReferences: {
          key: "Id",
          keyType: "Int32",
        },
        ConnectionReferenceEnvironments: {
          key: ["ConnectionReference", "Environment"],
          keyType: {
            ConnectionReference: "Int32",
            Environment: "Int32",
          },
        },
        EnvironmentVariables: {
          key: "Id",
          keyType: "Int32",
        },
        EnvironmentVariableEnvironments: {
          key: ["Environment", "EnvironmentVariable"],
          keyType: {
            EnvironmentVariable: "Int32",
            Environment: "Int32",
          },
        },
      },
    });
  }

  public useDate(date: Date): Date {
    return new Date(date.toUTCString().substr(0, 25));
  }
}
