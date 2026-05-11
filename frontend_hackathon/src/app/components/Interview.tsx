import { useApp } from "../context/AppContext";
import { useNavigate } from "react-router";
import { MessageSquare, Code, Users, Clock, Award, TrendingUp } from "lucide-react";

const INTERVIEW_TYPES = [
  {
    id: "technical" as const,
    name: "Technical Interview",
    description: "Test your understanding of core technical concepts and system design",
    icon: <MessageSquare className="h-8 w-8 text-blue-600" />,
    duration: "30-45 min",
    topics: ["Data structures", "Algorithms", "System design", "Best practices"],
  },
  {
    id: "coding" as const,
    name: "Coding Challenge",
    description: "Solve algorithmic problems and demonstrate your coding skills",
    icon: <Code className="h-8 w-8 text-green-600" />,
    duration: "45-60 min",
    topics: ["Problem solving", "Algorithm optimization", "Code quality", "Testing"],
  },
  {
    id: "behavioral" as const,
    name: "Behavioral Interview",
    description: "Showcase your soft skills and professional experiences",
    icon: <Users className="h-8 w-8 text-purple-600" />,
    duration: "30 min",
    topics: ["Leadership", "Teamwork", "Conflict resolution", "Communication"],
  },
];

export function Interview() {
  const { subscription, interviewSessions, startInterview } = useApp();
  const navigate = useNavigate();

  const canAccessInterviews = subscription.tier === "pro" || subscription.tier === "enterprise";

  const handleStartInterview = (type: "technical" | "coding" | "behavioral") => {
    if (!canAccessInterviews) {
      navigate("/subscription");
      return;
    }
    const sessionId = startInterview(type);
    navigate(`/interview/${sessionId}`);
  };

  const completedInterviews = interviewSessions.filter((s) => s.status === "completed");
  const averageScore = completedInterviews.length > 0
    ? completedInterviews.reduce((sum, s) => sum + (s.report?.overallScore || 0), 0) /
      completedInterviews.length
    : 0;

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold text-gray-900">AI Interview Assistant</h1>
        <p className="text-gray-600 mt-1">
          Practice interviews with AI-powered feedback and coaching
        </p>
      </div>

      {!canAccessInterviews && (
        <div className="bg-indigo-50 border border-indigo-200 rounded-lg p-6">
          <div className="flex items-start space-x-3">
            <Award className="h-6 w-6 text-indigo-600 mt-0.5" />
            <div className="flex-1">
              <div className="font-semibold text-indigo-900">
                Upgrade to Access Interview Assistant
              </div>
              <div className="text-sm text-indigo-700 mt-1">
                This feature is available on Pro and Enterprise plans. Upgrade now to
                start practicing interviews with AI guidance.
              </div>
              <button
                onClick={() => navigate("/subscription")}
                className="mt-3 px-4 py-2 bg-indigo-600 text-white rounded-lg hover:bg-indigo-700"
              >
                View Plans
              </button>
            </div>
          </div>
        </div>
      )}

      <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
        <div className="bg-white rounded-lg shadow p-6">
          <div className="flex items-center justify-between mb-2">
            <Clock className="h-8 w-8 text-blue-600" />
          </div>
          <div className="text-2xl font-bold text-gray-900">
            {interviewSessions.length}
          </div>
          <div className="text-sm text-gray-600">Total Sessions</div>
        </div>

        <div className="bg-white rounded-lg shadow p-6">
          <div className="flex items-center justify-between mb-2">
            <Award className="h-8 w-8 text-green-600" />
          </div>
          <div className="text-2xl font-bold text-gray-900">
            {completedInterviews.length}
          </div>
          <div className="text-sm text-gray-600">Completed</div>
        </div>

        <div className="bg-white rounded-lg shadow p-6">
          <div className="flex items-center justify-between mb-2">
            <TrendingUp className="h-8 w-8 text-purple-600" />
          </div>
          <div className="text-2xl font-bold text-gray-900">
            {averageScore > 0 ? Math.round(averageScore) : "-"}
          </div>
          <div className="text-sm text-gray-600">Average Score</div>
        </div>
      </div>

      <div>
        <h2 className="text-xl font-semibold text-gray-900 mb-4">Start New Interview</h2>
        <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
          {INTERVIEW_TYPES.map((type) => (
            <div
              key={type.id}
              className="bg-white rounded-lg shadow-lg p-6 hover:shadow-xl transition-shadow"
            >
              <div className="flex justify-center mb-4">{type.icon}</div>
              <h3 className="text-xl font-semibold text-gray-900 text-center mb-2">
                {type.name}
              </h3>
              <p className="text-gray-600 text-sm text-center mb-4">
                {type.description}
              </p>
              <div className="flex items-center justify-center space-x-1 text-sm text-gray-500 mb-4">
                <Clock className="h-4 w-4" />
                <span>{type.duration}</span>
              </div>
              <div className="mb-4">
                <div className="text-xs font-medium text-gray-700 mb-2">Topics:</div>
                <div className="flex flex-wrap gap-1">
                  {type.topics.map((topic) => (
                    <span
                      key={topic}
                      className="px-2 py-1 bg-gray-100 text-gray-700 rounded text-xs"
                    >
                      {topic}
                    </span>
                  ))}
                </div>
              </div>
              <button
                onClick={() => handleStartInterview(type.id)}
                disabled={!canAccessInterviews}
                className="w-full py-2 bg-indigo-600 text-white rounded-lg hover:bg-indigo-700 disabled:bg-gray-300 disabled:cursor-not-allowed"
              >
                {canAccessInterviews ? "Start Interview" : "Upgrade Required"}
              </button>
            </div>
          ))}
        </div>
      </div>

      {completedInterviews.length > 0 && (
        <div>
          <h2 className="text-xl font-semibold text-gray-900 mb-4">
            Recent Interview Sessions
          </h2>
          <div className="space-y-3">
            {completedInterviews.slice(-5).reverse().map((session) => (
              <div
                key={session.id}
                className="bg-white rounded-lg shadow p-4 hover:shadow-md transition-shadow cursor-pointer"
                onClick={() => navigate(`/interview/${session.id}`)}
              >
                <div className="flex items-center justify-between">
                  <div className="flex-1">
                    <div className="font-medium text-gray-900 capitalize">
                      {session.type} Interview
                    </div>
                    <div className="text-sm text-gray-600">
                      {new Date(session.date).toLocaleDateString()} at{" "}
                      {new Date(session.date).toLocaleTimeString()}
                    </div>
                  </div>
                  <div className="text-right">
                    <div className="text-2xl font-bold text-indigo-600">
                      {session.report?.overallScore || 0}
                    </div>
                    <div className="text-xs text-gray-600">Score</div>
                  </div>
                </div>
              </div>
            ))}
          </div>
        </div>
      )}
    </div>
  );
}
