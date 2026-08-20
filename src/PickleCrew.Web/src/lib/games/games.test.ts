import assert from 'node:assert/strict'
import { describe, test } from 'node:test'
import {
  changeGameFormat,
  changeParticipantKind,
  changeParticipantValue,
  changeSideScore,
  createInitialGameFormValues,
  createRecordGameRequest,
  isValidFinalScore,
  participantFieldName,
  type GameFormValues,
  type ParticipantKind,
} from './games.ts'

describe('isValidFinalScore', () => {
  const validScores = [
    [11, 0],
    [11, 9],
    [12, 10],
    [15, 13],
    [0, 11],
  ] as const

  for (const [firstScore, secondScore] of validScores) {
    test(`${firstScore}-${secondScore} is valid`, () => {
      assert.equal(isValidFinalScore({ firstScore, secondScore }), true)
    })
  }

  const invalidScores = [
    [-1, 11],
    [10, 10],
    [10, 9],
    [11, 10],
    [12, 9],
    [13, 10],
  ] as const

  for (const [firstScore, secondScore] of invalidScores) {
    test(`${firstScore}-${secondScore} is invalid`, () => {
      assert.equal(isValidFinalScore({ firstScore, secondScore }), false)
    })
  }
})

describe('Game form transitions', () => {
  test('adds doubles slots and clears obsolete slots when returning to singles', () => {
    let values = changeGameFormat({
      values: createInitialGameFormValues(),
      format: 'doubles',
    })
    values = setParticipant({
      values,
      sideIndex: 0,
      participantIndex: 1,
      kind: 'guest',
      value: 'Jamie',
    })

    values = changeGameFormat({ values, format: 'singles' })

    assert.equal(values.sides[0].participants.length, 1)

    values = changeGameFormat({ values, format: 'doubles' })

    assert.deepEqual(values.sides[0].participants[1], {
      kind: 'member',
      userId: '',
      guestName: '',
    })
  })

  test('changing participant type clears the previous participant value', () => {
    let values = setParticipant({
      values: createInitialGameFormValues(),
      sideIndex: 0,
      participantIndex: 0,
      kind: 'member',
      value: 'user_one',
    })

    values = changeParticipantKind({
      values,
      sideIndex: 0,
      participantIndex: 0,
      kind: 'guest',
    })

    assert.deepEqual(values.sides[0].participants[0], {
      kind: 'guest',
      userId: '',
      guestName: '',
    })
  })

  test('new initial values reset every field', () => {
    const initialValues = createInitialGameFormValues()
    const changedValues = changeSideScore({
      values: initialValues,
      sideIndex: 0,
      score: '11',
    })

    assert.notDeepEqual(changedValues, createInitialGameFormValues())
    assert.deepEqual(createInitialGameFormValues(), initialValues)
  })
})

describe('createRecordGameRequest', () => {
  test('builds a singles member request from valid values', () => {
    const result = createRecordGameRequest(createValidSinglesValues())

    assert.equal(result.ok, true)

    if (!result.ok) {
      return
    }

    assert.deepEqual(result.value, {
      format: 'singles',
      sides: [
        {
          score: 11,
          participants: [{ userId: 'user_one' }],
        },
        {
          score: 7,
          participants: [{ userId: 'user_two' }],
        },
      ],
    })
  })

  test('builds mixed doubles and normalizes Guest names', () => {
    let values = changeGameFormat({
      values: createInitialGameFormValues(),
      format: 'doubles',
    })
    values = setParticipant({
      values,
      sideIndex: 0,
      participantIndex: 0,
      kind: 'member',
      value: 'user_one',
    })
    values = setParticipant({
      values,
      sideIndex: 0,
      participantIndex: 1,
      kind: 'guest',
      value: '  Jamie  ',
    })
    values = setParticipant({
      values,
      sideIndex: 1,
      participantIndex: 0,
      kind: 'member',
      value: 'user_two',
    })
    values = setParticipant({
      values,
      sideIndex: 1,
      participantIndex: 1,
      kind: 'guest',
      value: 'Taylor',
    })
    values = setScores({ values, firstScore: '13', secondScore: '11' })

    const result = createRecordGameRequest(values)

    assert.equal(result.ok, true)

    if (!result.ok) {
      return
    }

    assert.deepEqual(result.value, {
      format: 'doubles',
      sides: [
        {
          score: 13,
          participants: [{ userId: 'user_one' }, { guestName: 'Jamie' }],
        },
        {
          score: 11,
          participants: [{ userId: 'user_two' }, { guestName: 'Taylor' }],
        },
      ],
    })
  })

  test('rejects repeated members', () => {
    let values = createValidSinglesValues()
    values = changeParticipantValue({
      values,
      sideIndex: 1,
      participantIndex: 0,
      value: 'user_one',
    })

    const result = createRecordGameRequest(values)

    assert.equal(result.ok, false)

    if (result.ok) {
      return
    }

    assert.equal(
      result.error.fieldErrors[
        participantFieldName({
          sideIndex: 1,
          participantIndex: 0,
          field: 'userId',
        })
      ],
      'A Crew member may participate only once in a Game.',
    )
  })

  test('rejects case-insensitive normalized duplicate Guest names', () => {
    let values = createValidSinglesValues()
    values = setParticipant({
      values,
      sideIndex: 0,
      participantIndex: 0,
      kind: 'guest',
      value: ' Jamie ',
    })
    values = setParticipant({
      values,
      sideIndex: 1,
      participantIndex: 0,
      kind: 'guest',
      value: 'jamie',
    })

    const result = createRecordGameRequest(values)

    assert.equal(result.ok, false)

    if (result.ok) {
      return
    }

    assert.equal(
      result.error.fieldErrors[
        participantFieldName({
          sideIndex: 1,
          participantIndex: 0,
          field: 'guestName',
        })
      ],
      'Guest names must be distinct within a Game.',
    )
  })

  test('rejects all-Guest games', () => {
    let values = createValidSinglesValues()
    values = setParticipant({
      values,
      sideIndex: 0,
      participantIndex: 0,
      kind: 'guest',
      value: 'Jamie',
    })
    values = setParticipant({
      values,
      sideIndex: 1,
      participantIndex: 0,
      kind: 'guest',
      value: 'Taylor',
    })

    const result = createRecordGameRequest(values)

    assert.equal(result.ok, false)

    if (result.ok) {
      return
    }

    assert.equal(
      result.error.fieldErrors.participants,
      'At least one participant must be an active member of the current Crew.',
    )
  })

  test('rejects blank participants and non-whole scores', () => {
    let values = createInitialGameFormValues()
    values = setScores({ values, firstScore: '11', secondScore: '7.5' })

    const result = createRecordGameRequest(values)

    assert.equal(result.ok, false)

    if (result.ok) {
      return
    }

    assert.equal(
      result.error.fieldErrors[
        participantFieldName({
          sideIndex: 0,
          participantIndex: 0,
          field: 'userId',
        })
      ],
      'Select an active Crew member for Side 1.',
    )
    assert.equal(
      result.error.fieldErrors['sides.1.score'],
      'Enter a non-negative whole number.',
    )
  })

  test('rejects a score where play should have ended earlier', () => {
    const values = setScores({
      values: createValidSinglesValues(),
      firstScore: '11',
      secondScore: '10',
    })

    const result = createRecordGameRequest(values)

    assert.equal(result.ok, false)

    if (result.ok) {
      return
    }

    const message = 'Enter a completed score played to 11 and won by two.'
    assert.equal(result.error.fieldErrors['sides.0.score'], message)
    assert.equal(result.error.fieldErrors['sides.1.score'], message)
  })

  test('rejects participant counts that do not match the format', () => {
    const values = {
      ...createValidSinglesValues(),
      format: 'doubles',
    } as const

    const result = createRecordGameRequest(values)

    assert.equal(result.ok, false)

    if (result.ok) {
      return
    }

    assert.equal(
      result.error.fieldErrors.participants,
      'Each Side must have exactly 2 participants for this format.',
    )
  })
})

function createValidSinglesValues(): GameFormValues {
  let values = createInitialGameFormValues()
  values = setParticipant({
    values,
    sideIndex: 0,
    participantIndex: 0,
    kind: 'member',
    value: 'user_one',
  })
  values = setParticipant({
    values,
    sideIndex: 1,
    participantIndex: 0,
    kind: 'member',
    value: 'user_two',
  })

  return setScores({ values, firstScore: '11', secondScore: '7' })
}

function setParticipant({
  values,
  sideIndex,
  participantIndex,
  kind,
  value,
}: {
  values: GameFormValues
  sideIndex: number
  participantIndex: number
  kind: ParticipantKind
  value: string
}): GameFormValues {
  const withKind = changeParticipantKind({
    values,
    sideIndex,
    participantIndex,
    kind,
  })

  return changeParticipantValue({
    values: withKind,
    sideIndex,
    participantIndex,
    value,
  })
}

function setScores({
  values,
  firstScore,
  secondScore,
}: {
  values: GameFormValues
  firstScore: string
  secondScore: string
}): GameFormValues {
  const withFirstScore = changeSideScore({
    values,
    sideIndex: 0,
    score: firstScore,
  })

  return changeSideScore({
    values: withFirstScore,
    sideIndex: 1,
    score: secondScore,
  })
}
