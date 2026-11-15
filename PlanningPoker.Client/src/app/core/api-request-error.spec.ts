import { ApiRequestError, RATE_LIMITED_MESSAGE, errorMessage } from './api-request-error';

describe('ApiRequestError', () => {
  const error = new ApiRequestError(400, [
    { code: 'validation_failed', message: 'Bad id.', metadata: { field: 'roomId' } },
  ]);

  it('uses the first server message as its message', () => {
    expect(error.message).toBe('Bad id.');
  });

  it('falls back to the status when there are no server errors', () => {
    expect(new ApiRequestError(502, []).message).toBe('Request failed with status 502');
  });

  it('matches a code with and without metadata', () => {
    expect(error.hasCode('validation_failed')).toBe(true);
    expect(error.hasCode('room_not_found')).toBe(false);
    expect(error.hasError('validation_failed', { field: 'roomId' })).toBe(true);
  });

  it('does not match when the metadata differs', () => {
    expect(error.hasError('validation_failed', { field: 'name' })).toBe(false);
    expect(error.hasError('validation_failed', { reason: 'required' })).toBe(false);
  });
});

describe('errorMessage', () => {
  it('returns the message of an API error', () => {
    expect(
      errorMessage(new ApiRequestError(500, [{ code: 'internal_error', message: 'Server down.' }])),
    ).toBe('Server down.');
  });

  it('returns the server message of a rate_limited response', () => {
    const limited = new ApiRequestError(429, [
      { code: 'rate_limited', message: 'Too many requests. Wait a minute and try again.' },
    ]);

    expect(errorMessage(limited)).toBe('Too many requests. Wait a minute and try again.');
  });

  it('shows a rate-limit message for a 429 with no body, instead of a bare status', () => {
    const limited = new ApiRequestError(429, []);

    expect(errorMessage(limited)).toBe(RATE_LIMITED_MESSAGE);
  });

  it('returns a generic message for anything else', () => {
    expect(errorMessage(new Error('socket reset'))).toBe('Something went wrong.');
    expect(errorMessage(undefined)).toBe('Something went wrong.');
  });
});
