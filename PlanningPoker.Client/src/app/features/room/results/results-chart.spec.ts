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

  it('shows the strictly highest mark next to the average', () => {
    const fixture = render();

    expect(fixture.nativeElement.querySelector('.most .value').textContent).toBe('5');
    expect(fixture.nativeElement.querySelector('.most').textContent).toContain('Most votes: 5');
  });

  it('shows the display text of the highest mark', () => {
    const fixture = TestBed.createComponent(ResultsChart);
    fixture.componentRef.setInput('title', 'All votes');
    fixture.componentRef.setInput('group', { ...GROUP, marks: [{ mark: 'coffee', percentage: 100 }], highestMark: 'coffee' });
    fixture.componentRef.setInput('cards', [{ value: 'coffee', display: '☕' }]);
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('.most .value').textContent).toBe('☕');
  });

  it('omits the highest mark on a tie', () => {
    const fixture = render({
      marks: [
        { mark: '3', percentage: 50 },
        { mark: '5', percentage: 50 },
      ],
      highestMark: null,
      averageMark: 4,
    });

    expect(fixture.nativeElement.querySelector('.most')).toBeNull();
  });

  it('shows "No votes" without a canvas or chart for an empty group, keeping a blank average', () => {
    const fixture = render({ marks: [], highestMark: null, averageMark: null });

    expect(fixture.nativeElement.querySelector('.no-votes').textContent).toBe('No votes');
    expect(fixture.nativeElement.querySelector('canvas')).toBeNull();
    expect(create).not.toHaveBeenCalled();
    expect(fixture.nativeElement.querySelector('.average .value').textContent).toBe('');
    expect(fixture.nativeElement.querySelector('.most')).toBeNull();
  });

  it('destroys the chart when the group becomes empty', () => {
    const fixture = render();

    fixture.componentRef.setInput('group', { marks: [], highestMark: null, averageMark: null });
    fixture.detectChanges();

    expect(handles[0].destroy).toHaveBeenCalledTimes(1);
    expect(create).toHaveBeenCalledTimes(1);
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
