import { useEffect } from "react";
import { Outlet, useNavigate, useLocation, Link } from "react-router";
import { useApp } from "../context/AppContext";
import { Home, BookOpen, MessageSquare, CreditCard, FileEdit, User, LogOut, Crown } from "lucide-react";

export function RootLayout() {
  const { user, signOut, subscription } = useApp();
  const navigate = useNavigate();
  const location = useLocation();

  useEffect(() => {
    if (!user && location.pathname !== "/signin") {
      navigate("/signin");
    } else if (user && !user.hasCompletedOnboarding && location.pathname !== "/onboarding") {
      navigate("/onboarding");
    }
  }, [user, navigate, location.pathname]);

  if (!user || location.pathname === "/signin") {
    return <Outlet />;
  }

  return (
    <div className="min-h-screen bg-gray-50">
      <nav className="bg-white border-b border-gray-200">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="flex justify-between h-16">
            <div className="flex items-center space-x-8">
              <div className="flex-shrink-0 flex items-center">
                <BookOpen className="h-8 w-8 text-indigo-600" />
                <span className="ml-2 text-xl font-semibold text-gray-900">
                  CareerPath
                </span>
              </div>
              {user.hasCompletedOnboarding && (
                <div className="flex space-x-4">
                  <NavLink to="/" icon={<Home className="h-5 w-5" />} label="Dashboard" />
                  <NavLink to="/roadmap" icon={<BookOpen className="h-5 w-5" />} label="Roadmap" />
                  <NavLink
                    to="/profile"
                    icon={<User className="h-5 w-5" />}
                    label="Profile"
                  />
                  <NavLink
                    to="/interview"
                    icon={<MessageSquare className="h-5 w-5" />}
                    label="Interview"
                  />
                  {(user.role === "editor" || user.role === "admin") && (
                    <NavLink
                      to="/content"
                      icon={<FileEdit className="h-5 w-5" />}
                      label="Content"
                    />
                  )}
                </div>
              )}
            </div>
            <div className="flex items-center space-x-4">
              <Link
                to="/subscription"
                className={`flex items-center space-x-2 px-3 py-2 rounded-lg transition-colors ${
                  subscription.tier === "free"
                    ? "bg-gradient-to-r from-indigo-600 to-purple-600 text-white hover:from-indigo-700 hover:to-purple-700"
                    : "text-indigo-600 hover:bg-indigo-50"
                }`}
              >
                <Crown className="h-4 w-4" />
                <span className="text-sm font-medium capitalize">
                  {subscription.tier === "free" ? "Upgrade" : subscription.tier}
                </span>
              </Link>
              <Link
                to="/profile"
                className="flex items-center space-x-2 text-gray-700 hover:text-gray-900"
              >
                <User className="h-4 w-4" />
                <span className="text-sm">{user.name}</span>
              </Link>
              <button
                onClick={signOut}
                className="flex items-center space-x-1 text-gray-600 hover:text-gray-900"
              >
                <LogOut className="h-4 w-4" />
                <span className="text-sm">Sign Out</span>
              </button>
            </div>
          </div>
        </div>
      </nav>
      <main className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
        <Outlet />
      </main>
    </div>
  );
}

function NavLink({ to, icon, label }: { to: string; icon: React.ReactNode; label: string }) {
  const location = useLocation();
  const isActive = location.pathname === to;

  return (
    <Link
      to={to}
      className={`flex items-center space-x-2 px-3 py-2 rounded-md text-sm ${
        isActive
          ? "bg-indigo-100 text-indigo-700"
          : "text-gray-600 hover:bg-gray-100 hover:text-gray-900"
      }`}
    >
      {icon}
      <span>{label}</span>
    </Link>
  );
}
