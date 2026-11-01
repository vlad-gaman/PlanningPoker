export interface LibraryCredit {
  name: string;
  license: string;
  url: string;
}

export interface IconCredit {
  slug: string;
  label: string;
}

/** Third-party libraries the client ships with that call for an attribution. */
export const LIBRARY_CREDITS: LibraryCredit[] = [
  { name: 'Chart.js', license: 'MIT license', url: 'https://www.chartjs.org' },
  {
    name: 'fireworks-js',
    license: 'MIT license',
    url: 'https://github.com/crashmax-dev/fireworks-js',
  },
];

export const ICON_CREDIT_URL = 'https://www.flaticon.com/free-icons/';

/** Flaticon sets behind the fist-of-five marks and the app icons. */
export const ICON_CREDITS: IconCredit[] = [
  { slug: 'poker', label: 'Poker' },
  { slug: 'fist', label: 'Fist' },
  { slug: 'finger', label: 'Finger' },
  { slug: 'two', label: 'Two' },
  { slug: 'three', label: 'Three' },
  { slug: 'four', label: 'Four' },
  { slug: 'fingers', label: 'Fingers' },
];
