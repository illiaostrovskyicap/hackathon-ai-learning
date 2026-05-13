import { useApp } from "../context/AppContext";
import { useNavigate } from "react-router";
import { BookOpen, Clock, CheckCircle2, PlayCircle, Circle } from "lucide-react";

export function Roadmap() {
  const { learningPlan } = useApp();
  console.log("ROADMAP CONTEXT lp: ", learningPlan);
  console.log("ROADMAP localstorage lp: ", localStorage.getItem("learningPlan"));
  const navigate = useNavigate();

  if (!learningPlan) {
    return (
      <div className="text-center py-12">
        <BookOpen className="h-16 w-16 text-gray-400 mx-auto mb-4" />
        <h2 className="text-xl font-semibold text-gray-900">No Learning Plan Yet</h2>
        <p className="text-gray-600 mt-2">Complete onboarding to get started</p>
      </div>
    );
  }

  const modules = Array.isArray(learningPlan.modules) ? learningPlan.modules : [];

  if (modules.length === 0) {
    return (
      <div className="text-center py-12">
        <BookOpen className="h-16 w-16 text-gray-400 mx-auto mb-4" />
        <h2 className="text-xl font-semibold text-gray-900">Roadmap is empty</h2>
        <p className="text-gray-600 mt-2">
          Your learning plan exists, but it does not contain modules yet.
        </p>
      </div>
    );
  }

  const normalizedModules = modules.map((module, index) => ({
    ...module,
    id: module.id || `module-${index + 1}`,
    title: module.title || `Module ${index + 1}`,
    description: module.description || "AI-generated learning module",
    estimatedHours: module.estimatedHours ?? 8,
    status: module.status ?? "not-started",
    topics: Array.isArray(module.topics) ? module.topics : [],
  }));

  const totalHours = normalizedModules.reduce(
    (sum, module) => sum + module.estimatedHours,
    0
  );

  const completedModules = normalizedModules.filter(
    (module) => module.status === "completed"
  ).length;

  const progressPercentage =
    normalizedModules.length > 0
      ? (completedModules / normalizedModules.length) * 100
      : 0;

  return (
    <div className="space-y-6">
      <div className="bg-gradient-to-r from-indigo-600 to-purple-600 rounded-lg shadow-lg p-8 text-white">
        <h1 className="text-3xl font-bold mb-2">
          {learningPlan.track} Learning Path
        </h1>

        <p className="text-indigo-100 mb-4">
          {learningPlan.experience} level • {totalHours} total hours
        </p>

        <div className="bg-white/20 rounded-full h-3 mb-2">
          <div
            className="bg-white rounded-full h-3 transition-all"
            style={{ width: `${progressPercentage}%` }}
          />
        </div>

        <div className="text-sm text-indigo-100">
          {completedModules} of {normalizedModules.length} modules completed
        </div>
      </div>

      <div className="space-y-4">
        {normalizedModules.map((module) => {
          const status = module.status;

          return (
            <div
              key={module.id}
              className="bg-white rounded-lg shadow p-6 cursor-pointer hover:shadow-md transition-all"
              onClick={() => navigate(`/module/${module.id}`)}
            >
              <div className="flex items-start space-x-4">
                <div className="flex-shrink-0 mt-1">
                  {status === "completed" ? (
                    <CheckCircle2 className="h-8 w-8 text-green-500" />
                  ) : status === "in-progress" ? (
                    <PlayCircle className="h-8 w-8 text-blue-500" />
                  ) : (
                    <Circle className="h-8 w-8 text-indigo-500" />
                  )}
                </div>

                <div className="flex-1 min-w-0">
                  <div className="flex items-center justify-between gap-4">
                    <h3 className="text-lg font-semibold text-gray-900">
                      {module.title}
                    </h3>

                    <div className="flex items-center space-x-1 text-gray-600">
                      <Clock className="h-4 w-4" />
                      <span className="text-sm">{module.estimatedHours}h</span>
                    </div>
                  </div>

                  <p className="text-gray-600 mt-1">{module.description}</p>

                  <div className="flex flex-wrap gap-2 mt-3">
                    {module.topics.map((topic) => (
                      <span
                        key={topic}
                        className="px-2 py-1 bg-gray-100 text-gray-700 rounded text-xs"
                      >
                        {topic}
                      </span>
                    ))}
                  </div>
                </div>
              </div>
            </div>
          );
        })}
      </div>
    </div>
  );
}