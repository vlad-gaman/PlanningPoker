import { TestBed } from '@angular/core/testing';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { of } from 'rxjs';
import { ApiRequestError } from '../../core/api-request-error';
import { RoomsService } from '../../core/rooms.service';
import { JOIN_RESPONSE, BOB } from '../../testing/fake-hub.testing';
import { RoomSettingsDialog, RoomSettingsDialogData } from './room-settings-dialog';

describe('RoomSettingsDialog in a room', () => {
  const close = vi.fn();
  const data: RoomSettingsDialogData = {
    configuration: JOIN_RESPONSE.configuration,
    candidates: [BOB],
    save: vi.fn(),
    transfer: vi.fn(),
  };

  function create() {
    TestBed.configureTestingModule({
      providers: [
        { provide: MatDialogRef, useValue: { close } },
        { provide: MAT_DIALOG_DATA, useValue: data },
        { provide: RoomsService, useValue: { cardSets: () => of([{ value: 'linear', display: 'Linear' }]) } },
      ],
    });
    const fixture = TestBed.createComponent(RoomSettingsDialog);
    fixture.detectChanges();
    return fixture;
  }

  beforeEach(() => {
    close.mockReset();
    vi.mocked(data.save).mockReset().mockResolvedValue(undefined);
    vi.mocked(data.transfer).mockReset().mockResolvedValue(undefined);
  });

  it('prefills the form with the room configuration', () => {
    const dialog = create().componentInstance as unknown as { form: { getRawValue(): unknown } };

    expect(dialog.form.getRawValue()).toMatchObject(JOIN_RESPONSE.configuration);
  });

  it('saves the configuration and closes', async () => {
    const fixture = create();
    await (fixture.componentInstance as unknown as { submit(): Promise<void> }).submit();

    expect(data.save).toHaveBeenCalledWith(JOIN_RESPONSE.configuration);
    expect(close).toHaveBeenCalled();
  });

  it('shows the server error inline and stays open', async () => {
    vi.mocked(data.save).mockRejectedValue(new ApiRequestError(0, [{ code: 'x', message: 'Too few', metadata: {} }]));
    const fixture = create();

    await (fixture.componentInstance as unknown as { submit(): Promise<void> }).submit();
    fixture.detectChanges();

    expect(close).not.toHaveBeenCalled();
    expect(fixture.nativeElement.querySelector('[role=alert]').textContent).toContain('Too few');
  });

  it('transfers ownership to the chosen member', async () => {
    const fixture = create();
    const component = fixture.componentInstance as unknown as {
      transferTarget: { set(v: string): void };
      transferOwnership(): Promise<void>;
    };
    component.transferTarget.set(BOB.guid);

    await component.transferOwnership();

    expect(data.transfer).toHaveBeenCalledWith(BOB.guid);
    expect(close).toHaveBeenCalled();
  });

  describe('the Alarm settings', () => {
    interface Form {
      getRawValue(): unknown;
      patchValue(value: unknown): void;
    }
    interface Dialog {
      form: Form;
      submit(): Promise<void>;
    }

    function dialogOf(fixture: ReturnType<typeof create>) {
      return fixture.componentInstance as unknown as Dialog;
    }

    it('shows the Alarm switch and length, prefilled from the room', () => {
      const fixture = create();

      expect(fixture.nativeElement.querySelector('#alarm-enabled')).not.toBeNull();
      expect((fixture.nativeElement.querySelector('#alarm-minutes') as HTMLInputElement).value).toBe('5');
    });

    it('saves a changed length and a switched off Alarm', async () => {
      const fixture = create();
      dialogOf(fixture).form.patchValue({ alarmEnabled: false, alarmMinutes: 25 });

      await dialogOf(fixture).submit();

      expect(data.save).toHaveBeenCalledWith({ ...JOIN_RESPONSE.configuration, alarmEnabled: false, alarmMinutes: 25 });
    });

    it.each([0, 61])('refuses a length of %i minutes and says what is allowed', async (minutes) => {
      const fixture = create();
      dialogOf(fixture).form.patchValue({ alarmMinutes: minutes });

      await dialogOf(fixture).submit();
      fixture.detectChanges();

      expect(data.save).not.toHaveBeenCalled();
      expect(fixture.nativeElement.textContent).toContain('Use 1 to 60 minutes.');
    });
  });
});
