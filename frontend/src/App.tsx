import { Activity, Download, Home, Settings } from "lucide-react";
import { ActivitiesPage } from "./pages/ActivitiesPage";
import { ConnectedPage } from "./pages/ConnectedPage";
import { ExportsPage } from "./pages/ExportsPage";
import { HomePage } from "./pages/HomePage";
import { SettingsPage } from "./pages/SettingsPage";

const routes = [
  { path: "/", label: "Home", icon: Home },
  { path: "/activities", label: "Activities", icon: Activity },
  { path: "/exports", label: "Exports", icon: Download },
  { path: "/settings", label: "Settings", icon: Settings }
];

export function App() {
  const path = window.location.pathname;
  const Page = path === "/connected"
    ? ConnectedPage
    : path === "/activities"
      ? ActivitiesPage
      : path === "/exports"
        ? ExportsPage
        : path === "/settings"
          ? SettingsPage
          : HomePage;

  return (
    <div className="shell">
      <aside className="sidebar">
        <a className="brand" href="/">Strava Exporter</a>
        <nav>
          {routes.map((route) => {
            const Icon = route.icon;
            return (
              <a className={path === route.path ? "active" : ""} href={route.path} key={route.path}>
                <Icon size={18} />
                {route.label}
              </a>
            );
          })}
        </nav>
      </aside>
      <main>
        <Page />
      </main>
    </div>
  );
}
