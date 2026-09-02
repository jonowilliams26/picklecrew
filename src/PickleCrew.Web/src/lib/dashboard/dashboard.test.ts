import assert from 'node:assert/strict'
import { describe, test } from 'node:test'
import {
  buildCrewHealthCalendar,
  formatAveragePoints,
  formatAveragePointsConceded,
  formatCount,
  formatCrewHealthDay,
  formatPercentage,
  formatRecord,
  formatSignedNumber,
  formatStreakPeriod,
  getBestPartnerPresentation,
  getCurrentStreakDetail,
  getFriendsOrLoversCardDetails,
  getInitials,
} from './dashboard.ts'

describe('dashboard formatting', () => {
  test('builds a week-aligned crew health calendar from sparse daily activity', () => {
    const calendar = buildCrewHealthCalendar({
      crewHealth: {
        startDate: '2026-03-01',
        endDate: '2026-03-09',
        days: [
          { date: '2026-03-01', gameCount: 1 },
          { date: '2026-03-04', gameCount: 3 },
          { date: '2026-03-09', gameCount: 7 },
        ],
      },
    })

    assert.equal(calendar.weekCount, 2)
    assert.equal(calendar.totalGames, 11)
    assert.equal(calendar.activeDayCount, 3)
    assert.deepEqual(calendar.monthLabels, [
      { label: 'Mar', weekIndex: 0 },
    ])
    assert.deepEqual(
      calendar.days.map((day) => ({
        date: day.date,
        gameCount: day.gameCount,
        intensity: day.intensity,
        weekIndex: day.weekIndex,
        dayOfWeek: day.dayOfWeek,
      })),
      [
        { date: '2026-03-01', gameCount: 1, intensity: 1, weekIndex: 0, dayOfWeek: 0 },
        { date: '2026-03-02', gameCount: 0, intensity: 0, weekIndex: 0, dayOfWeek: 1 },
        { date: '2026-03-03', gameCount: 0, intensity: 0, weekIndex: 0, dayOfWeek: 2 },
        { date: '2026-03-04', gameCount: 3, intensity: 3, weekIndex: 0, dayOfWeek: 3 },
        { date: '2026-03-05', gameCount: 0, intensity: 0, weekIndex: 0, dayOfWeek: 4 },
        { date: '2026-03-06', gameCount: 0, intensity: 0, weekIndex: 0, dayOfWeek: 5 },
        { date: '2026-03-07', gameCount: 0, intensity: 0, weekIndex: 0, dayOfWeek: 6 },
        { date: '2026-03-08', gameCount: 0, intensity: 0, weekIndex: 1, dayOfWeek: 0 },
        { date: '2026-03-09', gameCount: 7, intensity: 4, weekIndex: 1, dayOfWeek: 1 },
      ],
    )
  })

  test('formats crew health day activity', () => {
    assert.equal(
      formatCrewHealthDay({ date: '2026-03-09', gameCount: 1 }),
      '1 game on 9 Mar 2026',
    )
    assert.equal(
      formatCrewHealthDay({ date: '2026-03-10', gameCount: 4 }),
      '4 games on 10 Mar 2026',
    )
  })

  test('formats average points with at most two decimal places', () => {
    assert.equal(formatAveragePoints({ value: 10 }), '10')
    assert.equal(formatAveragePoints({ value: '9.666666' }), '9.67')
  })

  test('formats API decimal percentages as whole percentages', () => {
    assert.equal(formatPercentage({ value: 0.676 }), 68)
    assert.equal(formatPercentage({ value: '0.8' }), 80)
    assert.equal(formatPercentage({ value: 0 }), 0)
  })

  test('formats average points conceded to one decimal place', () => {
    assert.equal(formatAveragePointsConceded({ value: 4 }), '4.0')
    assert.equal(formatAveragePointsConceded({ value: '4.56' }), '4.6')
  })

  test('formats signed point differentials and average margins', () => {
    assert.equal(formatSignedNumber({ value: 72 }), '+72')
    assert.equal(formatSignedNumber({ value: 0 }), '0')
    assert.equal(formatSignedNumber({ value: -12 }), '-12')
    assert.equal(
      formatSignedNumber({ value: 13 / 6, maximumFractionDigits: 1 }),
      '+2.2',
    )
    assert.equal(
      formatSignedNumber({ value: '-3.5', maximumFractionDigits: 1 }),
      '-3.5',
    )
  })

  test('formats win-loss records', () => {
    assert.equal(formatRecord({ wins: 17, losses: 8 }), '17–8')
  })

  test('formats singular, plural, and irregular counts', () => {
    assert.equal(formatCount({ count: 1, singular: 'win' }), '1 win')
    assert.equal(formatCount({ count: 2, singular: 'win' }), '2 wins')
    assert.equal(
      formatCount({ count: 2, singular: 'person', plural: 'people' }),
      '2 people',
    )
  })

  test('creates Friends or lovers pairing card details', () => {
    const details = getFriendsOrLoversCardDetails({
      friendsOrLovers: {
        pairing: {
          players: [
            {
              userId: 'user-alpha',
              displayName: 'Alpha Player',
              profilePictureUrl: null,
            },
            {
              userId: 'user-beta',
              displayName: 'Beta Player',
              profilePictureUrl: 'https://example.test/beta.jpg',
            },
          ],
          wins: 7,
          losses: 5,
          gamesPlayed: 12,
          winPercentage: 7 / 12,
          pointDifferential: 9,
        },
      },
    })

    assert.deepEqual(details, {
      type: 'pairing',
      firstPlayer: {
        userId: 'user-alpha',
        displayName: 'Alpha Player',
        profilePictureUrl: null,
      },
      secondPlayer: {
        userId: 'user-beta',
        displayName: 'Beta Player',
        profilePictureUrl: 'https://example.test/beta.jpg',
      },
      gamesPlayed: 12,
      gamesTogetherLabel: 'games together',
    })
  })

  test('creates empty Friends or lovers card details', () => {
    assert.deepEqual(
      getFriendsOrLoversCardDetails({ friendsOrLovers: { pairing: null } }),
      { type: 'empty' },
    )
  })

  test('creates initials from up to two display-name words', () => {
    assert.equal(getInitials({ displayName: ' Maya Chen ' }), 'MC')
    assert.equal(getInitials({ displayName: 'Prince' }), 'P')
    assert.equal(getInitials({ displayName: ' ' }), '?')
  })

  test('describes current streak progress', () => {
    assert.equal(
      getCurrentStreakDetail({ currentWinStreak: 0, longestWinStreak: 4 }),
      'One win is all it takes to start some trouble',
    )
    assert.equal(
      getCurrentStreakDetail({ currentWinStreak: 3, longestWinStreak: 4 }),
      'one away from your best',
    )
    assert.equal(
      getCurrentStreakDetail({ currentWinStreak: 2, longestWinStreak: 5 }),
      '3 away from your best',
    )
    assert.equal(
      getCurrentStreakDetail({ currentWinStreak: 5, longestWinStreak: 5 }),
      'You’re matching your personal-best streak',
    )
  })

  test('builds available and empty Best Partner presentations', () => {
    assert.deepEqual(
      getBestPartnerPresentation({
        bestPartner: {
          userId: 'user_partner',
          displayName: 'Alex Partner',
          profilePictureUrl: null,
          wins: 8,
          losses: 2,
          gamesPlayed: 10,
          winPercentage: '0.8',
          pointsScored: 110,
          pointsConceded: 101,
          pointDifferential: 9,
        },
      }),
      {
        available: true,
        partner: {
          userId: 'user_partner',
          displayName: 'Alex Partner',
          profilePictureUrl: null,
          wins: 8,
          losses: 2,
          gamesPlayed: 10,
          winPercentage: '0.8',
          pointsScored: 110,
          pointsConceded: 101,
          pointDifferential: 9,
        },
        winPercentage: 80,
      },
    )
    assert.deepEqual(getBestPartnerPresentation({ bestPartner: null }), {
      available: false,
      title: 'No best partner yet',
      description:
        'Play at least three doubles games with the same crew member to qualify.',
    })
  })

  test('formats completed, active, and absent streak periods', () => {
    assert.equal(
      formatStreakPeriod({
        startedAtUtc: '2026-06-01T23:30:00Z',
        endedAtUtc: '2026-06-05T01:00:00Z',
      }),
      '1 June 2026–5 June 2026',
    )
    assert.equal(
      formatStreakPeriod({
        startedAtUtc: '2026-06-01T23:30:00Z',
        endedAtUtc: null,
      }),
      'Still cooking since 1 June 2026 — somebody hand them an L already',
    )
    assert.equal(
      formatStreakPeriod({ startedAtUtc: null, endedAtUtc: null }),
      'No streak recorded yet',
    )
  })
})
