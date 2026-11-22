import { CLOSING_URL, ClosingBeacon } from './closing-beacon';

describe('ClosingBeacon', () => {
  const sendBeacon = vi.fn();
  const beacon = new ClosingBeacon();

  beforeEach(() => {
    sendBeacon.mockReset().mockReturnValue(true);
    Object.defineProperty(navigator, 'sendBeacon', { value: sendBeacon, configurable: true });
  });

  afterEach(() => {
    Reflect.deleteProperty(navigator, 'sendBeacon');
  });

  it('sends the connection id as JSON when the page is hidden', async () => {
    const stop = beacon.watch(() => 'connection-1');

    window.dispatchEvent(new Event('pagehide'));

    expect(sendBeacon).toHaveBeenCalledOnce();
    const [url, body] = sendBeacon.mock.calls[0] as [string, Blob];
    expect(url).toBe(CLOSING_URL);
    expect(body.type).toBe('application/json');
    expect(JSON.parse(await body.text())).toEqual({ connectionId: 'connection-1' });
    stop();
  });

  it('reads the connection id when the page is hidden, not when watching starts', () => {
    let id = 'before';
    const stop = beacon.watch(() => id);
    id = 'after';

    window.dispatchEvent(new Event('pagehide'));

    expect(sendBeacon).toHaveBeenCalledOnce();
    stop();
  });

  it('sends nothing when there is no connection', () => {
    const stop = beacon.watch(() => null);

    window.dispatchEvent(new Event('pagehide'));

    expect(sendBeacon).not.toHaveBeenCalled();
    stop();
  });

  it('sends nothing after it was stopped', () => {
    beacon.watch(() => 'connection-1')();

    window.dispatchEvent(new Event('pagehide'));

    expect(sendBeacon).not.toHaveBeenCalled();
  });

  it('does not fail where the browser has no beacon', () => {
    Reflect.deleteProperty(navigator, 'sendBeacon');
    const stop = beacon.watch(() => 'connection-1');

    expect(() => window.dispatchEvent(new Event('pagehide'))).not.toThrow();
    stop();
  });
});
