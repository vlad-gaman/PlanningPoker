import { Person, PersonType } from '../../models/person';
import { orderPeople } from './order-people';

const person = (name: string, personType: PersonType): Person => ({ guid: name.toLowerCase(), name, personType });
const names = (people: Person[]) => people.map((p) => p.name);

describe('orderPeople', () => {
  it('lists voters before observers', () => {
    const result = orderPeople([person('Ann', 'dev'), person('Bob', 'obs'), person('Cid', 'dev')]);
    expect(names(result)).toEqual(['Ann', 'Cid', 'Bob']);
  });

  it('keeps developers and testers mixed in join order', () => {
    const result = orderPeople([person('Ann', 'test'), person('Bob', 'dev'), person('Cid', 'test'), person('Dee', 'dev')]);
    expect(names(result)).toEqual(['Ann', 'Bob', 'Cid', 'Dee']);
  });

  it('keeps join order inside each band', () => {
    const result = orderPeople([
      person('Zed', 'obs'),
      person('Yan', 'dev'),
      person('Xia', 'obs'),
      person('Wes', 'test'),
      person('Val', 'obs'),
    ]);
    expect(names(result)).toEqual(['Yan', 'Wes', 'Zed', 'Xia', 'Val']);
  });

  it('puts an observer owner at the bottom like any other observer', () => {
    const result = orderPeople([person('Ann', 'obs'), person('Bob', 'dev')]);
    expect(names(result)).toEqual(['Bob', 'Ann']);
  });

  it('returns an empty list for an empty list', () => {
    expect(orderPeople([])).toEqual([]);
  });

  it('keeps an observers-only list as it is', () => {
    const result = orderPeople([person('Bob', 'obs'), person('Ann', 'obs')]);
    expect(names(result)).toEqual(['Bob', 'Ann']);
  });

  it('does not change the input', () => {
    const input = [person('Ann', 'obs'), person('Bob', 'dev')];
    orderPeople(input);
    expect(names(input)).toEqual(['Ann', 'Bob']);
  });
});
