import { useState } from "react";
import { useApp } from "../context/AppContext";
import { useNavigate } from "react-router";
import { User, Mail, Globe, Award, Bell, Trash2, Save } from "lucide-react";

export function Profile() {
  const { user, learningPlan, updateProfile, deleteAccount } = useApp();
  const navigate = useNavigate();
  const [isEditing, setIsEditing] = useState(false);
  const [formData, setFormData] = useState({
    name: user?.name || "",
    preferences: {
      notifications: user?.preferences.notifications || false,
      emailUpdates: user?.preferences.emailUpdates || false,
    },
  });

  if (!user) return null;

  const handleSave = () => {
    updateProfile({
      name: formData.name,
      preferences: formData.preferences,
    });
    setIsEditing(false);
  };

  const handleDeleteAccount = () => {
    if (
      confirm(
        "Are you sure you want to delete your account? This action cannot be undone."
      )
    ) {
      deleteAccount();
      navigate("/signin");
    }
  };

  return (
    <div className="max-w-3xl mx-auto">
      <h1 className="text-3xl font-bold text-gray-900 mb-6">Profile</h1>

      <div className="bg-white rounded-lg shadow p-8 mb-6">
        <div className="flex items-center justify-between mb-6">
          <div className="flex items-center space-x-4">
            <div className="h-20 w-20 bg-indigo-100 rounded-full flex items-center justify-center">
              <User className="h-10 w-10 text-indigo-600" />
            </div>
            <div>
              {isEditing ? (
                <input
                  type="text"
                  value={formData.name}
                  onChange={(e) =>
                    setFormData({ ...formData, name: e.target.value })
                  }
                  className="text-2xl font-semibold text-gray-900 border-b-2 border-indigo-500 focus:outline-none"
                />
              ) : (
                <h2 className="text-2xl font-semibold text-gray-900">{user.name}</h2>
              )}
              <p className="text-gray-600">{user.email}</p>
              <p className="text-sm text-gray-500 capitalize">Role: {user.role}</p>
            </div>
          </div>
          <button
            onClick={() => (isEditing ? handleSave() : setIsEditing(true))}
            className="flex items-center space-x-2 px-4 py-2 bg-indigo-600 text-white rounded-lg hover:bg-indigo-700"
          >
            {isEditing ? (
              <>
                <Save className="h-4 w-4" />
                <span>Save</span>
              </>
            ) : (
              <span>Edit Profile</span>
            )}
          </button>
        </div>

        <div className="space-y-4 border-t pt-6">
          <div className="flex items-center space-x-3">
            <Mail className="h-5 w-5 text-gray-400" />
            <span className="text-gray-700">{user.email}</span>
          </div>
          {learningPlan && (
            <>
              <div className="flex items-center space-x-3">
                <Award className="h-5 w-5 text-gray-400" />
                <span className="text-gray-700">
                  Learning Path: {learningPlan.track}
                </span>
              </div>
              <div className="flex items-center space-x-3">
                <Globe className="h-5 w-5 text-gray-400" />
                <span className="text-gray-700">
                  Language: {learningPlan.language.toUpperCase()}
                </span>
              </div>
            </>
          )}
        </div>
      </div>

      <div className="bg-white rounded-lg shadow p-8 mb-6">
        <h3 className="text-xl font-semibold text-gray-900 mb-4">Preferences</h3>
        <div className="space-y-4">
          <div className="flex items-center justify-between">
            <div className="flex items-center space-x-3">
              <Bell className="h-5 w-5 text-gray-400" />
              <div>
                <div className="font-medium text-gray-900">Push Notifications</div>
                <div className="text-sm text-gray-600">
                  Receive notifications about your progress
                </div>
              </div>
            </div>
            <label className="relative inline-flex items-center cursor-pointer">
              <input
                type="checkbox"
                checked={formData.preferences.notifications}
                onChange={(e) =>
                  setFormData({
                    ...formData,
                    preferences: {
                      ...formData.preferences,
                      notifications: e.target.checked,
                    },
                  })
                }
                className="sr-only peer"
                disabled={!isEditing}
              />
              <div className="w-11 h-6 bg-gray-200 peer-focus:outline-none peer-focus:ring-4 peer-focus:ring-indigo-300 rounded-full peer peer-checked:after:translate-x-full peer-checked:after:border-white after:content-[''] after:absolute after:top-[2px] after:left-[2px] after:bg-white after:border-gray-300 after:border after:rounded-full after:h-5 after:w-5 after:transition-all peer-checked:bg-indigo-600"></div>
            </label>
          </div>

          <div className="flex items-center justify-between">
            <div className="flex items-center space-x-3">
              <Mail className="h-5 w-5 text-gray-400" />
              <div>
                <div className="font-medium text-gray-900">Email Updates</div>
                <div className="text-sm text-gray-600">
                  Receive weekly progress updates via email
                </div>
              </div>
            </div>
            <label className="relative inline-flex items-center cursor-pointer">
              <input
                type="checkbox"
                checked={formData.preferences.emailUpdates}
                onChange={(e) =>
                  setFormData({
                    ...formData,
                    preferences: {
                      ...formData.preferences,
                      emailUpdates: e.target.checked,
                    },
                  })
                }
                className="sr-only peer"
                disabled={!isEditing}
              />
              <div className="w-11 h-6 bg-gray-200 peer-focus:outline-none peer-focus:ring-4 peer-focus:ring-indigo-300 rounded-full peer peer-checked:after:translate-x-full peer-checked:after:border-white after:content-[''] after:absolute after:top-[2px] after:left-[2px] after:bg-white after:border-gray-300 after:border after:rounded-full after:h-5 after:w-5 after:transition-all peer-checked:bg-indigo-600"></div>
            </label>
          </div>
        </div>
      </div>

      {learningPlan && (
        <div className="bg-white rounded-lg shadow p-8 mb-6">
          <h3 className="text-xl font-semibold text-gray-900 mb-4">
            Learning Preferences
          </h3>
          <div className="grid grid-cols-2 gap-4">
            <div className="p-4 bg-gray-50 rounded-lg">
              <div className="text-sm text-gray-600">Experience Level</div>
              <div className="font-medium text-gray-900 capitalize">
                {learningPlan.experience}
              </div>
            </div>
            <div className="p-4 bg-gray-50 rounded-lg">
              <div className="text-sm text-gray-600">Career Track</div>
              <div className="font-medium text-gray-900">{learningPlan.track}</div>
            </div>
          </div>
        </div>
      )}

      <div className="bg-red-50 border border-red-200 rounded-lg p-6">
        <h3 className="text-lg font-semibold text-red-900 mb-2">Danger Zone</h3>
        <p className="text-sm text-red-700 mb-4">
          Once you delete your account, there is no going back. All your progress,
          assessments, and interview history will be permanently deleted.
        </p>
        <button
          onClick={handleDeleteAccount}
          className="flex items-center space-x-2 px-4 py-2 bg-red-600 text-white rounded-lg hover:bg-red-700"
        >
          <Trash2 className="h-4 w-4" />
          <span>Delete Account</span>
        </button>
      </div>
    </div>
  );
}
