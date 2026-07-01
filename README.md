# Ante & Mila - vjenčanje, gost-fotografije

Arhitektura: React (Vite) frontend + .NET 8 izolirani Azure Functions API, oboje hostano
zajedno na **Azure Static Web Apps** (besplatni tier, uvijek aktivno, bez CORS-a jer su
frontend i API na istoj domeni). Slike idu direktno u **Azure Blob Storage** preko SAS
tokena (backend ih nikad ne prima kroz sebe), a brojač od 10 slika po gostu čuva se u
**Azure Table Storage**.

## Struktura

- `client/` - React + Vite + TypeScript frontend
- `api/` - .NET 8 izolirani Azure Functions (upload endpointi + admin endpointi)
- `staticwebapp.config.json` - SPA routing za Static Web Apps

## Kako radi upload

1. Gost otvori stranicu, dobije `guestId` (UUID u `localStorage`).
2. Klikne "Slikaj i podijeli" -> otvara se nativna kamera aplikacija telefona
   (`<input type="file" capture="environment">`), bez gubitka kvalitete.
3. Frontend zove `POST /api/photos/request-upload` -> backend provjeri brojač u Table
   Storageu, ako je < 10 vrati kratkotrajni (15 min) SAS URL za upload jedne datoteke.
4. Frontend šalje originalnu datoteku direktno u Blob Storage (PUT na SAS URL) -
   backend nikad ne prima bajtove slike.
5. Frontend zove `POST /api/photos/confirm-upload` -> backend provjeri da blob
   stvarno postoji, atomski poveća brojač i zabilježi IP/User-Agent kao slabu
   naznaku (ne kao blokadu - gosti na istom venue WiFi-u dijele IP).

## Admin panel (`/admin`)

- Basic Auth (korisničko ime/lozinka iz environment varijabli `AdminUsername` /
  `AdminPassword` - **nikad u appsettings.json koji ide u git**).
- Grid svih fotografija (SAS read linkovi, vrijede 4h).
- Gumb "Preuzmi sve" generira SAS link na cijeli container + gotovu AzCopy naredbu.
  Namjerno nema custom "streaming ZIP" endpointa - kod izoliranog Functions modela
  se HTTP odgovor baferira u memoriji prije slanja, pa bi ZIP od nekoliko tisuća
  slika (~6 GB) mogao srušiti funkciju na dan vjenčanja. AzCopy / Azure Storage
  Explorer su Microsoftovi alati napravljeni baš za bulk download i puno su
  pouzdaniji za taj scenarij.

## Postavljanje Azure resursa

1. Prijavi se na [Azure for Students](https://azure.microsoft.com/free/students/)
   preko fakultetskog maila - $100 kredita, 12 mjeseci, bez kartice.
2. Kreiraj **Storage Account** (Standard LRS je dovoljan).
   - Uključi **soft delete** na blob containeru (zaštita od slučajnog brisanja).
3. Kreiraj **Static Web App** (Free plan) preko Azure Portala, poveži GitHub repo:
   - App location: `/client`
   - Api location: `/api`
   - Output location: `dist`
4. U Static Web App -> Configuration -> Application settings dodaj:
   - `AzureWebJobsStorage` = connection string storage accounta
   - `AdminUsername`, `AdminPassword` = tvoji admin podaci

## Lokalni razvoj

Preduvjeti: Node 18+, .NET 8 SDK, [Azure Functions Core Tools v4](https://learn.microsoft.com/azure/azure-functions/functions-run-local),
[Azurite](https://learn.microsoft.com/azure/storage/common/storage-use-azurite) (lokalni storage emulator), opcionalno [SWA CLI](https://azure.github.io/static-web-apps-cli/).

```bash
# Terminal 1 - lokalni storage emulator
azurite

# Terminal 2 - API
cd api
cp local.settings.json.example local.settings.json
dotnet restore
func start

# Terminal 3 - frontend
cd client
npm install
npm run dev
```

Frontend na `http://localhost:5173` prosljeđuje `/api/*` pozive na `http://localhost:7071`
(vidi `client/vite.config.ts`).

## Nakon vjenčanja

- Ne oslanjaj se dugoročno samo na Azure kao jedinu kopiju - odmah nakon eventa
  pokreni AzCopy naredbu iz admin panela i skini sve slike lokalno (+ backup na
  drugom disku / cloudu).
