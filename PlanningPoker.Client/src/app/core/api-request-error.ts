import { ApiError } from '../models/api';

export class ApiRequestError extends Error {
  constructor(
    readonly status: number,
    readonly errors: ApiError[],
  ) {
    super(errors[0]?.message ?? `Request failed with status ${status}`);
    this.name = 'ApiRequestError';
  }

  hasCode(code: string): boolean {
    return this.errors.some((error) => error.code === code);
  }
}
