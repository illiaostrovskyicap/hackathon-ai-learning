# PathfinderAI.Contracts

Contracts-only library for the PathfinderAI MVP backend.

## Included MVP areas

- `Accounts`: current user profile and preferences
- `Onboarding`: track selection and initial roadmap request
- `LearningPlans`: roadmap generation and retrieval
- `Assistant`: grounded Microsoft Learn-style Q&A session contracts
- `Progress`: module state and learning activity logging
- `Analytics`: dashboard summary payloads
- `IntegrationEvents`: event contracts for Azure messaging / telemetry pipelines

## Deferred from MVP

- interview simulator
- subscriptions and billing
- editorial CMS/admin backoffice
- advanced assessments

## Suggested API surface

- `GET /api/me`
- `PATCH /api/me`
- `DELETE /api/me`
- `GET /api/onboarding/tracks`
- `GET /api/onboarding/status`
- `POST /api/onboarding/complete`
- `POST /api/learning-plans`
- `GET /api/learning-plans/active`
- `POST /api/learning-plans/{learningPlanId}/regenerate`
- `POST /api/assistant/sessions`
- `POST /api/assistant/messages`
- `PATCH /api/learning-plans/{learningPlanId}/modules/{moduleId}`
- `POST /api/progress/activities`
- `POST /api/auth/login`
- `GET /api/dashboard/summary`
