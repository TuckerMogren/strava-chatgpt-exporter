import { useQuery } from "@tanstack/react-query";
import { useState } from "react";
import { downloadExport, getExports } from "../api/stravaExporterApi";

const prompt = `Analyze this Strava running data.

Focus on weekly mileage, pace trend, heart-rate drift, overtraining signs, long-run progression, easy-run consistency, and practical suggestions for the next 4 weeks.`;

export function ExportsPage() {
  const exportsQuery = useQuery({ queryKey: ["exports"], queryFn: getExports });
  const [error, setError] = useState<string | null>(null);

  async function handleDownload(exportId: string, format: "json" | "csv" | "markdown") {
    setError(null);
    try {
      await downloadExport(exportId, format);
    } catch (downloadError) {
      setError(downloadError instanceof Error ? downloadError.message : "Download failed.");
    }
  }

  return (
    <section className="page">
      <header>
        <h1>Exports</h1>
        <p>Download generated files or copy the prompt to use with ChatGPT.</p>
      </header>
      <button onClick={() => navigator.clipboard.writeText(prompt)}>Copy ChatGPT prompt</button>
      {error ? <p className="error">{error}</p> : null}
      <div className="list">
        {(exportsQuery.data ?? []).map((job) => (
          <article className="card" key={job.id}>
            <strong>{new Date(job.createdAtUtc).toLocaleString()}</strong>
            <div className="downloads">
              <button onClick={() => handleDownload(job.id, "json")}>JSON</button>
              <button onClick={() => handleDownload(job.id, "csv")}>CSV</button>
              <button onClick={() => handleDownload(job.id, "markdown")}>Markdown</button>
            </div>
          </article>
        ))}
      </div>
    </section>
  );
}
