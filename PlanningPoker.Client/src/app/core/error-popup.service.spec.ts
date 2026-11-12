import { TestBed } from '@angular/core/testing';
import { MatSnackBar } from '@angular/material/snack-bar';
import { ErrorPopupService } from './error-popup.service';

describe('ErrorPopupService', () => {
  const ref = { dismiss: vi.fn() };
  const snackBar = { open: vi.fn() };

  beforeEach(() => {
    ref.dismiss.mockReset();
    snackBar.open.mockReset().mockReturnValue(ref);
    TestBed.configureTestingModule({ providers: [{ provide: MatSnackBar, useValue: snackBar }] });
  });

  it('opens a dismissable snack bar with the message that closes by itself', () => {
    TestBed.inject(ErrorPopupService).show('Boom');

    expect(snackBar.open).toHaveBeenCalledWith(
      'Boom',
      'Dismiss',
      expect.objectContaining({ politeness: 'assertive', duration: 10000 }),
    );
  });

  it('keeps a sticky popup open until it is dismissed', () => {
    const popup = TestBed.inject(ErrorPopupService);

    popup.show('Gone', true);
    popup.dismiss();

    expect(snackBar.open).toHaveBeenCalledWith('Gone', 'Dismiss', expect.objectContaining({ duration: undefined }));
    expect(ref.dismiss).toHaveBeenCalledOnce();
  });

  it('does nothing when dismissed with no popup showing', () => {
    expect(() => TestBed.inject(ErrorPopupService).dismiss()).not.toThrow();
  });
});
