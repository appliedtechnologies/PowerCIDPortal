import { Component, Input, OnInit } from "@angular/core";
import { Application } from "src/app/shared/models/application.model";
import { ConnectionReference } from "src/app/shared/models/connectionreference.model";
import { ConnectionReferenceEnvironment } from "src/app/shared/models/connectionreferenceenvironment.model";
import { Environment } from "src/app/shared/models/environment.model";
import { EnvironmentVariable } from "src/app/shared/models/environmentvariable.model";
import { EnvironmentVariableEnvironment } from "src/app/shared/models/environmentvariableenvironment.model";
import { ConnectionReferenceService } from "src/app/shared/services/connectionreference.service";
import { ConnectionReferenceEnvironmentService } from "src/app/shared/services/connectionreferenceenvironment.service";
import { EnvironmentService } from "src/app/shared/services/environment.service";
import { EnvironmentVariableService } from "src/app/shared/services/environmentvariable.service";
import { EnvironmentVariableEnvironmentService } from "src/app/shared/services/environmentvariableenvironment.service";
import { LayoutParameter, LayoutService, NotificationType } from "src/app/shared/services/layout.service";

@Component({
    selector: "app-configure-deployment",
    templateUrl: "./configure-deployment.component.html",
    styleUrls: ["./configure-deployment.component.css"],
})
export class ConfigureDeploymentComponent implements OnInit {
    @Input() environment: Environment;
    @Input() application: Application;

    public connectionReferencesFromDataverse: ConnectionReference[];
    public environmentVariablesFromDataverse: EnvironmentVariable[];
    public existingConnectionReferences: ConnectionReference[];
    public existingEnvironmentVariables: EnvironmentVariable[];

    constructor(private connectionReferenceService: ConnectionReferenceService, private layoutService: LayoutService, private connectionReferenceEnvironmentService: ConnectionReferenceEnvironmentService, private environmentVariableService: EnvironmentVariableService, private environmentVariableEnvironmentService: EnvironmentVariableEnvironmentService){}

    public ngOnInit(): void {
        this.layoutService.change(LayoutParameter.ShowLoading, true);
        let promiseConnectionRefernecesFromDataverse = this.connectionReferenceService.getFromDataverseForApplication(this.application.Id);
        let promiseConnectionReferencesExisting = this.connectionReferenceService.getExistingForApplication(this.application.Id);
        let promiseEnvironmentVariablesFromDataverse = this.environmentVariableService.getFromDataverseForApplication(this.application.Id);
        let promiseEnvironmentVariablesExisting = this.environmentVariableService.getExistingForApplication(this.application.Id);
        Promise.all([promiseConnectionRefernecesFromDataverse, promiseConnectionReferencesExisting, promiseEnvironmentVariablesFromDataverse, promiseEnvironmentVariablesExisting])
        .then((data) => {
            this.connectionReferencesFromDataverse = data[0]["value"];
            this.existingConnectionReferences = data[1];
            this.environmentVariablesFromDataverse = data[2]["value"];
            this.existingEnvironmentVariables = data[3];
        })
        .finally(() =>{
            this.layoutService.change(LayoutParameter.ShowLoading, false);
        });
    }

    public onValueChangedConnectionIdTextBox(e: any, connectionReference: ConnectionReference){
        if(e.value != e.previousValue){
            let connectionId = e.value;
            let existingConnectionReference: ConnectionReference = this.existingConnectionReferences.find(e => e.MsId == connectionReference.MsId);
            if(!existingConnectionReference){
                //create connection reference
                this.connectionReferenceService.add(connectionReference)    
                    .then((createdConnectionReference) => {
                        this.saveNewConnectionId(createdConnectionReference, connectionId).then((createdConnectionReferenceEnvironment) => {
                            createdConnectionReference.ConnectionReferenceEnvironments = [createdConnectionReferenceEnvironment];
                            this.existingConnectionReferences.push(createdConnectionReference);
                            this.layoutService.notify({type: NotificationType.Success, message: "Changes have been saved", displayTime: 1000})
                        });
                    });
            }
            else{
                if(existingConnectionReference.ConnectionReferenceEnvironments?.find(e => e.Environment == this.environment.Id)){
                    this.updateConnectionId(existingConnectionReference, connectionId).then(() => {
                        this.layoutService.notify({type: NotificationType.Success, message: "Changes have been saved", displayTime: 1000})
                    });
                }
                else{
                    this.saveNewConnectionId(existingConnectionReference, connectionId).then((createdConnectionReferenceEnvironment) => {
                        existingConnectionReference.ConnectionReferenceEnvironments = [createdConnectionReferenceEnvironment];
                        this.layoutService.notify({type: NotificationType.Success, message: "Changes have been saved", displayTime: 1000})
                    });
                }
            }
        }
    }

    public onValueChangedEnvVarValueTextBox(e: any, environmentVariable: EnvironmentVariable){
        if(e.value != e.previousValue){
            let envVarValue = e.value;
            let existingEnvironmentVariables: EnvironmentVariable = this.existingEnvironmentVariables.find(e => e.MsId == environmentVariable.MsId);
            if(!existingEnvironmentVariables){
                //create environment variable
                this.environmentVariableService.add(environmentVariable)    
                    .then((createdEnvironmentVariable) => {
                        this.saveNewEnvVarValue(createdEnvironmentVariable, envVarValue).then((createdEnvironmentVariableEnvironment) => {
                            createdEnvironmentVariable.EnvironmentVariableEnvironments = [createdEnvironmentVariableEnvironment];
                            this.existingEnvironmentVariables.push(createdEnvironmentVariable);
                            this.layoutService.notify({type: NotificationType.Success, message: "Changes have been saved", displayTime: 1000})
                        });
                    });
            }
            else{
                if(existingEnvironmentVariables.EnvironmentVariableEnvironments?.find(e => e.Environment == this.environment.Id)){
                    this.updateEnvVarValue(existingEnvironmentVariables, envVarValue).then(() => {
                        this.layoutService.notify({type: NotificationType.Success, message: "Changes have been saved", displayTime: 1000})
                    });
                }
                else{
                    this.saveNewEnvVarValue(existingEnvironmentVariables, envVarValue).then((createdEnvironmentVariableEnvironment) => {
                        existingEnvironmentVariables.EnvironmentVariableEnvironments = [createdEnvironmentVariableEnvironment];
                        this.layoutService.notify({type: NotificationType.Success, message: "Changes have been saved", displayTime: 1000})
                    });
                }
            }
        }
    }

    public onInitializedConnectionIdTextBox(e: any, connectionReference: ConnectionReference){
        let connectionId = this.existingConnectionReferences.find(e => e.MsId == connectionReference.MsId)?.ConnectionReferenceEnvironments?.find(e =>e.Environment == this.environment.Id)?.ConnectionId
        if(connectionId)
            e.component.option("value", connectionId);
    }

    public onInitializedEnvVarValueTextBox(e: any, environmentVariable: EnvironmentVariable){
        let envVarValue = this.existingEnvironmentVariables.find(e => e.MsId == environmentVariable.MsId)?.EnvironmentVariableEnvironments?.find(e =>e.Environment == this.environment.Id)?.Value
        if(envVarValue)
            e.component.option("value", envVarValue);
    }

    private updateEnvVarValue(environmentVariable: EnvironmentVariable, value: string){
        return this.environmentVariableEnvironmentService.update(environmentVariable.Id, this.environment.Id, {Value: value});
    }

    private saveNewEnvVarValue(environmentVariable: EnvironmentVariable, value: string): Promise<EnvironmentVariableEnvironment>{
        let environmentVariableEnvironment: EnvironmentVariableEnvironment = {
            EnvironmentVariable: environmentVariable.Id,
            Environment: this.environment.Id,
            Value: value
        }; 

        return this.environmentVariableEnvironmentService.add(environmentVariableEnvironment);
    }

    private updateConnectionId(connectionReference: ConnectionReference, connectionId: string){
        return this.connectionReferenceEnvironmentService.update(connectionReference.Id, this.environment.Id, {ConnectionId: connectionId});
    }

    private saveNewConnectionId(connectionReference: ConnectionReference, connectionId: string): Promise<ConnectionReferenceEnvironment>{
        let connectionReferenceEnvironment: ConnectionReferenceEnvironment = {
            ConnectionReference: connectionReference.Id,
            Environment: this.environment.Id,
            ConnectionId: connectionId
        }; 

        return this.connectionReferenceEnvironmentService.add(connectionReferenceEnvironment);
    }
}