import { PersonType } from './person';

/** What GET /api/admin/overview returns (PlanningPoker.Controller/Models/AdminOverviewResponse.cs). It never carries votes. */
export interface AdminOverview {
  stats: AdminStats;
  rooms: AdminRoom[];
  issues: AdminIssue[];
}

export interface AdminStats {
  rooms: { total: number; voting: number; revealed: number; restored: number };
  people: { online: number; offgrid: number };
  uptimeSeconds: number;
  roomsRestoredAtStart: number;
  abandonedRoomsSweptLast24Hours: number;
  lastStoreWrite: { at: string | null; succeeded: boolean | null; failedRooms: number };
  failedLoginsLast24Hours: number;
}

export type AdminRoomState = 'voting' | 'revealed' | 'restored';

export interface AdminRoom {
  id: string;
  state: AdminRoomState;
  createdAt: string;
  lastActivityAt: string;
  people: AdminPerson[];
}

export interface AdminPerson {
  id: string;
  name: string;
  personType: PersonType;
  isOwner: boolean;
  status: 'online' | 'offgrid';
  /** When the person lost their last connection; null while connected. */
  lastSeenAt: string | null;
}

export type AdminIssueLevel = 'info' | 'warning' | 'error';

export interface AdminIssue {
  id: number;
  time: string;
  level: AdminIssueLevel;
  source: string;
  message: string;
}
