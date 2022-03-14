import { Injectable } from "@angular/core";
import { AppConfig } from "../config/app.config";

@Injectable()
export class LogService {

  private log(logLevel: LogLevel, message: any): void {
    //TODO log extern
  }

  public trace(object: any, annotation?: string): void {
    this.log(LogLevel.Trace, object);
    if (AppConfig.settings.logging.console) {
      if (annotation === undefined)
        console.trace(object);
      else
        console.trace(annotation, object);
    }
  }

  public debug(object: any, annotation?: string): void {
    this.log(LogLevel.Debug, object);
    if (AppConfig.settings.logging.console) {
      if (annotation === undefined)
        console.debug(object);
      else
        console.debug(annotation, object);
    }
  }

  public info(object: any, annotation?: string): void {
    this.log(LogLevel.Info, object);
    if (AppConfig.settings.logging.console) {
      if (annotation === undefined)
        console.info(object);
      else
        console.info(annotation, object);
    }
  }

  public warn(object: any, annotation?: string): void {
    this.log(LogLevel.Warn, object);
    if (AppConfig.settings.logging.console) {
      if (annotation === undefined)
        console.warn(object);
      else
        console.warn(annotation, object);
    }
  }

  public error(object: any, annotation?: string): void {
    this.log(LogLevel.Error, object);
    if (AppConfig.settings.logging.console) {
      if (annotation === undefined)
        console.error(object);
      else
        console.error(annotation, object);
    }
  }

  public fatal(object: any, annotation?: string): void {
    this.log(LogLevel.Fatal, object);
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