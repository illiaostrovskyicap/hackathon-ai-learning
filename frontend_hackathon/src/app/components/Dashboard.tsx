import { useApp } from "../context/AppContext";
import {
  BarChart,
  Bar,
  LineChart,
  Line,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  ResponsiveContainer,
  Legend,
} from "recharts";
import { BookOpen, Clock, Award, TrendingUp, CheckCircle2, Target } from "lucide-react";

export function Dashboard() {
  const { learningPlan, analytics } = useApp();

  if (!learningPlan) {
    return (
      <div className="text-center py-12">
        <BookOpen className="h-16 w-16 text-gray-400 mx-auto mb-4" />
        <h2 className="text-xl font-semibold text-gray-900">No Learning Plan Yet</h2>
        <p className="text-gray-600 mt-2">Complete onboarding to get started</p>
      </div>
    );
  }

  const statCards = [
    {
      label: "Total Hours",
      value: analytics.totalHours,
      icon: <Clock className="h-6 w-6 text-blue-600" />,
      bgColor: "bg-blue-50",
    },
    {
      label: "Modules Completed",
      value: `${analytics.moduleCompletion.completed}/${analytics.moduleCompletion.total}`,
      icon: <CheckCircle2 className="h-6 w-6 text-green-600" />,
      bgColor: "bg-green-50",
    },
    {
      label: "Technologies Mastered",
      value: analytics.technologiesMastered.length,
      icon: <Award className="h-6 w-6 text-purple-600" />,
      bgColor: "bg-purple-50",
    },
    {
      label: "Assessment Pass Rate",
      value: analytics.assessmentBreakdown.passed + analytics.assessmentBreakdown.failed > 0
        ? `${Math.round((analytics.assessmentBreakdown.passed / (analytics.assessmentBreakdown.passed + analytics.assessmentBreakdown.failed)) * 100)}%`
        : "N/A",
      icon: <TrendingUp className="h-6 w-6 text-indigo-600" />,
      bgColor: "bg-indigo-50",
    },
  ];

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold text-gray-900">Dashboard</h1>
        <p className="text-gray-600 mt-1">
          Track your progress on the {learningPlan.track} learning path
        </p>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
        {statCards.map((card) => (
          <div key={card.label} className="bg-white rounded-lg shadow p-6">
            <div className="flex items-center justify-between mb-2">
              <div className={`p-2 rounded-lg ${card.bgColor}`}>{card.icon}</div>
            </div>
            <div className="text-2xl font-bold text-gray-900">{card.value}</div>
            <div className="text-sm text-gray-600">{card.label}</div>
          </div>
        ))}
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        <div className="bg-white rounded-lg shadow p-6">
          <h3 className="text-lg font-semibold text-gray-900 mb-4">Weekly Activity</h3>
          <ResponsiveContainer width="100%" height={250}>
            <BarChart data={analytics.weeklyActivity}>
              <CartesianGrid strokeDasharray="3 3" />
              <XAxis dataKey="week" />
              <YAxis />
              <Tooltip />
              <Bar dataKey="hours" fill="#4f46e5" />
            </BarChart>
          </ResponsiveContainer>
        </div>

        <div className="bg-white rounded-lg shadow p-6">
          <h3 className="text-lg font-semibold text-gray-900 mb-4">
            Interview Score Trend
          </h3>
          {analytics.interviewScoreTrend.length > 0 ? (
            <ResponsiveContainer width="100%" height={250}>
              <LineChart data={analytics.interviewScoreTrend}>
                <CartesianGrid strokeDasharray="3 3" />
                <XAxis dataKey="date" />
                <YAxis domain={[0, 100]} />
                <Tooltip />
                <Line
                  type="monotone"
                  dataKey="score"
                  stroke="#10b981"
                  strokeWidth={2}
                />
              </LineChart>
            </ResponsiveContainer>
          ) : (
            <div className="h-[250px] flex items-center justify-center text-gray-500">
              Complete interviews to see your score trend
            </div>
          )}
        </div>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        <div className="bg-white rounded-lg shadow p-6">
          <h3 className="text-lg font-semibold text-gray-900 mb-4">
            Planned vs Actual Hours
          </h3>
          {analytics.plannedVsActual.length > 0 ? (
            <ResponsiveContainer width="100%" height={250}>
              <BarChart data={analytics.plannedVsActual}>
                <CartesianGrid strokeDasharray="3 3" />
                <XAxis dataKey="module" />
                <YAxis />
                <Tooltip />
                <Legend />
                <Bar dataKey="planned" fill="#94a3b8" name="Planned" />
                <Bar dataKey="actual" fill="#4f46e5" name="Actual" />
              </BarChart>
            </ResponsiveContainer>
          ) : (
            <div className="h-[250px] flex items-center justify-center text-gray-500">
              Complete modules to see effort comparison
            </div>
          )}
        </div>

        <div className="bg-white rounded-lg shadow p-6">
          <h3 className="text-lg font-semibold text-gray-900 mb-4">
            Technologies Mastered
          </h3>
          {analytics.technologiesMastered.length > 0 ? (
            <div className="flex flex-wrap gap-2">
              {analytics.technologiesMastered.map((tech) => (
                <span
                  key={tech}
                  className="px-3 py-1 bg-indigo-100 text-indigo-800 rounded-full text-sm"
                >
                  {tech}
                </span>
              ))}
            </div>
          ) : (
            <div className="h-[250px] flex items-center justify-center text-gray-500">
              Complete modules to start mastering technologies
            </div>
          )}
        </div>
      </div>

      <div className="bg-white rounded-lg shadow p-6">
        <h3 className="text-lg font-semibold text-gray-900 mb-4">Recent Modules</h3>
        <div className="space-y-3">
          {learningPlan.modules.slice(0, 3).map((module) => (
            <div
              key={module.id}
              className="flex items-center justify-between p-4 border border-gray-200 rounded-lg"
            >
              <div className="flex-1">
                <div className="font-medium text-gray-900">{module.title}</div>
                <div className="text-sm text-gray-600">{module.estimatedHours} hours</div>
              </div>
              <StatusBadge status={module.status} />
            </div>
          ))}
        </div>
      </div>
    </div>
  );
}

function StatusBadge({ status }: { status: string }) {
  const styles = {
    "not-started": "bg-gray-100 text-gray-700",
    "in-progress": "bg-blue-100 text-blue-700",
    completed: "bg-green-100 text-green-700",
  };

  const labels = {
    "not-started": "Not Started",
    "in-progress": "In Progress",
    completed: "Completed",
  };

  return (
    <span className={`px-3 py-1 rounded-full text-xs font-medium ${styles[status as keyof typeof styles]}`}>
      {labels[status as keyof typeof labels]}
    </span>
  );
}
