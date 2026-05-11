---
name: contract-to-controller
description: "Discover existing API contracts and implement or update an ASP.NET Core controller with correctly attributed REST endpoints. Use when adding a new domain API slice, wiring request/response records, or matching an OpenAPI path to controller actions."
argument-hint: "Domain or endpoint goal, for example: Implement onboarding controller from openapi spec"
---

# Contract To Controller

## What This Skill Produces

- A clear mapping from contract records in PathfinderAI.Contracts to controller actions in PathfinderAPI.
- A new or updated controller under PathfinderAPI/Controllers with endpoint attributes, request binding, and response types.
- A quick validation pass to ensure build health and route shape consistency.

## When To Use

- You need to add a new API area such as accounts, onboarding, analytics, progress, assistant, or learning plans.
- You need to implement controller endpoints from an OpenAPI contract.
- You need to update an existing controller after contract changes.

## Inputs To Collect

- Target domain: accounts, onboarding, analytics, assistant, progress, or learning plans.
- Contract source: existing records in PathfinderAI.Contracts and/or endpoint definitions in pathfinderai-mvp.openapi.yaml.
- Desired route base and HTTP verbs.

## Procedure

1. Discover the contract surface.

- Check for existing records in PathfinderAI.Contracts/<Domain>/.
- Identify request and response record names and property shapes.
- Reuse existing contracts whenever possible; only add new records if required by the endpoint goal.

2. Decide create vs update path.

- If a controller already exists in PathfinderAPI/Controllers for the domain, update it.
- If no controller exists, create a new <Domain>Controller.cs in PathfinderAPI/Controllers.

3. Establish controller skeleton.

- Add namespace PathfinderAPI.Controllers.
- Add [ApiController] and [Route("api/<resource>")] on the controller class.
- Import matching contract namespace, for example PathfinderAI.Contracts.Accounts.

4. Implement endpoint actions.

- Add one action per endpoint with the proper attribute: [HttpGet], [HttpPost], [HttpPatch], [HttpPut], [HttpDelete].
- Bind request contracts from body when needed using [FromBody].
- Return typed ActionResult<TResponse> for strongly typed API behavior.
- Use appropriate status helpers:
  - Ok(...) for successful reads/updates.
  - CreatedAtAction(...) or Created(...) for create operations.
  - Accepted(...) for asynchronous/queued operations.
  - NoContent() for successful operations without a payload.

5. Align endpoint semantics.

- Match route templates and action names to the OpenAPI path and operation intent.
- Ensure response contracts correspond to operation outputs.
- Keep behavior minimal and deterministic for MVP stubs.

6. Validate integration.

- Ensure PathfinderAPI/Program.cs still includes AddControllers and MapControllers.
- Run dotnet restore then dotnet build from repository root.
- Fix compile issues related to namespace imports, contract names, or return type mismatches.

## Decision Rules

- If an OpenAPI operation and contract records disagree, prefer explicit OpenAPI endpoint shape and adjust/add records in PathfinderAI.Contracts with minimal changes.
- If a contract type can be reused across endpoints, reuse it instead of duplicating records.
- If a delete endpoint accepts extra metadata (reason flags, hard-delete flag), use a request body contract only when required by the API contract.

## Completion Checks

- Controller compiles with no unresolved contract types.
- Every intended endpoint has the expected HTTP attribute and route.
- Each action returns a typed ActionResult with the correct response contract.
- The solution builds successfully.

## Repo Conventions To Follow

- Contracts are record types grouped by domain under PathfinderAI.Contracts/<Domain>/.
- Controllers are class-based under PathfinderAPI/Controllers with attribute routing.
- Keep edits focused and avoid unrelated refactors.
