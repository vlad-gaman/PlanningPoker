import { TestBed } from '@angular/core/testing';
import { ErrorPopupService } from './error-popup.service';
import { PopupErrorHandler } from './popup-error-handler';

describe('PopupErrorHandler', () => {
  it('logs the error and shows its message in a popup', () => {
    const popup = { show: vi.fn() };
    const log = vi.spyOn(console, 'error').mockImplementation(() => undefined);
    TestBed.configureTestingModule({
      providers: [PopupErrorHandler, { provide: ErrorPopupService, useValue: popup }],
    });

    TestBed.inject(PopupErrorHandler).handleError(new Error('crypto is gone'));

    expect(log).toHaveBeenCalled();
    expect(popup.show).toHaveBeenCalledWith('Something went wrong: crypto is gone');
    log.mockRestore();
  });
});
