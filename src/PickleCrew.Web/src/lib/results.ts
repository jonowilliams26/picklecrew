export type Success<TValue> = Readonly<{
  ok: true
  value: TValue
}>

export type Failure<TError> = Readonly<{
  ok: false
  error: TError
}>

export type Result<TValue, TError> = Success<TValue> | Failure<TError>

export function success<TValue>(value: TValue): Success<TValue> {
  return { ok: true, value }
}

export function failure<TError>(error: TError): Failure<TError> {
  return { ok: false, error }
}
