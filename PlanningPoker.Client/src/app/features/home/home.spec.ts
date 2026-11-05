import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MatDialog } from '@angular/material/dialog';
import { Router, provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';
import { ApiRequestError } from '../../core/api-request-error';
import { FlashMessageService } from '../../core/flash-message.service';
import { MeService } from '../../core/me.service';
import { RoomsService } from '../../core/rooms.service';
import { ERROR_CODES } from '../../models/api';
import { CreateRoomRequest, DEFAULT_ROOM_CONFIGURATION } from '../../models/room';
import { Home, extractRoomId } from './home';

const REQUEST: CreateRoomRequest = {
  useFunRoomName: true,
  configuration: { cardSet: 'linear', countdownSeconds: 7, showFireworks: false, maxParticipants: 9 },
};

describe('extractRoomId', () => {
  it.each([
    ['room-1', 'room-1'],
    ['  room-1  ', 'room-1'],
    ['https://host/room/room-1', 'room-1'],
    ['https://host/Room/room-1/?x=1#top', 'room-1'],
    ['', ''],
  ])('turns %j into %j', (input, expected) => {
    expect(extractRoomId(input)).toBe(expected);
  });
});

describe('Home', () => {
  const me = { get: vi.fn(), update: vi.fn() };
  const rooms = { create: vi.fn() };
  const dialog = { open: vi.fn() };
  let navigate: ReturnType<typeof vi.spyOn>;

  beforeEach(() => {
    [me.get, me.update, rooms.create, dialog.open].forEach((mock) => mock.mockReset());
    me.get.mockReturnValue(throwError(() => new ApiRequestError(401, [{ code: ERROR_CODES.noSession, message: 'x' }])));
    me.update.mockReturnValue(of({ guid: 'g', name: 'Ann', personType: 'dev' }));
    dialog.open.mockReturnValue({ afterClosed: () => of(REQUEST) });
    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        { provide: MeService, useValue: me },
        { provide: RoomsService, useValue: rooms },
        { provide: MatDialog, useValue: dialog },
      ],
    });
    navigate = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);
  });

  async function render(): Promise<{ fixture: ComponentFixture<Home>; element: HTMLElement }> {
    const fixture = TestBed.createComponent(Home);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
    return { fixture, element: fixture.nativeElement as HTMLElement };
  }

  function type(element: HTMLElement, label: string, value: string): void {
    const field = Array.from(element.querySelectorAll('mat-form-field')).find((f) => f.textContent?.includes(label));
    const input = field?.querySelector('input') as HTMLInputElement;
    input.value = value;
    input.dispatchEvent(new Event('input'));
  }

  function submit(element: HTMLElement, index: number): void {
    (element.querySelectorAll('form')[index] as HTMLFormElement).dispatchEvent(new Event('submit'));
  }

  it('prefills the form from the existing session', async () => {
    me.get.mockReturnValue(of({ guid: 'g', name: 'Zoe', personType: 'obs' }));

    const { element } = await render();

    expect((element.querySelector('input') as HTMLInputElement).value).toBe('Zoe');
  });

  function settingsButton(element: HTMLElement): HTMLButtonElement {
    return Array.from(element.querySelectorAll('button')).find((b) =>
      b.textContent?.includes('Room settings'),
    ) as HTMLButtonElement;
  }

  it('creates the room with default settings without opening the dialog', async () => {
    rooms.create.mockReturnValue(of({ id: 'new-room' }));
    const { fixture, element } = await render();

    type(element, 'Your name', 'Ann');
    submit(element, 0);
    await fixture.whenStable();

    expect(dialog.open).not.toHaveBeenCalled();
    expect(me.update).toHaveBeenCalledWith({ name: 'Ann', personType: 'dev' });
    expect(rooms.create).toHaveBeenCalledWith({ useFunRoomName: true, configuration: DEFAULT_ROOM_CONFIGURATION });
    expect(navigate).toHaveBeenCalledWith(['/room', 'new-room']);
  });

  it('creates the room with the settings chosen beforehand', async () => {
    rooms.create.mockReturnValue(of({ id: 'new-room' }));
    const { fixture, element } = await render();

    settingsButton(element).click();
    await fixture.whenStable();
    type(element, 'Your name', 'Ann');
    submit(element, 0);
    await fixture.whenStable();

    expect(dialog.open.mock.calls[0][1]).toEqual({
      data: { draft: { useFunRoomName: true, configuration: DEFAULT_ROOM_CONFIGURATION } },
    });
    expect(rooms.create).toHaveBeenCalledWith(REQUEST);
  });

  it('reopens the settings dialog with the previous choices', async () => {
    const { fixture, element } = await render();

    settingsButton(element).click();
    await fixture.whenStable();
    settingsButton(element).click();
    await fixture.whenStable();

    expect(dialog.open.mock.calls[1][1]).toEqual({ data: { draft: REQUEST } });
  });

  it('keeps the settings when the dialog is cancelled', async () => {
    dialog.open.mockReturnValue({ afterClosed: () => of(undefined) });
    rooms.create.mockReturnValue(of({ id: 'new-room' }));
    const { fixture, element } = await render();

    settingsButton(element).click();
    await fixture.whenStable();
    type(element, 'Your name', 'Ann');
    submit(element, 0);
    await fixture.whenStable();

    expect(rooms.create).toHaveBeenCalledWith({ useFunRoomName: true, configuration: DEFAULT_ROOM_CONFIGURATION });
  });

  it.each(['', 'A', 'x'.repeat(21)])('rejects the name %j before creating', async (name) => {
    const { fixture, element } = await render();

    type(element, 'Your name', name);
    submit(element, 0);
    await fixture.whenStable();
    fixture.detectChanges();

    expect(rooms.create).not.toHaveBeenCalled();
    expect(element.querySelector('mat-error')?.textContent).toContain('2 to 20');
  });

  it('shows the server error inline when creating fails', async () => {
    rooms.create.mockReturnValue(
      throwError(() => new ApiRequestError(409, [{ code: 'room_id_generation_failed', message: 'Try again.' }])),
    );
    const { fixture, element } = await render();

    type(element, 'Your name', 'Ann');
    submit(element, 0);
    await fixture.whenStable();
    fixture.detectChanges();

    expect(element.querySelector('.form-error')?.textContent).toContain('Try again.');
    expect(navigate).not.toHaveBeenCalled();
  });

  it('joins a room from a pasted link', async () => {
    const { fixture, element } = await render();

    type(element, 'Your name', 'Ann');
    type(element, 'Room name or link', 'https://host/room/room-9');
    submit(element, 1);
    await fixture.whenStable();

    expect(me.update).toHaveBeenCalledWith({ name: 'Ann', personType: 'dev' });
    expect(navigate).toHaveBeenCalledWith(['/room', 'room-9']);
  });

  it('does not join without a room', async () => {
    const { fixture, element } = await render();

    type(element, 'Your name', 'Ann');
    submit(element, 1);
    await fixture.whenStable();

    expect(navigate).not.toHaveBeenCalled();
  });

  it('shows the flash message and clears it on leaving', async () => {
    TestBed.inject(FlashMessageService).show('The room was closed.');

    const { fixture, element } = await render();
    expect(element.querySelector('.notice')?.textContent).toContain('The room was closed.');

    fixture.destroy();
    expect(TestBed.inject(FlashMessageService).message()).toBeNull();
  });
});
