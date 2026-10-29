import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';
import { ApiRequestError } from '../../core/api-request-error';
import { FlashMessageService } from '../../core/flash-message.service';
import { MeService } from '../../core/me.service';
import { RoomHubClient } from '../../core/room-hub.client';
import { ANN, JOIN_RESPONSE, fakeHub } from './fake-hub.testing';
import { ROOM_NOT_FOUND_MESSAGE, Room } from './room';

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
      providers: [
        provideRouter([]),
        { provide: MeService, useValue: me },
        { provide: RoomHubClient, useValue: hub },
      ],
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
    return throwError(() => new ApiRequestError(401, [{ code: 'no_session', message: 'No session.' }]));
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
    hub.invoke.mockRejectedValue(new ApiRequestError(0, [{ code: 'room_not_found', message: 'Gone.' }]));

    await render();

    expect(navigate).toHaveBeenCalledWith('/');
    expect(TestBed.inject(FlashMessageService).message()).toBe(ROOM_NOT_FOUND_MESSAGE);
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
