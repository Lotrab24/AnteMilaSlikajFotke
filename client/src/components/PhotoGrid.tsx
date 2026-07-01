import { PhotoInfo } from "../api";

interface Props {
  photos: PhotoInfo[];
}

export default function PhotoGrid({ photos }: Props) {
  if (photos.length === 0) {
    return <p>Jos nema poslanih fotografija.</p>;
  }

  return (
    <div className="photo-grid">
      {photos.map((photo) => (
        <a key={photo.blobName} href={photo.url} target="_blank" rel="noreferrer" className="photo-grid__item">
          <img src={photo.url} alt={photo.blobName} loading="lazy" />
        </a>
      ))}
    </div>
  );
}
