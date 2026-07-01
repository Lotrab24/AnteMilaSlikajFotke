export interface RequestUploadResponse {
  success: boolean;
  uploadUrl: string | null;
  blobName: string | null;
  usedSoFar: number;
  error: string | null;
}

export interface ConfirmUploadResponse {
  success: boolean;
  usedSoFar: number;
  error: string | null;
}

export interface PhotoInfo {
  blobName: string;
  url: string;
  uploadedAt: string | null;
  sizeBytes: number | null;
}

export interface ContainerSasResponse {
  sasUrl: string;
  azCopyCommand: string;
  expiresAt: string;
}

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL ?? "";

export async function requestUpload(guestId: string, fileName: string): Promise<RequestUploadResponse> {
  const res = await fetch(`${API_BASE_URL}/api/photos/request-upload`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ guestId, fileName })
  });
  return res.json();
}

export async function uploadToBlob(uploadUrl: string, file: File): Promise<void> {
  const res = await fetch(uploadUrl, {
    method: "PUT",
    headers: {
      "x-ms-blob-type": "BlockBlob",
      "Content-Type": file.type || "application/octet-stream"
    },
    body: file
  });
  if (!res.ok) {
    throw new Error(`Upload u pohranu nije uspio (status ${res.status}).`);
  }
}

export async function confirmUpload(guestId: string, blobName: string): Promise<ConfirmUploadResponse> {
  const res = await fetch(`${API_BASE_URL}/api/photos/confirm-upload`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ guestId, blobName })
  });
  return res.json();
}

function authHeader(username: string, password: string): string {
  return "Basic " + btoa(`${username}:${password}`);
}

export async function fetchAdminPhotos(username: string, password: string): Promise<PhotoInfo[]> {
  const res = await fetch(`${API_BASE_URL}/api/gallery/photos`, {
    headers: { Authorization: authHeader(username, password) }
  });
  if (!res.ok) {
    throw new Error("Neispravni admin podaci ili greska na serveru.");
  }
  return res.json();
}

export async function fetchContainerSas(username: string, password: string): Promise<ContainerSasResponse> {
  const res = await fetch(`${API_BASE_URL}/api/gallery/download-info`, {
    headers: { Authorization: authHeader(username, password) }
  });
  if (!res.ok) {
    throw new Error("Neispravni admin podaci ili greska na serveru.");
  }
  return res.json();
}
