import { TestBed } from '@angular/core/testing';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { AdminConfirmDialog } from './admin-confirm-dialog';

describe('AdminConfirmDialog', () => {
  const ref = { close: vi.fn() };

  beforeEach(() => {
    ref.close.mockReset();
    TestBed.configureTestingModule({
      providers: [
        { provide: MAT_DIALOG_DATA, useValue: { title: 'Close "blue-fox"?', detail: 'It cannot be undone.', confirmLabel: 'Close room' } },
        { provide: MatDialogRef, useValue: ref },
      ],
    });
  });

  it('shows the title, the detail and the confirm label', () => {
    const fixture = TestBed.createComponent(AdminConfirmDialog);
    fixture.detectChanges();
    const element = fixture.nativeElement as HTMLElement;

    expect(element.querySelector('h2')?.textContent).toContain('Close "blue-fox"?');
    expect(element.textContent).toContain('It cannot be undone.');
    expect(element.querySelector('.danger-button')?.textContent).toContain('Close room');
  });

  it('answers false from Close and Cancel and true from the confirm button', () => {
    const fixture = TestBed.createComponent(AdminConfirmDialog);
    fixture.detectChanges();
    const element = fixture.nativeElement as HTMLElement;

    element.querySelector<HTMLButtonElement>('button[aria-label="Close"]')!.click();
    element.querySelector<HTMLButtonElement>('.secondary-button')!.click();
    element.querySelector<HTMLButtonElement>('.danger-button')!.click();

    expect(ref.close.mock.calls.map((call) => call[0])).toEqual([false, false, true]);
  });
});
