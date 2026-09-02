# Domain

## Crew

A named group that users belong to within Picklecrew.

### Business rules

- A crew must have a name.
- Crew names do not need to be unique.
- Any active crew member may create another crew, regardless of their role in their current crew.
- The user who creates a crew is its administrator.
- A newly created crew becomes its creator's active crew.
- After creating a crew, its creator may invite friends or skip the invitation step.
- The invitation step is offered only as part of the uninterrupted crew-creation flow; after a refresh or later sign-in, the creator's active crew membership takes them to the dashboard instead.

### Relationships

- Each crew corresponds to exactly one WorkOS organization.
- Each WorkOS organization corresponds to exactly one crew.

## Crew health

A Crew's recent Game activity shown as a calendar.

### Business rules

- Crew health includes only Games recorded for that Crew within the preceding year.
- Each calendar day shows how many Games were played on that day.
- Calendar color intensity increases as more Games are played on a day.

## User

A person who uses Picklecrew.

### Business rules

- WorkOS is authoritative for whether a user's crew membership is active and for the role assigned to that membership.
- Each WorkOS organization membership has exactly one role.
- Crew administrator status is represented by the `admin` role on the user's WorkOS organization membership and is not stored separately by Picklecrew.
- A user must have an active membership in one or more crews.
- A user without an active crew membership must create a crew.
- A user may create more than one crew, with no application-defined limit.
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
- A crew administrator may assign the `admin` or `member` role to another active crew member.
- A crew administrator may not change their own role.
- A crew administrator may remove another active member from the crew.
- A crew administrator may not remove themselves from the crew.
- Any active crew member may leave their active crew after confirming the crew's exact name, unless they are the crew's only active administrator.
- The only active crew administrator must assign another active member as an administrator before leaving.
- Leaving a crew makes the member's own membership inactive without deleting their historical games or statistics in that crew.
- After leaving a crew, the user is signed out rather than automatically entering another crew or crew creation.
- Removing a crew member makes their membership inactive without deleting their historical games or statistics in that crew.

## Crew member statistics

A crew member's all-time cumulative game results within one crew.

### Business rules

- Crew member statistics are calculated separately for each crew by aggregating each recorded game's contribution.
- Each participating crew member has exactly one crew member statistics contribution for a recorded game.
- A crew member on the winning side of a recorded game gains one win; a crew member on the losing side gains one loss.
- Each participating crew member gains their side's full score as points scored and the opposing side's full score as points conceded.
- In doubles, each participating crew member receives the full contribution; scores are not divided between partners.
- Guests do not receive crew member statistics.
- A crew member on the winning side of a recorded game gains one **That's a Paddlin** when the final score is exactly 11–0.
- Each crew member on a winning doubles side gains one **That's a Paddlin**, including when another participant is a guest.
- Losing crew members and guests do not gain **That's a Paddlin**.
- Games played equals wins plus losses.
- Point differential equals points scored minus points conceded.
- Average margin equals point differential divided by games played and is zero when no games have been played.
- Win percentage equals wins divided by games played and is zero when no games have been played.
- A crew member gains 100 Crew score points plus their side's point differential when they win a game.
- A loss does not change a crew member's Crew score.
- Crew score is the sum of the Crew score points gained from all wins.
- Crew rank is calculated against every active crew member, including members without recorded games.
- Members with equal Crew scores share the same competition rank, and later ranks skip the tied positions (for example, 1, 1, 3).
- Members with equal Crew scores are ordered alphabetically by display name.
- Former crew members are excluded from the current leaderboard while their historical statistics are preserved.
- A current win streak counts consecutive wins through the crew member's newest recorded game and resets to zero when that member loses.
- Win streaks are calculated by played date and time, then by game identifier when games share the same played date and time.
- A longest win streak is the greatest current win streak the crew member has achieved.
- A longest win streak records when its first win was played and when the streak ended; its end is absent while that record-setting streak remains active.
- A crew's longest win streak is the greatest longest win streak held by one of its crew members.
- When active crew members hold equal longest win streaks, the member whose record-setting streak started most recently holds the crew's longest win streak.
- Recorded games are the source for date-range and doubles-pairing statistics.

### Relationships

- Each crew member's statistics belong to exactly one crew and one user.

## Brick Wall

The active crew member with the lowest all-time average points conceded per game in one crew.

### Business rules

- A member must have played at least three games to qualify as Brick Wall.
- Average points conceded equals total points conceded divided by games played.
- The qualifying active member with the lowest average points conceded is Brick Wall.
- Exactly one Brick Wall is selected; tied members are resolved by user identifier.
- Former members cannot be the current Brick Wall, while their historical personal statistics remain preserved.
- Statistics from another crew do not affect the selection.

### Relationships

- A crew has zero or one Brick Wall.

## Chief Paddler

The active crew member who holds the Crew's bragging-rights award for the most all-time **That's a Paddlin** wins.

### Business rules

- A Crew has no Chief Paddler when every active member has zero **That's a Paddlin** wins.
- The active member with the highest positive **That's a Paddlin** count is Chief Paddler.
- Exactly one Chief Paddler is selected; tied leaders are resolved by highest Crew score, then by user identifier.
- Former members cannot be the current Chief Paddler, while their historical personal statistics remain preserved.
- Statistics from another Crew do not affect the award.

### Relationships

- A Crew has zero or one Chief Paddler.

## Limp wrist syndrome

The active crew member who holds the crew's bragging-rights statistic for the most all-time losses.

### Business rules

- A crew has no **limp wrist syndrome** holder when every active member has zero losses.
- The active member with the highest positive loss count holds **limp wrist syndrome**.
- Exactly one holder is selected; tied members are resolved by lowest point differential, then by user identifier.
- Former members cannot be the current holder, while their historical personal statistics remain preserved.
- Statistics from another crew do not affect the selection.

### Relationships

- A crew has zero or one **limp wrist syndrome** holder.

## Point Machine

The active crew member with the highest all-time average points scored per game in the crew.

### Business rules

- Average points scored equals a crew member's total points scored divided by their games played.
- An active crew member qualifies after playing at least three games.
- The qualifying active member with the highest average points scored is the **Point Machine**.
- Tied members are resolved by most games played, then by user identifier.
- Former members cannot be the current Point Machine, while their historical personal statistics remain preserved.
- Statistics from another crew do not affect the selection.

### Relationships

- A crew has zero or one Point Machine.

## The Hot Paddle

The active crew member with the best recent form across their five newest games in one crew.

### Business rules

- A member must have played at least five games to qualify as The Hot Paddle.
- Recent form includes exactly the member's five newest games, ordered by played date and time, then by game identifier when games share the same played date and time.
- The qualifying active member with the most wins across those five games is The Hot Paddle.
- Members tied on wins are resolved by highest point differential across those five games.
- Former members cannot be the current Hot Paddle, while their historical games remain preserved.
- Games from another crew do not affect the selection.

### Relationships

- A crew has zero or one Hot Paddle.

## The Ice Man

The active crew member who holds the crew's bragging-rights award for the most all-time clutch wins.

### Business rules

- A clutch win is a win where the opposing side scores 9 or more points.
- A crew has no Ice Man when every active member has zero clutch wins.
- The active member with the highest positive clutch-win count is the Ice Man.
- Exactly one Ice Man is selected; tied members are resolved by highest Crew score, then by user identifier.
- Former members cannot be the current Ice Man, while their historical clutch wins remain preserved.
- Statistics from another crew do not affect the award.

### Relationships

- A crew has zero or one Ice Man.

## Matchup statistics

A crew member's all-time cumulative results against one opposing crew member within one crew.

### Business rules

- Matchup statistics are directional; each opposing pair has one matchup from each crew member's perspective.
- Matchup statistics are calculated separately for each crew by aggregating each recorded game's directional contributions.
- Each recorded game contributes one directional matchup result for every pair of opposing crew members.
- A crew member receives one win or loss and their side's full points scored and points conceded against each opposing crew member.
- In doubles, every crew member receives the full result against every crew member on the opposing side.
- Crew members on the same side do not have a matchup against one another.
- Guests do not have matchup statistics, but a game containing guests still contributes to matchups between opposing crew members.
- Games played equals wins plus losses.
- Win percentage equals wins divided by games played.
- Point differential equals points scored minus points conceded.
- Recording a game and applying all of its matchup contributions is atomic.

### Relationships

- Each matchup's statistics belong to exactly one crew, exactly one crew member, and exactly one opposing crew member.

## Nemesis

A crew member's active opposing crew member against whom they have the most all-time losses within one crew.

### Business rules

- A Nemesis is calculated separately for each crew from the crew member's directional matchup statistics.
- An opponent must have played at least three games against the crew member to qualify as their Nemesis.
- A crew member has no Nemesis until they have lost to a qualifying active opposing crew member.
- Guests and former crew members cannot be a Nemesis.
- Tied opponents are resolved by user identifier.

### Relationships

- Each crew member has zero or one Nemesis in each crew.
- Each Nemesis identifies exactly one opposing crew member.

## Easy Win

A crew member's active opposing crew member against whom they have the most all-time wins within one crew.

### Business rules

- An Easy Win is calculated separately for each crew from the crew member's directional matchup statistics.
- An opponent must have played at least three games against the crew member to qualify as their Easy Win.
- A crew member has no Easy Win until they have defeated a qualifying active opposing crew member.
- Guests and former crew members cannot be an Easy Win.
- Tied opponents are resolved by user identifier.

### Relationships

- Each crew member has zero or one Easy Win in each crew.
- Each Easy Win identifies exactly one opposing crew member.

## Biggest rivalry

The all-time head-to-head matchup between two active crew members whose win-loss record is closest to even within one crew.

### Business rules

- Matchup balance is the absolute difference between a crew member's win percentage and a 50% win percentage.
- A matchup must include at least three games to qualify.
- The qualifying matchup with the smallest balance difference is the biggest rivalry.
- When matchups have the same balance difference, the matchup with the most games played is selected.
- Remaining ties are resolved by the crew members' identifiers to select exactly one matchup.
- A crew has no biggest rivalry when no matchup qualifies.
- Former crew members cannot be part of the current biggest rivalry, while their historical matchup statistics remain preserved.
- Statistics from another crew do not affect the selection.

### Relationships

- A crew has zero or one biggest rivalry.
- Each biggest rivalry has exactly two active crew members.

## Doubles pairing statistics

A doubles pairing's all-time cumulative results within one crew.

### Business rules

- A doubles pairing consists of two crew members playing on the same side of a doubles game.
- Doubles pairing statistics are calculated by aggregating each qualifying recorded game's contribution.
- Each qualifying doubles game contributes exactly one result for each qualifying crew-member side.
- A doubles pairing is unordered.
- A side containing a guest does not receive doubles pairing statistics.
- A qualifying crew-member pairing receives its side's full score as points scored and the opposing side's full score as points conceded, including when the opposing side contains a guest.
- Singles games do not contribute to doubles pairing statistics.
- Games played equals wins plus losses.
- Point differential equals points scored minus points conceded.
- Win percentage equals wins divided by games played.
- A pairing must have played at least three games to qualify as a crew's best doubles pairing.
- A crew's best doubles pairing must contain two currently active crew members.
- Historical doubles pairing statistics remain when a player becomes inactive.
- The best doubles pairing is selected by highest win percentage, then most games played, then highest point differential.
- Exactly one pairing is selected when qualifying pairings exist.

### Relationships

- Each doubles pairing's statistics belong to exactly one crew and exactly two users.

## Best Partner

The currently active Crew member who forms the signed-in member's strongest qualifying doubles pairing in their active Crew.

### Business rules

- Only doubles pairings containing the signed-in member are considered.
- A pairing must have played at least three Games to qualify.
- The partner must be a currently active member of the Crew.
- Best Partner is selected by highest win percentage, then most Games played, then highest point differential.
- Singles Games and sides containing a Guest do not contribute to a pairing.
- A qualifying Crew-member side receives pairing statistics when the opposing side contains a Guest.
- Statistics from another Crew do not affect Best Partner.
- A member has no Best Partner when no pairing qualifies.

### Relationships

- A Crew member has zero or one Best Partner in their active Crew.

## Friends or lovers?

The active Crew's doubles pairing that has played the most all-time Games together.

### Business rules

- Only pairings containing two currently active Crew members are eligible.
- Games played equals the pairing's wins plus losses.
- A pairing must have played at least three Games together to qualify.
- The qualifying pairing with the most Games played is selected.
- Ties are resolved by most wins, then highest point differential.
- Exactly one pairing is selected when qualifying pairings exist.
- A Crew has no **Friends or lovers?** pairing when no pairing qualifies.
- Statistics from another Crew do not affect the selection.

### Relationships

- A Crew has zero or one **Friends or lovers?** pairing.

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
- After recording a game, starting a rematch keeps the same format, participants, and sides while leaving both scores empty for the new result.
- The played date and time are assigned automatically when the game is saved and cannot be changed.
- Each statistical contribution retains its game's played date and time.
- Any active member of the owning crew may record a game, including a game they did not participate in.
- Recording a game does not require participant approval.
- Possible duplicate games are allowed.
- Only crew administrators may edit or delete a recorded game.
- An administrator may edit a game's format, participants, guest names, and scores, but not its played date and time.
- An edited game must satisfy the same validation rules as a newly recorded game.
- Deleting a game removes all of its contributions from crew member statistics.
- Deleting a game recalculates win streaks from the crew's remaining games.
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
