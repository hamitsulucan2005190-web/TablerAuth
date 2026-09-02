# Teslim özeti — TablerAuth

Staj çıktısı: e-posta/şifre ve dış hesaplarla giriş, JWT API, Admin/User yetkisi, sağlayıcı ayarlarının veritabanından okunması.

Ön yüz Razor MVC’dir. React / Angular yoktur. Facebook ve Microsoft yoktur (hesaplara girilemedi).

Nasıl çalıştırılır: [README.md](../README.md). Yol haritası: [PLAN.md](PLAN.md).

## Cookie ve JWT neden ikisi birden?

- **Sayfalar** (login, dashboard, Users): Identity cookie. Formla giriş cookie basar. Tarayıcı her sayfa isteğinde cookie gönderir.
- **API** (`POST /api/auth/login`, `POST /api/auth/refresh`, `GET /api/me`): `Authorization: Bearer …`. Access yaklaşık 15 dakika; refresh veritabanında hash’lenir, kullanınca yenilenir (rotate). Eski refresh tekrar kullanılırsa o kullanıcının bütün aktif refresh’leri iptal edilir.

Sayfa için cookie, API için JWT karışımı bilinçlidir. MVC formları cookie ile açık kalır; Postman / başka bir istemci Bearer ister.

## Rol ve permission

Kimlik doğrulama (authentication) “kimsin” sorusudur. Yetkilendirme (authorization) “bu sayfayı açabilir misin” sorusudur.

- Roller: **Admin** ve **User**. Users sayfasında Admin verilir / alınır. Son Admin alınamaz.
- İzinler ayrı tabloda tutulmaz. Kod `Admin` rolünü `users.manage`, `roles.view`, `providers.manage` claim’lerine çevirir. User rolü izin vermez.
- Menü bu claim’lere bakarak link gizler. Asıl kilit `[Authorize(Policy = …)]` — User `/Users` yazsa da 403 alır.

JWT access token hem `role` hem `permission` claim taşır. `GET /api/me` ikisini de döner.

## Neden DB satırı yetmez?

`IdentityProviders` tablosuna satır eklemek login butonu için gerekli ama yeterli değildir.

- **Google / GitHub / LinkedIn:** her biri kendi NuGet paketi ve handler ailesidir (OAuth options).
- **Generic OIDC:** ayrı paket (`OpenIdConnect`). Formda Kind = Oidc, **Authority** (discovery adresi, ör. `https://accounts.google.com`) zorunludur. Callback yolu da benzersiz olmalı (`/signin-oidc`).
- Secret tabloda durmaz; yalnızca User Secrets / env / Key Vault anahtarının adı (`ClientSecretKey`) durur. Secret yoksa buton çıkmaz.
- Kapalı satırın callback yolu handler listesinden düşer (`/signin-github` 404).

Aynı Google uygulamasıyla OIDC yolunu denemek: Kind Oidc, Authority `https://accounts.google.com`, ayrı dönüş adresi `/signin-oidc` (konsola da eklenir).

## Secret’lar

Client secret, JWT imza anahtarı, SMTP şifresi, connection string `appsettings.json`’a düz yazılmaz ve git’e gitmez. User Secrets.

## Demo (3–5 dakika)

1. Kayıt ol → giriş yap → Dashboard’da adın görünsün. Profile’da roller ve izinler.
2. User olarak `/Users` yaz → Access denied. Admin menüsü görünmesin.
3. Admin ile giriş → Users’ta Make Admin / Remove Admin. Roles’ta Admin’in üç izni, User’da tire. Sign-in providers’da Google’ı Turn off → login’de buton kaybolsun; Turn on ile geri gelsin.
4. Google (ve varsa GitHub / LinkedIn) ile gerçek hesapla giriş; Profile’da bağlı yöntem.
5. İsteğe bağlı: Add provider → Oidc, Authority, kendi callback. Konsola dönüş adresini ekle.
6. API: `POST /api/auth/login` JSON `{ "email", "password" }` → access + refresh. `GET /api/me` header `Authorization: Bearer <access>` → 200, `roles` ve `permissions`; tokensız → 401.

## Test

```bash
dotnet test
```

Token rotate / eski refresh reuse, son Admin’in alınamaması, rol→izin eşlemesi, secret anahtarına yapıştırılmış secret reddi.
