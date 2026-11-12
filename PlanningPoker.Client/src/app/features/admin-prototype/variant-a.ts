// PROTOTYPE (issue 78) variant A: tabbed console. Login is two steps; the console has Rooms / People / Issues / Stats tabs
// with dense tables and inline "Close? Yes / No" confirmation on the row. Wipe me.
import { ChangeDetectionStrategy, Component, computed, input, signal } from '@angular/core';
import { PrototypeStore } from './prototype-store';

type Tab = 'rooms' | 'people' | 'issues' | 'stats';

@Component({
  selector: 'app-variant-a',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (!store().signedIn()) {
      <section class="login">
        <h1>Operator sign-in</h1>
        @if (!store().needsTotp()) {
          <label>Username <input #user autocomplete="username" /></label>
          <label>Password <input #pass type="password" autocomplete="current-password" /></label>
          <button type="button" (click)="store().login(user.value, pass.value)">Continue</button>
          <p class="hint">Try: any / any (signs in), password "wrong", password "lock", or user "mfa" for the code step.</p>
        } @else {
          <p>Enter the 6-digit code from your authenticator app.</p>
          <label>Code <input #code inputmode="numeric" maxlength="6" autocomplete="one-time-code" /></label>
          <button type="button" (click)="store().verifyTotp(code.value)">Sign in</button>
          <button type="button" class="link" (click)="store().logout()">Back</button>
          <p class="hint">Code for this prototype: 123456</p>
        }
        @if (store().loginError()) {
          <p class="error" role="alert">{{ store().loginError() }}</p>
        }
      </section>
    } @else {
      <section class="console">
        <div class="bar">
          <h1>Management</h1>
          <nav role="tablist">
            @for (t of tabs; track t.key) {
              <button type="button" role="tab" [class.on]="tab() === t.key" (click)="tab.set(t.key)">
                {{ t.label }}
                @if (t.key === 'issues' && errorCount() > 0) {
                  <span class="badge">{{ errorCount() }}</span>
                }
              </button>
            }
          </nav>
          <button type="button" class="link" (click)="store().logout()">Sign out</button>
        </div>

        @switch (tab()) {
          @case ('rooms') {
            <table>
              <thead><tr><th>Room</th><th>State</th><th>People</th><th>Created</th><th>Last activity</th><th></th></tr></thead>
              <tbody>
                @for (r of store().rooms(); track r.id) {
                  <tr>
                    <td>{{ r.name }}</td>
                    <td><span class="pill {{ r.state }}">{{ r.state }}</span></td>
                    <td>{{ r.people.length }}</td>
                    <td>{{ r.created }}</td>
                    <td>{{ r.lastActivity }}</td>
                    <td class="act">
                      @if (confirming() === 'room:' + r.id) {
                        Close room and notify {{ r.people.length }} people?
                        <button type="button" class="danger" (click)="store().closeRoom(r.id); confirming.set('')">Yes, close</button>
                        <button type="button" class="link" (click)="confirming.set('')">No</button>
                      } @else {
                        <button type="button" (click)="confirming.set('room:' + r.id)">Close room</button>
                      }
                    </td>
                  </tr>
                }
              </tbody>
            </table>
          }
          @case ('people') {
            <table>
              <thead><tr><th>Person</th><th>Role</th><th>Room</th><th>Status</th><th>Last seen</th><th></th></tr></thead>
              <tbody>
                @for (row of people(); track row.person.id) {
                  <tr>
                    <td>{{ row.person.name }}@if (row.person.isOwner) { <small> (owner)</small> }</td>
                    <td>{{ row.person.role }}</td>
                    <td>{{ row.room.name }}</td>
                    <td><span class="pill {{ row.person.status }}">{{ row.person.status }}</span></td>
                    <td>{{ row.person.lastSeen }}</td>
                    <td class="act">
                      @if (confirming() === 'p:' + row.person.id) {
                        Remove {{ row.person.name }}?
                        <button type="button" class="danger" (click)="store().removePerson(row.room.id, row.person.id); confirming.set('')">Yes, remove</button>
                        <button type="button" class="link" (click)="confirming.set('')">No</button>
                      } @else {
                        <button type="button" (click)="confirming.set('p:' + row.person.id)">Remove</button>
                      }
                    </td>
                  </tr>
                }
              </tbody>
            </table>
            <p class="hint">Votes are never shown here.</p>
          }
          @case ('issues') {
            <table>
              <thead><tr><th>Time</th><th>Level</th><th>Source</th><th>Message</th></tr></thead>
              <tbody>
                @for (i of store().issues(); track i.id) {
                  <tr>
                    <td>{{ i.time }}</td>
                    <td><span class="pill {{ i.level }}">{{ i.level }}</span></td>
                    <td>{{ i.source }}</td>
                    <td>{{ i.message }}</td>
                  </tr>
                }
              </tbody>
            </table>
          }
          @case ('stats') {
            <dl class="stats">
              <dt>Rooms</dt><dd>{{ store().rooms().length }}</dd>
              <dt>People online</dt><dd>{{ count('online') }}</dd>
              <dt>People offgrid</dt><dd>{{ count('offgrid') }}</dd>
              <dt>Restored rooms</dt><dd>{{ restored() }}</dd>
              <dt>Abandoned rooms swept (24 h)</dt><dd>{{ store().stats.abandonedSwept24h }}</dd>
              <dt>Uptime</dt><dd>{{ store().stats.uptime }}</dd>
              <dt>Last store write</dt><dd>{{ store().stats.lastWrite }}</dd>
            </dl>
          }
        }
      </section>
    }
  `,
  styles: `
    :host { display: block; padding: 1.5rem 1rem 5rem; color: var(--text-color); }
    .login { max-width: 22rem; margin: 3rem auto; display: grid; gap: 0.75rem; padding: 1.5rem; border: 1px solid var(--border-color);
      border-radius: 8px; background: var(--card-bg); }
    label { display: grid; gap: 0.25rem; font-size: 0.9rem; }
    input { padding: 0.4rem 0.5rem; border: 1px solid var(--card-border); border-radius: 4px; background: var(--bg-color); color: var(--text-color); }
    button { padding: 0.35rem 0.8rem; border: 1px solid var(--card-border); border-radius: 4px; background: var(--card-bg); color: var(--text-color); cursor: pointer; }
    button.danger { background: var(--verdict-bad); border-color: var(--verdict-bad); color: #fff; }
    button.link { border: 0; background: none; color: var(--primary-color); }
    .hint { color: var(--muted-color); font-size: 0.8rem; margin: 0; }
    .error { color: var(--verdict-bad); margin: 0; }
    .console { max-width: 1140px; margin: 0 auto; }
    .bar { display: flex; align-items: center; gap: 1.5rem; border-bottom: 1px solid var(--border-color); margin-bottom: 1rem; }
    .bar h1 { font-size: 1.1rem; margin: 0; }
    nav { display: flex; gap: 0.25rem; flex: 1; }
    nav button { border: 0; border-bottom: 3px solid transparent; border-radius: 0; background: none; padding: 0.6rem 0.9rem; }
    nav button.on { border-bottom-color: var(--primary-color); font-weight: 600; }
    .badge { background: var(--verdict-bad); color: #fff; border-radius: 999px; padding: 0 0.4rem; font-size: 0.75rem; margin-left: 0.25rem; }
    table { width: 100%; border-collapse: collapse; font-size: 0.9rem; }
    th, td { text-align: left; padding: 0.45rem 0.6rem; border-bottom: 1px solid var(--border-color); }
    th { color: var(--muted-color); font-weight: 600; }
    td.act { text-align: right; white-space: nowrap; }
    .pill { padding: 0.05rem 0.5rem; border-radius: 999px; font-size: 0.78rem; background: var(--panel-bg); border: 1px solid var(--border-color); }
    .pill.error, .pill.offgrid { color: var(--verdict-bad); }
    .pill.warning, .pill.restored { color: var(--verdict-mid); }
    .pill.online, .pill.voting { color: var(--verdict-good); }
    .stats { display: grid; grid-template-columns: max-content 1fr; gap: 0.4rem 1.5rem; max-width: 28rem; }
    .stats dt { color: var(--muted-color); } .stats dd { margin: 0; font-weight: 600; }
  `,
})
export class VariantA {
  readonly store = input.required<PrototypeStore>();
  protected readonly tabs: { key: Tab; label: string }[] = [
    { key: 'rooms', label: 'Rooms' }, { key: 'people', label: 'People' }, { key: 'issues', label: 'Issues' }, { key: 'stats', label: 'Stats' },
  ];
  protected readonly tab = signal<Tab>('rooms');
  protected readonly confirming = signal('');
  protected readonly people = computed(() => this.store().rooms().flatMap((room) => room.people.map((person) => ({ room, person }))));
  protected readonly errorCount = computed(() => this.store().issues().filter((i) => i.level === 'error').length);
  protected readonly restored = computed(() => this.store().rooms().filter((r) => r.state === 'restored').length);

  protected count(status: 'online' | 'offgrid'): number {
    return this.people().filter((x) => x.person.status === status).length;
  }
}
