/** The Operator session (PlanningPoker.Controller/Models/AdminSessionResponse.cs). */
export interface AdminSession {
  authenticated: boolean;
  username?: string | null;
  /** The anti-forgery token. Angular sends it for us from the XSRF-TOKEN cookie, so nothing here reads it. */
  csrfToken?: string | null;
}

/** What the password step led to: signed in, or the authenticator code is still needed. */
export type SignInStep = { kind: 'signedIn' } | { kind: 'code'; challenge: string };
