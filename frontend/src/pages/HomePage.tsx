import { useQuery } from "@tanstack/react-query";
import { getConnectUrl, getConnectionStatus } from "../api/stravaExporterApi";

export function HomePage() {
  const status = useQuery({ queryKey: ["connection-status"], queryFn: getConnectionStatus, retry: false });

  return (
    <section className="page">
      <header>
        <h1>Strava ChatGPT Exporter</h1>
        <p>Export your own Strava activities into JSON, CSV, and Markdown files for manual ChatGPT analysis.</p>
      </header>
      <div className="toolbar">
        <a className="primary" href={getConnectUrl()}>Connect Strava</a>
        <span className={status.data?.connected ? "pill ok" : "pill"}>{status.data?.connected ? `Connected athlete ${status.data.athleteId}` : "Not connected"}</span>
      </div>
    </section>
  );
}
