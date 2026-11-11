import { TestBed } from '@angular/core/testing';
import { MatSnackBar } from '@angular/material/snack-bar';
import { ErrorPopupService } from './error-popup.service';

describe('ErrorPopupService', () => {
  it('opens a dismissable snack bar with the message', () => {
    const snackBar = { open: vi.fn() };
    TestBed.configureTestingModule({ providers: [{ provide: MatSnackBar, useValue: snackBar }] });

    TestBed.inject(ErrorPopupService).show('Boom');

    expect(snackBar.open).toHaveBeenCalledWith('Boom', 'Dismiss', expect.objectContaining({ politeness: 'assertive' }));
  });
});
