import { Person } from '../../models/person';

/**
 * Voters first, then observers. A stable partition: within each group the input (join) order is kept,
 * so a person who changes role returns to their join position inside the new group.
 */
export function orderPeople(people: readonly Person[]): Person[] {
  const voters = people.filter((person) => person.personType !== 'obs');
  const observers = people.filter((person) => person.personType === 'obs');
  return [...voters, ...observers];
}
