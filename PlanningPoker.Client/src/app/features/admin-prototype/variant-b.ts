// PROTOTYPE (issue 78) variant B: one-page dashboard. Stat tiles on top, rooms as cards with their people inside, an issues
// feed beside them. Single login form with the optional code field; destructive actions open a modal dialog. Wipe me.
import { ChangeDetectionStrategy, Component, computed, input, signal } from '@angular/core';
import { PrototypeStore } from './prototype-store';

interface Pending {
  title: string;
  detail: string;
  action: () => void;
}

@Component({
  selector: 'app-variant-b',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (!store().signedIn()) {
      <section class="login">
        <form (submit)="$event.preventDefault(); submit(user.value, pass.value, code.value)">
          <h1>Management</h1>
          @if (store().loginError()) {
            <div class="banner" role="alert">{{ store().loginError() }}</div>
          }
          <input #user placeholder="Username" autocomplete="username" aria-label="Username" />
          <input #pass type="password" placeholder="Password" autocomplete="current-password" aria-label="Password" />
          <input #code placeholder="Authenticator code (if enabled)" inputmode="numeric" maxlength="6" aria-label="Authenticator code" />
          <button type="submit">Sign in</button>
          <p class="hint">Any username/password signs in. Password "wrong" or "lock" shows errors. Username "mfa" requires code 123456.</p>
        </form>
      </section>
    } @else {
      <section class="dash">
        <header>
          <h1>Management</h1>
          <button type="button" class="ghost" (click)="store().logout()">Sign out</button>
        </header>

        <div class="tiles">
          <div class="tile"><b>{{ store().rooms().length }}</b>Rooms</div>
          <div class="tile"><b>{{ online() }}</b>Online</div>
          <div class="tile"><b>{{ offgrid() }}</b>Offgrid</div>
          <div class="tile"><b>{{ store().stats.abandonedSwept24h }}</b>Swept (24 h)</div>
          <div class="tile"><b>{{ store().stats.uptime }}</b>Uptime</div>
          <div class="tile" [class.warn]="true"><b>{{ store().stats.lastWrite }}</b>Last store write</div>
        </div>

        <div class="cols">
          <div class="rooms">
            @for (r of store().rooms(); track r.id) {
              <article class="card">
                <div class="head">
                  <h2>{{ r.name }}</h2>
                  <span class="state {{ r.state }}">{{ r.state }}</span>
                  <button type="button" class="danger" (click)="askClose(r.id, r.name, r.people.length)">Close room</button>
                </div>
                <p class="meta">Created {{ r.created }} &middot; active {{ r.lastActivity }}</p>
                <ul class="chips">
                  @for (x of r.people; track x.id) {
                    <li [class.off]="x.status === 'offgrid'">
                      {{ x.name }} <small>{{ x.role }}@if (x.isOwner) { , owner }</small>
                      <button type="button" [attr.aria-label]="'Remove ' + x.name" (click)="askRemove(r.id, x.id, x.name, r.name)">&times;</button>
                    </li>
                  }
                </ul>
              </article>
            } @empty {
              <p class="hint">No live rooms.</p>
            }
          </div>

          <aside class="feed">
            <h2>Issues <small>{{ errors() }} errors</small></h2>
            @for (i of store().issues(); track i.id) {
              <div class="item {{ i.level }}">
                <span>{{ i.time }} &middot; {{ i.source }}</span>
                {{ i.message }}
              </div>
            }
          </aside>
        </div>
      </section>
    }

    @if (pending(); as p) {
      <div class="scrim" (click)="pending.set(null)">
        <div class="dialog" role="alertdialog" aria-modal="true" (click)="$event.stopPropagation()">
          <h2>{{ p.title }}</h2>
          <p>{{ p.detail }}</p>
          <div class="row">
            <button type="button" class="ghost" (click)="pending.set(null)">Cancel</button>
            <button type="button" class="danger" (click)="p.action(); pending.set(null)">Confirm</button>
          </div>
        </div>
      </div>
    }
  `,
  styles: `
    :host { display: block; padding: 1.5rem 1rem 5rem; color: var(--text-color); }
    h1 { font-size: 1.3rem; margin: 0; } h2 { font-size: 1rem; margin: 0; }
    input { padding: 0.5rem; border: 1px solid var(--card-border); border-radius: 6px; background: var(--bg-color); color: var(--text-color); }
    button { padding: 0.4rem 0.9rem; border: 0; border-radius: 6px; background: var(--primary-color); color: #fff; cursor: pointer; }
    button.ghost { background: none; color: var(--primary-color); border: 1px solid var(--border-color); }
    button.danger { background: var(--verdict-bad); }
    .login { max-width: 20rem; margin: 3rem auto; }
    form { display: grid; gap: 0.6rem; }
    .banner { background: var(--verdict-bad); color: #fff; padding: 0.5rem 0.7rem; border-radius: 6px; font-size: 0.9rem; }
    .hint { color: var(--muted-color); font-size: 0.8rem; margin: 0; }
    .dash { max-width: 1140px; margin: 0 auto; display: grid; gap: 1rem; }
    header { display: flex; justify-content: space-between; align-items: center; }
    .tiles { display: grid; grid-template-columns: repeat(auto-fit, minmax(8rem, 1fr)); gap: 0.6rem; }
    .tile { background: var(--panel-bg); border: 1px solid var(--border-color); border-radius: 8px; padding: 0.7rem; color: var(--muted-color); font-size: 0.8rem; }
    .tile b { display: block; font-size: 1.3rem; color: var(--text-color); }
    .cols { display: grid; grid-template-columns: 1fr 22rem; gap: 1rem; align-items: start; }
    @media (max-width: 800px) { .cols { grid-template-columns: 1fr; } }
    .rooms { display: grid; gap: 0.8rem; }
    .card { border: 1px solid var(--border-color); border-radius: 8px; padding: 0.8rem; background: var(--card-bg); }
    .head { display: flex; align-items: center; gap: 0.6rem; } .head h2 { flex: 1; }
    .meta { color: var(--muted-color); font-size: 0.8rem; margin: 0.3rem 0 0.6rem; }
    .state { font-size: 0.75rem; padding: 0.05rem 0.5rem; border-radius: 999px; border: 1px solid var(--border-color); }
    .state.restored { color: var(--verdict-mid); } .state.voting { color: var(--verdict-good); }
    .chips { list-style: none; margin: 0; padding: 0; display: flex; flex-wrap: wrap; gap: 0.4rem; }
    .chips li { display: flex; align-items: center; gap: 0.3rem; padding: 0.2rem 0.3rem 0.2rem 0.6rem; border-radius: 999px;
      background: var(--panel-bg); border: 1px solid var(--border-color); font-size: 0.85rem; }
    .chips li.off { opacity: 0.6; border-style: dashed; }
    .chips li button { background: none; color: var(--muted-color); padding: 0 0.4rem; font-size: 1rem; }
    .feed { border: 1px solid var(--border-color); border-radius: 8px; padding: 0.8rem; display: grid; gap: 0.5rem; background: var(--card-bg); }
    .feed small { color: var(--muted-color); font-weight: 400; }
    .item { font-size: 0.82rem; border-left: 3px solid var(--border-color); padding-left: 0.5rem; }
    .item span { display: block; color: var(--muted-color); font-size: 0.72rem; }
    .item.error { border-left-color: var(--verdict-bad); } .item.warning { border-left-color: var(--verdict-mid); }
    .scrim { position: fixed; inset: 0; background: rgba(0, 0, 0, 0.5); display: grid; place-items: center; z-index: 900; }
    .dialog { background: var(--card-bg); color: var(--text-color); padding: 1.2rem; border-radius: 10px; max-width: 24rem; display: grid; gap: 0.6rem; }
    .row { display: flex; justify-content: flex-end; gap: 0.5rem; }
  `,
})
export class VariantB {
  readonly store = input.required<PrototypeStore>();
  protected readonly pending = signal<Pending | null>(null);
  private readonly everyone = computed(() => this.store().rooms().flatMap((r) => r.people));
  protected readonly online = computed(() => this.everyone().filter((x) => x.status === 'online').length);
  protected readonly offgrid = computed(() => this.everyone().filter((x) => x.status === 'offgrid').length);
  protected readonly errors = computed(() => this.store().issues().filter((i) => i.level === 'error').length);

  protected submit(user: string, pass: string, code: string): void {
    const result = this.store().login(user, pass);
    if (result === 'totp') {
      this.store().verifyTotp(code);
    }
  }

  protected askClose(roomId: string, name: string, people: number): void {
    this.pending.set({
      title: `Close "${name}"?`,
      detail: `${people} people will be told the room was closed by an operator. It is deleted from the store and cannot be restored.`,
      action: () => this.store().closeRoom(roomId),
    });
  }

  protected askRemove(roomId: string, personId: string, name: string, room: string): void {
    this.pending.set({
      title: `Remove ${name}?`,
      detail: `${name} leaves "${room}" and loses their vote. They can rejoin as a new person.`,
      action: () => this.store().removePerson(roomId, personId),
    });
  }
}
