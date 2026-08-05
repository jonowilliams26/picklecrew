---
name: ef-core-code-quality
description: Apply repository Entity Framework Core code-quality conventions when writing, modifying, refactoring, or reviewing EF Core data-access code.
---

# EF Core Code Quality

Apply these conventions to all Entity Framework Core code in scope.

## Convention-first configuration

Use EF Core conventions and defaults unless a specific requirement calls for explicit configuration.

Do not configure any of the following in `OnModelCreating` or `IEntityTypeConfiguration` merely to restate an EF Core default:

- Table names
- Column names
- Maximum lengths
- Keys
- Relationships
- Indexes
- Delete behaviors
- Value conversions

Add explicit configuration only when the required behavior differs from EF Core's conventions or defaults.

## Entity properties

Make auto-incremented entity IDs non-publicly settable by using `private set`.

Use `init` for entity properties that must not change after creation.

```csharp
public class Post
{
    public int Id { get; private set; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required string Title { get; set; }
}
```

## Enum persistence

Configure persisted enum properties to use string conversion.

```csharp
builder
    .Property(post => post.Status)
    .HasConversion<string>();
```

This explicit conversion is required because storing enum names differs from EF Core's default numeric representation.

## Read-only queries

Use `.AsNoTracking()` when a read-only query returns entities.

```csharp
Post? post = await dbContext.Posts
    .AsNoTracking()
    .SingleOrDefaultAsync(post => post.Id == id);
```

When only specific columns are required, prefer a projection with `.Select()` instead of loading complete entities. Do not include an entity instance in the projection when the query is intended to avoid tracking.

```csharp
PostSummary? post = await dbContext.Posts
    .Where(post => post.Id == id)
    .Select(post => new PostSummary
    {
        Id = post.Id,
        Title = post.Title,
    })
    .SingleOrDefaultAsync();
```

When reviewing EF Core code, identify each violation and propose or apply a correction that follows these conventions.
