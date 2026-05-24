export interface ActivitySummary {
  id: number;
  name: string;
  type: string;
  startDateLocal: string;
  distanceMeters: number;
  distanceMiles: number;
  movingTimeSeconds: number;
  elapsedTimeSeconds: number;
  averagePaceSecondsPerMile?: number;
  averageHeartRate?: number;
  maxHeartRate?: number;
  elevationGainFeet?: number;
}

export interface CreateExportRequest {
  after?: string;
  before?: string;
  activityTypes: string[];
  includeActivityDetails: boolean;
  includeStreams: boolean;
  includePrivateNotes: boolean;
}

export interface CreateExportResponse {
  exportId: string;
  createdAtUtc: string;
  downloads: Record<"json" | "csv" | "markdown", string>;
}

export interface ExportJobSummary {
  id: string;
  createdAtUtc: string;
  request: CreateExportRequest;
}

export interface ConnectionStatus {
  connected: boolean;
  athleteId?: number;
}

const apiBaseUrl = import.meta.env.VITE_API_BASE_URL ?? "http://localhost:5080";

async function read<T>(response: Response): Promise<T> {
  if (!response.ok) {
    const body = await response.json().catch(() => null);
    throw new Error(body?.message ?? "Request failed.");
  }

  return response.json() as Promise<T>;
}

export function getConnectUrl(): string {
  return `${apiBaseUrl}/api/strava/connect`;
}

export function getDownloadUrl(path: string): string {
  return `${apiBaseUrl}${path}`;
}

export async function getConnectionStatus(): Promise<ConnectionStatus> {
  return read(await fetch(`${apiBaseUrl}/api/strava/status`));
}

export async function disconnectStrava(): Promise<void> {
  const response = await fetch(`${apiBaseUrl}/api/strava/disconnect`, { method: "DELETE" });
  if (!response.ok) {
    throw new Error("Failed to disconnect Strava.");
  }
}

export async function getActivities(after?: string, before?: string, type?: string): Promise<ActivitySummary[]> {
  const url = new URL(`${apiBaseUrl}/api/activities`);
  if (after) url.searchParams.set("after", after);
  if (before) url.searchParams.set("before", before);
  if (type) url.searchParams.set("type", type);
  return read(await fetch(url));
}

export async function createExport(request: CreateExportRequest): Promise<CreateExportResponse> {
  return read(await fetch(`${apiBaseUrl}/api/exports`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(request)
  }));
}

export async function getExports(): Promise<ExportJobSummary[]> {
  return read(await fetch(`${apiBaseUrl}/api/exports`));
}
