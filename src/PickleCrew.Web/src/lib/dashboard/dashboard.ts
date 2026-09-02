import type {
  BestPartnerHttpResponse,
  GetBestDoublesPairingHttpResponse,
  GetBestPartnerHttpResponse,
  GetBiggestRivalsHttpResponse,
  GetBrickWallHttpResponse,
  GetChiefPaddlerHttpResponse,
  GetIceManHttpResponse,
  GetCrewLeaderboardHttpResponse,
  GetCrewLongestWinStreakHttpResponse,
  GetEasyWinHttpResponse,
  GetFriendsOrLoversHttpResponse,
  GetLimpWristSyndromeHttpResponse,
  GetNemesisHttpResponse,
  GetPersonalStatisticsHttpResponse,
  GetPointMachineHttpResponse,
  GetRecentGamesHttpResponse,
} from '~/lib/api/generated/types.gen'
import type { Result } from '~/lib/results'

export type DashboardData = Readonly<{
  personalStatistics: GetPersonalStatisticsHttpResponse
  easyWin: GetEasyWinHttpResponse
  nemesis: GetNemesisHttpResponse
  recentGames: GetRecentGamesHttpResponse
  leaderboard: GetCrewLeaderboardHttpResponse
  longestWinStreak: GetCrewLongestWinStreakHttpResponse
  bestDoublesPairing: GetBestDoublesPairingHttpResponse
  bestPartner: GetBestPartnerHttpResponse
  biggestRivals: GetBiggestRivalsHttpResponse
  brickWall: GetBrickWallHttpResponse
  chiefPaddler: GetChiefPaddlerHttpResponse
  iceMan: GetIceManHttpResponse
  friendsOrLovers: GetFriendsOrLoversHttpResponse
  limpWristSyndrome: GetLimpWristSyndromeHttpResponse
  pointMachine: GetPointMachineHttpResponse
}>

type DashboardForbiddenError = Readonly<{
  type: 'dashboard-forbidden-error'
  message: string
}>

type DashboardUnavailableError = Readonly<{
  type: 'dashboard-unavailable-error'
  message: string
}>

export type DashboardResult = Result<
  DashboardData,
  DashboardForbiddenError | DashboardUnavailableError
>

export type BestPartnerPresentation =
  | Readonly<{
      available: false
      title: string
      description: string
    }>
  | Readonly<{
      available: true
      partner: BestPartnerHttpResponse
      winPercentage: number
    }>

export function getBestPartnerPresentation({
  bestPartner,
}: {
  bestPartner: BestPartnerHttpResponse | null
}): BestPartnerPresentation {
  if (!bestPartner) {
    return {
      available: false,
      title: 'No best partner yet',
      description:
        'Play at least three doubles games with the same crew member to qualify.',
    }
  }

  return {
    available: true,
    partner: bestPartner,
    winPercentage: formatPercentage({ value: bestPartner.winPercentage }),
  }
}

export function formatPercentage({
  value,
}: {
  value: number | string
}): number {
  return Math.round(Number(value) * 100)
}

export function formatAveragePoints({
  value,
}: {
  value: number | string
}): string {
  return Number(value).toLocaleString('en-AU', {
    maximumFractionDigits: 2,
  })
}

export function formatAveragePointsConceded({
  value,
}: {
  value: number | string
}): string {
  return Number(value).toLocaleString('en-AU', {
    minimumFractionDigits: 1,
    maximumFractionDigits: 1,
  })
}

export function formatSignedNumber({ value }: { value: number }): string {
  return value > 0 ? `+${value.toLocaleString('en-AU')}` : value.toLocaleString('en-AU')
}

export function formatRecord({
  wins,
  losses,
}: {
  wins: number
  losses: number
}): string {
  return `${wins.toLocaleString('en-AU')}–${losses.toLocaleString('en-AU')}`
}

export function formatCount({
  count,
  singular,
  plural = `${singular}s`,
}: {
  count: number
  singular: string
  plural?: string
}): string {
  return `${count.toLocaleString('en-AU')} ${count === 1 ? singular : plural}`
}

export function getFriendsOrLoversCardDetails({
  friendsOrLovers,
}: {
  friendsOrLovers: GetFriendsOrLoversHttpResponse
}) {
  const pairing = friendsOrLovers.pairing

  if (!pairing) {
    return { type: 'empty' as const }
  }

  const firstPlayer = pairing.players[0]
  const secondPlayer = pairing.players[1]

  if (pairing.players.length !== 2 || !firstPlayer || !secondPlayer) {
    throw new Error('A Friends or lovers pairing must contain exactly two players.')
  }

  return {
    type: 'pairing' as const,
    firstPlayer,
    secondPlayer,
    gamesPlayed: pairing.gamesPlayed,
    gamesTogetherLabel:
      pairing.gamesPlayed === 1 ? 'game together' : 'games together',
  }
}

export function getInitials({ displayName }: { displayName: string }): string {
  const initials = displayName
    .split(/\s+/)
    .filter(Boolean)
    .slice(0, 2)
    .map((part) => part.at(0)?.toUpperCase())
    .join('')

  return initials || '?'
}

export function getCurrentStreakDetail({
  currentWinStreak,
  longestWinStreak,
}: {
  currentWinStreak: number
  longestWinStreak: number
}): string {
  if (currentWinStreak === 0) {
    return 'One win is all it takes to start some trouble'
  }

  const winsUntilBest = longestWinStreak - currentWinStreak

  if (winsUntilBest <= 0) {
    return 'You’ve got the hot paddle — personal best in progress'
  }

  return winsUntilBest === 1
    ? 'one away from your best'
    : `${winsUntilBest} away from your best`
}

export function formatStreakPeriod({
  startedAtUtc,
  endedAtUtc,
}: {
  startedAtUtc: string | null
  endedAtUtc: string | null
}): string {
  if (!startedAtUtc) {
    return 'No streak recorded yet'
  }

  if (!endedAtUtc) {
    return `Still cooking since ${formatDate({ dateTimeUtc: startedAtUtc })} — somebody hand them an L already`
  }

  return `${formatDate({ dateTimeUtc: startedAtUtc })}–${formatDate({ dateTimeUtc: endedAtUtc })}`
}

function formatDate({ dateTimeUtc }: { dateTimeUtc: string }): string {
  return new Intl.DateTimeFormat('en-AU', {
    day: 'numeric',
    month: 'short',
    year: 'numeric',
    timeZone: 'UTC',
  }).format(new Date(dateTimeUtc))
}
