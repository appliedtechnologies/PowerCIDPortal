import { Pipe, PipeTransform } from '@angular/core';
import { LogService } from '../services/log.service';
import { AppRoleAssignment } from '../models/approleassignment.model';
import { AppConfig } from '../config/app.config';

@Pipe({
    name: 'hasUserRole',
    standalone: false
})
export class HasUserRole implements PipeTransform {
    constructor(private logService: LogService) {}

    transform(appRoleAssignments: AppRoleAssignment[], roleNameKey: any): any {
        const appRoleId: string = AppConfig.settings.azure.appRoleIds[roleNameKey];
        const hasUserRole: boolean = appRoleAssignments.some(e => e.AppRoleId == appRoleId);
        return hasUserRole;
    }
}