import { HubConnection, HubConnectionState } from '@microsoft/signalr';
import { ApiRequestError } from './api-request-error';
import { RoomHubClient } from './room-hub.client';

function fakeConnection(invokeResult?: unknown) {
  return {
    state: HubConnectionState.Disconnected,
    start: vi.fn().mockResolvedValue(undefined),
    stop: vi.fn().mockResolvedValue(undefined),
    on: vi.fn(),
    onreconnected: vi.fn(),
    invoke: vi.fn().mockResolvedValue(invokeResult),
  };
}

function clientWith(connection: ReturnType<typeof fakeConnection>): RoomHubClient {
  const client = new RoomHubClient();
  client.factory = () => connection as unknown as HubConnection;
  return client;
}

describe('RoomHubClient', () => {
  it('starts the connection on connect', async () => {
    const connection = fakeConnection();
    await clientWith(connection).connect();

    expect(connection.start).toHaveBeenCalledOnce();
  });

  it('does not restart an already started connection', async () => {
    const connection = fakeConnection();
    const client = clientWith(connection);
    await client.connect();
    connection.state = HubConnectionState.Connected;

    await client.connect();

    expect(connection.start).toHaveBeenCalledOnce();
  });

  it('registers handlers on the connection', async () => {
    const connection = fakeConnection();
    const client = clientWith(connection);
    await client.connect();
    const handler = vi.fn();

    client.on('RoomChanged', handler);

    expect(connection.on).toHaveBeenCalledWith('RoomChanged', handler);
  });

  it('calls the handler when the connection reconnects', async () => {
    const connection = fakeConnection();
    const client = clientWith(connection);
    await client.connect();
    const handler = vi.fn();

    client.onReconnected(handler);
    connection.onreconnected.mock.calls[0][0]('new-id');

    expect(handler).toHaveBeenCalledOnce();
  });

  it('unwraps data from a successful invocation', async () => {
    const connection = fakeConnection({ data: { ok: true }, errors: null });
    const client = clientWith(connection);
    await client.connect();

    const result = await client.invoke<{ ok: boolean }>('Ping', 1);

    expect(connection.invoke).toHaveBeenCalledWith('Ping', 1);
    expect(result).toEqual({ ok: true });
  });

  it('throws ApiRequestError when the response carries errors', async () => {
    const connection = fakeConnection({ data: null, errors: [{ code: 'not_found', message: 'Gone.' }] });
    const client = clientWith(connection);
    await client.connect();

    const failure = client.invoke('Ping');

    await expect(failure).rejects.toBeInstanceOf(ApiRequestError);
    await expect(failure).rejects.toMatchObject({ message: 'Gone.' });
  });

  it('throws when invoking before connecting', async () => {
    await expect(new RoomHubClient().invoke('Ping')).rejects.toThrow('not connected');
  });

  it('stops the connection on disconnect', async () => {
    const connection = fakeConnection();
    const client = clientWith(connection);
    await client.connect();

    await client.disconnect();

    expect(connection.stop).toHaveBeenCalledOnce();
  });
});
