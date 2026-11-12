// PROTOTYPE (issue 78): throwaway stub state for the management page variants. Wipe me.
import { signal } from '@angular/core';

export type PersonStatus = 'online' | 'offgrid';
export type RoomState = 'voting' | 'revealed' | 'restored';
export type IssueLevel = 'error' | 'warning' | 'info';

export interface ProtoPerson {
  id: string;
  name: string;
  role: 'Developer' | 'Tester' | 'SRE' | 'Observer';
  status: PersonStatus;
  isOwner: boolean;
  lastSeen: string;
}

export interface ProtoRoom {
  id: string;
  name: string;
  state: RoomState;
  created: string;
  lastActivity: string;
  people: ProtoPerson[];
}

export interface ProtoIssue {
  id: number;
  time: string;
  level: IssueLevel;
  source: string;
  message: string;
}

export type LoginResult = 'ok' | 'bad' | 'locked' | 'totp';

const p = (
  id: string,
  name: string,
  role: ProtoPerson['role'],
  status: PersonStatus,
  isOwner = false,
  lastSeen = 'now',
): ProtoPerson => ({ id, name, role, status, isOwner, lastSeen });

export class PrototypeStore {
  // Login scenarios: password "wrong" fails, "lock" is locked out, username "mfa" asks for a TOTP code (123456 passes).
  readonly signedIn = signal(false);
  readonly needsTotp = signal(false);
  readonly loginError = signal('');
  private failures = 0;

  readonly rooms = signal<ProtoRoom[]>([
    {
      id: 'r1', name: 'Sprint 42 refinement', state: 'voting', created: '09:02', lastActivity: '2 min ago',
      people: [
        p('a', 'Alice', 'Developer', 'online', true), p('b', 'Bob', 'Developer', 'online'), p('c', 'Chen', 'Tester', 'online'),
        p('d', 'Dana', 'SRE', 'offgrid', false, '4 min ago'), p('e', 'Eli', 'Observer', 'online'),
      ],
    },
    {
      id: 'r2', name: 'Payments squad', state: 'revealed', created: '08:15', lastActivity: '11 min ago',
      people: [p('f', 'Farah', 'Developer', 'online', true), p('g', 'Gus', 'Tester', 'online')],
    },
    {
      id: 'r3', name: 'Platform backlog', state: 'restored', created: 'yesterday', lastActivity: '7 h ago',
      people: [
        p('h', 'Hana', 'Developer', 'offgrid', true, '7 h ago'), p('i', 'Ivo', 'SRE', 'offgrid', false, '7 h ago'),
        p('j', 'Jun', 'Tester', 'offgrid', false, '7 h ago'),
      ],
    },
    {
      id: 'r4', name: 'Mobile estimates', state: 'voting', created: '10:40', lastActivity: 'just now',
      people: [p('k', 'Kai', 'Developer', 'online', true), p('l', 'Lena', 'Developer', 'online'), p('m', 'Moe', 'Observer', 'online')],
    },
  ]);

  readonly issues = signal<ProtoIssue[]>([
    { id: 5, time: '10:44', level: 'error', source: 'RoomRepository', message: 'Persisting room r4 failed (SQLite busy); retrying' },
    { id: 4, time: '10:31', level: 'warning', source: 'Sweep', message: '2 abandoned rooms removed from memory' },
    { id: 3, time: '09:58', level: 'info', source: 'Startup', message: '1 room restored from store (3 people, all offgrid)' },
    { id: 2, time: '09:12', level: 'error', source: 'RoomHub', message: 'Unhandled exception in Vote: person not found (POST /roomhub)' },
    { id: 1, time: '08:03', level: 'warning', source: 'Admin', message: 'Failed login for operator from 203.0.113.9 (2 of 5)' },
  ]);

  readonly stats = { uptime: '3 d 4 h', abandonedSwept24h: 6, lastWrite: '10:44 (1 retry)' };

  private nextIssue = 6;

  login(user: string, password: string): LoginResult {
    this.loginError.set('');
    if (password === 'lock' || this.failures >= 5) {
      this.loginError.set('Too many failed attempts. Try again in 15 minutes.');
      return 'locked';
    }
    if (password === 'wrong' || !user || !password) {
      this.failures++;
      this.loginError.set(`Invalid username or password (${this.failures} of 5).`);
      return 'bad';
    }
    if (user === 'mfa') {
      this.needsTotp.set(true);
      return 'totp';
    }
    this.signedIn.set(true);
    this.log('info', 'Admin', 'Operator signed in');
    return 'ok';
  }

  verifyTotp(code: string): boolean {
    if (code === '123456') {
      this.needsTotp.set(false);
      this.signedIn.set(true);
      this.log('info', 'Admin', 'Operator signed in (TOTP)');
      return true;
    }
    this.loginError.set('Invalid code.');
    return false;
  }

  logout(): void {
    this.signedIn.set(false);
    this.needsTotp.set(false);
    this.loginError.set('');
  }

  closeRoom(roomId: string): void {
    const room = this.rooms().find((r) => r.id === roomId);
    if (!room) return;
    this.rooms.update((rs) => rs.filter((r) => r.id !== roomId));
    this.log('info', 'Admin', `Operator closed room "${room.name}" (${room.people.length} people notified)`);
  }

  removePerson(roomId: string, personId: string): void {
    const room = this.rooms().find((r) => r.id === roomId);
    const person = room?.people.find((x) => x.id === personId);
    if (!room || !person) return;
    this.rooms.update((rs) => rs.map((r) => (r.id === roomId ? { ...r, people: r.people.filter((x) => x.id !== personId) } : r)));
    this.log('info', 'Admin', `Operator removed ${person.name} from "${room.name}" (removed by operator)`);
  }

  private log(level: IssueLevel, source: string, message: string): void {
    const now = new Date();
    const time = `${String(now.getHours()).padStart(2, '0')}:${String(now.getMinutes()).padStart(2, '0')}`;
    this.issues.update((is) => [{ id: this.nextIssue++, time, level, source, message }, ...is]);
  }
}
