import { Pipe, PipeTransform } from '@angular/core';
import { Solution } from '../models/solution.model';
import { LogService } from '../services/log.service';

@Pipe({
    name: 'isPatch'
})
export class IsPatchPipe implements PipeTransform {
    constructor() {}

    transform(solution: Solution, args?: any): boolean {
        let isUpgrade: boolean = "ApplyManually" in solution;
        return !isUpgrade;
    }
}