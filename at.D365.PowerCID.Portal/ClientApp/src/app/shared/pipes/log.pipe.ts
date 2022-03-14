import { Pipe, PipeTransform } from '@angular/core';
import { LogService } from '../services/log.service';

@Pipe({
    name: 'log'
})
export class LogPipe implements PipeTransform {
    constructor(private logService: LogService) {}

    transform(value: any, args?: any): any {
        this.logService.debug(value);
        return null;
    }
}