import { useRef, useState } from "react";
import { getGuestId } from "../hooks/useGuestId";
import { requestUpload, uploadToBlob, confirmUpload } from "../api";

const MAX_PHOTOS = 10;

export default function PhotoUploader() {
  const inputRef = useRef<HTMLInputElement>(null);
  const [usedSoFar, setUsedSoFar] = useState<number | null>(null);
  const [status, setStatus] = useState<"idle" | "uploading" | "error" | "success">("idle");
  const [message, setMessage] = useState<string>("");

  const remaining = usedSoFar === null ? MAX_PHOTOS : Math.max(0, MAX_PHOTOS - usedSoFar);

  async function handleFileSelected(e: React.ChangeEvent<HTMLInputElement>) {
    const file = e.target.files?.[0];
    e.target.value = "";
    if (!file) return;

    setStatus("uploading");
    setMessage("Slanje fotografije...");

    try {
      const guestId = getGuestId();
      const requested = await requestUpload(guestId, file.name);

      if (!requested.success || !requested.uploadUrl || !requested.blobName) {
        setUsedSoFar(requested.usedSoFar);
        setStatus("error");
        setMessage(requested.error ?? "Nije moguce poslati fotografiju.");
        return;
      }

      await uploadToBlob(requested.uploadUrl, file);
      const confirmed = await confirmUpload(guestId, requested.blobName);

      setUsedSoFar(confirmed.usedSoFar);
      if (confirmed.success) {
        setStatus("success");
        setMessage("Fotografija je uspjesno poslana. Hvala!");
      } else {
        setStatus("error");
        setMessage(confirmed.error ?? "Nesto je posla po zlu.");
      }
    } catch (err) {
      setStatus("error");
      setMessage(err instanceof Error ? err.message : "Neocekivana greska.");
    }
  }

  return (
    <div className="uploader">
      <p className="uploader__counter">
        {remaining > 0
          ? `Preostalo fotografija: ${remaining} / ${MAX_PHOTOS}`
          : "Iskoristili ste svih 10 fotografija. Hvala vam!"}
      </p>

      <input
        ref={inputRef}
        type="file"
        accept="image/*"
        capture="environment"
        onChange={handleFileSelected}
        disabled={status === "uploading" || remaining <= 0}
        style={{ display: "none" }}
      />

      <button
        className="uploader__button"
        onClick={() => inputRef.current?.click()}
        disabled={status === "uploading" || remaining <= 0}
      >
        {status === "uploading" ? "Slanje..." : "Slikaj i podijeli"}
      </button>

      {message && <p className={`uploader__message uploader__message--${status}`}>{message}</p>}
    </div>
  );
}
