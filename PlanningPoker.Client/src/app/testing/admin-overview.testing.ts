import { AdminOverview, AdminPerson, AdminRoom } from '../models/admin-overview';

export const OWNER: AdminPerson = {
  id: 'p-ann',
  name: 'Ann',
  personType: 'dev',
  isOwner: true,
  status: 'online',
  lastSeenAt: null,
};

export const GUEST: AdminPerson = {
  id: 'p-bob',
  name: 'Bob',
  personType: 'test',
  isOwner: false,
  status: 'online',
  lastSeenAt: null,
};

export const AWAY: AdminPerson = {
  id: 'p-cat',
  name: 'Cat',
  personType: 'sre',
  isOwner: false,
  status: 'offgrid',
  lastSeenAt: '2026-03-01T09:30:00+00:00',
};

export const VOTING_ROOM: AdminRoom = {
  id: 'blue-fox',
  state: 'voting',
  createdAt: '2026-03-01T09:00:00+00:00',
  lastActivityAt: '2026-03-01T10:00:00+00:00',
  people: [OWNER, GUEST, AWAY],
};

export const RESTORED_ROOM: AdminRoom = {
  id: 'old-owl',
  state: 'restored',
  createdAt: '2026-02-28T09:00:00+00:00',
  lastActivityAt: '2026-02-28T10:00:00+00:00',
  people: [{ ...AWAY, id: 'p-dan', name: 'Dan' }],
};

export const OVERVIEW: AdminOverview = {
  stats: {
    rooms: { total: 2, voting: 1, revealed: 0, restored: 1 },
    people: { online: 2, offgrid: 2 },
    uptimeSeconds: 273_600,
    roomsRestoredAtStart: 1,
    abandonedRoomsSweptLast24Hours: 6,
    lastStoreWrite: { at: '2026-03-01T10:44:00+00:00', succeeded: true, failedRooms: 0 },
    failedLoginsLast24Hours: 3,
  },
  rooms: [VOTING_ROOM, RESTORED_ROOM],
  issues: [
    { id: 3, time: '2026-03-01T10:44:00+00:00', level: 'error', source: 'RoomRepository', message: 'Persisting room failed' },
    { id: 2, time: '2026-03-01T10:31:00+00:00', level: 'warning', source: 'Sweep', message: 'Swept 2 rooms' },
    { id: 1, time: '2026-03-01T09:58:00+00:00', level: 'info', source: 'Startup', message: 'Restored 1 rooms from the store' },
  ],
};
