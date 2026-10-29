/** Event names the server pushes on the room hub. Must match PlanningPoker.Controller/Constants/HubEvents.cs. */
export const HUB_EVENTS = {
  PersonJoined: 'PersonJoined',
  PersonExited: 'PersonExited',
  OwnershipTransferred: 'OwnershipTransferred',
  RoomDisposed: 'RoomDisposed',
  CountdownReset: 'CountdownReset',
} as const;

export type HubEvent = (typeof HUB_EVENTS)[keyof typeof HUB_EVENTS];
