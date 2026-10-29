import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { ApiRequestError } from '../../core/api-request-error';
import { MeService } from '../../core/me.service';
import { Person, PersonType } from '../../models/person';

@Component({
  selector: 'app-home',
  imports: [MatButtonModule, MatFormFieldModule, MatInputModule, MatSelectModule],
  templateUrl: './home.html',
  styleUrl: './home.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Home {
  private readonly meService = inject(MeService);

  protected readonly name = signal('');
  protected readonly personType = signal<PersonType>('dev');
  protected readonly person = signal<Person | null>(null);
  protected readonly error = signal<string | null>(null);

  protected setName(event: Event): void {
    this.name.set((event.target as HTMLInputElement).value);
  }

  protected save(event?: Event): void {
    event?.preventDefault();
    this.error.set(null);
    this.meService.update({ name: this.name(), personType: this.personType() }).subscribe({
      next: (person) => this.person.set(person),
      error: (error: unknown) =>
        this.error.set(error instanceof ApiRequestError ? error.message : 'Something went wrong.'),
    });
  }
}
