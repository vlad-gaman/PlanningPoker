import { TestBed } from '@angular/core/testing';
import {
  CARD_BACKS,
  CARD_BACK_STORAGE_KEY,
  CardBackService,
  DEFAULT_CARD_BACK,
  isCardBackId,
  resolveCardBack,
} from './card-back.service';

describe('CardBackService', () => {
  beforeEach(() => localStorage.clear());

  it('offers at least four designs, with classic the default', () => {
    expect(CARD_BACKS.length).toBeGreaterThanOrEqual(4);
    expect(DEFAULT_CARD_BACK).toBe('classic');
    expect(TestBed.inject(CardBackService).cardBack()).toBe('classic');
  });

  it('restores a stored design', () => {
    localStorage.setItem(CARD_BACK_STORAGE_KEY, 'dots');

    expect(TestBed.inject(CardBackService).cardBack()).toBe('dots');
  });

  it('ignores an unknown stored value', () => {
    localStorage.setItem(CARD_BACK_STORAGE_KEY, 'sparkles');

    expect(TestBed.inject(CardBackService).cardBack()).toBe('classic');
  });

  it('sets and persists the picked design', () => {
    const service = TestBed.inject(CardBackService);

    service.set('waves');

    expect(service.cardBack()).toBe('waves');
    expect(localStorage.getItem(CARD_BACK_STORAGE_KEY)).toBe('waves');
  });

  it('knows its own ids and falls back to classic for any other', () => {
    expect(isCardBackId('stripes')).toBe(true);
    expect(isCardBackId('Stripes')).toBe(false);
    expect(resolveCardBack('checker')).toBe('checker');
    expect(resolveCardBack('sparkles')).toBe('classic');
    expect(resolveCardBack(undefined)).toBe('classic');
  });
});
