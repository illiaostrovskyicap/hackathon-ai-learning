import { useState } from "react";
import { useParams, useNavigate } from "react-router";
import { useApp } from "../context/AppContext";
import { ArrowLeft, BookOpen, Video, FileText, CheckCircle2, PlayCircle } from "lucide-react";

export function ModuleDetail() {
  const { moduleId } = useParams();
  const { learningPlan, updateModuleStatus, submitAssessment } = useApp();
  const navigate = useNavigate();
  const [showAssessment, setShowAssessment] = useState(false);
  const [answers, setAnswers] = useState<number[]>([]);

  const module = learningPlan?.modules.find((m) => m.id === moduleId);

  if (!module) {
    return (
      <div className="text-center py-12">
        <p className="text-gray-600">Module not found</p>
      </div>
    );
  }

  const handleStartModule = () => {
    updateModuleStatus(module.id, "in-progress");
  };

  const handleStartAssessment = () => {
    setShowAssessment(true);
    setAnswers(new Array(module.assessment?.questions.length || 0).fill(-1));
  };

  const handleSubmitAssessment = () => {
    submitAssessment(module.id, answers);
    setShowAssessment(false);
    navigate("/roadmap");
  };

  const resourceIcons = {
    article: <FileText className="h-5 w-5" />,
    video: <Video className="h-5 w-5" />,
    exercise: <BookOpen className="h-5 w-5" />,
  };

  if (showAssessment && module.assessment) {
    return (
      <div className="max-w-3xl mx-auto">
        <button
          onClick={() => setShowAssessment(false)}
          className="flex items-center space-x-2 text-gray-600 hover:text-gray-900 mb-6"
        >
          <ArrowLeft className="h-4 w-4" />
          <span>Back to Module</span>
        </button>

        <div className="bg-white rounded-lg shadow p-8">
          <h2 className="text-2xl font-bold text-gray-900 mb-6">Assessment</h2>

          <div className="space-y-6">
            {module.assessment.questions.map((question, qIndex) => (
              <div key={question.id} className="border-b pb-6 last:border-b-0">
                <div className="font-medium text-gray-900 mb-3">
                  {qIndex + 1}. {question.text}
                </div>
                <div className="space-y-2">
                  {question.options.map((option, oIndex) => (
                    <label
                      key={oIndex}
                      className="flex items-center space-x-3 p-3 border border-gray-200 rounded-lg hover:bg-gray-50 cursor-pointer"
                    >
                      <input
                        type="radio"
                        name={`question-${qIndex}`}
                        checked={answers[qIndex] === oIndex}
                        onChange={() => {
                          const newAnswers = [...answers];
                          newAnswers[qIndex] = oIndex;
                          setAnswers(newAnswers);
                        }}
                        className="h-4 w-4 text-indigo-600"
                      />
                      <span className="text-gray-700">{option}</span>
                    </label>
                  ))}
                </div>
              </div>
            ))}
          </div>

          <button
            onClick={handleSubmitAssessment}
            disabled={answers.some((a) => a === -1)}
            className="mt-6 w-full py-3 bg-indigo-600 text-white rounded-lg hover:bg-indigo-700 disabled:opacity-50 disabled:cursor-not-allowed"
          >
            Submit Assessment
          </button>
        </div>
      </div>
    );
  }

  return (
    <div className="max-w-4xl mx-auto">
      <button
        onClick={() => navigate("/roadmap")}
        className="flex items-center space-x-2 text-gray-600 hover:text-gray-900 mb-6"
      >
        <ArrowLeft className="h-4 w-4" />
        <span>Back to Roadmap</span>
      </button>

      <div className="bg-white rounded-lg shadow p-8">
        <div className="flex items-start justify-between mb-6">
          <div>
            <h1 className="text-3xl font-bold text-gray-900 mb-2">{module.title}</h1>
            <p className="text-gray-600">{module.description}</p>
          </div>
          <StatusBadge status={module.status} />
        </div>

        <div className="flex items-center space-x-6 mb-8 pb-6 border-b">
          <div className="text-sm text-gray-600">
            <span className="font-medium">Estimated Time:</span> {module.estimatedHours} hours
          </div>
          <div className="flex flex-wrap gap-2">
            {module.topics.map((topic) => (
              <span
                key={topic}
                className="px-3 py-1 bg-indigo-100 text-indigo-800 rounded-full text-sm"
              >
                {topic}
              </span>
            ))}
          </div>
        </div>

        <div className="mb-8">
          <h2 className="text-xl font-semibold text-gray-900 mb-4">Learning Resources</h2>
          <div className="space-y-3">
            {module.resources.map((resource) => (
              <div
                key={resource.id}
                className="flex items-center space-x-3 p-4 border border-gray-200 rounded-lg hover:bg-gray-50"
              >
                <div className="text-indigo-600">{resourceIcons[resource.type]}</div>
                <div className="flex-1">
                  <div className="font-medium text-gray-900">{resource.title}</div>
                  <div className="text-sm text-gray-600 capitalize">{resource.type}</div>
                </div>
              </div>
            ))}
          </div>
        </div>

        {module.status === "not-started" && (
          <button
            onClick={handleStartModule}
            className="w-full flex items-center justify-center space-x-2 py-3 bg-indigo-600 text-white rounded-lg hover:bg-indigo-700"
          >
            <PlayCircle className="h-5 w-5" />
            <span>Start Module</span>
          </button>
        )}

        {module.status === "in-progress" && module.assessment && !module.assessment.score && (
          <button
            onClick={handleStartAssessment}
            className="w-full flex items-center justify-center space-x-2 py-3 bg-indigo-600 text-white rounded-lg hover:bg-indigo-700"
          >
            <CheckCircle2 className="h-5 w-5" />
            <span>Take Assessment</span>
          </button>
        )}

        {module.status === "completed" && module.assessment?.score !== undefined && (
          <div className="bg-green-50 border border-green-200 rounded-lg p-6">
            <div className="flex items-center space-x-3">
              <CheckCircle2 className="h-6 w-6 text-green-600" />
              <div>
                <div className="font-semibold text-green-900">Module Completed!</div>
                <div className="text-sm text-green-700">
                  Assessment Score: {Math.round(module.assessment.score)}%
                </div>
              </div>
            </div>
          </div>
        )}
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
    <span className={`px-4 py-2 rounded-full text-sm font-medium ${styles[status as keyof typeof styles]}`}>
      {labels[status as keyof typeof labels]}
    </span>
  );
}
