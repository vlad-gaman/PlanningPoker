import { FormBuilder } from '@angular/forms';
import { createPersonForm } from './person-form';

describe('createPersonForm', () => {
  const form = () => createPersonForm(new FormBuilder());

  it('starts with an empty name and the first person type', () => {
    expect(form().getRawValue()).toEqual({ name: '', personType: 'dev' });
  });

  it.each(['A', 'x'.repeat(21)])('rejects the name %j', (name) => {
    const personForm = form();
    personForm.controls.name.setValue(name);

    expect(personForm.controls.name.invalid).toBe(true);
  });

  it.each(['Al', 'x'.repeat(20)])('accepts the name %j', (name) => {
    const personForm = form();
    personForm.controls.name.setValue(name);

    expect(personForm.controls.name.valid).toBe(true);
  });
});
