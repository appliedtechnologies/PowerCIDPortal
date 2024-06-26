import { Pipe, PipeTransform } from '@angular/core';
import { Action } from 'rxjs/internal/scheduler/Action';
import { Patch } from '../models/patch.model';
import { Solution } from '../models/solution.model';
import { LogService } from '../services/log.service';
import { IsPatchPipe } from './is-patch.pipe';
import DataSource from 'devextreme/data/data_source';

@Pipe({
    name: 'isPatchDeletable'
})
export class IsPatchDeletablePipe implements PipeTransform {
    constructor(private isPatchPipe: IsPatchPipe) {}

    transform(patch: Patch, solutionsDataSource: DataSource): boolean {
        let hasNewerUpdate: boolean = solutionsDataSource.items().find(e => !this.isPatchPipe.transform(e) && e.Id > patch.Id) !== undefined;
        let hasSuccessfulImports: boolean = patch.Actions.find(e => e.Type == 2 && e.Result == 1) !== undefined;
        let hasRunningActions: boolean = patch.Actions.find(e => e.Status == 1 || e.Status == 2) !== undefined;
        return !hasSuccessfulImports && !hasRunningActions && !hasNewerUpdate;
    }
}