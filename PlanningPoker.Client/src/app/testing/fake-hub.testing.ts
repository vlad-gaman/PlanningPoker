import { HubEvent } from '../models/hub-events';

/** Test double for RoomHubClient that lets a spec push server events and reconnects. */
export function fakeHub(joinResponse: unknown = undefined) {
  const handlers = new Map<string, (event: unknown) => void>();
  let reconnected: (() => void) | undefined;
  return {
    connect: vi.fn().mockResolvedValue(undefined),
    disconnect: vi.fn().mockResolvedValue(undefined),
    on: vi.fn((method: string, handler: (event: unknown) => void) => handlers.set(method, handler)),
    onReconnected: vi.fn((handler: () => void) => (reconnected = handler)),
    invoke: vi.fn().mockResolvedValue(joinResponse),
    emit: (method: HubEvent, event: unknown) => handlers.get(method)?.(event),
    reconnect: () => reconnected?.(),
  };
}

export const ANN = { guid: 'g-ann', name: 'Ann', personType: 'dev' } as const;
export const BOB = { guid: 'g-bob', name: 'Bob', personType: 'test' } as const;

export const JOIN_RESPONSE = {
  people: [ANN],
  voteStatus: {
    votes: {},
    hasEveryoneVoted: false,
    votingFinished: false,
    countdown: null,
    statistics: null,
  },
  cards: [
    { value: '1', display: null },
    { value: '5', display: null },
    { value: 'coffee', display: '☕' },
  ],
  ownerGuid: ANN.guid,
  configuration: {
    cardSet: 'linear',
    countdownSeconds: 5,
    showFireworks: true,
    maxParticipants: 50,
  },
};

/** Statistics of two votes, 3 and 5, both developers. */
export const STATISTICS = {
  all: {
    marks: [
      { mark: '1', percentage: 50 },
      { mark: '5', percentage: 50 },
    ],
    highestMark: null,
    averageMark: 3,
  },
  developers: { marks: [{ mark: '1', percentage: 100 }], highestMark: '1', averageMark: 1 },
  testers: { marks: [{ mark: '5', percentage: 100 }], highestMark: '5', averageMark: 5 },
  consensus: false,
};

export const CONSENSUS_STATISTICS = {
  all: { marks: [{ mark: '5', percentage: 100 }], highestMark: '5', averageMark: 5 },
  developers: { marks: [{ mark: '5', percentage: 100 }], highestMark: '5', averageMark: 5 },
  testers: { marks: [], highestMark: null, averageMark: null },
  consensus: true,
};

export function revealed(statistics: unknown) {
  return { votes: {}, hasEveryoneVoted: true, votingFinished: true, countdown: null, statistics };
}
