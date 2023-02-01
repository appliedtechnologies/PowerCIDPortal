import { ComponentFixture, TestBed } from '@angular/core/testing';

import { EnvironmentVariableComponent } from './environment-variable.component';

describe('EnvironmentVariableComponent', () => {
  let component: EnvironmentVariableComponent;
  let fixture: ComponentFixture<EnvironmentVariableComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ EnvironmentVariableComponent ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(EnvironmentVariableComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
