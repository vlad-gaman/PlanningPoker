export interface ApiError {
  code: string;
  message: string;
  metadata?: Record<string, string> | null;
}

export interface ApiResponse<T> {
  data: T | null;
  errors: ApiError[] | null;
}
