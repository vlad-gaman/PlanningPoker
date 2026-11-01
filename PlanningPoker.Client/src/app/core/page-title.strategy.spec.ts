import { TestBed } from '@angular/core/testing';
import { Title } from '@angular/platform-browser';
import { Router, provideRouter } from '@angular/router';
import { PageTitleStrategy } from './page-title.strategy';

describe('PageTitleStrategy', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideRouter([
          { path: 'credits', title: 'Credits', children: [] },
          { path: 'untitled', children: [] },
        ]),
        PageTitleStrategy,
      ],
    });
  });

  async function titleAfter(url: string): Promise<string> {
    const strategy = TestBed.inject(PageTitleStrategy);
    const router = TestBed.inject(Router);
    await router.navigateByUrl(url);
    strategy.updateTitle(router.routerState.snapshot);
    return TestBed.inject(Title).getTitle();
  }

  it('suffixes the route title with the product name', async () => {
    expect(await titleAfter('/credits')).toBe('Credits - Planning Poker');
  });

  it('uses the product name alone when a route has no title', async () => {
    expect(await titleAfter('/untitled')).toBe('Planning Poker');
  });
});
