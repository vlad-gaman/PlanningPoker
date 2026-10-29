import { ApiError } from '../models/api';

export const UNEXPECTED_ERROR_MESSAGE = 'Something went wrong.';

export class ApiRequestError extends Error {
  constructor(
    readonly status: number,
    readonly errors: ApiError[],
  ) {
    super(errors[0]?.message ?? `Request failed with status ${status}`);
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
