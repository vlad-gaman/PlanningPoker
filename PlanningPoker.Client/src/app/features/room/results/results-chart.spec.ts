import { TestBed } from '@angular/core/testing';
import { ThemeService } from '../../../core/theme.service';
import { CardOption, GroupStatistics } from '../../../models/room';
import { ChartFactory } from './chart-factory';
import { ResultsChart } from './results-chart';

const CARDS: CardOption[] = [{ value: '5', display: null }];
const GROUP: GroupStatistics = {
  marks: [{ mark: '5', percentage: 100 }],
  highestMark: '5',
  averageMark: 5,
};

describe('ResultsChart', () => {
  const handles: { destroy: ReturnType<typeof vi.fn>; draw: ReturnType<typeof vi.fn> }[] = [];
  const create = vi.fn();

  beforeEach(() => {
    handles.length = 0;
    create.mockReset();
    create.mockImplementation(() => {
      const handle = { destroy: vi.fn(), draw: vi.fn() };
      handles.push(handle);
      return handle;
    });
    TestBed.configureTestingModule({
      providers: [{ provide: ChartFactory, useValue: { create } }],
    });
  });

  function render(group: GroupStatistics = GROUP) {
    const fixture = TestBed.createComponent(ResultsChart);
    fixture.componentRef.setInput('title', 'All votes');
    fixture.componentRef.setInput('group', group);
    fixture.componentRef.setInput('cards', CARDS);
    fixture.detectChanges();
    return fixture;
  }

  it('draws a chart on the canvas', () => {
    const fixture = render();

    expect(create).toHaveBeenCalledTimes(1);
    expect(create.mock.calls[0][0]).toBe(fixture.nativeElement.querySelector('canvas'));
  });

  it('describes the chart to assistive technology', () => {
    const fixture = render();

    const canvas = fixture.nativeElement.querySelector('canvas') as HTMLCanvasElement;
    expect(canvas.getAttribute('role')).toBe('img');
    expect(canvas.getAttribute('aria-label')).toBe('All votes: 5 100%');
  });

  it('shows the average', () => {
    const fixture = render();

    expect(fixture.nativeElement.querySelector('.average').textContent).toContain('Average: 5');
  });

  it('shows an average of zero', () => {
    const fixture = render({ ...GROUP, averageMark: 0 });

    expect(fixture.nativeElement.querySelector('.average .value').textContent).toBe('0');
  });

  it('leaves the average blank when no mark is numeric', () => {
    const fixture = render({ ...GROUP, averageMark: null });

    expect(fixture.nativeElement.querySelector('.average .value').textContent).toBe('');
  });

  it('replaces the chart when the statistics change', () => {
    const fixture = render();

    fixture.componentRef.setInput('group', { ...GROUP, averageMark: 3 });
    fixture.detectChanges();

    expect(handles[0].destroy).toHaveBeenCalledTimes(1);
    expect(create).toHaveBeenCalledTimes(2);
  });

  it('replaces the chart when the theme changes', () => {
    render();

    TestBed.inject(ThemeService).set('dark');
    TestBed.tick();

    expect(handles[0].destroy).toHaveBeenCalledTimes(1);
    expect(create).toHaveBeenCalledTimes(2);
  });

  it('destroys the chart with the component', () => {
    const fixture = render();

    fixture.destroy();

    expect(handles[0].destroy).toHaveBeenCalledTimes(1);
  });
});
