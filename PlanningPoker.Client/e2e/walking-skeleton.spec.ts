import { expect, test } from '@playwright/test';

test('app loads at the root', async ({ page }) => {
  await page.goto('/');
  await expect(page.getByRole('heading', { name: 'Planning Poker' })).toBeVisible();
});

test('saving a name issues an HttpOnly, Secure, SameSite=Lax pp_session cookie', async ({ page, context }) => {
  await page.goto('/');
  const response = await page.request.put('/api/me', { data: { name: 'Ann', personType: 'dev' } });
  expect(response.status()).toBe(200);

  const cookie = (await context.cookies()).find((c) => c.name === 'pp_session');
  expect(cookie).toBeDefined();
  expect(cookie?.httpOnly).toBe(true);
  expect(cookie?.secure).toBe(true);
  expect(cookie?.sameSite).toBe('Lax');
  expect(cookie?.expires).toBe(-1);
});

for (const [path, heading] of [
  ['/room/xyz', 'Room xyz'],
  ['/Room/xyz', 'Room xyz'],
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

test('swagger UI is served', async ({ request }) => {
  const response = await request.get('/swagger/v1/swagger.json');

  expect(response.status()).toBe(200);
  expect(Object.keys((await response.json()).paths)).toEqual(
    expect.arrayContaining(['/api/me', '/api/card-sets', '/api/rooms', '/api/rooms/{id}']),
  );
});
