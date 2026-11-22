import { CLOSING_URL, ClosingBeacon } from './closing-beacon';

/**
 * The page-hide listener is captured instead of dispatching `pagehide` on the shared window: specs of other files may have
 * left listeners of their own on it, and those would call the mocked beacon too.
 */
describe('ClosingBeacon', () => {
  const sendBeacon = vi.fn();
  const beacon = new ClosingBeacon();
  let pageHide: (() => void) | undefined;
  let removed: unknown;

  beforeEach(() => {
    pageHide = undefined;
    removed = undefined;
    sendBeacon.mockReset().mockReturnValue(true);
    Object.defineProperty(navigator, 'sendBeacon', { value: sendBeacon, configurable: true });
    vi.spyOn(window, 'addEventListener').mockImplementation(((type: string, listener: () => void) => {
      if (type === 'pagehide') {
        pageHide = listener;
      }
    }) as typeof window.addEventListener);
    vi.spyOn(window, 'removeEventListener').mockImplementation(((type: string, listener: unknown) => {
      if (type === 'pagehide') {
        removed = listener;
      }
    }) as typeof window.removeEventListener);
  });

  afterEach(() => {
    vi.restoreAllMocks();
    Reflect.deleteProperty(navigator, 'sendBeacon');
  });

  it('sends the connection id as JSON when the page is hidden', async () => {
    beacon.watch(() => 'connection-1');

    pageHide!();

    expect(sendBeacon).toHaveBeenCalledOnce();
    const [url, body] = sendBeacon.mock.calls[0] as [string, Blob];
    expect(url).toBe(CLOSING_URL);
    expect(body.type).toBe('application/json');
    expect(JSON.parse(await body.text())).toEqual({ connectionId: 'connection-1' });
  });

  it('reads the connection id when the page is hidden, not when watching starts', async () => {
    let id = 'before';
    beacon.watch(() => id);
    id = 'after';

    pageHide!();

    const [, body] = sendBeacon.mock.calls[0] as [string, Blob];
    expect(JSON.parse(await body.text())).toEqual({ connectionId: 'after' });
  });

  it('sends nothing when there is no connection', () => {
    beacon.watch(() => null);

    pageHide!();

    expect(sendBeacon).not.toHaveBeenCalled();
  });

  it('stops listening when stopped', () => {
    const stop = beacon.watch(() => 'connection-1');

    stop();

    expect(removed).toBe(pageHide);
  });

  it('does not fail where the browser has no beacon', () => {
    Reflect.deleteProperty(navigator, 'sendBeacon');
    beacon.watch(() => 'connection-1');

    expect(() => pageHide!()).not.toThrow();
  });
});
