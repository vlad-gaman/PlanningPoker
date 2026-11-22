/** The name that earns the rose crown and the wand. */
const LOLLE_NAME = 'lolle';

/** Whether a person's name is Lolle, ignoring case and surrounding spaces. */
export function isLolle(name: string): boolean {
  return name.trim().toLowerCase() === LOLLE_NAME;
}
