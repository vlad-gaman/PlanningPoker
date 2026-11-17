import { formatUptime } from './admin-format';

describe('formatUptime', () => {
  it.each([
    [0, '0 s'],
    [42, '42 s'],
    [59.9, '59 s'],
    [60, '1 m'],
    [7 * 60 + 30, '7 m'],
    [3600, '1 h 0 m'],
    [2 * 3600 + 5 * 60, '2 h 5 m'],
    [86400, '1 d 0 h'],
    [273_600, '3 d 4 h'],
    [-5, '0 s'],
  ])('shows %d seconds as %s', (seconds, expected) => {
    expect(formatUptime(seconds)).toBe(expected);
  });
});
