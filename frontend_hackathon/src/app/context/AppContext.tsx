import { createContext, useContext, useState, useEffect, ReactNode } from "react";
import { login } from "../api";
import { generateLearningPlan, getActiveLearningPlan, sendInterviewMessage as sendInterviewMessageApi } from "../api";

export interface User {
  id: string;
  name: string;
  email: string;
  avatar?: string;
  hasCompletedOnboarding: boolean;
  role: "user" | "editor" | "admin";
  createdAt: string;
  preferences: {
    notifications: boolean;
    emailUpdates: boolean;
  };
}

export interface LearningPlan {
  id: string;
  track: string;
  experience: string;
  language: string;
  generatedAt: string;
  modules: Module[];
}

export interface Module {
  id: string;
  title: string;
  description: string;
  estimatedHours: number;
  status: "not-started" | "in-progress" | "completed";
  topics: string[];
  resources: Resource[];
  assessment?: Assessment;
}

export interface Resource {
  id: string;
  title: string;
  type: "article" | "video" | "exercise";
  url: string;
}

export interface Assessment {
  id: string;
  questions: Question[];
  score?: number;
  completedAt?: string;
}

export interface Question {
  id: string;
  text: string;
  options: string[];
  correctAnswer: number;
  userAnswer?: number;
}

export interface AnalyticsData {
  totalHours: number;
  weeklyActivity: { week: string; hours: number }[];
  technologiesMastered: string[];
  assessmentBreakdown: { passed: number; failed: number };
  moduleCompletion: { completed: number; total: number };
  interviewScoreTrend: { date: string; score: number }[];
  plannedVsActual: { module: string; planned: number; actual: number }[];
}

export interface Subscription {
  id: string;
  tier: "free" | "pro" | "enterprise";
  status: "active" | "cancelled" | "expired";
  startDate: string;
  endDate?: string;
  features: string[];
}

export interface InterviewSession {
  id: string;
  date: string;
  type: "technical" | "coding" | "behavioral";
  status: "in-progress" | "completed";
  messages: InterviewMessage[];
  report?: InterviewReport;
}

export interface InterviewMessage {
  id: string;
  role: "assistant" | "user";
  content: string;
  timestamp: string;
}

export interface InterviewReport {
  overallScore: number;
  strengths: string[];
  improvements: string[];
  questionScores: { question: string; score: number; feedback: string }[];
}

export interface ContentItem {
  id: string;
  title: string;
  type: "article" | "video" | "exercise";
  url: string;
  topics: string[];
  difficulty: "beginner" | "intermediate" | "advanced";
  createdAt: string;
  updatedAt: string;
  createdBy: string;
}

interface AppContextType {
  user: User | null;
  signIn: (email: string, password: string) => Promise<void>;
  signOut: () => void;
  updateProfile: (updates: Partial<User>) => void;
  deleteAccount: () => void;
  completeOnboarding: (
  track: string,
  experience: string,
  language: string,
  goal: string,
  weeklyHours: number,
  focusAreas: string[]
) => Promise<void>;
  learningPlan: LearningPlan | null;
  updateModuleStatus: (moduleId: string, status: Module["status"]) => void;
  submitAssessment: (moduleId: string, answers: number[]) => void;
  analytics: AnalyticsData;
  subscription: Subscription;
  upgradeSubscription: (tier: "pro" | "enterprise") => void;
  cancelSubscription: () => void;
  interviewSessions: InterviewSession[];
  startInterview: (type: InterviewSession["type"]) => string;
  sendInterviewMessage: (sessionId: string, message: string) => void;
  completeInterview: (sessionId: string) => void;
  contentLibrary: ContentItem[];
  addContent: (content: Omit<ContentItem, "id" | "createdAt" | "updatedAt">) => void;
  updateContent: (id: string, updates: Partial<ContentItem>) => void;
  deleteContent: (id: string) => void;
  completeModuleAssessment: (moduleId: string) => void;
}

const AppContext = createContext<AppContextType | undefined>(undefined);

export function AppProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<User | null>(null);
  const [learningPlan, setLearningPlan] = useState<LearningPlan | null>(null);
  const [subscription, setSubscription] = useState<Subscription>({
    id: "sub-free",
    tier: "free",
    status: "active",
    startDate: new Date().toISOString(),
    features: ["Basic roadmap", "3 modules per month", "Community support"],
  });
  const [interviewSessions, setInterviewSessions] = useState<InterviewSession[]>([]);
  const [contentLibrary, setContentLibrary] = useState<ContentItem[]>([]);
  const [analytics, setAnalytics] = useState<AnalyticsData>({
    totalHours: 0,
    weeklyActivity: [],
    technologiesMastered: [],
    assessmentBreakdown: { passed: 0, failed: 0 },
    moduleCompletion: { completed: 0, total: 0 },
    interviewScoreTrend: [],
    plannedVsActual: [],
  });

  useEffect(() => {
    const savedUser = localStorage.getItem("user");
    const savedPlan = localStorage.getItem("learningPlan");
    const savedSubscription = localStorage.getItem("subscription");
    const savedInterviews = localStorage.getItem("interviewSessions");
    const savedContent = localStorage.getItem("contentLibrary");

    if (savedUser) setUser(JSON.parse(savedUser));
    if (savedPlan) setLearningPlan(JSON.parse(savedPlan));
    if (savedSubscription) setSubscription(JSON.parse(savedSubscription));
    if (savedInterviews) setInterviewSessions(JSON.parse(savedInterviews));
    if (savedContent) setContentLibrary(JSON.parse(savedContent));
  }, []);

  const signIn = async (email: string, password: string): Promise<User> => {
    const result = await login(email, password);

    let signedInUser: User = {
      ...result.user,
      preferences: result.user.preferences ?? {
        notifications: true,
        emailUpdates: true,
      },
    };

    localStorage.setItem("authToken", result.token);

    try {
      const activePlan = await getActiveLearningPlan(signedInUser.id);

      if (activePlan) {
        signedInUser = {
          ...signedInUser,
          hasCompletedOnboarding: true,
        };

        setLearningPlan(activePlan);
        localStorage.setItem("learningPlan", JSON.stringify(activePlan));
      }
    } catch {
      // no saved plan
    }

    setUser(signedInUser);
    localStorage.setItem("user", JSON.stringify(signedInUser));

    return signedInUser;
  };
  const updateProfile = (updates: Partial<User>) => {
    if (!user) return;
    const updatedUser = { ...user, ...updates };
    setUser(updatedUser);
    localStorage.setItem("user", JSON.stringify(updatedUser));
  };

  const deleteAccount = () => {
    setUser(null);
    setLearningPlan(null);
    setSubscription({
      id: "sub-free",
      tier: "free",
      status: "active",
      startDate: new Date().toISOString(),
      features: ["Basic roadmap", "3 modules per month", "Community support"],
    });
    localStorage.clear();
  };

  const signOut = () => {
    setUser(null);
    setLearningPlan(null);
    setInterviewSessions([]);
    localStorage.removeItem("user");
    localStorage.removeItem("learningPlan");
    localStorage.removeItem("interviewSessions");
  };

  const completeOnboarding = async (
  track: string,
  experience: string,
  language: string,
  goal: string,
  weeklyHours: number,
  focusAreas: string[]
): Promise<void> => {
    if (!user) return;

    const plan = await generateLearningPlan({
      userId: user.id,
      track,
      experience,
      language,
      goal,
      weeklyHours,
      focusAreas,
    });

    const updatedUser = { ...user, hasCompletedOnboarding: true };

    setUser(updatedUser);
    setLearningPlan(plan);

    localStorage.setItem("user", JSON.stringify(updatedUser));
    localStorage.setItem("learningPlan", JSON.stringify(plan));

    updateAnalytics(plan);
  };


  const updateModuleStatus = (moduleId: string, status: Module["status"]) => {
    if (!learningPlan) return;

    const updatedModules = learningPlan.modules.map((m) =>
      m.id === moduleId ? { ...m, status } : m
    );

    const updatedPlan = { ...learningPlan, modules: updatedModules };
    setLearningPlan(updatedPlan);
    localStorage.setItem("learningPlan", JSON.stringify(updatedPlan));
    updateAnalytics(updatedPlan);
  };

  const submitAssessment = (moduleId: string, answers: number[]) => {
    if (!learningPlan) return;

    const updatedModules = learningPlan.modules.map((m) => {
      if (m.id === moduleId && m.assessment) {
        const questionsWithAnswers = m.assessment.questions.map((q, idx) => ({
          ...q,
          userAnswer: answers[idx],
        }));

        const correctCount = questionsWithAnswers.filter(
          (q) => q.userAnswer === q.correctAnswer
        ).length;

        const score = (correctCount / questionsWithAnswers.length) * 100;

        return {
          ...m,
          status: "completed" as const,
          assessment: {
            ...m.assessment,
            questions: questionsWithAnswers,
            score,
            completedAt: new Date().toISOString(),
          },
        };
      }
      return m;
    });

    const updatedPlan = { ...learningPlan, modules: updatedModules };
    setLearningPlan(updatedPlan);
    localStorage.setItem("learningPlan", JSON.stringify(updatedPlan));
    updateAnalytics(updatedPlan);
  };

  const completeModuleAssessment = (moduleId: string) => {
    if (!learningPlan) return;

    const updatedPlan = {
      ...learningPlan,
      modules: learningPlan.modules.map((module) =>
        module.id === moduleId
          ? {
            ...module,
            status: "completed" as const,
            assessment: {
              ...(module.assessment ?? {}),
              score: 100,
            },
          }
          : module
      ),
    };

    setLearningPlan(updatedPlan);
    localStorage.setItem("learningPlan", JSON.stringify(updatedPlan));
  };

  const upgradeSubscription = (tier: "pro" | "enterprise") => {
    const features = tier === "pro"
      ? ["Unlimited modules", "AI Interview Assistant", "Priority support", "Advanced analytics"]
      : ["Everything in Pro", "Content management", "Team collaboration", "Dedicated support", "Custom integrations"];

    const newSub: Subscription = {
      id: "sub-" + Date.now(),
      tier,
      status: "active",
      startDate: new Date().toISOString(),
      features,
    };
    setSubscription(newSub);
    localStorage.setItem("subscription", JSON.stringify(newSub));
  };

  const cancelSubscription = () => {
    const cancelledSub = {
      ...subscription,
      status: "cancelled" as const,
      endDate: new Date(Date.now() + 30 * 24 * 60 * 60 * 1000).toISOString(),
    };
    setSubscription(cancelledSub);
    localStorage.setItem("subscription", JSON.stringify(cancelledSub));
  };

  const startInterview = (type: InterviewSession["type"]) => {
    const sessionId = "interview-" + Date.now();
    const newSession: InterviewSession = {
      id: sessionId,
      date: new Date().toISOString(),
      type,
      status: "in-progress",
      messages: [
        {
          id: "msg-1",
          role: "assistant",
          content: getInterviewGreeting(type),
          timestamp: new Date().toISOString(),
        },
      ],
    };

    const updated = [...interviewSessions, newSession];
    setInterviewSessions(updated);
    localStorage.setItem("interviewSessions", JSON.stringify(updated));
    return sessionId;
  };

  const sendInterviewMessage = async (sessionId: string, content: string) => {
    const session = interviewSessions.find((s) => s.id === sessionId);
    if (!session || !user) return;

    const userMessage = {
      id: crypto.randomUUID(),
      role: "user" as const,
      content,
      timestamp: new Date().toISOString(),
    };

    const updatedMessages = [...session.messages, userMessage];

    setInterviewSessions((prev) =>
      prev.map((s) =>
        s.id === sessionId
          ? { ...s, messages: updatedMessages }
          : s
      )
    );

    const result = await sendInterviewMessageApi({
      userId: user.id,
      sessionId,
      interviewType: session.type,
      track: user.careerTrack ?? "software-development",
      experience: user.experienceLevel ?? "beginner",
      message: content,
      history: updatedMessages.map((m) => ({
        role: m.role,
        content: m.content,
      })),
    });

    const assistantMessage = {
      id: crypto.randomUUID(),
      role: "assistant" as const,
      content: result.reply,
      timestamp: new Date().toISOString(),
    };

    setInterviewSessions((prev) =>
      prev.map((s) =>
        s.id === sessionId
          ? {
            ...s,
            status: result.isComplete ? "completed" : s.status,
            messages: [...updatedMessages, assistantMessage],
            report: result.isComplete
              ? {
                overallScore: result.score,
                strengths: result.strengths ?? [],
                improvements: result.improvements ?? [],
                questionScores: [],
              }
              : s.report,
          }
          : s
      )
    );
  };

  const completeInterview = (sessionId: string) => {
    const session = interviewSessions.find((s) => s.id === sessionId);
    if (!session) return;

    const report: InterviewReport = {
      overallScore: 75 + Math.floor(Math.random() * 20),
      strengths: [
        "Clear communication",
        "Good problem-solving approach",
        "Strong technical knowledge",
      ],
      improvements: [
        "Consider edge cases more thoroughly",
        "Practice time complexity analysis",
      ],
      questionScores: [
        { question: "Technical fundamentals", score: 85, feedback: "Excellent understanding" },
        { question: "Problem-solving", score: 78, feedback: "Good approach, minor improvements needed" },
        { question: "Communication", score: 90, feedback: "Very clear and concise" },
      ],
    };

    const completedSession = {
      ...session,
      status: "completed" as const,
      report,
    };

    const updated = interviewSessions.map((s) =>
      s.id === sessionId ? completedSession : s
    );
    setInterviewSessions(updated);
    localStorage.setItem("interviewSessions", JSON.stringify(updated));

    updateAnalyticsWithInterview(report.overallScore);
  };

  const addContent = (content: Omit<ContentItem, "id" | "createdAt" | "updatedAt">) => {
    const newContent: ContentItem = {
      ...content,
      id: "content-" + Date.now(),
      createdAt: new Date().toISOString(),
      updatedAt: new Date().toISOString(),
    };

    const updated = [...contentLibrary, newContent];
    setContentLibrary(updated);
    localStorage.setItem("contentLibrary", JSON.stringify(updated));
  };

  const updateContent = (id: string, updates: Partial<ContentItem>) => {
    const updated = contentLibrary.map((c) =>
      c.id === id ? { ...c, ...updates, updatedAt: new Date().toISOString() } : c
    );
    setContentLibrary(updated);
    localStorage.setItem("contentLibrary", JSON.stringify(updated));
  };

  const deleteContent = (id: string) => {
    const updated = contentLibrary.filter((c) => c.id !== id);
    setContentLibrary(updated);
    localStorage.setItem("contentLibrary", JSON.stringify(updated));
  };

  const updateAnalytics = (plan: LearningPlan) => {
    const completed = plan.modules.filter((m) => m.status === "completed");
    const totalHours = completed.reduce((sum, m) => sum + m.estimatedHours, 0);

    const technologiesMastered = [
      ...new Set(completed.flatMap((m) => m.topics)),
    ];

    const assessments = plan.modules
      .filter((m) => m.assessment?.score !== undefined)
      .map((m) => m.assessment!);

    const passed = assessments.filter((a) => a.score! >= 70).length;
    const failed = assessments.length - passed;

    const weeklyActivity = [
      { week: "Week 1", hours: 8 },
      { week: "Week 2", hours: 12 },
      { week: "Week 3", hours: totalHours > 20 ? 15 : 10 },
      { week: "Week 4", hours: totalHours > 30 ? 18 : 8 },
    ];

    const plannedVsActual = plan.modules.slice(0, 3).map((m) => ({
      module: m.title.substring(0, 20),
      planned: m.estimatedHours,
      actual: m.status === "completed" ? m.estimatedHours + Math.floor(Math.random() * 5) - 2 : 0,
    }));

    setAnalytics({
      totalHours,
      weeklyActivity,
      technologiesMastered,
      assessmentBreakdown: { passed, failed },
      moduleCompletion: { completed: completed.length, total: plan.modules.length },
      interviewScoreTrend: analytics.interviewScoreTrend,
      plannedVsActual,
    });
  };

  const updateAnalyticsWithInterview = (score: number) => {
    setAnalytics({
      ...analytics,
      interviewScoreTrend: [
        ...analytics.interviewScoreTrend,
        { date: new Date().toLocaleDateString(), score },
      ].slice(-5),
    });
  };

  return (
    <AppContext.Provider
      value={{
        user,
        signIn,
        signOut,
        updateProfile,
        deleteAccount,
        completeOnboarding,
        learningPlan,
        updateModuleStatus,
        submitAssessment,
        analytics,
        subscription,
        upgradeSubscription,
        cancelSubscription,
        interviewSessions,
        startInterview,
        sendInterviewMessage,
        completeInterview,
        contentLibrary,
        addContent,
        updateContent,
        deleteContent,
        completeModuleAssessment
      }}
    >
      {children}
    </AppContext.Provider>
  );
}

export function useApp() {
  const context = useContext(AppContext);
  if (!context) throw new Error("useApp must be used within AppProvider");
  return context;
}

function getInterviewGreeting(type: string): string {
  const greetings = {
    technical: "Hello! I'm your AI interview assistant. Today we'll conduct a technical interview covering fundamental concepts and problem-solving. Let's start with a warm-up question: Can you explain what happens when you type a URL into a browser and press enter?",
    coding: "Welcome to your coding interview! I'll present you with programming challenges to assess your algorithmic thinking. Let's begin: Write a function to find the two numbers in an array that sum to a target value. How would you approach this?",
    behavioral: "Hi there! In this behavioral interview, we'll discuss your experiences and soft skills. Let's start: Tell me about a time when you faced a significant challenge in a project. How did you handle it?",
  };
  return greetings[type as keyof typeof greetings] || greetings.technical;
}

function generateInterviewResponse(type: string, userMessage: string, messageCount: number): string {
  const responses = [
    "That's a good point. Let me follow up: How would you handle error cases in this scenario?",
    "Interesting approach! Can you explain the time and space complexity of your solution?",
    "I appreciate your thorough answer. Now, let's dive deeper into another aspect...",
    "Good explanation. How would this scale with larger datasets?",
    "That demonstrates solid understanding. Let's move to the next question: What strategies do you use for debugging complex issues?",
  ];

  if (messageCount >= 8) {
    return "Thank you for your responses! That concludes our interview. I'll now generate your performance report with detailed feedback.";
  }

  return responses[Math.min(messageCount / 2, responses.length - 1)];
}

function generateInitialContent(): ContentItem[] {
  return [
    {
      id: "content-1",
      title: "Introduction to React Hooks",
      type: "article",
      url: "https://react.dev/hooks",
      topics: ["React", "JavaScript", "Frontend"],
      difficulty: "beginner",
      createdAt: new Date(Date.now() - 7 * 24 * 60 * 60 * 1000).toISOString(),
      updatedAt: new Date(Date.now() - 7 * 24 * 60 * 60 * 1000).toISOString(),
      createdBy: "editor-1",
    },
    {
      id: "content-2",
      title: "Node.js Performance Optimization",
      type: "video",
      url: "https://youtube.com/watch?v=example",
      topics: ["Node.js", "Backend", "Performance"],
      difficulty: "advanced",
      createdAt: new Date(Date.now() - 5 * 24 * 60 * 60 * 1000).toISOString(),
      updatedAt: new Date(Date.now() - 5 * 24 * 60 * 60 * 1000).toISOString(),
      createdBy: "editor-1",
    },
  ];
}

function generateModules(track: string, experience: string): Module[] {
  const modules: Module[] = [
    {
      id: "mod-1",
      title: `${track} Fundamentals`,
      description: `Core concepts and foundations of ${track.toLowerCase()} development`,
      estimatedHours: 12,
      status: "not-started",
      topics: ["Basics", "Architecture", "Best Practices"],
      resources: [
        {
          id: "res-1",
          title: `Introduction to ${track}`,
          type: "article",
          url: "#",
        },
        {
          id: "res-2",
          title: `${track} Tutorial Series`,
          type: "video",
          url: "#",
        },
      ],
      assessment: {
        id: "assess-1",
        questions: [
          {
            id: "q1",
            text: `What is the primary purpose of ${track.toLowerCase()}?`,
            options: [
              "Building user interfaces",
              "Database management",
              "Network security",
              "All of the above",
            ],
            correctAnswer: 0,
          },
          {
            id: "q2",
            text: "Which principle is most important for code quality?",
            options: [
              "Speed over readability",
              "Clarity and maintainability",
              "Using advanced features",
              "Minimal documentation",
            ],
            correctAnswer: 1,
          },
        ],
      },
    },
    {
      id: "mod-2",
      title: `Advanced ${track} Patterns`,
      description: "Design patterns and advanced techniques",
      estimatedHours: 18,
      status: "not-started",
      topics: ["Design Patterns", "Performance", "Scalability"],
      resources: [
        {
          id: "res-3",
          title: "Design Patterns Guide",
          type: "article",
          url: "#",
        },
        {
          id: "res-4",
          title: "Hands-on Exercises",
          type: "exercise",
          url: "#",
        },
      ],
      assessment: {
        id: "assess-2",
        questions: [
          {
            id: "q3",
            text: "What is the main benefit of design patterns?",
            options: [
              "Faster compilation",
              "Reusable solutions to common problems",
              "Smaller file sizes",
              "Better syntax highlighting",
            ],
            correctAnswer: 1,
          },
        ],
      },
    },
    {
      id: "mod-3",
      title: `${track} in Production`,
      description: "Deployment, monitoring, and maintenance",
      estimatedHours: 15,
      status: "not-started",
      topics: ["Deployment", "Monitoring", "CI/CD"],
      resources: [
        {
          id: "res-5",
          title: "Production Best Practices",
          type: "article",
          url: "#",
        },
      ],
    },
  ];

  return modules;
}
