import { createFileRoute } from '@tanstack/react-router'
import { CrewCreationFlow } from '~/components/crew-creation-flow'

export const Route = createFileRoute('/_authenticated/onboarding')({
  component: OnboardingPage,
})

function OnboardingPage() {
  return <CrewCreationFlow mode="onboarding" />
}
