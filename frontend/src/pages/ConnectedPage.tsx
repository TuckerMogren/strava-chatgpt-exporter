export function ConnectedPage() {
  return (
    <section className="page">
      <header>
        <h1>Strava connected</h1>
        <p>Your tokens are stored on the backend. You can fetch activities and create exports now.</p>
      </header>
      <a className="primary" href="/activities">Open Activities</a>
    </section>
  );
}
