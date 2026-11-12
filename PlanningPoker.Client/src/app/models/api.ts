export interface ApiError {
  code: string;
  message: string;
  metadata?: Record<string, string> | null;
}

export interface ApiResponse<T> {
  data: T | null;
  errors: ApiError[] | null;
}

/** Error codes the server returns (PlanningPoker.Common/Constants/ErrorCodes.cs). */
export const ERROR_CODES = {
  noSession: 'no_session',
  roomNotFound: 'room_not_found',
  personNotFound: 'person_not_found',
  validationFailed: 'validation_failed',
  unauthorized: 'unauthorized',
  adminInvalidCredentials: 'admin_invalid_credentials',
  adminLoginLocked: 'admin_login_locked',
  adminTotpRequired: 'admin_totp_required',
  adminTotpInvalid: 'admin_totp_invalid',
  adminLoginExpired: 'admin_login_expired',
} as const;

/** Metadata field name the server uses when a room id is malformed. */
export const ROOM_ID_FIELD = 'roomId';
