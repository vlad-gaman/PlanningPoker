import { FormBuilder, Validators } from '@angular/forms';
import { NAME_MAX_LENGTH, NAME_MIN_LENGTH, PERSON_TYPES } from '../../models/person';

/** The name and role fields used by the home page and the room's name step. */
export function createPersonForm(builder: FormBuilder) {
  return builder.nonNullable.group({
    name: ['', [Validators.required, Validators.minLength(NAME_MIN_LENGTH), Validators.maxLength(NAME_MAX_LENGTH)]],
    personType: PERSON_TYPES[0].value,
  });
}
