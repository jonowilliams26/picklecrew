# Domain

## Crew

A named group that users belong to within Picklecrew.

### Business rules

- A crew must have a name.
- Each crew has a UUID used as its public identifier.
- Crew names do not need to be unique.
- The user who creates a crew is its administrator.

### Relationships

- Each crew corresponds to exactly one WorkOS organization.
- Each WorkOS organization corresponds to exactly one crew.

## User

A person who uses Picklecrew.

### Business rules

- WorkOS is authoritative for whether a user's crew membership is active.
- A user must have an active membership in one or more crews.
- A user without an active crew membership must create a crew.
- Any active crew membership allows a user to skip onboarding.
- A signed-in user operates within the context of one active crew.
- The user's current active crew remains selected when its WorkOS organization membership is active.
- When the user has active crew memberships but no valid active crew context, the first active membership becomes active.
- Users with multiple crew memberships may switch their active crew.
