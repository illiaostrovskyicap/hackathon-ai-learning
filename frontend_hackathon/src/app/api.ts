const API_BASE_URL =
  import.meta.env.VITE_API_BASE_URL || "http://localhost:5136";

function normalizeLearningPlan(data: any) {
  return {
    id: data.id ?? data.Id,
    track: data.track ?? data.Track,
    experience: data.experience ?? data.Experience,
    language: data.language ?? data.Language,
    generatedAt: data.generatedAt ?? data.GeneratedAt,

    modules: (data.modules ?? data.Modules ?? []).map((m: any) => ({
      id: m.id ?? m.Id,
      title: m.title ?? m.Title,
      description: m.description ?? m.Description,
      status: m.status ?? m.Status ?? "not-started",
      estimatedHours: m.estimatedHours ?? m.EstimatedHours ?? 8,
      topics: m.topics ?? m.Topics ?? [],
      resources: (m.resources ?? m.Resources ?? []).map((r: any) => ({
        id: r.id ?? r.Id,
        title: r.title ?? r.Title,
        type: r.type ?? r.Type,
        url: r.url ?? r.Url,
      })),
      subModules: (m.subModules ?? m.SubModules ?? []).map((sm: any) => ({
        id: sm.id ?? sm.Id,
        title: sm.title ?? sm.Title,
        description: sm.description ?? sm.Description,
        estimatedHours: sm.estimatedHours ?? sm.EstimatedHours ?? 4,
        topics: sm.topics ?? sm.Topics ?? [],
        projectTask: sm.projectTask ?? sm.ProjectTask ?? "",
        resources: (sm.resources ?? sm.Resources ?? []).map((r: any) => ({
          id: r.id ?? r.Id,
          title: r.title ?? r.Title,
          type: r.type ?? r.Type,
          url: r.url ?? r.Url,
        })),
      })),
    })),
  };
}

export async function login(email: string, password: string) {
  const response = await fetch(`${API_BASE_URL}/api/auth/login`, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
    },
    body: JSON.stringify({ email, password }),
  });

  if (!response.ok) {
    throw new Error("Login failed");
  }

  return response.json();
}

export async function getMe(token: string) {
  const response = await fetch(`${API_BASE_URL}/api/auth/me`, {
    headers: {
      Authorization: `Bearer ${token}`,
    },
  });

  if (!response.ok) {
    throw new Error("Failed to load current user");
  }

  return response.json();
}

export async function generateLearningPlan(payload: {
  userId?: string;
  track: string;
  experience: string;
  language: string;
  goal?: string;
  weeklyHours?: number;
  focusAreas?: string[];
}) {
  const response = await fetch(`${API_BASE_URL}/api/learning-plans/generate-roadmap`, {
  method: "POST",
  headers: {
    "Content-Type": "application/json",
  Authorization: `Bearer ${localStorage.getItem("authToken") || ""}`,
  },
  body: JSON.stringify(payload),
});

  if (!response.ok) {
  const details = await response.text();
  throw new Error(`Failed to generate roadmap: ${response.status} ${details}`);
  }

  const data = await response.json();
  return normalizeLearningPlan(data);
}

export async function getActiveLearningPlan(userId: string) {
  const response = await fetch(
    `${API_BASE_URL}/api/learning-plans/active?userId=${encodeURIComponent(userId)}`,
    {
      headers: {
        Authorization: `Bearer ${localStorage.getItem("authToken") || ""}`,
      },
    }
  );

  if (response.status === 404) {
    return null;
  }

  if (!response.ok) {
    const details = await response.text();
    throw new Error(`Failed to load active learning plan: ${response.status} ${details}`);
  }

  const data = await response.json();
  return normalizeLearningPlan(data);
}

export type InterviewChatMessage = {
  role: "user" | "assistant";
  content: string;
};

export type InterviewAgentResult = {
  reply: string;
  feedback: string;
  score: number;
  isComplete: boolean;
  strengths: string[];
  improvements: string[];
};

export async function sendInterviewMessage(payload: {
  userId: string;
  sessionId: string;
  interviewType: string;
  track: string;
  experience: string;
  message: string;
  history: { role: string; content: string }[];
}) {
  console.log("INTERVIEW API PAYLOAD", payload);

  const response = await fetch(`${API_BASE_URL}/api/interviews/message`, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
    },
    body: JSON.stringify(payload),
  });

  if (!response.ok) {
    throw new Error(await response.text());
  }

  return response.json();
}

export async function getInterviewSessions(userId: string) {
  const response = await fetch(`${API_BASE_URL}/api/interviews/user/${userId}`);
  if (!response.ok) throw new Error(await response.text());
  return response.json();
}

export async function getInterviewStats(userId: string) {
  const response = await fetch(`${API_BASE_URL}/api/interviews/user/${userId}/stats`);
  if (!response.ok) throw new Error(await response.text());
  return response.json();
}

export async function getDashboard(userId: string) {
  const response = await fetch(
    `${API_BASE_URL}/api/dashboard/${userId}`
  );

  if (!response.ok) {
    throw new Error(await response.text());
  }

  return response.json();
}

export async function completeModuleAssessmentApi(payload: {
  userId: string;
  learningPlanId: string;
  moduleId: string;
  moduleTitle: string;
  score: number;
}) {
  const response = await fetch(`${API_BASE_URL}/api/learning-plans/module-assessment`, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
    },
    body: JSON.stringify(payload),
  });

  if (!response.ok) {
    throw new Error(await response.text());
  }

  return response.json();
}

