import { useState } from "react";
import PhotoGrid from "../components/PhotoGrid";
import { PhotoInfo, ContainerSasResponse, fetchAdminPhotos, fetchContainerSas } from "../api";

export default function Admin() {
  const [username, setUsername] = useState("");
  const [password, setPassword] = useState("");
  const [authed, setAuthed] = useState(false);
  const [photos, setPhotos] = useState<PhotoInfo[]>([]);
  const [sas, setSas] = useState<ContainerSasResponse | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  async function login() {
    setLoading(true);
    setError(null);
    try {
      const list = await fetchAdminPhotos(username, password);
      setPhotos(list);
      setAuthed(true);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Greska pri prijavi.");
    } finally {
      setLoading(false);
    }
  }

  async function loadDownloadInstructions() {
    setLoading(true);
    setError(null);
    try {
      const response = await fetchContainerSas(username, password);
      setSas(response);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Greska pri dohvacanju SAS linka.");
    } finally {
      setLoading(false);
    }
  }

  if (!authed) {
    return (
      <main className="admin admin--login">
        <h1>Admin prijava</h1>
        <input placeholder="Korisnicko ime" value={username} onChange={(e) => setUsername(e.target.value)} />
        <input
          placeholder="Lozinka"
          type="password"
          value={password}
          onChange={(e) => setPassword(e.target.value)}
        />
        <button onClick={login} disabled={loading}>
          {loading ? "Provjera..." : "Prijava"}
        </button>
        {error && <p className="admin__error">{error}</p>}
      </main>
    );
  }

  return (
    <main className="admin">
      <h1>Sve fotografije ({photos.length})</h1>

      <section className="admin__download">
        <button onClick={loadDownloadInstructions} disabled={loading}>
          Preuzmi sve (AzCopy upute)
        </button>
        {sas && (
          <div className="admin__sas">
            <p>Naredba vrijedi do {new Date(sas.expiresAt).toLocaleString("hr-HR")}:</p>
            <code>{sas.azCopyCommand}</code>
          </div>
        )}
      </section>

      {error && <p className="admin__error">{error}</p>}

      <PhotoGrid photos={photos} />
    </main>
  );
}
