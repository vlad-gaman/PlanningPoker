export interface LibraryCredit {
  name: string;
  license: string;
  url: string;
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
