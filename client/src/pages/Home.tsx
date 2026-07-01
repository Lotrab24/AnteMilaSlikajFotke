import PhotoUploader from "../components/PhotoUploader";

export default function Home() {
  return (
    <main className="home">
      <h1>Ante &amp; Mila</h1>
      <p>Podijelite s nama do 10 fotografija s naseg vjencanja!</p>
      <PhotoUploader />
    </main>
  );
}
