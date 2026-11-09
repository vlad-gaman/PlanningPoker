import { formatRoundTime, spokenRoundTime } from './format-round-time';

describe('formatRoundTime', () => {
  it.each([
    [0, '0:00'],
    [5, '0:05'],
    [42, '0:42'],
    [65, '1:05'],
    [599, '9:59'],
    [3599, '59:59'],
    [3600, '1:00:00'],
    [3725, '1:02:05'],
    [36000, '10:00:00'],
    [-3, '0:00'],
    [7.9, '0:07'],
  ])('formats %s seconds as %s', (seconds, expected) => {
    expect(formatRoundTime(seconds)).toBe(expected);
  });
});

describe('spokenRoundTime', () => {
  it.each([
    [0, '0 seconds'],
    [1, '1 second'],
    [42, '42 seconds'],
    [60, '1 minute 0 seconds'],
    [65, '1 minute 5 seconds'],
    [3600, '1 hour 0 minutes 0 seconds'],
    [7325, '2 hours 2 minutes 5 seconds'],
  ])('says %s seconds as "%s"', (seconds, expected) => {
    expect(spokenRoundTime(seconds)).toBe(expected);
  });
});
