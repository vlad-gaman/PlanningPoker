import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { MatDialog } from '@angular/material/dialog';
import { of, throwError } from 'rxjs';
import { ApiRequestError } from '../../core/api-request-error';
import { ErrorPopupService } from '../../core/error-popup.service';
import { MeService } from '../../core/me.service';
import { RoomDefaultsService } from '../../core/room-defaults.service';
import { RoomHubClient } from '../../core/room-hub.client';
import { RoomsService } from '../../core/rooms.service';
import { ERROR_CODES } from '../../models/api';
import {
  ANN,
  BOB,
  CONSENSUS_STATISTICS,
  JOIN_RESPONSE,
  STATISTICS,
  fakeHub,
  revealed,
} from '../../testing/fake-hub.testing';
import { ALARM_SOUND_STORAGE_KEY } from '../../core/alarm-sound.service';
import { AlarmPlayer } from './alarm/alarm-player';
import { FireworksLauncher } from './fireworks/fireworks-launcher';
import { HUB_EVENTS } from '../../models/hub-events';
import { RoomStore } from './room-store';
import { Room } from './room';

async function settle(fixture: ComponentFixture<unknown>): Promise<void> {
  await fixture.whenStable();
  await new Promise((resolve) => setTimeout(resolve));
  fixture.detectChanges();
}

describe('Room', () => {
  const me = { get: vi.fn(), update: vi.fn() };
  const fireworks = { launch: vi.fn(), dispose: vi.fn() };
  const launcher = { create: vi.fn() };
  const dialog = { open: vi.fn() };
  const popup = { show: vi.fn(), dismiss: vi.fn() };
  const alarmPlayer = { play: vi.fn() };
  const rooms = { exists: vi.fn() };
  let hub: ReturnType<typeof fakeHub>;
  let navigate: ReturnType<typeof vi.spyOn>;

  beforeEach(() => {
    me.get.mockReset();
    me.update.mockReset();
    hub = fakeHub(JOIN_RESPONSE);
    fireworks.launch.mockReset();
    fireworks.dispose.mockReset();
    launcher.create.mockReset();
    dialog.open.mockReset();
    popup.show.mockReset();
    popup.dismiss.mockReset();
    alarmPlayer.play.mockReset().mockResolvedValue(undefined);
    localStorage.removeItem(ALARM_SOUND_STORAGE_KEY);
    rooms.exists.mockReset();
    rooms.exists.mockReturnValue(of({ id: 'room-1', exists: true }));
    launcher.create.mockReturnValue(fireworks);
    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        { provide: MeService, useValue: me },
        { provide: FireworksLauncher, useValue: launcher },
        { provide: MatDialog, useValue: dialog },
        { provide: ErrorPopupService, useValue: popup },
        { provide: AlarmPlayer, useValue: alarmPlayer },
        { provide: RoomsService, useValue: rooms },
      ],
    });
    // The room provides its own hub client at component level, so the fake replaces it there.
    TestBed.overrideComponent(Room, {
      set: { providers: [RoomStore, { provide: RoomHubClient, useValue: hub }] },
    });
    navigate = vi.spyOn(TestBed.inject(Router), 'navigateByUrl').mockResolvedValue(true);
  });

  async function render(id = 'room-1'): Promise<{ fixture: ComponentFixture<Room>; element: HTMLElement }> {
    const fixture = TestBed.createComponent(Room);
    fixture.componentRef.setInput('id', id);
    fixture.detectChanges();
    await settle(fixture);
    return { fixture, element: fixture.nativeElement as HTMLElement };
  }

  function noSession() {
    return throwError(() => new ApiRequestError(401, [{ code: ERROR_CODES.noSession, message: 'No session.' }]));
  }



  describe('entering the room', () => {
    it('joins straight away when the visitor already has a name', async () => {
      me.get.mockReturnValue(of(ANN));

      const { element } = await render();

      expect(hub.invoke).toHaveBeenCalledWith('JoinRoom', 'room-1');
      expect(element.querySelector('form')).toBeNull();
      expect(element.textContent).toContain('Ann');
      expect(element.querySelector('.person .crown')).not.toBeNull();
      expect(element.textContent).not.toContain('(owner)');
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

      expect(me.update).toHaveBeenCalledWith({ name: 'Ann', personType: 'dev', cardBack: 'classic' });
      expect(hub.invoke).toHaveBeenCalledWith('JoinRoom', 'room-1');
      expect(element.querySelector('.people')).not.toBeNull();
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

    it('shows a refused join inline on the name form', async () => {
      me.get.mockReturnValue(of(ANN));
      hub.invoke.mockRejectedValue(new ApiRequestError(500, [{ code: 'internal_error', message: 'Server down.' }]));

      const { fixture, element } = await render();

      expect(element.querySelector('.form-error')?.textContent).toContain('Server down.');
      expect(element.querySelector('form')).not.toBeNull();
      fixture.destroy();
    });

    it('also shows a refused join in the error popup', async () => {
      me.get.mockReturnValue(of(ANN));
      hub.invoke.mockRejectedValue(new ApiRequestError(500, [{ code: 'internal_error', message: 'Server down.' }]));

      await render();

      expect(popup.show).toHaveBeenCalledWith(expect.stringContaining('Server down.'));
    });

    it('shows the new room when the route id changes', async () => {
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

      expect(hub.invoke).toHaveBeenLastCalledWith('JoinRoom', 'room-2');
      expect(element.querySelector('.people')).toBeNull();
      expect(element.textContent).not.toContain('Bob');

      respond(JOIN_RESPONSE);
      await settle(fixture);

      expect(element.querySelector('.people')).not.toBeNull();
      expect(element.textContent).not.toContain('Bob');
    });
  });

  describe('the room heading', () => {
    it('links each word of a fun room name to a Google search, except words that are not linkable', async () => {
      me.get.mockReturnValue(of(ANN));
      rooms.exists.mockReturnValue(
        of({
          id: '42_Adoring_Lewd_people',
          exists: true,
          nameWords: [
            { text: 'Adoring', linkable: true },
            { text: 'Lewd', linkable: false },
            { text: 'people', linkable: true },
          ],
        }),
      );

      const { element } = await render('42_Adoring_Lewd_people');

      const heading = element.querySelector('h1') as HTMLElement;
      const links = Array.from(heading.querySelectorAll('a'));
      expect(heading.textContent?.replace(/\s+/g, ' ').trim()).toBe('Room 42_Adoring_Lewd_people');
      expect(links.map((link) => link.textContent)).toEqual(['Adoring', 'people']);
      expect(links[0].getAttribute('href')).toBe('https://www.google.com/search?q=adoring+meaning');
      expect(links[0].getAttribute('target')).toBe('_blank');
      expect(links[0].getAttribute('rel')).toBe('noopener noreferrer');
      expect(links[0].getAttribute('aria-label')).toBe('Search Google for Adoring');
    });

    it('shows a room name that is not a fun name as plain text', async () => {
      me.get.mockReturnValue(of(ANN));
      rooms.exists.mockReturnValue(of({ id: 'sprint_42', exists: true, nameWords: null }));

      const { element } = await render('sprint_42');

      const heading = element.querySelector('h1') as HTMLElement;
      expect(heading.querySelector('a')).toBeNull();
      expect(heading.textContent?.trim()).toBe('Room sprint_42');
    });

    it('copies the room name and the full room link', async () => {
      me.get.mockReturnValue(of(ANN));
      const writeText = vi.fn().mockResolvedValue(undefined);
      vi.stubGlobal('navigator', { clipboard: { writeText } });
      const { fixture, element } = await render();
      const [name, link] = Array.from(element.querySelectorAll<HTMLButtonElement>('button[aria-label^="Copy room"]'));

      name.click();
      link.click();
      await settle(fixture);

      expect(writeText).toHaveBeenNthCalledWith(1, 'room-1');
      expect(writeText).toHaveBeenNthCalledWith(2, `${location.origin}/room/room-1`);
      vi.unstubAllGlobals();
    });

    it('falls back to the legacy copy command when the clipboard API is unavailable', async () => {
      me.get.mockReturnValue(of(ANN));
      vi.stubGlobal('navigator', { clipboard: { writeText: vi.fn().mockRejectedValue(new Error('insecure')) } });
      const exec = vi.fn().mockReturnValue(true);
      document.execCommand = exec;
      const { fixture, element } = await render();

      element.querySelector<HTMLButtonElement>('button[aria-label^="Copy room"]')?.click();
      await settle(fixture);

      expect(exec).toHaveBeenCalledWith('copy');
      vi.unstubAllGlobals();
    });
  });

  describe('participants', () => {
    it("gives Lolle a rose crown and a wand, but only a wand beside the owner's crown", async () => {
      const lolle = { guid: 'g-lolle', name: 'Lolle', personType: 'obs' } as const;
      me.get.mockReturnValue(of(ANN));
      hub.invoke.mockResolvedValue({ ...JOIN_RESPONSE, people: [ANN, lolle] });

      const { element } = await render();

      const rows = Array.from(element.querySelectorAll('.person'));
      const [ann, lolleRow] = rows;
      expect(ann.querySelector('.crown:not(.lolle-crown)')).not.toBeNull();
      expect(ann.querySelector('.lolle-crown, .wand')).toBeNull();
      expect(lolleRow.querySelector('.lolle-crown')).not.toBeNull();
      expect(lolleRow.querySelector('.wand')).not.toBeNull();
    });

    it('keeps only the owner crown on Lolle when Lolle owns the room, still with the wand', async () => {
      const lolle = { guid: 'g-lolle', name: 'lolle', personType: 'dev' } as const;
      me.get.mockReturnValue(of(lolle));
      hub.invoke.mockResolvedValue({ ...JOIN_RESPONSE, people: [lolle], ownerGuid: lolle.guid });

      const { element } = await render();

      expect(element.querySelectorAll('.person .crown')).toHaveLength(1);
      expect(element.querySelector('.person .lolle-crown')).toBeNull();
      expect(element.querySelector('.person .wand')).not.toBeNull();
    });

    it('tags each tile with its short role and role colour key', async () => {
      me.get.mockReturnValue(of(ANN));
      hub.invoke.mockResolvedValueOnce({ ...JOIN_RESPONSE, people: [ANN, BOB] });
      const { element } = await render();

      const tiles = [...element.querySelectorAll('li.person')];
      expect(tiles.map((tile) => [tile.getAttribute('data-role'), tile.getAttribute('data-short')])).toEqual([
        ['dev', 'DEV'],
        ['test', 'QA'],
      ]);
    });

    it('flags offgrid people on their tile and leaves online people unflagged', async () => {
      me.get.mockReturnValue(of(ANN));
      hub.invoke.mockResolvedValue({ ...JOIN_RESPONSE, people: [ANN, { ...BOB, isOffgrid: true }] });

      const { element } = await render();

      const rows = Array.from(element.querySelectorAll('.person'));
      expect(rows.find((row) => row.textContent?.includes('Bob'))?.getAttribute('title')).toContain('Not connected');
      expect(rows.find((row) => row.textContent?.includes('Ann'))?.getAttribute('title')).toBeNull();
    });

    it('greys out the tile of an offgrid person', async () => {
      me.get.mockReturnValue(of(ANN));
      hub.invoke.mockResolvedValue({ ...JOIN_RESPONSE, people: [ANN, { ...BOB, isOffgrid: true }] });

      const { element } = await render();

      const tiles = Array.from(element.querySelectorAll('.person'));
      expect(tiles.find((tile) => tile.textContent?.includes('Bob'))?.classList).toContain('offgrid-person');
      expect(tiles.find((tile) => tile.textContent?.includes('Ann'))?.classList).not.toContain('offgrid-person');
    });

    it('tells you when you lose the server, greys out your own tile and clears it when you are back', async () => {
      me.get.mockReturnValue(of(ANN));
      const { fixture, element } = await render();
      expect(popup.show).not.toHaveBeenCalled();

      hub.dropConnection();
      await settle(fixture);

      expect(popup.show).toHaveBeenCalledWith(expect.stringContaining('disconnected from the server'), true);
      expect(element.querySelector('.person')?.classList).toContain('offgrid-person');

      hub.reconnect();
      await settle(fixture);

      expect(popup.dismiss).toHaveBeenCalled();
      expect(element.querySelector('.person')?.classList).not.toContain('offgrid-person');
    });
  });

  describe('round history', () => {
    it('shows the round history of the room beside the cards', async () => {
      me.get.mockReturnValue(of(ANN));
      hub.invoke.mockResolvedValue({
        ...JOIN_RESPONSE,
        roundHistory: [
          { round: 2, averageMark: 4.5, revealedAt: '2026-03-01T10:30:00+00:00' },
          { round: 1, averageMark: null, revealedAt: '2026-03-01T10:00:00+00:00' },
        ],
      });

      const { element } = await render();
      const history = element.querySelector('.rail app-round-history')!;

      expect(Array.from(history.querySelectorAll('li .round')).map((item) => item.textContent)).toEqual(['Round 2', 'Round 1']);
      expect(history.querySelector('li .average')?.textContent?.trim()).toBe('4.5');
    });

    it('adds a round to the history for everyone when the votes are revealed', async () => {
      me.get.mockReturnValue(of(ANN));
      const { fixture, element } = await render();
      expect(element.querySelector('app-round-history')?.textContent).toContain('No rounds revealed yet.');

      hub.emit(HUB_EVENTS.VotesShown, {
        ...revealed(STATISTICS),
        roundHistory: [{ round: 1, averageMark: 5, revealedAt: '2026-03-01T10:00:00+00:00' }],
      });
      await settle(fixture);

      expect(element.querySelector('app-round-history li .round')?.textContent).toBe('Round 1');
      expect(element.querySelector('app-round-history li .average')?.textContent?.trim()).toBe('5');
    });
  });

  describe('voting', () => {
    const OBS = { guid: 'g-obs', name: 'Olga', personType: 'obs' } as const;

    function buttonLabelled(element: HTMLElement, text: string): HTMLButtonElement {
      return Array.from(element.querySelectorAll('button')).find((b) =>
        b.textContent?.includes(text),
      ) as HTMLButtonElement;
    }

    function markOf(element: HTMLElement, name: string): string {
      const row = Array.from(element.querySelectorAll('li.person')).find((r) => r.textContent?.includes(name));
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
        statistics: null,
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

    it('shows the round timer for everyone, and keeps it through the reveal and the countdown', async () => {
      const { fixture, element } = await render();
      const timer = () => element.querySelector('app-round-timer [role="timer"]');
      expect(timer()?.textContent).toContain('Round time');

      hub.emit(HUB_EVENTS.CountdownUpdate, { countdown: 3, reset: false });
      await settle(fixture);
      expect(timer()).not.toBeNull();

      hub.emit(HUB_EVENTS.VotesShown, revealed(STATISTICS));
      await settle(fixture);
      expect(timer()).not.toBeNull();
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

      hub.emit(HUB_EVENTS.VotesShown, {
        votes: {},
        hasEveryoneVoted: true,
        votingFinished: true,
        countdown: null,
        statistics: null,
      });
      await settle(fixture);

      const radios = Array.from(element.querySelectorAll<HTMLInputElement>('app-card-picker input[type="radio"]'));
      expect(radios.length).toBeGreaterThan(0);
      expect(radios.every((radio) => radio.disabled)).toBe(true);
    });

    it('disables the cards for an observer', async () => {
      me.get.mockReturnValue(of(OBS));

      const { element } = await render();

      const radios = Array.from(element.querySelectorAll<HTMLInputElement>('app-card-picker input[type="radio"]'));
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

      hub.emit(HUB_EVENTS.VotesShown, {
        votes: {},
        hasEveryoneVoted: true,
        votingFinished: true,
        countdown: null,
        statistics: null,
      });
      await settle(fixture);

      expect(buttonLabelled(element, 'Show votes').disabled).toBe(true);
      expect(buttonLabelled(element, 'Clear votes').disabled).toBe(false);
    });
  });

  describe('results', () => {
    beforeEach(() => me.get.mockReturnValue(of(ANN)));

    it('shows no results while voting is open', async () => {
      const { element } = await render();

      expect(element.querySelector('app-results')).toBeNull();
      expect(element.querySelector('app-fireworks')).not.toBeNull();
    });

    it('shows the results once the votes are revealed, and removes them when cleared', async () => {
      const { fixture, element } = await render();

      hub.emit(HUB_EVENTS.VotesShown, revealed(STATISTICS));
      await settle(fixture);
      expect(element.querySelector('app-results')).not.toBeNull();

      hub.emit(HUB_EVENTS.VotesCleared, { roundStartedAt: '2026-01-01T12:10:00+00:00' });
      await settle(fixture);
      expect(element.querySelector('app-results')).toBeNull();
    });

    it('shows the results straight away when joining a revealed room', async () => {
      hub.invoke.mockResolvedValue({ ...JOIN_RESPONSE, voteStatus: revealed(STATISTICS) });

      const { element } = await render();

      expect(element.querySelector('app-results')).not.toBeNull();
    });

    it('launches fireworks when a live reveal ends in consensus', async () => {
      const { fixture } = await render();

      hub.emit(HUB_EVENTS.VotesShown, revealed(CONSENSUS_STATISTICS));
      await settle(fixture);

      expect(launcher.create).toHaveBeenCalledTimes(1);
      expect(fireworks.launch).toHaveBeenCalled();
    });

    it('launches no fireworks for a consensus that was already there when joining', async () => {
      hub.invoke.mockResolvedValue({
        ...JOIN_RESPONSE,
        voteStatus: revealed(CONSENSUS_STATISTICS),
      });

      await render();

      expect(launcher.create).not.toHaveBeenCalled();
    });

    it('stops the fireworks when the votes are cleared', async () => {
      const { fixture } = await render();
      hub.emit(HUB_EVENTS.VotesShown, revealed(CONSENSUS_STATISTICS));
      await settle(fixture);

      hub.emit(HUB_EVENTS.VotesCleared, { roundStartedAt: '2026-01-01T12:10:00+00:00' });
      await settle(fixture);

      expect(fireworks.dispose).toHaveBeenCalled();
    });

    it('stops the fireworks when the user clears the votes', async () => {
      const { fixture, element } = await render();
      hub.emit(HUB_EVENTS.VotesShown, revealed(CONSENSUS_STATISTICS));
      await settle(fixture);

      const clear = Array.from(element.querySelectorAll('button')).find((b) => b.textContent?.includes('Clear votes'));
      (clear as HTMLButtonElement).click();

      expect(fireworks.dispose).toHaveBeenCalled();
    });

    it('launches fireworks again for the next consensus reveal', async () => {
      const { fixture } = await render();
      hub.emit(HUB_EVENTS.VotesShown, revealed(CONSENSUS_STATISTICS));
      await settle(fixture);
      hub.emit(HUB_EVENTS.VotesCleared, { roundStartedAt: '2026-01-01T12:10:00+00:00' });
      await settle(fixture);

      hub.emit(HUB_EVENTS.VotesShown, revealed(CONSENSUS_STATISTICS));
      await settle(fixture);

      expect(launcher.create).toHaveBeenCalledTimes(2);
    });
  });

  it('the Leave room button returns to the home page', async () => {
    me.get.mockReturnValue(of(ANN));
    const { fixture, element } = await render();

    (
      Array.from(element.querySelectorAll('button')).find((b) => b.textContent?.includes('Leave')) as HTMLElement
    ).click();
    await settle(fixture);

    expect(navigate).toHaveBeenCalledWith('/');
  });

  describe('room management', () => {
    const radioFor = (element: HTMLElement, value: string) =>
      element.querySelector(`[aria-label="Your role"] input[value="${value}"]`) as HTMLInputElement;

    it('checks the radio of the own role', async () => {
      me.get.mockReturnValue(of(ANN));

      const { element } = await render();

      expect(radioFor(element, 'dev').checked).toBe(true);
      expect(radioFor(element, 'obs').checked).toBe(false);
    });

    it('sends the chosen role to the hub', async () => {
      me.get.mockReturnValue(of(ANN));
      const { fixture, element } = await render();

      radioFor(element, 'obs').click();
      await settle(fixture);

      expect(hub.invoke).toHaveBeenLastCalledWith('ChangePersonType', 'obs');
    });

    it('follows a role change pushed by the server', async () => {
      me.get.mockReturnValue(of(ANN));
      const { fixture, element } = await render();

      hub.emit(HUB_EVENTS.PersonTypeChanged, {
        person: { ...ANN, personType: 'obs' },
        voteStatus: JOIN_RESPONSE.voteStatus,
      });
      await settle(fixture);

      expect(radioFor(element, 'obs').checked).toBe(true);
      const radios = Array.from(element.querySelectorAll<HTMLInputElement>('app-card-picker input[type="radio"]'));
      expect(radios.every((radio) => radio.disabled)).toBe(true);
    });

    it('shows Settings to the owner only and opens the dialog with the other members', async () => {
      me.get.mockReturnValue(of(ANN));
      const { fixture, element } = await render();
      hub.emit(HUB_EVENTS.PersonJoined, { person: BOB, vote: null, personType: 'test' });
      await settle(fixture);

      const settings = Array.from(element.querySelectorAll('button')).find((b) => b.textContent?.includes('Settings'));
      settings?.click();

      const data = dialog.open.mock.calls[0][1].data;
      expect(data.configuration).toEqual(JOIN_RESPONSE.configuration);
      expect(data.candidates).toEqual([BOB]);
    });

    it('remembers the saved settings as the defaults for new rooms', async () => {
      localStorage.clear();
      me.get.mockReturnValue(of(ANN));
      const { fixture, element } = await render();
      const settings = Array.from(element.querySelectorAll('button')).find((b) => b.textContent?.includes('Settings'));
      settings?.click();
      const updated = { ...JOIN_RESPONSE.configuration, countdownSeconds: 11 };

      await dialog.open.mock.calls[0][1].data.save(updated);
      await settle(fixture);

      expect(TestBed.inject(RoomDefaultsService).load().configuration).toEqual(updated);
    });

    it('hides Settings from a member who is not the owner', async () => {
      me.get.mockReturnValue(of(BOB));

      const { element } = await render();

      expect(Array.from(element.querySelectorAll('button')).some((b) => b.textContent?.includes('Settings'))).toBe(
        false,
      );
    });

    it('shows Settings after ownership is transferred to the user', async () => {
      me.get.mockReturnValue(of(BOB));
      const { fixture, element } = await render();

      hub.emit(HUB_EVENTS.OwnershipTransferred, { newOwner: BOB });
      await settle(fixture);

      expect(Array.from(element.querySelectorAll('button')).some((b) => b.textContent?.includes('Settings'))).toBe(
        true,
      );
    });
  });

  describe('the card set on big screens', () => {
    const WATCHER = { guid: 'g-watch', name: 'Olga', personType: 'obs' } as const;

    beforeEach(() => {
      me.get.mockReturnValue(of(ANN));
      hub.invoke.mockResolvedValue({ ...JOIN_RESPONSE, people: [ANN, BOB] });
    });

    function strip(element: HTMLElement): HTMLElement {
      return element.querySelector<HTMLElement>('.strip')!;
    }

    it('places the card set above the people list, out of the rail', async () => {
      const { element } = await render();

      const people = element.querySelector('ul.people')!;
      expect(strip(element).contains(element.querySelector('app-card-picker'))).toBe(true);
      expect(strip(element).compareDocumentPosition(people) & Node.DOCUMENT_POSITION_FOLLOWING).toBeTruthy();
      expect(element.querySelector('.rail app-card-picker')).toBeNull();
      expect(element.querySelector('.rail app-round-history')).not.toBeNull();
    });

    it('keeps the card set open while voting, including after the user has voted', async () => {
      const { fixture, element } = await render();

      hub.emit(HUB_EVENTS.VoteCast, { personGuid: ANN.guid });
      await settle(fixture);

      expect(strip(element).classList).not.toContain('revealed');
    });

    it('collapses the card set once the votes are revealed', async () => {
      const { fixture, element } = await render();

      hub.emit(HUB_EVENTS.VotesShown, revealed(STATISTICS));
      await settle(fixture);

      expect(strip(element).classList).toContain('revealed');
    });

    it('reopens the card set when the next round voting opens', async () => {
      const { fixture, element } = await render();
      hub.emit(HUB_EVENTS.VotesShown, revealed(STATISTICS));
      await settle(fixture);

      hub.emit(HUB_EVENTS.VotesCleared, { roundStartedAt: '2026-01-01T12:10:00+00:00' });
      await settle(fixture);

      expect(strip(element).classList).not.toContain('revealed');
    });

    it('keeps the card set collapsed after a reload of a revealed round', async () => {
      hub.invoke.mockResolvedValue({ ...JOIN_RESPONSE, people: [ANN, BOB], voteStatus: revealed(STATISTICS) });

      const { element } = await render();

      expect(strip(element).classList).toContain('revealed');
    });

    it('collapses the card set for an observer once the votes are revealed', async () => {
      me.get.mockReturnValue(of(WATCHER));
      const { fixture, element } = await render();

      hub.emit(HUB_EVENTS.VotesShown, revealed(STATISTICS));
      await settle(fixture);

      expect(strip(element).classList).toContain('revealed');
    });
  });

  describe('the Alarm', () => {
    const ENDS_AT = '2026-01-01T12:15:00+00:00';

    afterEach(() => vi.useRealTimers());

    function alarmButton(element: HTMLElement): HTMLButtonElement | null {
      return element.querySelector('app-alarm-button button');
    }

    it('shows the Alarm button, with the icon of the chosen sound', async () => {
      localStorage.setItem(ALARM_SOUND_STORAGE_KEY, 'duck');
      me.get.mockReturnValue(of(ANN));
      const { element } = await render();

      expect(alarmButton(element)).not.toBeNull();
      expect(alarmButton(element)!.textContent).toContain('🦆');
    });

    it('shows no Alarm button when the room turned the Alarm off', async () => {
      hub.invoke.mockResolvedValue({ ...JOIN_RESPONSE, configuration: { ...JOIN_RESPONSE.configuration, alarmEnabled: false } });
      me.get.mockReturnValue(of(ANN));
      const { element } = await render();

      expect(alarmButton(element)).toBeNull();
    });

    it('starts, then cancels, then resets through the hub', async () => {
      me.get.mockReturnValue(of(ANN));
      const { fixture, element } = await render();

      alarmButton(element)!.click();
      expect(hub.invoke).toHaveBeenCalledWith('StartAlarm');

      hub.emit(HUB_EVENTS.AlarmChanged, { status: 'running', endsAt: ENDS_AT });
      fixture.detectChanges();
      alarmButton(element)!.click();
      expect(hub.invoke).toHaveBeenCalledWith('CancelAlarm');

      hub.emit(HUB_EVENTS.AlarmChanged, { status: 'elapsed', endsAt: ENDS_AT });
      fixture.detectChanges();
      alarmButton(element)!.click();
      expect(hub.invoke).toHaveBeenCalledWith('ResetAlarm');
    });

    it('plays this person\'s own sound once when the Alarm elapses', async () => {
      localStorage.setItem(ALARM_SOUND_STORAGE_KEY, 'gong');
      me.get.mockReturnValue(of(ANN));
      const { fixture } = await render();

      hub.emit(HUB_EVENTS.AlarmChanged, { status: 'elapsed', endsAt: ENDS_AT });
      fixture.detectChanges();
      await fixture.whenStable();

      expect(alarmPlayer.play).toHaveBeenCalledOnce();
      expect(alarmPlayer.play).toHaveBeenCalledWith('gong');
    });

    it('plays nothing and does not jingle for an Alarm that was already elapsed when joining', async () => {
      hub.invoke.mockResolvedValue({ ...JOIN_RESPONSE, alarm: { status: 'elapsed', endsAt: ENDS_AT } });
      me.get.mockReturnValue(of(ANN));
      const { element } = await render();

      expect(alarmPlayer.play).not.toHaveBeenCalled();
      expect(alarmButton(element)!.textContent).toContain("Time's up");
      expect(alarmButton(element)!.classList.contains('jingle')).toBe(false);
    });

    it('jingles for a few seconds after a live elapse, and stops when it is reset', async () => {
      me.get.mockReturnValue(of(ANN));
      const { fixture, element } = await render();
      vi.useFakeTimers({ toFake: ['setTimeout'] });

      hub.emit(HUB_EVENTS.AlarmChanged, { status: 'elapsed', endsAt: ENDS_AT });
      fixture.detectChanges();
      expect(alarmButton(element)!.classList.contains('jingle')).toBe(true);

      vi.advanceTimersByTime(3500);
      fixture.detectChanges();
      expect(alarmButton(element)!.classList.contains('jingle')).toBe(false);

      hub.emit(HUB_EVENTS.AlarmChanged, { status: 'idle', endsAt: null });
      fixture.detectChanges();
      expect(alarmButton(element)!.classList.contains('jingle')).toBe(false);
    });

    it('plays nothing when it starts or is cancelled', async () => {
      me.get.mockReturnValue(of(ANN));
      const { fixture } = await render();

      hub.emit(HUB_EVENTS.AlarmChanged, { status: 'running', endsAt: ENDS_AT });
      hub.emit(HUB_EVENTS.AlarmChanged, { status: 'idle', endsAt: null });
      fixture.detectChanges();
      await fixture.whenStable();

      expect(alarmPlayer.play).not.toHaveBeenCalled();
    });

    it('lets Silent keep the jingle but make no sound', async () => {
      localStorage.setItem(ALARM_SOUND_STORAGE_KEY, 'silent');
      me.get.mockReturnValue(of(ANN));
      const { fixture, element } = await render();

      hub.emit(HUB_EVENTS.AlarmChanged, { status: 'elapsed', endsAt: ENDS_AT });
      fixture.detectChanges();

      expect(alarmPlayer.play).toHaveBeenCalledWith('silent');
      expect(alarmButton(element)!.classList.contains('jingle')).toBe(true);
    });
  });
});
