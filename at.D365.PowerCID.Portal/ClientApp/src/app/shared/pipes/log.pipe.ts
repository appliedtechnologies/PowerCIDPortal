import { Pipe, PipeTransform } from '@angular/core';
import { LogService } from '../services/log.service';

@Pipe({
    name: 'log',
    standalone: false
})
export class LogPipe implements PipeTransform {
    constructor(private logService: LogService) {}

    transform(value: unknown): null {
        this.logService.debug(value);
        return null;
    }
}