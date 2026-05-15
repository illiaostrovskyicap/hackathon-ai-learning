const API_BASE_URL =
  import.meta.env.VITE_API_BASE_URL || "http://localhost:5136";

function normalizeLearningPlan(data: any) {
  return {
    id: data.id ?? data.Id,
    userId: data.userId ?? data.UserId,
    track: data.track ?? data.Track,
    experience: data.experience ?? data.Experience,
    language: data.language ?? data.Language,
    generatedAt: data.generatedAt ?? data.GeneratedAt,
    onboardingSnapshot: {
      track: data.onboardingSnapshot?.track ?? data.OnboardingSnapshot?.Track ?? data.track ?? data.Track,
      experience:
        data.onboardingSnapshot?.experience ?? data.OnboardingSnapshot?.Experience ?? data.experience ?? data.Experience,
      language:
        data.onboardingSnapshot?.language ?? data.OnboardingSnapshot?.Language ?? data.language ?? data.Language,
      goal: data.onboardingSnapshot?.goal ?? data.OnboardingSnapshot?.Goal ?? "get-job-ready",
      weeklyHours:
        data.onboardingSnapshot?.weeklyHours ?? data.OnboardingSnapshot?.WeeklyHours ?? 6,
      focusAreas:
        data.onboardingSnapshot?.focusAreas ?? data.OnboardingSnapshot?.FocusAreas ?? [],
      createdAtUtc:
        data.onboardingSnapshot?.createdAtUtc ?? data.OnboardingSnapshot?.CreatedAtUtc ?? data.generatedAt ?? data.GeneratedAt,
    },
    skillMatrix: {
      skills: (data.skillMatrix?.skills ?? data.SkillMatrix?.Skills ?? []).map((s: any) => ({
        key: s.key ?? s.Key,
        name: s.name ?? s.Name,
        score: s.score ?? s.Score ?? 0,
        level: s.level ?? s.Level ?? "beginner",
        lastUpdatedAt: s.lastUpdatedAt ?? s.LastUpdatedAt,
      })),
      weakSkills: data.skillMatrix?.weakSkills ?? data.SkillMatrix?.WeakSkills ?? [],
    },
    adaptiveState: {
      updates: (data.adaptiveState?.updates ?? data.AdaptiveState?.Updates ?? []).map((u: any) => ({
        triggeredAt: u.triggeredAt ?? u.TriggeredAt,
        skillKey: u.skillKey ?? u.SkillKey,
        skillName: u.skillName ?? u.SkillName,
        previousLevel: u.previousLevel ?? u.PreviousLevel,
        newLevel: u.newLevel ?? u.NewLevel,
        targetModuleId: u.targetModuleId ?? u.TargetModuleId,
        addedSubModuleId: u.addedSubModuleId ?? u.AddedSubModuleId,
        summary: u.summary ?? u.Summary,
      })),
    },

    modules: (data.modules ?? data.Modules ?? []).map((m: any) => ({
      id: m.id ?? m.Id,
      title: m.title ?? m.Title,
      description: m.description ?? m.Description,
      status: m.status ?? m.Status ?? "not-started",
      estimatedHours: m.estimatedHours ?? m.EstimatedHours ?? 8,
      startedAt: m.startedAt ?? m.StartedAt,
      completedAt: m.completedAt ?? m.CompletedAt,
      lastOpenedAt: m.lastOpenedAt ?? m.LastOpenedAt,
      topics: m.topics ?? m.Topics ?? [],
      skills: (m.skills ?? m.Skills ?? []).map((s: any) => ({
        key: s.key ?? s.Key,
        name: s.name ?? s.Name,
        weight: s.weight ?? s.Weight ?? 0,
      })),
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
        status: sm.status ?? sm.Status ?? "not-started",
        startedAt: sm.startedAt ?? sm.StartedAt,
        completedAt: sm.completedAt ?? sm.CompletedAt,
        lastOpenedAt: sm.lastOpenedAt ?? sm.LastOpenedAt,
        topics: sm.topics ?? sm.Topics ?? [],
        skills: (sm.skills ?? sm.Skills ?? []).map((s: any) => ({
          key: s.key ?? s.Key,
          name: s.name ?? s.Name,
          weight: s.weight ?? s.Weight ?? 0,
        })),
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

export async function updateLearningProgress(
  learningPlanId: string,
  payload: {
    userId: string;
    itemType: "module" | "submodule";
    itemId: string;
    action: "open" | "complete";
    minutesSpent?: number;
    assessmentScore?: number;
    reflection?: string;
    resourceTitle?: string;
  }
) {
  const response = await fetch(`${API_BASE_URL}/api/learning-plans/${encodeURIComponent(learningPlanId)}/progress`, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
      Authorization: `Bearer ${localStorage.getItem("authToken") || ""}`,
    },
    body: JSON.stringify({
      ...payload,
      occurredAtUtc: new Date().toISOString(),
    }),
  });

  if (!response.ok) {
    const details = await response.text();
    throw new Error(`Failed to update progress: ${response.status} ${details}`);
  }

  const data = await response.json();
  return {
    learningPlan: normalizeLearningPlan(data.learningPlan ?? data.LearningPlan),
    skillMatrix: {
      skills: (data.skillMatrix?.skills ?? data.SkillMatrix?.Skills ?? []).map((s: any) => ({
        key: s.key ?? s.Key,
        name: s.name ?? s.Name,
        score: s.score ?? s.Score ?? 0,
        level: s.level ?? s.Level ?? "beginner",
        lastUpdatedAt: s.lastUpdatedAt ?? s.LastUpdatedAt,
      })),
      weakSkills: data.skillMatrix?.weakSkills ?? data.SkillMatrix?.WeakSkills ?? [],
    },
    promotedSkills: (data.promotedSkills ?? data.PromotedSkills ?? []).map((s: any) => ({
      key: s.key ?? s.Key,
      name: s.name ?? s.Name,
      previousLevel: s.previousLevel ?? s.PreviousLevel,
      newLevel: s.newLevel ?? s.NewLevel,
      score: s.score ?? s.Score ?? 0,
    })),
    pathwayUpdated: data.pathwayUpdated ?? data.PathwayUpdated ?? false,
  };
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

