import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { AdminSessionService } from '../../core/admin-session.service';
import { AdminHome } from './admin-home';

describe('AdminHome', () => {
  const session = { username: () => 'operator', signOut: vi.fn() };

  beforeEach(() => {
    session.signOut.mockReset().mockResolvedValue(undefined);
    TestBed.configureTestingModule({ providers: [provideRouter([]), { provide: AdminSessionService, useValue: session }] });
  });

  it('shows who is signed in', () => {
    const fixture = TestBed.createComponent(AdminHome);
    fixture.detectChanges();

    expect((fixture.nativeElement as HTMLElement).textContent).toContain('Signed in as operator');
  });

  it('signs out from the button', () => {
    const fixture = TestBed.createComponent(AdminHome);
    fixture.detectChanges();

    (fixture.nativeElement as HTMLElement).querySelector<HTMLButtonElement>('button')!.click();

    expect(session.signOut).toHaveBeenCalledOnce();
  });
});
