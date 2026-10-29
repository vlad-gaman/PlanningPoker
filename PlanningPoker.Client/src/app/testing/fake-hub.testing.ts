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
  voteStatus: { votes: {}, hasEveryoneVoted: false, votingFinished: false, countdown: null },
  cards: [
    { value: '1', display: null },
    { value: '5', display: null },
    { value: 'coffee', display: '☕' },
  ],
  ownerGuid: ANN.guid,
  configuration: { cardSet: 'linear', countdownSeconds: 5, showFireworks: true, maxParticipants: 50 },
};
