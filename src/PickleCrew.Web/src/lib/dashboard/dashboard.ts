import type {
  BestPartnerHttpResponse,
  GetBestDoublesPairingHttpResponse,
  GetBestPartnerHttpResponse,
  GetBiggestRivalsHttpResponse,
  GetBrickWallHttpResponse,
  GetChiefPaddlerHttpResponse,
  GetCrewHealthHttpResponse,
  GetIceManHttpResponse,
  GetCrewLeaderboardHttpResponse,
  GetCrewLongestWinStreakHttpResponse,
  GetEasyWinHttpResponse,
  GetFriendsOrLoversHttpResponse,
  GetHotPaddleHttpResponse,
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
  crewHealth: GetCrewHealthHttpResponse
  leaderboard: GetCrewLeaderboardHttpResponse
  longestWinStreak: GetCrewLongestWinStreakHttpResponse
  bestDoublesPairing: GetBestDoublesPairingHttpResponse
  bestPartner: GetBestPartnerHttpResponse
  biggestRivals: GetBiggestRivalsHttpResponse
  brickWall: GetBrickWallHttpResponse
  chiefPaddler: GetChiefPaddlerHttpResponse
  iceMan: GetIceManHttpResponse
  friendsOrLovers: GetFriendsOrLoversHttpResponse
  hotPaddle: GetHotPaddleHttpResponse
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

export type CrewHealthCalendarDay = Readonly<{
  date: string
  gameCount: number
  intensity: 0 | 1 | 2 | 3 | 4
  weekIndex: number
  dayOfWeek: number
}>

export type CrewHealthMonthLabel = Readonly<{
  label: string
  weekIndex: number
}>

export type CrewHealthCalendar = Readonly<{
  days: ReadonlyArray<CrewHealthCalendarDay>
  monthLabels: ReadonlyArray<CrewHealthMonthLabel>
  weekCount: number
  totalGames: number
  activeDayCount: number
}>

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

export function buildCrewHealthCalendar({
  crewHealth,
}: {
  crewHealth: GetCrewHealthHttpResponse
}): CrewHealthCalendar {
  const startDate = parseUtcDate({ date: crewHealth.startDate })
  const endDate = parseUtcDate({ date: crewHealth.endDate })

  if (endDate < startDate) {
    throw new Error('Crew health end date must not precede its start date.')
  }

  const gameCountsByDate = new Map<string, number>()

  for (const day of crewHealth.days) {
    if (gameCountsByDate.has(day.date)) {
      throw new Error('Crew health must contain at most one entry per date.')
    }

    gameCountsByDate.set(day.date, day.gameCount)
  }

  const days: CrewHealthCalendarDay[] = []
  const monthLabels: CrewHealthMonthLabel[] = []
  const seenMonthWeeks = new Set<number>()
  const startDayOffset = startDate.getUTCDay()
  let totalGames = 0
  let activeDayCount = 0

  for (
    let date = startDate, dayIndex = 0;
    date <= endDate;
    date = addUtcDays({ date, days: 1 }), dayIndex++
  ) {
    const isoDate = toIsoDate({ date })
    const gameCount = gameCountsByDate.get(isoDate) ?? 0
    const weekIndex = Math.floor((startDayOffset + dayIndex) / 7)

    if (dayIndex === 0 || date.getUTCDate() === 1) {
      if (!seenMonthWeeks.has(weekIndex)) {
        monthLabels.push({
          label: new Intl.DateTimeFormat('en-AU', {
            month: 'short',
            timeZone: 'UTC',
          }).format(date),
          weekIndex,
        })
        seenMonthWeeks.add(weekIndex)
      }
    }

    totalGames += gameCount
    activeDayCount += gameCount > 0 ? 1 : 0
    days.push({
      date: isoDate,
      gameCount,
      intensity: getCrewHealthIntensity({ gameCount }),
      weekIndex,
      dayOfWeek: date.getUTCDay(),
    })
  }

  return {
    days,
    monthLabels,
    weekCount:
      Math.floor((startDayOffset + days.length - 1) / 7) + 1,
    totalGames,
    activeDayCount,
  }
}

export function formatCrewHealthDay({
  date,
  gameCount,
}: {
  date: string
  gameCount: number
}): string {
  const formattedDate = new Intl.DateTimeFormat('en-AU', {
    day: 'numeric',
    month: 'short',
    year: 'numeric',
    timeZone: 'UTC',
  }).format(parseUtcDate({ date }))

  return `${formatCount({ count: gameCount, singular: 'game' })} on ${formattedDate}`
}

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

export function formatSignedNumber({
  value,
  maximumFractionDigits = 0,
}: {
  value: number | string
  maximumFractionDigits?: number
}): string {
  const numericValue = Number(value)
  const formattedValue = numericValue.toLocaleString('en-AU', {
    maximumFractionDigits,
  })

  return numericValue > 0 ? `+${formattedValue}` : formattedValue
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
    return 'You’re matching your personal-best streak'
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

function getCrewHealthIntensity({
  gameCount,
}: {
  gameCount: number
}): 0 | 1 | 2 | 3 | 4 {
  if (gameCount <= 0) {
    return 0
  }

  if (gameCount === 1) {
    return 1
  }

  if (gameCount === 2) {
    return 2
  }

  if (gameCount === 3) {
    return 3
  }

  return 4
}

function parseUtcDate({ date }: { date: string }): Date {
  const parsedDate = new Date(`${date}T00:00:00Z`)

  if (Number.isNaN(parsedDate.valueOf())) {
    throw new Error(`Invalid calendar date: ${date}`)
  }

  return parsedDate
}

function addUtcDays({ date, days }: { date: Date; days: number }): Date {
  const nextDate = new Date(date)
  nextDate.setUTCDate(nextDate.getUTCDate() + days)

  return nextDate
}

function toIsoDate({ date }: { date: Date }): string {
  return date.toISOString().slice(0, 10)
}

function formatDate({ dateTimeUtc }: { dateTimeUtc: string }): string {
  return new Intl.DateTimeFormat('en-AU', {
    day: 'numeric',
    month: 'short',
    year: 'numeric',
    timeZone: 'UTC',
  }).format(new Date(dateTimeUtc))
}
