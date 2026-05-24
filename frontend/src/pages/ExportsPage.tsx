import { useQuery } from "@tanstack/react-query";
import { useState } from "react";
import { downloadExport, getExports } from "../api/stravaExporterApi";

type ExportFormat = "json" | "markdown" | "csv";

const prompt = `Analyze this Strava running data.

Focus on weekly mileage, pace trend, heart-rate drift, overtraining signs, long-run progression, easy-run consistency, and practical suggestions for the next 4 weeks.`;

export function ExportsPage() {
  const exportsQuery = useQuery({ queryKey: ["exports"], queryFn: getExports });
  const [error, setError] = useState<string | null>(null);
  const [selectedFormats, setSelectedFormats] = useState<Record<string, ExportFormat>>({});

  async function handleDownload(exportId: string) {
    setError(null);
    const format = selectedFormats[exportId] ?? "json";
    try {
      await downloadExport(exportId, format);
    } catch (downloadError) {
      setError(downloadError instanceof Error ? downloadError.message : "Download failed.");
    }
  }

  function selectFormat(exportId: string, format: ExportFormat) {
    setSelectedFormats((current) => ({ ...current, [exportId]: format }));
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
            <div className="export-actions">
              <div className="format-picker" role="group" aria-label="Export format">
                {(["json", "markdown", "csv"] as const).map((format) => (
                  <button
                    className={(selectedFormats[job.id] ?? "json") === format ? "selected" : ""}
                    key={format}
                    onClick={() => selectFormat(job.id, format)}
                    type="button"
                  >
                    {format === "json" ? "JSON" : format === "markdown" ? "Markdown" : "CSV"}
                  </button>
                ))}
              </div>
              <button className="primary" onClick={() => handleDownload(job.id)} type="button">Download</button>
            </div>
          </article>
        ))}
      </div>
    </section>
  );
}
