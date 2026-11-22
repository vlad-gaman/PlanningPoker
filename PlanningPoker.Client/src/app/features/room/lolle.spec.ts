import { isLolle } from './lolle';

describe('isLolle', () => {
  it.each(['Lolle', 'lolle', ' LOLLE '])('matches %j', (name) => {
    expect(isLolle(name)).toBe(true);
  });

  it.each(['Lolle2', 'Lollepop', 'Lol le', '', 'Ann'])('does not match %j', (name) => {
    expect(isLolle(name)).toBe(false);
  });
});
