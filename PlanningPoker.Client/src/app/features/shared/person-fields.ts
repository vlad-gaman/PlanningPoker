import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { ReactiveFormsModule } from '@angular/forms';
import { PERSON_TYPES } from '../../models/person';
import { PersonForm } from './person-form';

let nextId = 0;

/** The name and role fields of the home page and the room's name step, laid out as two labelled fields. */
@Component({
  selector: 'app-person-fields',
  imports: [ReactiveFormsModule],
  templateUrl: './person-fields.html',
  styleUrl: './person-fields.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PersonFields {
  readonly form = input.required<PersonForm>();

  protected readonly personTypes = PERSON_TYPES;
  protected readonly nameId = `person-name-${nextId}`;
  protected readonly roleId = `person-role-${nextId++}`;
}
