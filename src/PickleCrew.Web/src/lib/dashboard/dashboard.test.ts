import assert from 'node:assert/strict'
import { describe, test } from 'node:test'
import {
  formatCount,
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
  test('formats API decimal percentages as whole percentages', () => {
    assert.equal(formatPercentage({ value: 0.676 }), 68)
    assert.equal(formatPercentage({ value: '0.8' }), 80)
    assert.equal(formatPercentage({ value: 0 }), 0)
  })

  test('formats signed point differentials', () => {
    assert.equal(formatSignedNumber({ value: 72 }), '+72')
    assert.equal(formatSignedNumber({ value: 0 }), '0')
    assert.equal(formatSignedNumber({ value: -12 }), '-12')
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
      'You’ve got the hot paddle — personal best in progress',
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
        'Play at least three doubles Games with the same Crew member to qualify.',
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
