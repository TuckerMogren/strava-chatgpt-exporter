import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { disconnectStrava, getConnectionStatus } from "../api/stravaExporterApi";

export function SettingsPage() {
  const queryClient = useQueryClient();
  const status = useQuery({ queryKey: ["connection-status"], queryFn: getConnectionStatus });
  const disconnect = useMutation({
    mutationFn: disconnectStrava,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["connection-status"] })
  });

  return (
    <section className="page">
      <header>
        <h1>Settings</h1>
        <p>{status.data?.connected ? `Connected athlete ${status.data.athleteId}` : "Strava is not connected."}</p>
      </header>
      <button onClick={() => disconnect.mutate()} disabled={!status.data?.connected || disconnect.isPending}>Disconnect Strava</button>
    </section>
  );
}
