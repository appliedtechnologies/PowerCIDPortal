import { Pipe, PipeTransform } from '@angular/core';
import { Action } from 'rxjs/internal/scheduler/Action';
import { Patch } from '../models/patch.model';
import { Solution } from '../models/solution.model';
import { LogService } from '../services/log.service';

@Pipe({
    name: 'isPatchDeletable'
})
export class IsPatchDeletablePipe implements PipeTransform {
    constructor() {}

    transform(patch: Patch, args?: any): boolean {
        console.log(patch);
        let hasSuccessfulImports: boolean = patch.Actions.filter(e => e.Type == 2 && e.Result == 1).length > 0;
        let hasRunningActions: boolean = patch.Actions.filter(e => e.Status == 1 || e.Status == 2).length > 0;
        return !hasSuccessfulImports && !hasRunningActions;
    }
}