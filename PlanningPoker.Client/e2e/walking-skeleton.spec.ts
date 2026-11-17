import { expect, test } from '@playwright/test';

test('app loads at the root', async ({ page }) => {
  await page.goto('/');
  await expect(page.getByRole('heading', { name: 'Planning Poker' })).toBeVisible();
});

test('saving a name issues an HttpOnly, SameSite=Lax pp_session cookie (not Secure over plain HTTP)', async ({ page, context }) => {
  await page.goto('/');
  const response = await page.request.put('/api/me', { data: { name: 'Ann', personType: 'dev' } });
  expect(response.status()).toBe(200);

  const cookie = (await context.cookies()).find((c) => c.name === 'pp_session');
  expect(cookie).toBeDefined();
  expect(cookie?.httpOnly).toBe(true);
  // The app is served over plain HTTP here (and on the LAN), so the cookie is Secure only when the request is HTTPS.
  expect(cookie?.secure).toBe(false);
  expect(cookie?.sameSite).toBe('Lax');
  expect(cookie?.expires).toBe(-1);
});

// A room link to a room that does not exist sends the visitor home with a message.
for (const path of ['/room/xyz', '/Room/xyz']) {
  test(`deep link ${path} to a missing room goes home with a message`, async ({ page }) => {
    await page.goto(path);
    await expect(page).toHaveURL(/\/$/);
    await expect(page.getByRole('heading', { name: 'Planning Poker' })).toBeVisible();
    await expect(page.getByRole('status')).toContainText('does not exist');
  });
}

for (const [path, heading] of [
  ['/credits', 'Credits'],
  ['/Credits', 'Credits'],
] as const) {
  test(`deep link ${path} renders the SPA`, async ({ page }) => {
    await page.goto(path);
    await expect(page.getByRole('heading', { name: heading })).toBeVisible();
  });
}

test('unknown /api routes return a JSON ApiResponse error, not index.html', async ({ request }) => {
  const response = await request.get('/api/unknown');

  expect(response.status()).toBe(404);
  expect(response.headers()['content-type']).toContain('application/json');
  expect((await response.json()).errors[0].code).toBe('not_found');
});

test('swagger is not served outside Development', async ({ request }) => {
  const response = await request.get('/swagger/v1/swagger.json');

  expect(response.status()).toBe(404);
});
