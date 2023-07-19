import { Pipe, PipeTransform } from '@angular/core';
import { LogService } from '../services/log.service';
import { AppRoleAssignment } from '../models/approleassignment.model';
import { AppConfig } from '../config/app.config';

@Pipe({
    name: 'hasUserRole'
})
export class HasUserRole implements PipeTransform {
    constructor(private logService: LogService) {}

    transform(appRoleAssignments: AppRoleAssignment[], roleNameKey: any): any {
        let appRoleId: string = AppConfig.settings.azure.appRoleIds[roleNameKey];
        let hasUserRole: boolean = appRoleAssignments.some(e => e.AppRoleId == appRoleId);
        return hasUserRole;
    }
}