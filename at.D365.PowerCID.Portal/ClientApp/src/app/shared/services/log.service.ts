import { Injectable } from "@angular/core";
import { AppConfig } from "../config/app.config";

@Injectable()
export class LogService {

  private log(): void {
    //TODO log extern
  }

  public trace(object: unknown, annotation?: string): void {
    this.log();
    if (AppConfig.settings.logging.console) {
      if (annotation === undefined)
        console.trace(object);
      else
        console.trace(annotation, object);
    }
  }

  public debug(object: unknown, annotation?: string): void {
    this.log();
    if (AppConfig.settings.logging.console) {
      if (annotation === undefined)
        console.debug(object);
      else
        console.debug(annotation, object);
    }
  }

  public info(object: unknown, annotation?: string): void {
    this.log();
    if (AppConfig.settings.logging.console) {
      if (annotation === undefined)
        console.info(object);
      else
        console.info(annotation, object);
    }
  }

  public warn(object: unknown, annotation?: string): void {
    this.log();
    if (AppConfig.settings.logging.console) {
      if (annotation === undefined)
        console.warn(object);
      else
        console.warn(annotation, object);
    }
  }

  public error(object: unknown, annotation?: string): void {
    this.log();
    if (AppConfig.settings.logging.console) {
      if (annotation === undefined)
        console.error(object);
      else
        console.error(annotation, object);
    }
  }

  public fatal(object: unknown, annotation?: string): void {
    this.log();
    if (AppConfig.settings.logging.console) {
      if (annotation === undefined)
        console.error(object);
      else
        console.error(annotation, object);
    }
  }

  private getNameOfLogLevel(logLevel: LogLevel): string {
    switch (logLevel) {
      case LogLevel.Trace:
        return "Trace";
      case LogLevel.Debug:
        return "Debug";
      case LogLevel.Info:
        return "Info";
      case LogLevel.Warn:
        return "Warn";
      case LogLevel.Error:
        return "Error";
      case LogLevel.Fatal:
        return "Fatal";
      default:
        return "Unknown";
    }
  }
}

export enum LogLevel {
  Trace,
  Debug,
  Info,
  Warn,
  Error,
  Fatal
}