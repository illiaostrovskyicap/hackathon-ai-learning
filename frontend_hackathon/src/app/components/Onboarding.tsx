import { useState } from "react";
import { useNavigate } from "react-router";
import { useApp } from "../context/AppContext";
import { ArrowRight, ArrowLeft, Loader2 } from "lucide-react";

const CAREER_TRACKS = [
  { id: "frontend", name: "Frontend Development", icon: "🎨" },
  { id: "backend", name: "Backend Development", icon: "⚙️" },
  { id: "fullstack", name: "Full Stack Development", icon: "🚀" },
  { id: "mobile", name: "Mobile Development", icon: "📱" },
  { id: "devops", name: "DevOps Engineering", icon: "🔧" },
  { id: "data", name: "Data Science", icon: "📊" },
];

const EXPERIENCE_LEVELS = [
  { id: "beginner", name: "Beginner", description: "Just starting out" },
  { id: "intermediate", name: "Intermediate", description: "Some experience" },
  { id: "advanced", name: "Advanced", description: "Looking to specialize" },
];

const LANGUAGES = [
  { id: "en", name: "English" },
  { id: "es", name: "Spanish" },
  { id: "fr", name: "French" },
  { id: "de", name: "German" },
];

export function Onboarding() {
  const [step, setStep] = useState(1);
  const [track, setTrack] = useState("");
  const [experience, setExperience] = useState("");
  const [language, setLanguage] = useState("en");
  const [generating, setGenerating] = useState(false);
  const { completeOnboarding } = useApp();
  const navigate = useNavigate();
  const [goal, setGoal] = useState("get-job-ready");
  const [weeklyHours, setWeeklyHours] = useState(6);
  const [focusAreas, setFocusAreas] = useState<string[]>([]);

  const handleComplete = async () => {
    setGenerating(true);

    try {
      await completeOnboarding(track, experience, language, goal, weeklyHours, focusAreas);
      navigate("/roadmap");
    } catch (error) {
      console.error(error);
      alert("Failed to generate roadmap. Check backend/agents connection.");
    } finally {
      setGenerating(false);
    }
  };

  return (
    <div className="min-h-screen flex items-center justify-center bg-gradient-to-br from-indigo-50 via-white to-purple-50 -mt-8">
      <div className="max-w-3xl w-full p-8">
        <div className="mb-8">
          <div className="flex justify-between items-center mb-2">
            {[1, 2, 3, 4, 5, 6].map((s) => (
              <div
                key={s}
                className={`h-2 flex-1 mx-1 rounded-full ${
                  s <= step ? "bg-indigo-600" : "bg-gray-200"
                }`}
              />
            ))}
          </div>
          <p className="text-sm text-gray-600 text-center">Step {step} of 6</p>
        </div>

        <div className="bg-white rounded-2xl shadow-lg p-8">
          {step === 1 && (
            <div>
              <h2 className="text-2xl font-bold text-gray-900 mb-2">
                Choose Your Career Track
              </h2>
              <p className="text-gray-600 mb-6">
                Select the path that aligns with your career goals
              </p>
              <div className="grid grid-cols-2 gap-4">
                {CAREER_TRACKS.map((t) => (
                  <button
                    key={t.id}
                    onClick={() => setTrack(t.id)}
                    className={`p-6 rounded-lg border-2 text-left transition-all ${
                      track === t.id
                        ? "border-indigo-600 bg-indigo-50"
                        : "border-gray-200 hover:border-gray-300"
                    }`}
                  >
                    <div className="text-3xl mb-2">{t.icon}</div>
                    <div className="font-medium text-gray-900">{t.name}</div>
                  </button>
                ))}
              </div>
            </div>
          )}

          {step === 2 && (
            <div>
              <h2 className="text-2xl font-bold text-gray-900 mb-2">
                What's Your Experience Level?
              </h2>
              <p className="text-gray-600 mb-6">
                This helps us customize your learning path
              </p>
              <div className="space-y-3">
                {EXPERIENCE_LEVELS.map((exp) => (
                  <button
                    key={exp.id}
                    onClick={() => setExperience(exp.id)}
                    className={`w-full p-4 rounded-lg border-2 text-left transition-all ${
                      experience === exp.id
                        ? "border-indigo-600 bg-indigo-50"
                        : "border-gray-200 hover:border-gray-300"
                    }`}
                  >
                    <div className="font-medium text-gray-900">{exp.name}</div>
                    <div className="text-sm text-gray-600">{exp.description}</div>
                  </button>
                ))}
              </div>
            </div>
          )}

          {step === 3 && (
            <div>
              <h2 className="text-3xl font-bold text-gray-900 mb-2">
                What's your main goal?
              </h2>
              <p className="text-gray-600 mb-8">
                This helps us shape your roadmap
              </p>

              <div className="grid gap-4">
                {[
                  { id: "get-job-ready", title: "Get job-ready", subtitle: "Prepare for real work and interviews" },
                  { id: "level-up-current-role", title: "Level up in current role", subtitle: "Strengthen skills for your current position" },
                  { id: "switch-track", title: "Switch career track", subtitle: "Move into a new technical direction" },
                  { id: "build-project", title: "Build a portfolio project", subtitle: "Learn through a practical project" },
                ].map((item) => (
                  <button
                    key={item.id}
                    onClick={() => setGoal(item.id)}
                    className={`text-left p-6 rounded-xl border-2 transition ${goal === item.id
                        ? "border-indigo-600 bg-indigo-50"
                        : "border-gray-200 hover:border-gray-300"
                      }`}
                  >
                    <div className="font-semibold text-gray-900">{item.title}</div>
                    <div className="text-sm text-gray-600 mt-1">{item.subtitle}</div>
                  </button>
                ))}
              </div>
            </div>
          )}

          {step === 4 && (
            <div>
              <h2 className="text-3xl font-bold text-gray-900 mb-2">
                How much time can you study weekly?
              </h2>
              <p className="text-gray-600 mb-8">
                We'll adjust module size and pacing
              </p>

              <div className="grid grid-cols-2 gap-4">
                {[3, 6, 10, 15].map((hours) => (
                  <button
                    key={hours}
                    onClick={() => setWeeklyHours(hours)}
                    className={`p-6 rounded-xl border-2 text-center transition ${weeklyHours === hours
                        ? "border-indigo-600 bg-indigo-50"
                        : "border-gray-200 hover:border-gray-300"
                      }`}
                  >
                    <div className="text-2xl font-bold">{hours}h</div>
                    <div className="text-sm text-gray-600">per week</div>
                  </button>
                ))}
              </div>
            </div>
          )}

          {step === 5 && (
            <div>
              <h2 className="text-3xl font-bold text-gray-900 mb-2">
                Choose focus areas
              </h2>
              <p className="text-gray-600 mb-8">
                Pick topics you want to prioritize
              </p>

              <div className="grid grid-cols-2 gap-4">
                {[
                  "Fundamentals",
                  "Architecture",
                  "Testing",
                  "Production",
                  "Performance",
                  "Interview Prep",
                  "Project Practice",
                  "Cloud & Deployment",
                ].map((area) => {
                  const selected = focusAreas.includes(area);

                  return (
                    <button
                      key={area}
                      onClick={() =>
                        setFocusAreas((prev) =>
                          selected ? prev.filter((x) => x !== area) : [...prev, area]
                        )
                      }
                      className={`p-5 rounded-xl border-2 text-left transition ${selected
                          ? "border-indigo-600 bg-indigo-50"
                          : "border-gray-200 hover:border-gray-300"
                        }`}
                    >
                      {area}
                    </button>
                  );
                })}
              </div>
            </div>
          )}

          {step === 6 && (
            <div>
              <h2 className="text-2xl font-bold text-gray-900 mb-2">
                Preferred Language
              </h2>
              <p className="text-gray-600 mb-6">Choose your learning language</p>
              <div className="grid grid-cols-2 gap-4 mb-8">
                {LANGUAGES.map((lang) => (
                  <button
                    key={lang.id}
                    onClick={() => setLanguage(lang.id)}
                    className={`p-4 rounded-lg border-2 text-center transition-all ${
                      language === lang.id
                        ? "border-indigo-600 bg-indigo-50"
                        : "border-gray-200 hover:border-gray-300"
                    }`}
                  >
                    <div className="font-medium text-gray-900">{lang.name}</div>
                  </button>
                ))}
              </div>

              {generating && (
                <div className="mb-6 p-4 bg-blue-50 rounded-lg">
                  <div className="flex items-center space-x-3">
                    <Loader2 className="h-5 w-5 text-blue-600 animate-spin" />
                    <div>
                      <div className="font-medium text-blue-900">
                        Generating Your Roadmap
                      </div>
                      <div className="text-sm text-blue-700">
                        Creating a personalized learning plan for {track}...
                      </div>
                    </div>
                  </div>
                </div>
              )}
            </div>
          )}

          <div className="flex justify-between mt-8 pt-6 border-t">
            {step > 1 && !generating && (
              <button
                onClick={() => setStep(step - 1)}
                className="flex items-center space-x-2 px-4 py-2 text-gray-700 hover:text-gray-900"
              >
                <ArrowLeft className="h-4 w-4" />
                <span>Back</span>
              </button>
            )}
            {step < 6 && (
              <button
                onClick={() => setStep(step + 1)}
                disabled={!track || (step === 2 && !experience)}
                className="ml-auto flex items-center space-x-2 px-6 py-2 bg-indigo-600 text-white rounded-lg hover:bg-indigo-700 disabled:opacity-50 disabled:cursor-not-allowed"
              >
                <span>Continue</span>
                <ArrowRight className="h-4 w-4" />
              </button>
            )}
            {step === 6 && !generating && (
              <button
                onClick={handleComplete}
                className="ml-auto flex items-center space-x-2 px-6 py-2 bg-indigo-600 text-white rounded-lg hover:bg-indigo-700"
              >
                <span>Generate Roadmap</span>
                <ArrowRight className="h-4 w-4" />
              </button>
            )}
          </div>
        </div>
      </div>
    </div>
  );
}
