# PathfinderAI Playground Notes

This file explains how the team should use Azure Foundry Playground and Foundry Local with the current Python agent scaffold.

## Goal

Separate prompt iteration from backend integration work.

Use Playground to refine:

- system prompts
- output style
- guardrails
- grounded answer behavior
- roadmap formatting

Use code to implement:

- API contracts
- auth
- orchestration
- context assembly
- telemetry
- Microsoft Learn MCP integration

## Azure Foundry Playground

Use Azure Foundry Playground when:

- the team wants to compare model behavior in the Azure-hosted environment
- prompt wording is still unstable
- stakeholders need to review outputs quickly
- you want to validate model/deployment choices

Recommended inputs:

- prompt from `prompts/*.system.md`
- sample learner context JSON
- representative user questions
- the exact deployment name you plan to use in `AZURE_OPENAI_DEPLOYMENT_NAME`

## Foundry Local

Use Foundry Local when:

- you want low-cost local iteration
- you want prompt experiments without cloud dependency
- you want faster local feedback loops
- you do not need the full Azure-hosted behavior

Important constraint:

Foundry Local is useful for prompt testing and local model experiments, but it is not a drop-in replacement for the Azure Foundry backend architecture. Treat it as a dev workflow, not as the final multi-user runtime.

## Team workflow recommendation

1. Draft or update the system prompt in `prompts/`.
2. Test the prompt in Azure Foundry Playground.
3. If useful, run quick local experiments with Foundry Local.
4. Approve the prompt text with product/backend stakeholders.
5. Sync the approved prompt into the Python agent class.
6. Validate the same prompt through the FastAPI endpoint.

## Rule for prompt ownership

The prompt source of truth should not live only in Python code.

Keep prompt text versioned in `prompts/` so:

- product can review it
- backend can diff it
- prompt changes are traceable
- Playground and runtime stay aligned
