import { Page } from '@playwright/test';

interface RoomDefaults {
  useFunRoomName?: boolean;
  cardSet?: string;
  countdownSeconds?: number;
  showFireworks?: boolean;
}

/** Stores what new rooms are created with in the browser, as the app does after the owner saved settings. */
export async function seedRoomDefaults(page: Page, defaults: RoomDefaults): Promise<void> {
  const { useFunRoomName = false, ...rest } = defaults;
  const overrides = Object.fromEntries(Object.entries(rest).filter(([, value]) => value !== undefined));
  const stored = {
    useFunRoomName,
    configuration: { cardSet: 'modified-fibonacci', countdownSeconds: 5, showFireworks: true, maxParticipants: 50, ...overrides },
  };
  await page.addInitScript((value) => {
    if (!localStorage.getItem('pp-room-defaults')) {
      localStorage.setItem('pp-room-defaults', JSON.stringify(value));
    }
  }, stored);
}
