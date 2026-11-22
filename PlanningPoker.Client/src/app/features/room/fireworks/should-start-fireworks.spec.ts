import { shouldStartFireworks } from './should-start-fireworks';

describe('shouldStartFireworks', () => {
  const live = { isNewReveal: true, consensus: true, showFireworks: true };

  it('starts for a live reveal that ended in consensus in a room with fireworks on', () => {
    expect(shouldStartFireworks(live)).toBe(true);
  });

  it('does not start for a reveal that was already there on joining', () => {
    expect(shouldStartFireworks({ ...live, isNewReveal: false })).toBe(false);
  });

  it('does not start without consensus', () => {
    expect(shouldStartFireworks({ ...live, consensus: false })).toBe(false);
  });

  it('does not start when the room turned fireworks off', () => {
    expect(shouldStartFireworks({ ...live, showFireworks: false })).toBe(false);
  });
});
