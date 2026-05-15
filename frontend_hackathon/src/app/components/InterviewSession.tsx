import { useState, useEffect, useRef } from "react";
import { useParams, useNavigate } from "react-router";
import { useApp } from "../context/AppContext";
import { ArrowLeft, Send, CheckCircle2, TrendingUp, AlertCircle } from "lucide-react";

export function InterviewSession() {
  const { sessionId } = useParams();
  const { user, interviewSessions, sendInterviewMessage } = useApp();

  const navigate = useNavigate();
  const [message, setMessage] = useState("");
  const [isLoading, setIsLoading] = useState(false);
  const messagesEndRef = useRef<HTMLDivElement>(null);
  const session = interviewSessions.find((s) => s.id === sessionId);

  useEffect(() => {
    messagesEndRef.current?.scrollIntoView({ behavior: "smooth" });
  }, [session?.messages]);

  if (!session) {
    return (
      <div className="text-center py-12">
        <p className="text-gray-600">Interview session not found</p>
      </div>
    );
  }

  const handleSend = async () => {
    if (!session) return;

    const text = message.trim();
    if (!text || isLoading) return;

    setIsLoading(true);
    setMessage("");

    try {
      await sendInterviewMessage(session.id, text);
    } catch (error) {
      console.error("Interview send failed:", error);
      alert("Failed to send interview message");
    } finally {
      setIsLoading(false);
    }
  };

  const handleKeyPress = (e: React.KeyboardEvent) => {
    if (e.key === "Enter" && !e.shiftKey) {
      e.preventDefault();
      handleSend();
    }
  };

  if (session.status === "completed" && session.report) {
    return (
      <div className="max-w-4xl mx-auto">
        <button
          onClick={() => navigate("/interview")}
          className="flex items-center space-x-2 text-gray-600 hover:text-gray-900 mb-6"
        >
          <ArrowLeft className="h-4 w-4" />
          <span>Back to Interviews</span>
        </button>

        <div className="bg-white rounded-lg shadow p-8">
          <div className="text-center mb-8">
            <CheckCircle2 className="h-16 w-16 text-green-500 mx-auto mb-4" />
            <h1 className="text-3xl font-bold text-gray-900 mb-2">
              Interview Complete!
            </h1>
            <p className="text-gray-600">
              Here's your performance report and feedback
            </p>
          </div>

          <div className="bg-gradient-to-r from-indigo-50 to-purple-50 rounded-lg p-6 mb-6">
            <div className="text-center">
              <div className="text-5xl font-bold text-indigo-600 mb-2">
                {session.report.overallScore}
              </div>
              <div className="text-gray-700">Overall Score</div>
            </div>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-6 mb-6">
            <div className="bg-green-50 rounded-lg p-6">
              <div className="flex items-center space-x-2 mb-4">
                <TrendingUp className="h-5 w-5 text-green-600" />
                <h3 className="font-semibold text-green-900">Strengths</h3>
              </div>
              <ul className="space-y-2">
                {session.report.strengths.map((strength, idx) => (
                  <li key={idx} className="flex items-start space-x-2">
                    <CheckCircle2 className="h-4 w-4 text-green-600 mt-1 flex-shrink-0" />
                    <span className="text-green-800 text-sm">{strength}</span>
                  </li>
                ))}
              </ul>
            </div>

            <div className="bg-orange-50 rounded-lg p-6">
              <div className="flex items-center space-x-2 mb-4">
                <AlertCircle className="h-5 w-5 text-orange-600" />
                <h3 className="font-semibold text-orange-900">Areas to Improve</h3>
              </div>
              <ul className="space-y-2">
                {session.report.improvements.map((improvement, idx) => (
                  <li key={idx} className="flex items-start space-x-2">
                    <AlertCircle className="h-4 w-4 text-orange-600 mt-1 flex-shrink-0" />
                    <span className="text-orange-800 text-sm">{improvement}</span>
                  </li>
                ))}
              </ul>
            </div>
          </div>

          <div className="border-t pt-6">
            <h3 className="font-semibold text-gray-900 mb-4">
              Question-by-Question Breakdown
            </h3>
            <div className="space-y-4">
              {session.report.questionScores.map((q, idx) => (
                <div key={idx} className="bg-gray-50 rounded-lg p-4">
                  <div className="flex items-center justify-between mb-2">
                    <div className="font-medium text-gray-900">{q.question}</div>
                    <div className="text-xl font-bold text-indigo-600">
                      {q.score}
                    </div>
                  </div>
                  <div className="text-sm text-gray-600">{q.feedback}</div>
                  <div className="mt-2 bg-gray-200 rounded-full h-2">
                    <div
                      className="bg-indigo-600 rounded-full h-2"
                      style={{ width: `${q.score}%` }}
                    />
                  </div>
                </div>
              ))}
            </div>
          </div>

          <button
            onClick={() => navigate("/interview")}
            className="mt-8 w-full py-3 bg-indigo-600 text-white rounded-lg hover:bg-indigo-700"
          >
            Start Another Interview
          </button>
        </div>
      </div>
    );
  }

  return (
    <div className="max-w-4xl mx-auto h-[calc(100vh-12rem)] flex flex-col">
      <div className="mb-4 flex items-center justify-between">
        <button
          onClick={() => navigate("/interview")}
          className="flex items-center space-x-2 text-gray-600 hover:text-gray-900"
        >
          <ArrowLeft className="h-4 w-4" />
          <span>Back</span>
        </button>
        <div className="text-sm text-gray-600 capitalize">
          {session.type} Interview
        </div>
      </div>

      <div className="flex-1 bg-white rounded-lg shadow overflow-hidden flex flex-col">
        <div className="flex-1 overflow-y-auto p-6 space-y-4">
          {session.messages.map((msg) => (
            <div
              key={msg.id}
              className={`flex ${msg.role === "user" ? "justify-end" : "justify-start"}`}
            >
              <div
                className={`max-w-[80%] rounded-lg px-4 py-3 ${msg.role === "user"
                    ? "bg-indigo-600 text-white"
                    : "bg-gray-100 text-gray-900"
                  }`}
              >
                <div className="text-sm">{msg.content}</div>
                <div
                  className={`text-xs mt-1 ${msg.role === "user" ? "text-indigo-200" : "text-gray-500"
                    }`}
                >
                  {new Date(msg.timestamp).toLocaleTimeString()}
                </div>
              </div>
            </div>
          ))}
          <div ref={messagesEndRef} />
        </div>

        <div className="border-t bg-white p-4">
          <div className="flex items-end gap-3">
            <textarea
              value={message}
              onChange={(e) => setMessage(e.target.value)}
              onKeyDown={handleKeyPress}
              placeholder="Type your response..."
              rows={2}
              disabled={isLoading}
              className="min-h-[72px] flex-1 px-4 py-3 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-indigo-500 resize-none disabled:bg-gray-100"
            />

            <button
              onClick={handleSend}
              disabled={!message.trim() || isLoading}
              className="h-[72px] w-[72px] flex items-center justify-center bg-indigo-600 text-white rounded-lg hover:bg-indigo-700 disabled:bg-gray-300 disabled:cursor-not-allowed"
            >
              <Send className="h-5 w-5" />
            </button>
          </div>

          <div className="text-xs text-gray-500 mt-2 text-center">
            {isLoading
              ? "AI is evaluating your answer..."
              : "Answer the current question to continue"}
          </div>
        </div>

        </div>
      </div>
  );
}
