# TablerAuth

Staj projesi: e-posta/şifre ve dış hesaplarla (Google, GitHub, LinkedIn, generic OpenID Connect) giriş. MVC sayfaları cookie ile açık kalır; API JWT ister. Sağlayıcı ayarları veritabanından okunur; secret’lar tabloda durmaz. Admin rolü `users.manage` / `roles.view` / `providers.manage` izinlerine çevrilir; kilit backend policy’dedir.

Ön yüz Razor MVC’dir. React / Angular yoktur.

Teslim özeti: [docs/TESLIM.md](docs/TESLIM.md).

## Nasıl çalıştırılır

1. .NET 8 SDK ve Docker.

2. Veritabanı:

```bash
docker compose up -d
```

MSSQL `localhost:1433` üzerinde açılır. `sa` şifresi varsayılan olarak `LocalDev_ChangeMe1` (veya `MSSQL_SA_PASSWORD` ortam değişkeni).

3. Secret’lar (`src/TablerAuth.Web` için). Değerleri sohbete veya `appsettings.json`’a yazma:

```bash
cd src/TablerAuth.Web
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost,1433;Database=TablerAuth;User Id=sa;Password=LocalDev_ChangeMe1;TrustServerCertificate=True;Encrypt=True;"
dotnet user-secrets set "Jwt:SigningKey" "EN_AZ_32_BYTE_BIR_ANAHTAR"
dotnet user-secrets set "Seed:AdminPassword" "AdminSifren"
```

JWT anahtarı örneği:

```bash
python3 -c "import secrets,base64; print(base64.b64encode(secrets.token_bytes(64)).decode())"
```

4. Uygulama (migration açılışta uygulanır):

```bash
dotnet run --project src/TablerAuth.Web --launch-profile https
```

Tarayıcı: `https://localhost:7258` (Chrome). Safari HTTPS’te “güvenli bağlantı kurulamadı” derse `http://localhost:5059`.

Google / GitHub / LinkedIn, açtığın adresi birebir ister. Safari’de 5059 ile giriyorsan konsola HTTP adresini de ekle; yoksa `redirect_uri_mismatch` görürsün.

İlk admin: `admin@tabler.local` + User Secrets’taki `Seed:AdminPassword`.

## Hangi secret’lar

| Anahtar | Nerede | Ne işe yarar |
|---|---|---|
| `ConnectionStrings:DefaultConnection` | User Secrets | MSSQL |
| `Jwt:SigningKey` | User Secrets | Access token imzası (en az 32 byte) |
| `Seed:AdminPassword` | User Secrets | İlk Admin hesabı |
| `Authentication:Google:ClientSecret` | User Secrets | Google |
| `Authentication:GitHub:ClientSecret` | User Secrets | GitHub |
| `Authentication:LinkedIn:ClientSecret` | User Secrets | LinkedIn |
| `Authentication:Oidc:ClientSecret` | User Secrets | Generic OpenID Connect (formdaki anahtar adı farklı olabilir) |
| `Email:Smtp:User` | User Secrets | Şifre sıfırlama mailini gönderen Gmail adresi |
| `Email:Smtp:Password` | User Secrets | Gmail uygulama şifresi (hesap şifresi değil) |

İlgili ClientId (App ID) gizli değildir; Identity Providers ekranındaki forma yazılır. Tabloda secret’ın kendisi değil, yukarıdaki anahtar adı (`ClientSecretKey`) tutulur.

Konsolda kayıtlı dönüş adresleri — ikisini de ekle:

- `https://localhost:7258/signin-google` ve `http://localhost:5059/signin-google`
- `https://localhost:7258/signin-github` ve `http://localhost:5059/signin-github`
- `https://localhost:7258/signin-linkedin` ve `http://localhost:5059/signin-linkedin`
- Generic OIDC eklediysen o satırın Return path’i, örneğin `https://localhost:7258/signin-oidc` ve `http://localhost:5059/signin-oidc`

## Cookie, JWT ve permission

- **Sayfalar** (login, dashboard, admin): Identity cookie. Formla giriş cookie basar.
- **API** (`POST /api/auth/login`, `POST /api/auth/refresh`, `GET /api/me`): `Authorization: Bearer …`. Access ~15 dakika; refresh veritabanında hash’lenir ve kullanınca yenilenir (rotate). Token’sız `/api/me` → 401. Access token’da `role` ve `permission` claim’leri vardır.

Admin sayfaları `[Authorize(Policy = …)]` ile kilitlenir (`users.manage`, `roles.view`, `providers.manage`). Menü gizleme güvenlik değildir.

## Neden DB satırı yetmez?

Yeni bir Google kopyası eklemek satır yeter (aynı handler). GitHub veya LinkedIn ayrı paket ve handler ister; klasik OIDC discovery her sağlayıcıda çalışmaz.

Generic OpenID Connect ayrı handler’dır: admin formunda Kind = **Oidc**, Authority zorunlu (ör. `https://accounts.google.com`), callback benzersiz olmalı (`/signin-oidc`). Secret yine User Secrets’ta. Kapalı satırın callback yolu kayıtlı olmaz (`/signin-github` 404).

Facebook ve Microsoft yok: hesaplara girilemedi, üç hazır sağlayıcı + isteğe bağlı generic OIDC yeterli görüldü.

## Test

```bash
dotnet test
```

## Demo (3–5 dakika)

1. Kayıt ol → giriş yap → Dashboard’da adın görünsün. Profile’da roller ve izinler.
2. User olarak `/Users` yaz → 403 / Access denied. Admin menüsü görünmesin.
3. Admin ile giriş → Users’ta bir hesaba Make Admin / Remove Admin. Roles’ta Admin’in üç izni görünsün. Sign-in providers’da Google’ı Turn off → login’de buton kaybolsun; Turn on ile geri gelsin.
4. Google (ve varsa GitHub / LinkedIn) ile gerçek hesapla giriş; Profile’da bağlı yöntem görünsün.
5. API: `POST /api/auth/login` JSON `{ "email", "password" }` → access + refresh. `GET /api/me` header `Authorization: Bearer <access>` → 200 (`roles` + `permissions`); tokensız → 401.

Yol haritası: [docs/PLAN.md](docs/PLAN.md). Teslim: [docs/TESLIM.md](docs/TESLIM.md). Günlük raporlar: [docs/gunluk-raporlar](docs/gunluk-raporlar).
