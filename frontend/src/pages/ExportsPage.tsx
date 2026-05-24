import { useQuery } from "@tanstack/react-query";
import { getDownloadUrl, getExports } from "../api/stravaExporterApi";

const prompt = `Analyze this Strava running data.

Focus on weekly mileage, pace trend, heart-rate drift, overtraining signs, long-run progression, easy-run consistency, and practical suggestions for the next 4 weeks.`;

export function ExportsPage() {
  const exportsQuery = useQuery({ queryKey: ["exports"], queryFn: getExports });

  return (
    <section className="page">
      <header>
        <h1>Exports</h1>
        <p>Download generated files or copy the prompt to use with ChatGPT.</p>
      </header>
      <button onClick={() => navigator.clipboard.writeText(prompt)}>Copy ChatGPT prompt</button>
      <div className="list">
        {(exportsQuery.data ?? []).map((job) => (
          <article className="card" key={job.id}>
            <strong>{new Date(job.createdAtUtc).toLocaleString()}</strong>
            <div className="downloads">
              <a href={getDownloadUrl(`/api/exports/${job.id}/download?format=json`)}>JSON</a>
              <a href={getDownloadUrl(`/api/exports/${job.id}/download?format=csv`)}>CSV</a>
              <a href={getDownloadUrl(`/api/exports/${job.id}/download?format=markdown`)}>Markdown</a>
            </div>
          </article>
        ))}
      </div>
    </section>
  );
}
