// PROTOTYPE (issue 78) variant C: master-detail. Split-screen login; a room list on the left, the selected room's people on
// the right, stats in a slim top strip, issues in a collapsible bottom drawer. Closing a room means typing its name. Wipe me.
import { ChangeDetectionStrategy, Component, computed, input, signal } from '@angular/core';
import { PrototypeStore } from './prototype-store';

@Component({
  selector: 'app-variant-c',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (!store().signedIn()) {
      <section class="split">
        <div class="brand">
          <h1>Planning Poker</h1>
          <p>Operator access only.</p>
        </div>
        <div class="form">
          @if (!store().needsTotp()) {
            <h2>Sign in</h2>
            <input #user placeholder="Username" aria-label="Username" autocomplete="username" />
            <input #pass type="password" placeholder="Password" aria-label="Password" autocomplete="current-password" />
            <button type="button" (click)="store().login(user.value, pass.value)">Sign in</button>
            <p class="hint">Any / any signs in; "wrong" and "lock" passwords fail; user "mfa" asks for code 123456.</p>
          } @else {
            <h2>Two-step check</h2>
            <input #code placeholder="6-digit code" aria-label="Authenticator code" inputmode="numeric" maxlength="6" />
            <button type="button" (click)="store().verifyTotp(code.value)">Verify</button>
          }
          @if (store().loginError()) {
            <p class="error" role="alert">{{ store().loginError() }}</p>
          }
        </div>
      </section>
    } @else {
      <section class="app">
        <header class="strip">
          <strong>Management</strong>
          <span>{{ store().rooms().length }} rooms</span>
          <span>{{ online() }} online</span>
          <span>{{ offgrid() }} offgrid</span>
          <span>up {{ store().stats.uptime }}</span>
          <span>{{ store().stats.abandonedSwept24h }} swept / 24 h</span>
          <span>store write {{ store().stats.lastWrite }}</span>
          <button type="button" class="ghost" (click)="store().logout()">Sign out</button>
        </header>

        <div class="main">
          <nav class="list" aria-label="Rooms">
            @for (r of store().rooms(); track r.id) {
              <button type="button" [class.on]="selected()?.id === r.id" (click)="select(r.id)">
                <i class="dot {{ r.state }}"></i>
                <span>{{ r.name }}<small>{{ r.people.length }} people &middot; {{ r.lastActivity }}</small></span>
              </button>
            } @empty {
              <p class="hint pad">No live rooms.</p>
            }
          </nav>

          <div class="detail">
            @if (selected(); as room) {
              <div class="dhead">
                <div>
                  <h2>{{ room.name }}</h2>
                  <p class="hint">{{ room.state }} &middot; created {{ room.created }}</p>
                </div>
                @if (!closing()) {
                  <button type="button" class="danger" (click)="closing.set(true)">Close room&hellip;</button>
                }
              </div>

              @if (closing()) {
                <div class="typed">
                  Type <code>{{ room.name }}</code> to close this room for {{ room.people.length }} people.
                  <input #typed aria-label="Room name to confirm" (input)="typedName.set(typed.value)" />
                  <button type="button" class="danger" [disabled]="typedName() !== room.name" (click)="doClose(room.id)">Close</button>
                  <button type="button" class="ghost" (click)="closing.set(false)">Cancel</button>
                </div>
              }

              <ul class="people">
                @for (x of room.people; track x.id) {
                  <li>
                    <i class="dot {{ x.status }}"></i>
                    <span class="n">{{ x.name }}@if (x.isOwner) { <small>owner</small> }</span>
                    <span class="r">{{ x.role }}</span>
                    <span class="s">{{ x.status === 'online' ? 'online' : 'offgrid, seen ' + x.lastSeen }}</span>
                    <button type="button" class="ghost" (click)="room_remove.set(x.id)">Remove</button>
                    @if (room_remove() === x.id) {
                      <span class="undo">Removed by operator.
                        <button type="button" class="danger" (click)="store().removePerson(room.id, x.id); room_remove.set('')">Confirm</button>
                        <button type="button" class="ghost" (click)="room_remove.set('')">Undo</button></span>
                    }
                  </li>
                }
              </ul>
            } @else {
              <p class="hint pad">Select a room.</p>
            }
          </div>
        </div>

        <footer class="drawer" [class.open]="drawer()">
          <button type="button" class="tab" (click)="drawer.set(!drawer())">
            Issues <b>{{ errors() }}</b> errors &middot; {{ store().issues().length }} entries {{ drawer() ? '&#9662;' : '&#9652;' }}
          </button>
          @if (drawer()) {
            <div class="log">
              @for (i of store().issues(); track i.id) {
                <div class="line {{ i.level }}"><span>{{ i.time }}</span><span>{{ i.source }}</span><span>{{ i.message }}</span></div>
              }
            </div>
          }
        </footer>
      </section>
    }
  `,
  styles: `
    :host { display: block; color: var(--text-color); padding-bottom: 4rem; }
    h1 { margin: 0; } h2 { margin: 0; font-size: 1.1rem; }
    input { padding: 0.5rem; border: 1px solid var(--card-border); border-radius: 4px; background: var(--bg-color); color: var(--text-color); }
    button { padding: 0.4rem 0.8rem; border: 0; border-radius: 4px; background: var(--primary-color); color: #fff; cursor: pointer; }
    button:disabled { opacity: 0.4; cursor: default; }
    button.ghost { background: none; border: 1px solid var(--border-color); color: var(--text-color); }
    button.danger { background: var(--verdict-bad); }
    .hint { color: var(--muted-color); font-size: 0.8rem; margin: 0; } .pad { padding: 1rem; }
    .error { color: var(--verdict-bad); margin: 0; }
    .split { display: grid; grid-template-columns: 1fr 1fr; min-height: 70vh; }
    @media (max-width: 700px) { .split { grid-template-columns: 1fr; } }
    .brand { background: linear-gradient(135deg, var(--avatar-from), var(--avatar-to)); color: #fff; display: grid; place-content: center; padding: 2rem; }
    .form { display: grid; gap: 0.7rem; align-content: center; padding: 2rem; max-width: 22rem; }
    .strip { display: flex; flex-wrap: wrap; gap: 1.2rem; align-items: center; padding: 0.5rem 1rem; background: var(--panel-bg);
      border-bottom: 1px solid var(--border-color); font-size: 0.85rem; }
    .strip strong { margin-right: auto; } .strip span { color: var(--muted-color); }
    .main { display: grid; grid-template-columns: 17rem 1fr; min-height: 55vh; }
    @media (max-width: 700px) { .main { grid-template-columns: 1fr; } }
    .list { border-right: 1px solid var(--border-color); display: grid; align-content: start; }
    .list button { display: flex; gap: 0.6rem; align-items: center; text-align: left; background: none; color: var(--text-color);
      border-radius: 0; border-bottom: 1px solid var(--border-color); padding: 0.7rem 1rem; }
    .list button.on { background: var(--card-hover-bg); }
    .list small { display: block; color: var(--muted-color); }
    .dot { width: 0.6rem; height: 0.6rem; border-radius: 50%; background: var(--muted-color); flex: none; }
    .dot.voting, .dot.online { background: var(--verdict-good); } .dot.restored { background: var(--verdict-mid); }
    .dot.offgrid { background: var(--verdict-bad); }
    .detail { padding: 1rem 1.5rem; }
    .dhead { display: flex; justify-content: space-between; align-items: start; margin-bottom: 0.8rem; }
    .typed { display: flex; flex-wrap: wrap; gap: 0.5rem; align-items: center; padding: 0.7rem; margin-bottom: 0.8rem; border: 1px solid var(--verdict-bad); border-radius: 6px; }
    .people { list-style: none; margin: 0; padding: 0; }
    .people li { display: grid; grid-template-columns: 1rem 9rem 7rem 1fr auto; gap: 0.6rem; align-items: center; padding: 0.5rem 0; border-bottom: 1px solid var(--border-color); }
    .people li .undo { grid-column: 1 / -1; font-size: 0.85rem; background: var(--panel-bg); padding: 0.4rem 0.6rem; border-radius: 4px; display: flex; gap: 0.5rem; align-items: center; }
    .people small { color: var(--muted-color); margin-left: 0.3rem; } .people .s, .people .r { color: var(--muted-color); font-size: 0.85rem; }
    .drawer { position: fixed; left: 0; right: 0; bottom: 0; background: var(--card-bg); border-top: 1px solid var(--border-color); z-index: 800; }
    .drawer .tab { width: 100%; text-align: left; background: var(--panel-bg); color: var(--text-color); border-radius: 0; }
    .drawer.open .log { max-height: 40vh; overflow: auto; padding-bottom: 3.5rem; }
    .line { display: grid; grid-template-columns: 4rem 9rem 1fr; gap: 0.6rem; padding: 0.25rem 1rem; font: 0.8rem ui-monospace, monospace; }
    .line.error { color: var(--verdict-bad); } .line.warning { color: var(--verdict-mid); }
  `,
})
export class VariantC {
  readonly store = input.required<PrototypeStore>();
  protected readonly selectedId = signal('r1');
  protected readonly closing = signal(false);
  protected readonly typedName = signal('');
  protected readonly room_remove = signal('');
  protected readonly drawer = signal(false);
  protected readonly selected = computed(() => this.store().rooms().find((r) => r.id === this.selectedId()) ?? this.store().rooms()[0] ?? null);
  private readonly everyone = computed(() => this.store().rooms().flatMap((r) => r.people));
  protected readonly online = computed(() => this.everyone().filter((x) => x.status === 'online').length);
  protected readonly offgrid = computed(() => this.everyone().filter((x) => x.status === 'offgrid').length);
  protected readonly errors = computed(() => this.store().issues().filter((i) => i.level === 'error').length);

  protected select(id: string): void {
    this.selectedId.set(id);
    this.closing.set(false);
    this.typedName.set('');
    this.room_remove.set('');
  }

  protected doClose(id: string): void {
    this.store().closeRoom(id);
    this.closing.set(false);
    this.typedName.set('');
  }
}
