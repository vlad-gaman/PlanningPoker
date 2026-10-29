import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { Subject, of, throwError } from 'rxjs';
import { ApiRequestError } from '../../core/api-request-error';
import { FlashMessageService } from '../../core/flash-message.service';
import { MeService } from '../../core/me.service';
import { RoomHubClient } from '../../core/room-hub.client';
import { ERROR_CODES } from '../../models/api';
import { ANN, BOB, JOIN_RESPONSE, fakeHub } from '../../testing/fake-hub.testing';
import { HUB_EVENTS } from '../../models/hub-events';
import { ROOM_NOT_FOUND_MESSAGE, RoomStore } from './room-store';
import { Room } from './room';

async function settle(fixture: ComponentFixture<unknown>): Promise<void> {
  await fixture.whenStable();
  await new Promise((resolve) => setTimeout(resolve));
  fixture.detectChanges();
}

describe('Room', () => {
  const me = { get: vi.fn(), update: vi.fn() };
  let hub: ReturnType<typeof fakeHub>;
  let navigate: ReturnType<typeof vi.spyOn>;

  beforeEach(() => {
    me.get.mockReset();
    me.update.mockReset();
    hub = fakeHub(JOIN_RESPONSE);
    TestBed.configureTestingModule({
      providers: [provideRouter([]), { provide: MeService, useValue: me }],
    });
    // The room provides its own hub client at component level, so the fake replaces it there.
    TestBed.overrideComponent(Room, {
      set: { providers: [RoomStore, { provide: RoomHubClient, useValue: hub }] },
    });
    navigate = vi.spyOn(TestBed.inject(Router), 'navigateByUrl').mockResolvedValue(true);
  });

  async function render(): Promise<{ fixture: ComponentFixture<Room>; element: HTMLElement }> {
    const fixture = TestBed.createComponent(Room);
    fixture.componentRef.setInput('id', 'room-1');
    fixture.detectChanges();
    await settle(fixture);
    return { fixture, element: fixture.nativeElement as HTMLElement };
  }

  function noSession() {
    return throwError(() => new ApiRequestError(401, [{ code: ERROR_CODES.noSession, message: 'No session.' }]));
  }

  it('joins straight away when the visitor already has a name', async () => {
    me.get.mockReturnValue(of(ANN));

    const { element } = await render();

    expect(hub.invoke).toHaveBeenCalledWith('JoinRoom', 'room-1');
    expect(element.querySelector('form')).toBeNull();
    expect(element.textContent).toContain('Ann');
    expect(element.textContent).toContain('(owner)');
  });

  it('asks for a name when there is no session, then saves it and joins', async () => {
    me.get.mockReturnValue(noSession());
    me.update.mockReturnValue(of(ANN));
    const { fixture, element } = await render();
    expect(hub.invoke).not.toHaveBeenCalled();

    const input = element.querySelector('input') as HTMLInputElement;
    input.value = 'Ann';
    input.dispatchEvent(new Event('input'));
    (element.querySelector('form') as HTMLFormElement).dispatchEvent(new Event('submit'));
    await settle(fixture);

    expect(me.update).toHaveBeenCalledWith({ name: 'Ann', personType: 'dev' });
    expect(hub.invoke).toHaveBeenCalledWith('JoinRoom', 'room-1');
    expect(element.querySelector('table')).not.toBeNull();
  });

  it('does not save a name that is too short', async () => {
    me.get.mockReturnValue(noSession());
    const { fixture, element } = await render();

    const input = element.querySelector('input') as HTMLInputElement;
    input.value = 'A';
    input.dispatchEvent(new Event('input'));
    (element.querySelector('form') as HTMLFormElement).dispatchEvent(new Event('submit'));
    await settle(fixture);

    expect(me.update).not.toHaveBeenCalled();
  });

  it('goes home with a message when the room does not exist', async () => {
    me.get.mockReturnValue(of(ANN));
    hub.invoke.mockRejectedValue(new ApiRequestError(0, [{ code: ERROR_CODES.roomNotFound, message: 'Gone.' }]));

    await render();

    expect(navigate).toHaveBeenCalledWith('/');
    expect(TestBed.inject(FlashMessageService).message()).toBe(ROOM_NOT_FOUND_MESSAGE);
  });

  it('goes home with a message when the room id is malformed', async () => {
    me.get.mockReturnValue(of(ANN));
    hub.invoke.mockRejectedValue(
      new ApiRequestError(400, [
        { code: ERROR_CODES.validationFailed, message: 'Bad id.', metadata: { field: 'roomId' } },
      ]),
    );

    await render();

    expect(navigate).toHaveBeenCalledWith('/');
    expect(TestBed.inject(FlashMessageService).message()).toBe(ROOM_NOT_FOUND_MESSAGE);
  });

  it('shows other errors inline on the name step and closes the hub', async () => {
    me.get.mockReturnValue(of(ANN));
    hub.invoke.mockRejectedValue(new ApiRequestError(500, [{ code: 'internal_error', message: 'Server down.' }]));

    const { fixture, element } = await render();

    expect(element.querySelector('.form-error')?.textContent).toContain('Server down.');
    expect(element.querySelector('form')).not.toBeNull();
    expect(navigate).not.toHaveBeenCalled();
    expect(hub.disconnect).toHaveBeenCalled();
    fixture.destroy();
  });

  it('shows other validation failures inline instead of going home', async () => {
    me.get.mockReturnValue(of(ANN));
    hub.invoke.mockRejectedValue(
      new ApiRequestError(400, [{ code: ERROR_CODES.validationFailed, message: 'Bad name.', metadata: { field: 'name' } }]),
    );

    const { element } = await render();

    expect(element.querySelector('.form-error')?.textContent).toContain('Bad name.');
    expect(navigate).not.toHaveBeenCalled();
  });

  it('closes the hub when joining fails with an unexpected error', async () => {
    me.get.mockReturnValue(of(ANN));
    hub.invoke.mockRejectedValue(new Error('socket reset'));

    const { element } = await render();

    expect(element.querySelector('.form-error')?.textContent).toContain('Something went wrong.');
    expect(hub.disconnect).toHaveBeenCalled();
  });

  it('leaves the old room and joins the new one when the route id changes', async () => {
    me.get.mockReturnValue(of(ANN));
    hub.invoke.mockResolvedValueOnce({ ...JOIN_RESPONSE, people: [ANN, BOB] });
    const { fixture, element } = await render();
    expect(element.textContent).toContain('Bob');
    // The second JoinRoom (for room-2) is held, so the switch can be seen while it is loading.
    let respond!: (response: unknown) => void;
    hub.invoke.mockImplementationOnce(() => new Promise((resolve) => (respond = resolve)));

    fixture.componentRef.setInput('id', 'room-2');
    fixture.detectChanges();
    await settle(fixture);

    expect(hub.disconnect).toHaveBeenCalled();
    expect(hub.invoke).toHaveBeenLastCalledWith('JoinRoom', 'room-2');
    expect(fixture.debugElement.injector.get(RoomStore).people()).toEqual([]);
    expect(element.querySelector('table')).toBeNull();
    expect(element.textContent).not.toContain('Bob');

    respond(JOIN_RESPONSE);
    await settle(fixture);

    expect(fixture.debugElement.injector.get(RoomStore).people()).toEqual([ANN]);
    expect(element.querySelector('table')).not.toBeNull();
    expect(element.textContent).not.toContain('Bob');
  });

  it('does not join the old room when the id changes while the name check is pending', async () => {
    const pendingCheck = new Subject<typeof ANN>();
    me.get.mockReturnValueOnce(pendingCheck).mockReturnValue(of(ANN));
    const fixture = TestBed.createComponent(Room);
    fixture.componentRef.setInput('id', 'room-1');
    fixture.detectChanges();
    await settle(fixture);

    fixture.componentRef.setInput('id', 'room-2');
    fixture.detectChanges();
    await settle(fixture);
    pendingCheck.next(ANN);
    pendingCheck.complete();
    await settle(fixture);

    expect(hub.invoke).toHaveBeenCalledOnce();
    expect(hub.invoke).toHaveBeenCalledWith('JoinRoom', 'room-2');
    expect(fixture.nativeElement.querySelector('table')).not.toBeNull();
  });

  it('ignores the snapshot of the old room when its JoinRoom answers after the id changed', async () => {
    me.get.mockReturnValue(of(ANN));
    let respondOld!: (response: unknown) => void;
    hub.invoke.mockImplementationOnce(() => new Promise((resolve) => (respondOld = resolve)));
    const fixture = TestBed.createComponent(Room);
    fixture.componentRef.setInput('id', 'room-1');
    fixture.detectChanges();
    await settle(fixture);

    fixture.componentRef.setInput('id', 'room-2');
    fixture.detectChanges();
    await settle(fixture);
    respondOld({ ...JOIN_RESPONSE, people: [ANN, BOB] });
    await settle(fixture);

    expect(fixture.debugElement.injector.get(RoomStore).people()).toEqual([ANN]);
    expect(fixture.nativeElement.querySelector('table')).not.toBeNull();
    expect(fixture.nativeElement.textContent).not.toContain('Bob');
  });

  describe('voting', () => {
    const OBS = { guid: 'g-obs', name: 'Olga', personType: 'obs' } as const;

    function buttonLabelled(element: HTMLElement, text: string): HTMLButtonElement {
      return Array.from(element.querySelectorAll('button')).find((b) => b.textContent?.includes(text)) as HTMLButtonElement;
    }

    function markOf(element: HTMLElement, name: string): string {
      const row = Array.from(element.querySelectorAll('tr.mat-mdc-row')).find((r) => r.textContent?.includes(name));
      return row?.querySelector('.mark')?.textContent?.trim() ?? '';
    }

    beforeEach(() => {
      me.get.mockReturnValue(of(ANN));
      hub.invoke.mockResolvedValue({ ...JOIN_RESPONSE, people: [ANN, BOB, OBS] });
    });

    it('shows a hidden marker for a voter, nothing for a non-voter and nothing for an observer', async () => {
      const { fixture, element } = await render();

      hub.emit(HUB_EVENTS.VoteCast, { personGuid: BOB.guid });
      await settle(fixture);

      expect(markOf(element, 'Bob')).toBe('▮');
      expect(markOf(element, 'Ann')).toBe('');
      expect(markOf(element, 'Olga')).toBe('');
    });

    it('shows the revealed marks using the card display', async () => {
      const { fixture, element } = await render();

      hub.emit(HUB_EVENTS.VotesShown, {
        votes: { [ANN.guid]: 'coffee', [BOB.guid]: '5' },
        hasEveryoneVoted: true,
        votingFinished: true,
        countdown: null,
      });
      await settle(fixture);

      expect(markOf(element, 'Ann')).toBe('☕');
      expect(markOf(element, 'Bob')).toBe('5');
    });

    it('shows the countdown line only while a countdown runs', async () => {
      const { fixture, element } = await render();
      expect(element.querySelector('.countdown')).toBeNull();

      hub.emit(HUB_EVENTS.CountdownUpdate, { countdown: 3, reset: false });
      await settle(fixture);
      expect(element.querySelector('.countdown')?.textContent).toContain('Showing results in 3!');

      hub.emit(HUB_EVENTS.CountdownReset, { countdown: 5, reset: true });
      await settle(fixture);
      expect(element.querySelector('.countdown')).toBeNull();
    });

    it('votes with the picked card', async () => {
      const { fixture, element } = await render();

      (element.querySelector('input[type="radio"][value="5"]') as HTMLInputElement).click();
      await settle(fixture);

      expect(hub.invoke).toHaveBeenCalledWith('Vote', '5');
      expect((element.querySelector('input[type="radio"][value="5"]') as HTMLInputElement).checked).toBe(true);
    });

    it('disables the cards once the votes are revealed', async () => {
      const { fixture, element } = await render();

      hub.emit(HUB_EVENTS.VotesShown, { votes: {}, hasEveryoneVoted: true, votingFinished: true, countdown: null });
      await settle(fixture);

      const radios = Array.from(element.querySelectorAll<HTMLInputElement>('input[type="radio"]'));
      expect(radios.length).toBeGreaterThan(0);
      expect(radios.every((radio) => radio.disabled)).toBe(true);
    });

    it('disables the cards for an observer', async () => {
      me.get.mockReturnValue(of(OBS));

      const { element } = await render();

      const radios = Array.from(element.querySelectorAll<HTMLInputElement>('input[type="radio"]'));
      expect(radios.every((radio) => radio.disabled)).toBe(true);
    });

    it('Clear votes and Show votes call the hub', async () => {
      const { fixture, element } = await render();

      buttonLabelled(element, 'Clear votes').click();
      await settle(fixture);
      expect(hub.invoke).toHaveBeenLastCalledWith('ClearVotes');

      buttonLabelled(element, 'Show votes').click();
      await settle(fixture);
      expect(hub.invoke).toHaveBeenLastCalledWith('ForceShowVotes');
    });

    it('disables Show votes once the votes are revealed', async () => {
      const { fixture, element } = await render();

      hub.emit(HUB_EVENTS.VotesShown, { votes: {}, hasEveryoneVoted: true, votingFinished: true, countdown: null });
      await settle(fixture);

      expect(buttonLabelled(element, 'Show votes').disabled).toBe(true);
      expect(buttonLabelled(element, 'Clear votes').disabled).toBe(false);
    });
  });

  it('leave returns to the home page', async () => {
    me.get.mockReturnValue(of(ANN));
    const { fixture, element } = await render();

    (Array.from(element.querySelectorAll('button')).find((b) => b.textContent?.includes('Leave')) as HTMLElement).click();
    await settle(fixture);

    expect(hub.disconnect).toHaveBeenCalled();
    expect(navigate).toHaveBeenCalledWith('/');
  });
});
