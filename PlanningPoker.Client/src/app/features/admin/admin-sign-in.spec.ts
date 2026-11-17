import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { AdminSessionService } from '../../core/admin-session.service';
import { ApiRequestError } from '../../core/api-request-error';
import { FlashMessageService } from '../../core/flash-message.service';
import { ERROR_CODES } from '../../models/api';
import { AdminSignIn } from './admin-sign-in';

function failure(status: number, code: string, message: string): ApiRequestError {
  return new ApiRequestError(status, [{ code, message }]);
}

describe('AdminSignIn', () => {
  const session = { signIn: vi.fn(), completeCode: vi.fn() };
  let navigate: ReturnType<typeof vi.spyOn>;

  beforeEach(() => {
    session.signIn.mockReset();
    session.completeCode.mockReset();
    TestBed.configureTestingModule({
      providers: [provideRouter([]), { provide: AdminSessionService, useValue: session }],
    });
    navigate = vi.spyOn(TestBed.inject(Router), 'navigateByUrl').mockResolvedValue(true);
  });

  async function render(): Promise<{ fixture: ComponentFixture<AdminSignIn>; element: HTMLElement }> {
    const fixture = TestBed.createComponent(AdminSignIn);
    fixture.detectChanges();
    await fixture.whenStable();
    return { fixture, element: fixture.nativeElement as HTMLElement };
  }

  async function settle(fixture: ComponentFixture<unknown>): Promise<void> {
    await fixture.whenStable();
    await new Promise((resolve) => setTimeout(resolve));
    fixture.detectChanges();
  }

  function type(element: HTMLElement, selector: string, value: string): void {
    const input = element.querySelector<HTMLInputElement>(selector)!;
    input.value = value;
    input.dispatchEvent(new Event('input'));
  }

  async function submit(fixture: ComponentFixture<unknown>, element: HTMLElement): Promise<void> {
    element.querySelector('form')!.dispatchEvent(new Event('submit'));
    await settle(fixture);
  }

  async function fillPassword(fixture: ComponentFixture<unknown>, element: HTMLElement): Promise<void> {
    type(element, '#operator-username', 'operator');
    type(element, '#operator-password', 'secret');
    await submit(fixture, element);
  }

  const text = (element: HTMLElement) => element.textContent ?? '';

  it('starts on the username and password step', async () => {
    const { element } = await render();

    expect(text(element)).toContain('Operator sign-in');
    expect(element.querySelector('#operator-username')).not.toBeNull();
    expect(element.querySelector('#operator-code')).toBeNull();
  });

  it('asks for both fields without calling the server', async () => {
    const { fixture, element } = await render();

    await submit(fixture, element);

    expect(session.signIn).not.toHaveBeenCalled();
    expect(element.querySelector('[role="alert"]')?.textContent).toContain('Enter your username and password.');
  });

  it('signs in with the credentials and goes to the management page', async () => {
    session.signIn.mockResolvedValue({ kind: 'signedIn' });
    const { fixture, element } = await render();

    await fillPassword(fixture, element);

    expect(session.signIn).toHaveBeenCalledWith('operator', 'secret');
    expect(navigate).toHaveBeenCalledWith('/admin');
  });

  it('shows the server message for a wrong username or password and stays on the step', async () => {
    session.signIn.mockRejectedValue(failure(401, ERROR_CODES.adminInvalidCredentials, 'The username or password is incorrect.'));
    const { fixture, element } = await render();

    await fillPassword(fixture, element);

    expect(element.querySelector('.form-error')?.textContent).toContain('The username or password is incorrect.');
    expect(navigate).not.toHaveBeenCalled();
    expect(element.querySelector('#operator-code')).toBeNull();
  });

  it('shows the lockout message', async () => {
    session.signIn.mockRejectedValue(failure(429, ERROR_CODES.adminLoginLocked, 'Too many failed sign-in attempts. Try again later.'));
    const { fixture, element } = await render();

    await fillPassword(fixture, element);

    expect(element.querySelector('.form-error')?.textContent).toContain('Too many failed sign-in attempts.');
  });

  it('shows a generic message when the failure is not from the server', async () => {
    session.signIn.mockRejectedValue(new Error('offline'));
    const { fixture, element } = await render();

    await fillPassword(fixture, element);

    expect(element.querySelector('.form-error')?.textContent).toContain('Something went wrong.');
  });

  it('moves to a separate code screen when the server asks for a code', async () => {
    session.signIn.mockResolvedValue({ kind: 'code', challenge: 'abc.def' });
    const { fixture, element } = await render();

    await fillPassword(fixture, element);

    expect(text(element)).toContain('Enter your code');
    expect(element.querySelector('#operator-username')).toBeNull();
    expect(element.querySelector('#operator-code')).not.toBeNull();
    expect(navigate).not.toHaveBeenCalled();
  });

  async function onCodeStep(): Promise<{ fixture: ComponentFixture<AdminSignIn>; element: HTMLElement }> {
    session.signIn.mockResolvedValue({ kind: 'code', challenge: 'abc.def' });
    const view = await render();
    await fillPassword(view.fixture, view.element);
    return view;
  }

  it('sends the challenge and the code, then goes to the management page', async () => {
    session.completeCode.mockResolvedValue(undefined);
    const { fixture, element } = await onCodeStep();

    type(element, '#operator-code', '123456');
    await submit(fixture, element);

    expect(session.completeCode).toHaveBeenCalledWith('abc.def', '123456');
    expect(navigate).toHaveBeenCalledWith('/admin');
  });

  it('accepts a code typed with a space in the middle', async () => {
    session.completeCode.mockResolvedValue(undefined);
    const { fixture, element } = await onCodeStep();

    type(element, '#operator-code', '123 456');
    await submit(fixture, element);

    expect(session.completeCode).toHaveBeenCalledWith('abc.def', '123 456');
  });

  it('asks for six digits without calling the server', async () => {
    const { fixture, element } = await onCodeStep();

    type(element, '#operator-code', '12ab');
    await submit(fixture, element);

    expect(session.completeCode).not.toHaveBeenCalled();
    expect(element.querySelector('[role="alert"]')?.textContent).toContain('Enter the 6-digit code.');
  });

  it('shows a wrong code inline and clears the field', async () => {
    session.completeCode.mockRejectedValue(failure(401, ERROR_CODES.adminTotpInvalid, 'The code is incorrect.'));
    const { fixture, element } = await onCodeStep();

    type(element, '#operator-code', '000000');
    await submit(fixture, element);

    expect(element.querySelector('.form-error')?.textContent).toContain('The code is incorrect.');
    expect(element.querySelector<HTMLInputElement>('#operator-code')?.value).toBe('');
    expect(navigate).not.toHaveBeenCalled();
  });

  it('shows the lockout message on the code step', async () => {
    session.completeCode.mockRejectedValue(failure(429, ERROR_CODES.adminLoginLocked, 'Too many failed sign-in attempts. Try again later.'));
    const { fixture, element } = await onCodeStep();

    type(element, '#operator-code', '000000');
    await submit(fixture, element);

    expect(element.querySelector('.form-error')?.textContent).toContain('Too many failed sign-in attempts.');
  });

  it('returns to the password step with the message when the first step has expired', async () => {
    session.completeCode.mockRejectedValue(failure(401, ERROR_CODES.adminLoginExpired, 'The sign-in step expired. Start again.'));
    const { fixture, element } = await onCodeStep();

    type(element, '#operator-code', '123456');
    await submit(fixture, element);

    expect(element.querySelector('#operator-username')).not.toBeNull();
    expect(element.querySelector('#operator-code')).toBeNull();
    expect(element.querySelector('.form-error')?.textContent).toContain('The sign-in step expired. Start again.');
  });

  it('goes back to the password step from the Back link', async () => {
    const { fixture, element } = await onCodeStep();

    element.querySelector<HTMLButtonElement>('.link-button')!.click();
    await settle(fixture);

    expect(text(element)).toContain('Operator sign-in');
    expect(element.querySelector('#operator-username')).not.toBeNull();
    expect(element.querySelector('.form-error')).toBeNull();
  });

  it('empties the password once the step is done', async () => {
    const { fixture, element } = await onCodeStep();
    element.querySelector<HTMLButtonElement>('.link-button')!.click();
    await settle(fixture);

    expect(element.querySelector<HTMLInputElement>('#operator-password')?.value).toBe('');
  });

  it('shows the message the session left, such as an expired session', async () => {
    TestBed.inject(FlashMessageService).show('Your session expired. Sign in again.');

    const { fixture, element } = await render();
    fixture.detectChanges();

    expect(element.querySelector('.notice')?.textContent).toContain('Your session expired.');
  });

  it('clears that message when the page is left', async () => {
    const flash = TestBed.inject(FlashMessageService);
    flash.show('Your session expired. Sign in again.');
    const { fixture } = await render();

    fixture.destroy();

    expect(flash.message()).toBeNull();
  });
});
