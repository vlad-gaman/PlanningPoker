import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { ApiRequestError } from '../../core/api-request-error';
import { MeService } from '../../core/me.service';
import { Home } from './home';

describe('Home', () => {
  const update = vi.fn();

  beforeEach(() => {
    update.mockReset();
    TestBed.configureTestingModule({ providers: [{ provide: MeService, useValue: { update } }] });
  });

  function render() {
    const fixture = TestBed.createComponent(Home);
    fixture.detectChanges();
    const element = fixture.nativeElement as HTMLElement;
    const input = element.querySelector('input') as HTMLInputElement;
    const form = element.querySelector('form') as HTMLFormElement;
    return { fixture, element, input, form };
  }

  it('submits the entered name and shows the welcome message', async () => {
    update.mockReturnValue(of({ guid: 'g', name: 'Ann', personType: 'dev' }));
    const { fixture, element, input, form } = render();

    input.value = 'Ann';
    input.dispatchEvent(new Event('input'));
    form.dispatchEvent(new Event('submit'));
    await fixture.whenStable();

    expect(update).toHaveBeenCalledWith({ name: 'Ann', personType: 'dev' });
    expect(element.querySelector('.welcome')?.textContent).toContain('Ann');
  });

  it('shows the API error message when saving fails', async () => {
    update.mockReturnValue(
      throwError(() => new ApiRequestError(400, [{ code: 'validation_failed', message: 'Name is too short.' }])),
    );
    const { fixture, element, form } = render();

    form.dispatchEvent(new Event('submit'));
    await fixture.whenStable();

    expect(element.querySelector('.error')?.textContent).toContain('Name is too short.');
    expect(element.querySelector('.welcome')).toBeNull();
  });
});
