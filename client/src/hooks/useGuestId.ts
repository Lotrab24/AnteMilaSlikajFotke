const STORAGE_KEY = "wedding_guest_id";

export function getGuestId(): string {
  let guestId = localStorage.getItem(STORAGE_KEY);
  if (!guestId) {
    guestId = crypto.randomUUID();
    localStorage.setItem(STORAGE_KEY, guestId);
  }
  return guestId;
}
