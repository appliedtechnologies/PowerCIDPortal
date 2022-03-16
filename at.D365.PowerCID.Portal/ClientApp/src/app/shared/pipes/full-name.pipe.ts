import { Pipe, PipeTransform } from '@angular/core';
import { LogService } from '../services/log.service';

@Pipe({
    name: 'fullname'
})
export class FullNamePipe implements PipeTransform {
    constructor(private logService: LogService) {}

    transform(value: any, args?: any): any {
        return `${value.Firstname} ${value.Lastname}`;
    }
}