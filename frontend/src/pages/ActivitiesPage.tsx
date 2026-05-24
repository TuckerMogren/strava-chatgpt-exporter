import { useMutation, useQuery } from "@tanstack/react-query";
import { useState } from "react";
import { createExport, getActivities, getConnectionStatus, getConnectUrl, getDownloadUrl } from "../api/stravaExporterApi";

export function ActivitiesPage() {
  const [after, setAfter] = useState("");
  const [before, setBefore] = useState("");
  const [type, setType] = useState("Run");
  const status = useQuery({ queryKey: ["connection-status"], queryFn: getConnectionStatus, retry: false });
  const activities = useQuery({
    queryKey: ["activities", after, before, type],
    queryFn: () => getActivities(after || undefined, before || undefined, type || undefined),
    enabled: false
  });
  const exportMutation = useMutation({
    mutationFn: () => createExport({
      after: after || undefined,
      before: before || undefined,
      activityTypes: type ? [type] : [],
      includeActivityDetails: true,
      includeStreams: false,
      includePrivateNotes: false
    })
  });

  return (
    <section className="page">
      <header>
        <h1>Activities</h1>
        <p>Filter your Strava activities before generating ChatGPT-ready exports.</p>
      </header>
      {status.data?.connected === false ? (
        <div className="notice">
          <strong>Strava is not connected.</strong>
          <span>Connect your account before fetching activities.</span>
          <a className="primary" href={getConnectUrl()}>Connect Strava</a>
        </div>
      ) : null}
      <div className="filters">
        <label>After<input type="date" value={after} onChange={(event) => setAfter(event.target.value)} /></label>
        <label>Before<input type="date" value={before} onChange={(event) => setBefore(event.target.value)} /></label>
        <label>Type<input value={type} onChange={(event) => setType(event.target.value)} /></label>
        <button onClick={() => activities.refetch()} disabled={!status.data?.connected}>Fetch</button>
        <button onClick={() => exportMutation.mutate()} disabled={!status.data?.connected || exportMutation.isPending}>Create export</button>
      </div>
      {activities.error ? <p className="error">{activities.error.message}</p> : null}
      {exportMutation.data ? (
        <div className="downloads">
          <a href={getDownloadUrl(exportMutation.data.downloads.json)}>JSON</a>
          <a href={getDownloadUrl(exportMutation.data.downloads.csv)}>CSV</a>
          <a href={getDownloadUrl(exportMutation.data.downloads.markdown)}>Markdown</a>
        </div>
      ) : null}
      <table>
        <thead>
          <tr><th>Date</th><th>Name</th><th>Type</th><th>Distance</th><th>Pace</th><th>Avg HR</th></tr>
        </thead>
        <tbody>
          {(activities.data ?? []).map((activity) => (
            <tr key={activity.id}>
              <td>{activity.startDateLocal.slice(0, 10)}</td>
              <td>{activity.name}</td>
              <td>{activity.type}</td>
              <td>{activity.distanceMiles.toFixed(2)} mi</td>
              <td>{formatPace(activity.averagePaceSecondsPerMile)}</td>
              <td>{activity.averageHeartRate?.toFixed(0) ?? ""}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </section>
  );
}

function formatPace(seconds?: number) {
  if (!seconds) return "";
  const minutes = Math.floor(seconds / 60);
  return `${minutes}:${Math.round(seconds % 60).toString().padStart(2, "0")}/mi`;
}
