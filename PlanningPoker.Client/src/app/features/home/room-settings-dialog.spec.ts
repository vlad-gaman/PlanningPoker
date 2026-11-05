import { TestBed } from '@angular/core/testing';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { of } from 'rxjs';
import { ApiRequestError } from '../../core/api-request-error';
import { RoomsService } from '../../core/rooms.service';
import { JOIN_RESPONSE, BOB } from '../../testing/fake-hub.testing';
import { RoomSettingsDialog, RoomSettingsDialogData } from './room-settings-dialog';

describe('RoomSettingsDialog drafting a new room', () => {
  const close = vi.fn();
  const draft = {
    useFunRoomName: false,
    configuration: { cardSet: 'linear', countdownSeconds: 7, showFireworks: false, maxParticipants: 9 },
  };

  it('prefills from the draft and returns the edited draft', async () => {
    close.mockReset();
    TestBed.configureTestingModule({
      providers: [
        { provide: MatDialogRef, useValue: { close } },
        { provide: MAT_DIALOG_DATA, useValue: { draft } },
        { provide: RoomsService, useValue: { cardSets: () => of([]) } },
      ],
    });
    const fixture = TestBed.createComponent(RoomSettingsDialog);
    fixture.detectChanges();

    await (fixture.componentInstance as unknown as { submit(): Promise<void> }).submit();

    expect(close).toHaveBeenCalledWith(draft);
  });
});

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
});
