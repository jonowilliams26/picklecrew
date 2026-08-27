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
- A participating crew member's display name is captured when a game is recorded as a historical fallback.
- Game history uses a member's current WorkOS display name when available; otherwise, it uses the display name captured with the game.
- Captured display names are not synchronized when a member's WorkOS display name changes.
- A crew's roster contains its active WorkOS organization members and pending crew invitation recipients.
- Every active crew member may view the crew's pending invitation recipients.
- Active roster entries display the member's assigned role; pending invitation recipients are identified as pending and are not active crew members.
- Expired, revoked, accepted, and otherwise non-pending crew invitations do not appear as invitation recipients in the roster.
- Crew roster entries are ordered alphabetically by display name or invitation email address.

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
- Win percentage equals wins divided by games played and is zero when no games have been played.
- Crew score equals all-time wins multiplied by 100.
- Crew rank is calculated against every active crew member, including members without recorded games.
- Members with equal Crew scores share the same competition rank, and later ranks skip the tied positions (for example, 1, 1, 3).
- Members with equal Crew scores are ordered alphabetically by display name.
- Former crew members are excluded from the current leaderboard while their historical statistics are preserved.
- A current win streak counts consecutive wins through the crew member's newest recorded game and resets to zero when that member loses.
- A longest win streak is the greatest current win streak the crew member has achieved.
- A longest win streak records when its first win was played and when the streak ended; its end is absent while that record-setting streak remains active.
- A crew's longest win streak is the greatest longest win streak held by one of its crew members.
- Recorded games are the source for date-range and doubles-pairing statistics.

### Relationships

- Each crew member's statistics belong to exactly one crew and one user.

## Doubles pairing statistics

A doubles pairing's all-time cumulative results within one crew.

### Business rules

- A doubles pairing consists of two crew members playing on the same side of a doubles game.
- A doubles pairing is unordered.
- A side containing a guest does not receive doubles pairing statistics.
- A qualifying crew-member pairing receives its side's full score as points scored and the opposing side's full score as points conceded, including when the opposing side contains a guest.
- Singles games do not contribute to doubles pairing statistics.
- Games played equals wins plus losses.
- Point differential equals points scored minus points conceded.
- Win percentage equals wins divided by games played.
- No minimum number of games is required for a pairing to qualify as a crew's best doubles pairing.
- A crew's best doubles pairing must contain two currently active crew members.
- Historical doubles pairing statistics remain when a player becomes inactive.
- The best doubles pairing is selected by highest win percentage, then most games played, then highest point differential.
- Exactly one pairing is selected when qualifying pairings exist.

### Relationships

- Each doubles pairing's statistics belong to exactly one crew and exactly two users.

## Crew invitation

A request sent through WorkOS for a person identified by email address to join a crew.

### Business rules

- **Crew invitation** is the canonical domain term; onboarding may describe the action as "invite friends."
- During the optional onboarding invitation step, a crew administrator sends one crew invitation at a time and may send additional invitations before continuing to the dashboard.
- The invitation step does not limit how many crew invitations an administrator may send.
- Any crew administrator may send onboarding invitations.
- After onboarding, only crew administrators may send crew invitations from the crew roster.
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
- A user's last-ten game summary contains at most their ten newest games in the active crew in which they participated.
- A last-ten win streak counts consecutive wins backward from the newest game, stops at the first loss, and never examines a game outside the ten-game summary.
- The last-ten win streak is zero when the newest game is a loss or when no games exist.
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
