import { Injectable } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { ApiResponse } from '../models/api';
import { ApiRequestError } from './api-request-error';

export type HubConnectionFactory = () => signalR.HubConnection;

const MAX_RECONNECT_DELAY_MS = 30_000;

/** Retries without end, backing off from 1 s up to 30 s, so a client outlasts a server restart however long it takes. */
export const reconnectDelayMs = (previousRetryCount: number): number =>
  Math.min(MAX_RECONNECT_DELAY_MS, 1000 * 2 ** previousRetryCount);

export const defaultHubConnectionFactory: HubConnectionFactory = () =>
  new signalR.HubConnectionBuilder()
    .withUrl('/hubs/room')
    .withAutomaticReconnect({ nextRetryDelayInMilliseconds: (context) => reconnectDelayMs(context.previousRetryCount) })
    .build();

/** Wraps the SignalR connection; hub invocations that return an ApiResponse are unwrapped. Each room provides its own. */
@Injectable()
export class RoomHubClient {
  private connection?: signalR.HubConnection;
  factory: HubConnectionFactory = defaultHubConnectionFactory;

  async connect(): Promise<void> {
    this.connection ??= this.factory();
    if (this.connection.state === signalR.HubConnectionState.Disconnected) {
      await this.connection.start();
    }
  }

  async disconnect(): Promise<void> {
    // Clear the field first so a connect() started while this stop is still running gets a fresh connection.
    const stopping = this.connection;
    this.connection = undefined;
    await stopping?.stop();
  }

  on<T extends unknown[]>(method: string, handler: (...args: T) => void): void {
    this.connection?.on(method, handler as (...args: unknown[]) => void);
  }

  /** Runs when the connection drops and the automatic reconnect starts trying. */
  onReconnecting(handler: () => void): void {
    this.connection?.onreconnecting(() => handler());
  }

  /** Runs when the connection is closed for good (it was stopped, or reconnecting gave up). */
  onClosed(handler: () => void): void {
    this.connection?.onclose(() => handler());
  }

  /** Runs after the automatic reconnect succeeded; the server has forgotten the old connection by then. */
  onReconnected(handler: () => void): void {
    this.connection?.onreconnected(() => handler());
  }

  async invoke<T>(method: string, ...args: unknown[]): Promise<T> {
    if (!this.connection) {
      throw new Error('Hub is not connected');
    }
    const response = await this.connection.invoke<ApiResponse<T>>(method, ...args);
    if (response.errors?.length) {
      throw new ApiRequestError(0, response.errors);
    }
    return response.data as T;
  }
}
