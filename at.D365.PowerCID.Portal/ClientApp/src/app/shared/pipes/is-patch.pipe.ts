import { Pipe, PipeTransform } from '@angular/core';
import { Solution } from '../models/solution.model';

@Pipe({
    name: 'isPatch',
    standalone: false
})
export class IsPatchPipe implements PipeTransform {
    transform(solution: Solution): boolean {
        const isUpgrade: boolean = "ApplyManually" in solution;
        return !isUpgrade;
    }
}