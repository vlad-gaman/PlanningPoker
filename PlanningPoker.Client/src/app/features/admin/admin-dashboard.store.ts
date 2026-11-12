import { Injectable, OnDestroy, computed, inject, signal } from '@angular/core';
import { EMPTY, Observable, Subscription, catchError, distinctUntilChanged, exhaustMap, fromEvent, interval, map, of, startWith, switchMap, tap } from 'rxjs';
import { AdminOverviewService } from '../../core/admin-overview.service';
import { ApiRequestError, errorMessage } from '../../core/api-request-error';
import { ERROR_CODES } from '../../models/api';
import { AdminOverview } from '../../models/admin-overview';

/** How often the overview is asked for while the page is in front. */
export const POLL_INTERVAL_MS = 5000;

/** The overview the management page shows, kept fresh by polling while the tab is visible. Provided by the page, so it lives and dies with it. */
@Injectable()
export class AdminDashboardStore implements OnDestroy {
  private readonly overviewService = inject(AdminOverviewService);
  private polling: Subscription | null = null;

  private readonly overviewState = signal<AdminOverview | null>(null);
  private readonly refreshFailedState = signal(false);
  private readonly actionErrorState = signal<string | null>(null);

  /** The latest overview; null until the first answer. */
  readonly overview = this.overviewState.asReadonly();
  /** True while the last refresh failed; the previous overview stays on screen. */
  readonly refreshFailed = this.refreshFailedState.asReadonly();
  /** Why the last close or remove failed; cleared by the next action. */
  readonly actionError = this.actionErrorState.asReadonly();
  readonly errorCount = computed(() => this.overviewState()?.issues.filter((issue) => issue.level === 'error').length ?? 0);

  /** Starts polling now and every few seconds, paused while the tab is hidden and resumed (with an immediate refresh) when it is shown again. */
  start(): void {
    this.stop();
    const visible = fromEvent(document, 'visibilitychange').pipe(
      startWith(null),
      map(() => !document.hidden),
      distinctUntilChanged(),
    );
    this.polling = visible
      .pipe(
        switchMap((isVisible) => (isVisible ? interval(POLL_INTERVAL_MS).pipe(startWith(0)) : EMPTY)),
        // A slow answer is waited for instead of piling requests up.
        exhaustMap(() => this.load()),
      )
      .subscribe();
  }

  stop(): void {
    this.polling?.unsubscribe();
    this.polling = null;
  }

  ngOnDestroy(): void {
    this.stop();
  }

  async refresh(): Promise<void> {
    await new Promise<void>((resolve) => this.load().subscribe({ complete: resolve }));
  }

  /** Closes the room. True when it is gone (closed now, or already gone); false after setting <see cref="actionError"/>. */
  async closeRoom(roomId: string): Promise<boolean> {
    return this.act(this.overviewService.closeRoom(roomId));
  }

  async removePerson(roomId: string, personId: string): Promise<boolean> {
    return this.act(this.overviewService.removePerson(roomId, personId));
  }

  private async act(call: Observable<unknown>): Promise<boolean> {
    this.actionErrorState.set(null);
    let done = true;
    await new Promise<void>((resolve) =>
      call.subscribe({
        error: (error: unknown) => {
          // Already gone is what the Operator wanted; anything else is worth saying.
          const gone = error instanceof ApiRequestError && (error.hasCode(ERROR_CODES.roomNotFound) || error.hasCode(ERROR_CODES.personNotFound));
          if (!gone) {
            this.actionErrorState.set(errorMessage(error));
            done = false;
          }
          resolve();
        },
        complete: resolve,
      }),
    );
    await this.refresh();
    return done;
  }

  private load(): Observable<void> {
    return this.overviewService.get().pipe(
      tap((overview) => {
        this.overviewState.set(overview);
        this.refreshFailedState.set(false);
      }),
      map(() => undefined),
      catchError(() => {
        this.refreshFailedState.set(true);
        return of(undefined);
      }),
    );
  }
}
