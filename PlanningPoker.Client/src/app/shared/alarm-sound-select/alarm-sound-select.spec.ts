import { TestBed } from '@angular/core/testing';
import { ALARM_SOUND_STORAGE_KEY, AlarmSoundService } from '../../core/alarm-sound.service';
import { AlarmPlayer } from '../../features/room/alarm/alarm-player';
import { AlarmSoundSelect } from './alarm-sound-select';

describe('AlarmSoundSelect', () => {
  const player = { play: vi.fn() };

  beforeEach(() => {
    localStorage.clear();
    player.play.mockReset().mockResolvedValue(undefined);
    TestBed.configureTestingModule({ providers: [{ provide: AlarmPlayer, useValue: player }] });
  });

  function create() {
    const fixture = TestBed.createComponent(AlarmSoundSelect);
    fixture.detectChanges();
    const select = (fixture.nativeElement as HTMLElement).querySelector('select') as HTMLSelectElement;
    return { fixture, select };
  }

  it('offers every sound with its icon, Silent last, as a labelled dropdown', () => {
    const { select } = create();

    const labels = Array.from(select.options).map((option) => option.textContent?.trim());
    expect(labels).toHaveLength(12);
    expect(labels[0]).toBe('🔔 Bell');
    expect(labels.at(-1)).toBe('🔕 Silent');
    expect(select.getAttribute('aria-label')).toBe('Alarm sound');
  });

  it('shows the stored sound', () => {
    localStorage.setItem(ALARM_SOUND_STORAGE_KEY, 'rooster');

    expect(create().select.value).toBe('rooster');
  });

  it('remembers a picked sound and plays it as a preview', () => {
    const { select } = create();

    select.value = 'duck';
    select.dispatchEvent(new Event('change'));

    expect(TestBed.inject(AlarmSoundService).sound()).toBe('duck');
    expect(localStorage.getItem(ALARM_SOUND_STORAGE_KEY)).toBe('duck');
    expect(player.play).toHaveBeenCalledWith('duck');
  });

  it('ignores a value that is not a sound', () => {
    const { fixture } = create();

    fixture.componentInstance.pick('trombone');

    expect(TestBed.inject(AlarmSoundService).sound()).toBe('bell');
    expect(player.play).not.toHaveBeenCalled();
  });

  it('follows a sound changed elsewhere', () => {
    const { fixture, select } = create();

    TestBed.inject(AlarmSoundService).set('gong');
    fixture.detectChanges();

    expect(select.value).toBe('gong');
  });
});
