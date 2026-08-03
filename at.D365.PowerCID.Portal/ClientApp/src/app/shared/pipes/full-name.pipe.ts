import { Pipe, PipeTransform } from '@angular/core';
import { LogService } from '../services/log.service';
import { User } from '../models/user.model';

@Pipe({
    name: 'fullname',
    standalone: false
})
export class FullNamePipe implements PipeTransform {
    constructor(private logService: LogService) {}

    transform(value: Partial<User> | null | undefined): string {
        return `${value?.Firstname} ${value?.Lastname}`;
    }
}