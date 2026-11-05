import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';
import { ApiRequestError } from '../../core/api-request-error';
import { FlashMessageService } from '../../core/flash-message.service';
import { MeService } from '../../core/me.service';
import { RoomDefaultsService } from '../../core/room-defaults.service';
import { RoomsService } from '../../core/rooms.service';
import { ERROR_CODES } from '../../models/api';
import { CreateRoomRequest } from '../../models/room';
import { Home, extractRoomId } from './home';

const REMEMBERED: CreateRoomRequest = {
  useFunRoomName: false,
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
  let navigate: ReturnType<typeof vi.spyOn>;

  beforeEach(() => {
    localStorage.clear();
    [me.get, me.update, rooms.create].forEach((mock) => mock.mockReset());
    me.get.mockReturnValue(throwError(() => new ApiRequestError(401, [{ code: ERROR_CODES.noSession, message: 'x' }])));
    me.update.mockReturnValue(of({ guid: 'g', name: 'Ann', personType: 'dev' }));
    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        { provide: MeService, useValue: me },
        { provide: RoomsService, useValue: rooms },
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
    const labelElement = Array.from(element.querySelectorAll('label')).find((l) =>
      l.textContent?.toLowerCase().includes(label.toLowerCase()),
    );
    const input = element.querySelector(`#${labelElement?.htmlFor}`) as HTMLInputElement;
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

  it('saves the person, creates the room with the default settings and opens it', async () => {
    rooms.create.mockReturnValue(of({ id: 'new-room' }));
    const { fixture, element } = await render();

    type(element, 'Your name', 'Ann');
    submit(element, 0);
    await fixture.whenStable();

    expect(me.update).toHaveBeenCalledWith({ name: 'Ann', personType: 'dev' });
    expect(rooms.create).toHaveBeenCalledWith({
      useFunRoomName: true,
      configuration: { cardSet: 'modified-fibonacci', countdownSeconds: 5, showFireworks: true, maxParticipants: 50 },
    });
    expect(navigate).toHaveBeenCalledWith(['/room', 'new-room']);
  });

  it('creates the room with the remembered settings straight away, without a dialog', async () => {
    const defaults = TestBed.inject(RoomDefaultsService);
    defaults.saveConfiguration(REMEMBERED.configuration);
    defaults.saveUseFunRoomName(REMEMBERED.useFunRoomName);
    rooms.create.mockReturnValue(of({ id: 'new-room' }));
    const { fixture, element } = await render();

    type(element, 'Your name', 'Ann');
    submit(element, 0);
    await fixture.whenStable();

    expect(rooms.create).toHaveBeenCalledWith(REMEMBERED);
    expect(element.querySelector('mat-dialog-container')).toBeNull();
  });

  it('remembers the fun room name choice for the next visit', async () => {
    rooms.create.mockReturnValue(of({ id: 'new-room' }));
    const { fixture, element } = await render();
    const checkbox = element.querySelector('#use-fun-room-name') as HTMLInputElement;
    expect(checkbox.checked).toBe(true);

    checkbox.click();
    type(element, 'Your name', 'Ann');
    submit(element, 0);
    await fixture.whenStable();

    expect(rooms.create).toHaveBeenCalledWith(expect.objectContaining({ useFunRoomName: false }));
    expect(TestBed.inject(RoomDefaultsService).load().useFunRoomName).toBe(false);
  });

  it.each(['', 'A', 'x'.repeat(21)])('rejects the name %j without creating a room', async (name) => {
    const { fixture, element } = await render();

    type(element, 'Your name', name);
    submit(element, 0);
    await fixture.whenStable();
    fixture.detectChanges();

    expect(rooms.create).not.toHaveBeenCalled();
    expect(element.querySelector('.field-error')?.textContent).toContain('2 to 20');
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
    type(element, 'Room Name:', 'https://host/room/room-9');
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
