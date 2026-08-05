---
name: csharp-code-quality
description: Apply repository C# code-quality conventions when writing, modifying, refactoring, or reviewing C# code.
---

# C# Code Quality

Apply these conventions to all C# code in scope.

## Local variable type inference

Use `var` for local variables when an explicit type is not required and does not add value. Keep an explicit type only when it is needed for correctness or makes otherwise unclear code easier to understand.

```csharp
var customer = await customerRepository.GetAsync(customerId);
var retryCount = 0;
```

## Private fields

- Name private fields using lower camel case.
- Do not prefix private field names with underscores.
- Rename all references when correcting a field name.

```csharp
private readonly ILogger logger;
private int retryCount;
```

## Member names

Use `nameof` whenever a string value identifies a member. Do not use member-name string literals.

```csharp
ArgumentNullException.ThrowIfNull(customer, nameof(customer));
OnPropertyChanged(nameof(DisplayName));
```

## Multiline invocations

For a method invocation split across multiple lines, place the closing parenthesis on its own line and align it with the statement containing the invocation. Do not append it to the final argument.

```csharp
// Bad
using var response = await fixture.ApiClient.PostAsJsonAsync(
    "/persistence-probes",
    request,
    timeout.Token);

// Good
using var response = await fixture.ApiClient.PostAsJsonAsync(
    "/persistence-probes",
    request,
    timeout.Token
);
```

## Conditional statements

Always use braces for `if` statement bodies, including single statements and early returns. Format opening braces on the line after the condition.

```csharp
// Bad
if (condition) return;

// Good
if (condition)
{
    return;
}
```

## Immutable data types

Define immutable data types as `record` types whose properties are `init` only. Do not model immutable data with a `class` or properties using `set`.

```csharp
public record Customer
{
    public required string Name { get; init; }
    public required string EmailAddress { get; init; }
}
```

## Test names

Name test methods using `SystemUnderTest_WhenCondition_ShouldResult`.

- `SystemUnderTest` identifies the member or behavior being tested.
- `WhenCondition` describes the relevant input or state.
- `ShouldResult` describes the expected observable outcome.

Use PascalCase within each segment and separate the segments with underscores.

```csharp
public void CalculateTotal_WhenCartIsEmpty_ShouldReturnZero()
public void Subscribe_WhenEmailAddressIsInvalid_ShouldRejectSubscription()
```

## Code colocation

Prefer colocating closely related code in the same file instead of applying a one-type-per-file rule.

The following is an example of this guidance:

```csharp
public record Subscription
{
    public required SubscriptionStatus Status { get; init; }
}

public enum SubscriptionStatus
{
    Trial,
    Active,
    Cancelled,
}

public static class SubscriptionStatusExtensions
{
    public static bool AllowsAccess(this SubscriptionStatus status) =>
        status is SubscriptionStatus.Trial or SubscriptionStatus.Active;
}
```

When reviewing C# code, identify each violation and propose or apply a correction that follows these conventions.
