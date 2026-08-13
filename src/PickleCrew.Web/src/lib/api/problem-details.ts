import type {
  HttpValidationProblemDetails,
  ProblemDetails,
} from './generated/types.gen.ts'

export function getProblemDetailsMessage({
  problemDetails,
  fallbackMessage,
}: {
  problemDetails: HttpValidationProblemDetails | ProblemDetails
  fallbackMessage: string
}): string {
  const validationErrors =
    'errors' in problemDetails ? problemDetails.errors : undefined

  if (validationErrors) {
    return Object.values(validationErrors).flat().join(' ')
  }

  return problemDetails.detail ?? problemDetails.title ?? fallbackMessage
}
