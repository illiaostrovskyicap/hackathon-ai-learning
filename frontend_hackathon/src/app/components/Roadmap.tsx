import { useApp } from "../context/AppContext";
import { useNavigate } from "react-router";
import { BookOpen, Clock, CheckCircle2, PlayCircle, Circle } from "lucide-react";

export function Roadmap() {
  const { learningPlan } = useApp();
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

  const totalHours = learningPlan.modules.reduce((sum, m) => sum + m.estimatedHours, 0);
  const completedModules = learningPlan.modules.filter((m) => m.status === "completed").length;
  const progressPercentage = (completedModules / learningPlan.modules.length) * 100;

  return (
    <div className="space-y-6">
      <div className="bg-gradient-to-r from-indigo-600 to-purple-600 rounded-lg shadow-lg p-8 text-white">
        <h1 className="text-3xl font-bold mb-2">{learningPlan.track} Learning Path</h1>
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
          {completedModules} of {learningPlan.modules.length} modules completed
        </div>
      </div>

      <div className="space-y-4">
        {learningPlan.modules.map((module, index) => {
          const isLocked = index > 0 && learningPlan.modules[index - 1].status !== "completed";

          return (
            <div
              key={module.id}
              className={`bg-white rounded-lg shadow p-6 ${
                isLocked ? "opacity-60" : "cursor-pointer hover:shadow-md"
              } transition-all`}
              onClick={() => !isLocked && navigate(`/module/${module.id}`)}
            >
              <div className="flex items-start space-x-4">
                <div className="flex-shrink-0 mt-1">
                  {module.status === "completed" ? (
                    <CheckCircle2 className="h-8 w-8 text-green-500" />
                  ) : module.status === "in-progress" ? (
                    <PlayCircle className="h-8 w-8 text-blue-500" />
                  ) : isLocked ? (
                    <Circle className="h-8 w-8 text-gray-300" />
                  ) : (
                    <Circle className="h-8 w-8 text-indigo-500" />
                  )}
                </div>

                <div className="flex-1 min-w-0">
                  <div className="flex items-center justify-between">
                    <h3 className="text-lg font-semibold text-gray-900">{module.title}</h3>
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

                  {isLocked && (
                    <div className="mt-3 text-sm text-gray-500 italic">
                      Complete previous module to unlock
                    </div>
                  )}
                </div>
              </div>
            </div>
          );
        })}
      </div>
    </div>
  );
}
