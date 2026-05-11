import { createBrowserRouter } from "react-router";
import { RootLayout } from "./components/RootLayout";
import { SignIn } from "./components/SignIn";
import { Onboarding } from "./components/Onboarding";
import { Dashboard } from "./components/Dashboard";
import { Roadmap } from "./components/Roadmap";
import { ModuleDetail } from "./components/ModuleDetail";
import { Profile } from "./components/Profile";
import { Subscription } from "./components/Subscription";
import { Interview } from "./components/Interview";
import { InterviewSession } from "./components/InterviewSession";
import { ContentManagement } from "./components/ContentManagement";
import { NotFound } from "./components/NotFound";

export const router = createBrowserRouter([
  {
    path: "/",
    Component: RootLayout,
    children: [
      { index: true, Component: Dashboard },
      { path: "signin", Component: SignIn },
      { path: "onboarding", Component: Onboarding },
      { path: "roadmap", Component: Roadmap },
      { path: "module/:moduleId", Component: ModuleDetail },
      { path: "interview", Component: Interview },
      { path: "interview/:sessionId", Component: InterviewSession },
      { path: "subscription", Component: Subscription },
      { path: "content", Component: ContentManagement },
      { path: "profile", Component: Profile },
      { path: "*", Component: NotFound },
    ],
  },
]);
