import { TestBed } from '@angular/core/testing';
import { CONSENSUS_STATISTICS, STATISTICS } from '../../../testing/fake-hub.testing';
import { ChartFactory } from './chart-factory';
import { Results } from './results';

describe('Results', () => {
  const create = vi.fn(() => ({ destroy: vi.fn(), draw: vi.fn() }));

  beforeEach(() => {
    create.mockClear();
    TestBed.configureTestingModule({
      providers: [{ provide: ChartFactory, useValue: { create } }],
    });
  });

  function render() {
    const fixture = TestBed.createComponent(Results);
    fixture.componentRef.setInput('statistics', STATISTICS);
    fixture.componentRef.setInput('cards', []);
    fixture.detectChanges();
    return fixture;
  }

  it('shows the three charts with their titles', () => {
    const fixture = render();

    const titles = [...fixture.nativeElement.querySelectorAll('figcaption')].map(
      (el) => (el as HTMLElement).textContent,
    );
    expect(titles).toEqual(['All votes', 'Developers votes', 'Testers votes']);
    expect(create).toHaveBeenCalledTimes(3);
  });

  it('shows the average of each group', () => {
    const fixture = render();

    const averages = [...fixture.nativeElement.querySelectorAll('.average .value')].map(
      (el) => (el as HTMLElement).textContent,
    );
    expect(averages).toEqual(['3', '1', '5']);
  });

  it('leaves the average of an empty group blank', () => {
    const fixture = TestBed.createComponent(Results);
    fixture.componentRef.setInput('statistics', CONSENSUS_STATISTICS);
    fixture.componentRef.setInput('cards', []);
    fixture.detectChanges();

    const averages = [...fixture.nativeElement.querySelectorAll('.average .value')].map(
      (el) => (el as HTMLElement).textContent,
    );
    expect(averages).toEqual(['5', '5', '']);
  });
});
