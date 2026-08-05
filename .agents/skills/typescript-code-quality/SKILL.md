---
name: typescript-code-quality
description: Apply repository TypeScript code-quality conventions when writing, modifying, refactoring, or reviewing TypeScript code.
---

# TypeScript Code Quality

Apply these conventions to all TypeScript code in scope.

## Known failures

Prefer errors as values over throwing for known failure cases. Represent results as an explicit discriminated union:

```typescript
export type Success<TValue> = Readonly<{
  ok: true;
  value: TValue;
}>;

export type Failure<TError> = Readonly<{
  ok: false;
  error: TError;
}>;

export type Result<TValue, TError> =
  | Success<TValue>
  | Failure<TError>;
```

Use `success(value)` and `failure(error)` helper functions to construct results without type assertions:

```typescript
export function success<TValue>(value: TValue): Success<TValue> {
  return { ok: true, value };
}

export function failure<TError>(error: TError): Failure<TError> {
  return { ok: false, error };
}
```

Narrow results using the `ok` discriminator. Do not infer success or failure from whether a value or error is truthy:

```typescript
const result = findUser(id);
if (!result.ok) {
  handleFindUserError(result.error);
  return;
}

useUser(result.value);
```

For asynchronous operations, return `Promise<Result<TValue, TError>>`, not `Result<Promise<TValue>, TError>`.

## Error types

Represent each expected domain and application failure as its own plain discriminated object type. Compose those types into a union where a `Result` can return multiple failures. Use a consistent literal `type` property and include only the data needed to handle each failure:

```typescript
type UserNotFoundError = {
  readonly type: "user-not-found";
  readonly userId: string;
};

type UserStoreUnavailableError = {
  readonly type: "user-store-unavailable";
  readonly cause: unknown;
};

function findUser(
  id: string,
): Result<User, UserNotFoundError | UserStoreUnavailableError> {
  const user = users.get(id);
  if (!user) {
    return failure({
      type: "user-not-found",
      userId: id,
    });
  }

  return success(user);
}
```

Use the error's `type` discriminator for exhaustive handling. Do not rely on `instanceof` for expected failures.

Use `cause: unknown` when preserving a value caught from JavaScript or an external dependency because JavaScript can throw any value. Use `cause: Error` only after explicitly narrowing or normalizing the caught value. Add a cause only to failures that wrap another failure; ordinary domain outcomes do not need one.

Extend `Error` only when exception behavior is required, such as a useful stack trace, interoperability with an API that expects `Error`, or a failure that may genuinely be thrown. Do not create an `Error` subclass solely to classify an expected failure returned in a `Result`.

Throw an `Error` for unexpected programming errors and violated invariants. Convert anticipated domain, application, and external failures into typed `Result` failures at the appropriate boundary.

## Type safety

Do not use `any` unless it is absolutely necessary. Preserve concrete types and narrow uncertain values instead of bypassing type checking.

Before using an external API, inspect its declarations in `node_modules` to determine the actual exported types and signatures. Do not guess external API types.

## Imports

Use top-level imports only. Do not use inline or dynamic imports, including:

- `await import("package")`
- `import("package").Type`
- Dynamic type imports

Use top-level value or type imports instead:

```typescript
import { createClient } from "package";
import type { ClientOptions } from "package";
```

## Conditional statements

Always use braces for `if` statement bodies, including single statements and early returns. Format opening braces on the same line as the condition.

```typescript
// Bad
if (condition) return;

// Good
if (condition) {
  return;
}
```

## Function parameters

When a function accepts multiple parameters, use one object parameter so each argument's purpose is explicit at the call site.

Inline the object parameter type when it is not reused. Do not create a single-use type or interface solely for the parameter object.

```typescript
function createUser({
  name,
  emailAddress,
}: {
  name: string;
  emailAddress: string;
}): User {
  // Function implementation.
}

const user = createUser({ name, emailAddress });
```

Create a named parameter type only when it has genuine reuse.

When reviewing TypeScript code, identify each violation and propose or apply a correction that follows these conventions.
