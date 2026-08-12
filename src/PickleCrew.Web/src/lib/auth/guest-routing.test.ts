import assert from 'node:assert/strict'
import test from 'node:test'
import { decideGuestHomepageRoute } from './guest-routing.ts'

test('renders the homepage for a signed-out user', () => {
  assert.deepEqual(decideGuestHomepageRoute({ isSignedIn: false }), {
    type: 'render',
  })
})

test('redirects a signed-in user to the dashboard', () => {
  assert.deepEqual(decideGuestHomepageRoute({ isSignedIn: true }), {
    type: 'redirect',
    destination: '/dashboard',
  })
})
