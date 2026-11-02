import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FormBuilder } from '@angular/forms';
import { PersonFields } from './person-fields';
import { createPersonForm } from './person-form';

describe('PersonFields', () => {
  let fixture: ComponentFixture<PersonFields>;
  let form: ReturnType<typeof createPersonForm>;

  beforeEach(() => {
    form = createPersonForm(TestBed.inject(FormBuilder));
    fixture = TestBed.createComponent(PersonFields);
    fixture.componentRef.setInput('form', form);
    fixture.detectChanges();
  });

  const element = () => fixture.nativeElement as HTMLElement;

  it('labels the name input and lists the three roles', () => {
    const label = element().querySelector('label') as HTMLLabelElement;
    const input = element().querySelector(`#${label.htmlFor}`) as HTMLInputElement;
    expect(label.textContent).toContain('Your Name:');
    expect(input).toBeTruthy();
    expect(Array.from(element().querySelectorAll('option')).map((o) => o.textContent?.trim())).toEqual([
      'Developer',
      'Tester',
      'Observer',
    ]);
  });

  it('writes typed values to the form', () => {
    const input = element().querySelector('input') as HTMLInputElement;
    input.value = 'Zoe';
    input.dispatchEvent(new Event('input'));
    expect(form.controls.name.value).toBe('Zoe');
  });

  it('shows the name error only after the field was touched', () => {
    expect(element().querySelector('.field-error')).toBeNull();
    form.controls.name.markAsTouched();
    fixture.detectChanges();
    expect(element().querySelector('.field-error')?.textContent).toContain('2 to 20');
  });
});
