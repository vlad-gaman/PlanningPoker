import { ApiError } from '../models/api';

export const UNEXPECTED_ERROR_MESSAGE = 'Something went wrong.';

/** Shown for a 429 whose body carries no server message (a proxy in front of the app can answer one itself). */
export const RATE_LIMITED_MESSAGE = 'Too many requests. Wait a minute and try again.';

const TOO_MANY_REQUESTS = 429;

export class ApiRequestError extends Error {
  constructor(
    readonly status: number,
    readonly errors: ApiError[],
  ) {
    super(
      errors[0]?.message ??
        (status === TOO_MANY_REQUESTS
          ? RATE_LIMITED_MESSAGE
          : `Request failed with status ${status}`),
    );
    this.name = 'ApiRequestError';
  }

  hasCode(code: string): boolean {
    return this.hasError(code);
  }

  /** True when an error with this code exists and carries every given metadata entry. */
  hasError(code: string, metadata: Record<string, string> = {}): boolean {
    return this.errors.some(
      (error) =>
        error.code === code &&
        Object.entries(metadata).every(([key, value]) => error.metadata?.[key] === value),
    );
  }
}

/** The message to show for any failure: the server's message for API errors, a generic one otherwise. */
export function errorMessage(error: unknown): string {
  return error instanceof ApiRequestError ? error.message : UNEXPECTED_ERROR_MESSAGE;
}
