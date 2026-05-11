const API_BASE_URL =
  import.meta.env.VITE_API_BASE_URL || "http://localhost:5136";

export async function getTracks() {
  const response = await fetch(`${API_BASE_URL}/api/onboarding/tracks`);

  if (!response.ok) {
    throw new Error("Failed to load tracks");
  }

  return response.json();
}

export async function startAssistantSession(payload: {
  userId?: string;
  learningPlanId?: string;
}) {
  const response = await fetch(`${API_BASE_URL}/api/assistant/sessions`, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
    },
    body: JSON.stringify(payload),
  });

  if (!response.ok) {
    throw new Error("Failed to start assistant session");
  }

  return response.json();
}

export async function sendAssistantMessage(payload: {
  sessionId?: string;
  message: string;
}) {
  const response = await fetch(`${API_BASE_URL}/api/assistant/messages`, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
    },
    body: JSON.stringify(payload),
  });

  if (!response.ok) {
    throw new Error("Failed to send assistant message");
  }

  return response.json();
}