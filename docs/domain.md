# Domain

## Crew

A named group that users belong to within Picklecrew.

### Business rules

- A crew must have a name.
- Crew names do not need to be unique.
- The user who creates a crew is its administrator.
- A newly created crew becomes its creator's active crew.
- After creating a crew during onboarding, its creator may invite friends or skip the invitation step.
- The invitation step is offered only as part of the uninterrupted crew-creation flow; after a refresh or later sign-in, the creator's active crew membership takes them to the dashboard instead.

### Relationships

- Each crew corresponds to exactly one WorkOS organization.
- Each WorkOS organization corresponds to exactly one crew.

## User

A person who uses Picklecrew.

### Business rules

- WorkOS is authoritative for whether a user's crew membership is active and for the role assigned to that membership.
- Each WorkOS organization membership has exactly one role.
- Crew administrator status is represented by the `admin` role on the user's WorkOS organization membership and is not stored separately by Picklecrew.
- A user must have an active membership in one or more crews.
- A user without an active crew membership must create a crew.
- Any active crew membership allows a user to skip onboarding.
- A signed-in user operates within the context of one active crew.
- Application data accessed after sign-in is scoped to the user's active crew.
- The user's current active crew remains selected when its WorkOS organization membership is active.
- When the user has active crew memberships but no valid active crew context, the first active membership becomes active.
- Users with multiple crew memberships may switch their active crew.
- A user's statistics are calculated separately for each crew.
- A user's historical games and statistics remain in a crew after their membership becomes inactive.
- A crew's roster contains its active WorkOS organization members and displays each member's assigned role.
- Users in a crew's roster are ordered alphabetically by display name.

## Crew member statistics

A crew member's all-time cumulative game results within one crew.

### Business rules

- Crew member statistics are calculated separately for each crew.
- A crew member on the winning side of a recorded game gains one win; a crew member on the losing side gains one loss.
- Each participating crew member gains their side's full score as points scored and the opposing side's full score as points conceded.
- In doubles, each participating crew member receives the full contribution; scores are not divided between partners.
- Guests do not receive crew member statistics.
- Games played equals wins plus losses.
- Point differential equals points scored minus points conceded.
- Win percentage equals wins divided by games played.
- Recorded games are the source for date-range, streak, and doubles-pairing statistics.

### Relationships

- Each crew member's statistics belong to exactly one crew and one user.

## Crew invitation

A request sent through WorkOS for a person identified by email address to join a crew.

### Business rules

- **Crew invitation** is the canonical domain term; onboarding may describe the action as "invite friends."
- During the optional onboarding invitation step, a crew administrator sends one crew invitation at a time and may send additional invitations before continuing to the dashboard.
- The invitation step does not limit how many crew invitations an administrator may send.
- Any crew administrator may send onboarding invitations.
- A crew invitation grants the standard member role, not the administrator role, if accepted.
- An active crew member cannot be invited to that crew.
- A person who uses Picklecrew but is not an active member of the crew may be invited.
- An existing pending invitation is not sent again.
- A failed crew invitation may be retried without affecting invitations sent previously.
- Invitation expiry, locale, and email delivery use WorkOS defaults.

### Relationships

- Each crew invitation is associated with exactly one crew.
- Each crew invitation targets exactly one email address.

## Game

A completed pickleball game with a final score between two sides.

### Business rules

- A game is either singles with one player on each side or doubles with two players on each side.
- Each competing group in a game is called a side.
- A player may appear on only one side in a game.
- Scores are non-negative whole numbers.
- A game is played to 11 points.
- A final score from 11–0 through 11–9 is valid.
- After 10–10, the winning side must finish exactly two points ahead.
- A score is invalid if play should have ended earlier.
- Only completed games with one winning side are recorded.
- A game may include any combination of active crew members and guests, but at least one participant must be an active member of the owning crew.
- Games are recorded immediately after they are played.
- The played date and time are assigned automatically when the game is saved and cannot be changed.
- Any active member of the owning crew may record a game, including a game they did not participate in.
- Recording a game does not require participant approval.
- Possible duplicate games are allowed.
- Only crew administrators may edit or delete a recorded game.
- An administrator may edit a game's format, participants, guest names, and scores, but not its played date and time.
- An edited game must satisfy the same validation rules as a newly recorded game.
- Deleting a game removes all of its contributions from crew member statistics.
- A recorded game contributes to each participating crew member's statistics, including when they play with or against guests.
- Game statistics are calculated within the owning crew only.
- Only active members of the owning crew may view its games.

### Relationships

- Each game belongs to exactly one crew.
- Each game records exactly one recorder, who must be an active member of the owning crew when the game is saved.
- Each singles game has exactly two players.
- Each doubles game has exactly four players.
- Every participating crew member must have an active membership in the owning crew when the game is saved.

## Side

One of the two competing groups in a game.

### Business rules

- A side has one player in singles and two players in doubles.
- A side may contain active crew members, guests, or both.

### Relationships

- Each game has exactly two sides.

## Guest

A person who participates in one game without being an active member of its crew.

### Business rules

- A guest is entered with a nonblank name while recording a game.
- Guest names must be distinct within a game but do not need to be unique across games.
- A guest exists only within the game in which they participate and cannot be reused in another game.
- Statistics are not calculated for guests.
- A guest's participation is not transferred or claimed if they later join the crew as a user.

### Relationships

- Each guest participates in exactly one game.
