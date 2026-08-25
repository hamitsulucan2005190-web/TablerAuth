# TablerAuth — 8 Fazlı Öğrenme Planı

Staj projesi: kullanıcılar hem e-posta/şifre hem de dış sağlayıcılarla (Google, Microsoft, GitHub, LinkedIn) giriş yapabilsin. JWT, rol bazlı menü, sağlayıcı ayarları mümkün olduğunca veritabanından gelsin.

Bu dosya yol haritasıdır. Her fazda sırayla: **neden** → **hangi dosya ne işe yarar** → **kod** → **nasıl test edilir**. Bir fazın başarı kriteri dolmadan sonrakine geçilmez.

**Ön yüz kararı (kilitli):** Razor MVC (`.cshtml`). React/Angular yok.

**4. dış giriş:** LinkedIn. Google, Microsoft ve GitHub sabit; dördüncü LinkedIn.

---

## Nasıl öğreneceğiz?

Her önemli adımdan sonra üç soru:

1. Ne değişti? (hangi dosyalar)
2. Bu dosya neden var?
3. Nasıl test ederiz?

Controller'lara iş mantığı yazılmaz; servislere delege edilir. Secret (şifre, ClientSecret, connection string) `appsettings.json`'a düz yazılmaz, git'e commit edilmez.

### Günlük rapor

Her günün sonunda `docs/gunluk-raporlar/YYYY-MM-DD.md` yazılır. Şablon: `docs/gunluk-raporlar/SABLON.md`. Üç başlık zorunlu:

1. Ne yapmayı planladın?
2. Hangi sorunlar ile karşılaştın?
3. Bu sorunları nasıl çözdün?

Kısa tut (yarım sayfa yeter).

---

## Klasör yapısı — hangi klasör ne işe yarar?

```
tabler.io/
  TablerAuth.sln                 Çözüm: dört projeyi bir arada derler
  src/
    TablerAuth.Web/              Tarayıcının konuştuğu yer: sayfalar, controller, Program.cs
    TablerAuth.Application/      İş kuralları: login, token üret, DTO, servis arayüzleri
    TablerAuth.Domain/           "Ne": User, Role, IdentityProvider entity'leri (tablo karşılıkları)
    TablerAuth.Infrastructure/   "Nasıl": EF Core, MSSQL, Identity, JWT, OAuth handler'lar
  docs/
    PLAN.md                      Bu dosya
    gunluk-raporlar/             Günlük kısa raporlar
  docker-compose.yml             Yerel MSSQL (Faz 3'te kullanılır)
  .cursorrules                   Cursor'ın uyacağı proje kuralları
  .gitignore                     bin/obj/secret commit olmasın
```

**Neden katman?** Login butonuna basınca `AccountController` "kullanıcıyı kaydet" demeli; SQL yazmamalı. SQL Infrastructure'ın işi. Controller şişmesin, test edilebilir olsun.

Bağımlılık yönü (tersine referans yok):

```
Web → Application → Domain
Web → Infrastructure  (yalnızca Program.cs'te DI kaydı için)
Infrastructure → Domain
Infrastructure → Application (servis uygulamaları)
```

---

## Faz sırası

```
Faz 1 (çalışan MVC + secrets + git)
  → Faz 2 (Tabler sayfaları gerçek projede)
    → Faz 3 (Identity + MSSQL + kayıt/login cookie)
      → Faz 4 (JWT)  → Faz 5 (roller ve menü)
                    ↘ Faz 6 (yalnızca Google) → Faz 7 (dinamik + Microsoft, GitHub, LinkedIn)
                                                  → Faz 8 (temizlik + README + teslim)
```

JWT'yi ilk günden koymuyoruz: hem Identity hem token hem OAuth aynı anda karışır. Cookie ile "kullanıcı kim?" otursun, sonra token, sonra tek Google, en sonda dinamik sağlayıcılar.

---

## Faz 1 — Ortam ve altyapı

**Öğreneceğin kavramlar:** solution vs project, `Program.cs` (uygulamanın kapısı), User Secrets, `appsettings.json` vs secret.

**Ne yapılır**

- Wget ile inen Tabler sitesi aynası silinir; tutulan: Tabler CSS/JS ve Razor taslakları.
- `dotnet new` ile 4 katmanlı çözüm; Web MVC ayağa kalkar.
- Connection string User Secrets'ta; `appsettings.json`'a düz yazılmaz.
- Repo kökünde `.cursorrules` ve `.gitignore`.
- Bu klasörde git; GitHub remote.

**Hangi dosya ne işe yarar?**

| Dosya | Görevi |
|---|---|
| `TablerAuth.sln` | Projeleri bir arada tutan çözüm |
| `*.csproj` | Paketler ve derleme ayarı |
| `Program.cs` | Uygulama açılınca ne yüklenecek (MVC, ileride auth/DB) |
| `appsettings.json` | Gizli olmayan ayarlar (log seviyesi, token süresi) |
| User Secrets | Connection string, ClientSecret — makinede kalır, git'e gitmez |
| `.gitignore` | `bin/`, `obj/`, `*.user`, secret dosyaları commit olmasın |
| `docker-compose.yml` | Yerel SQL Server konteyneri (Faz 3'te `update-database` için) |

**Başarı kriteri:** `dotnet run` tarayıcıda açılır; secret'ta connection string anahtarı vardır; GitHub bağlıdır. MSSQL bu fazda bağlanmak zorunda değildir (DbContext Faz 3).

**Senin yapman gerekenler:** Cursor'da Continue with GitHub; gerçek connection string'i sohbete yapıştırmadan secrets'a eklemek.

---

## Faz 2 — Tabler UI

**Öğreneceğin kavramlar:** Razor View, Layout, `wwwroot`, Tag Helper (`asp-controller`).

Kökte duran taslaklar Faz 2'de `src/TablerAuth.Web/Views` altına taşınır. Formlar henüz DB'ye yazmaz.

**Hangi dosya ne işe yarar?**

| Dosya | Görevi |
|---|---|
| `_ViewStart.cshtml` | Her sayfa varsayılan olarak `_Layout` kullansın |
| `_ViewImports.cshtml` | `asp-controller` gibi Tag Helper'ları açar |
| `_Layout.cshtml` | Tek iskelet; içerik `@RenderBody()` ile gelir |
| `wwwroot/` | Tarayıcının indirdiği statik dosyalar (CSS/JS) |
| `HomeController` | `/` adresine hangi view'ın gideceğini söyler |
| `Login.cshtml` / `Register.cshtml` | Görsel formlar; POST henüz Identity'ye bağlı değil |

Layout'taki `@if (User.IsInRole("Admin"))` taslağı kalır; Identity yokken menü görünmez — bu beklenen.

**Başarı kriteri:** Login ve Register Tabler görünümüyle açılır; submit "kayıt oldu" demez.

---

## Faz 3 — Identity ile kayıt / login

**Öğreneceğin kavramlar:** ASP.NET Identity, `DbContext`, migration, password hash, cookie auth, anti-forgery token.

**Ne yapılır**

- Identity paketleri + `ApplicationDbContext`
- `dotnet ef migrations add InitialCreate` → `dotnet ef database update`
- `AccountController`: Register POST kullanıcı yaratsın, Login POST cookie yazsın
- Seed: `Admin` / `User` rolleri + bir admin kullanıcı

**Hangi dosya ne işe yarar?**

| Dosya | Görevi |
|---|---|
| `ApplicationUser` | Identity User tablosuna ekstra alan (ad) |
| `ApplicationDbContext` | C# sınıflarını SQL tablolarına bağlar |
| `Migrations/` | Şema geçmişi; elle SQL yazılmaz |
| `AccountController` | Form POST'unu karşılar, servise delege eder |
| `DataSeeder` | İlk rolleri ve admin'i ekler |

Kendi hash fonksiyonu yazılmaz. Identity'nin hasher'ı kullanılır.

**Başarı kriteri:** Kayıt ol → login ol → Dashboard'da adın görünür. JWT henüz yok; cookie yeterli.

---

## Faz 4 — JWT

**Öğreneceğin kavramlar:** access vs refresh token, Bearer header, claim, token rotation, `[Authorize]`.

**Ne yapılır**

- `ITokenService`: kısa ömürlü access (~15 dk) + DB'de refresh (rotate)
- `Program.cs`'e JWT Bearer middleware
- Login başarılı olunca token dön
- `[Authorize]`'lı test endpoint — Postman'de `Authorization: Bearer ...`

**Hangi dosya ne işe yarar?**

| Dosya | Görevi |
|---|---|
| `TokenService` | JWT imzalar, refresh üretir/döndürür |
| `RefreshToken` entity | Eski token'ı geçersiz kılmak için DB kaydı |
| JWT middleware | Her istekte imza ve süreyi doğrular |
| API login/refresh uçları | JSON token döner |

**Cookie vs JWT (bilinçli ayrım):** MVC sayfaları cookie ile açık kalabilir; API uçları Bearer ister. "Sayfa için cookie, API için JWT" karışımı stajda sık sorulur; Faz 8 README'sinde yazılacak.

**Başarı kriteri:** Login token verir; korumalı endpoint tokensız 401, tokenla 200.

---

## Faz 5 — Rol ve menü

**Öğreneceğin kavramlar:** authentication ≠ authorization. Frontend'de menüyü gizlemek güvenlik değildir.

Layout taslağı zaten `@if (User.IsInRole("Admin"))` kullanır. Asıl iş: Users / Roles / Identity Providers sayfalarını `[Authorize(Roles = "Admin")]` ile kilitlemek. User rolündeki biri URL'yi elle yazsa da 403 almalı.

**Başarı kriteri:** Admin menüyü görür; User görmez; User `/Users` yoluna gitse backend reddeder.

---

## Faz 6 — Yalnızca Google OIDC (en kritik öğrenme)

**Öğreneceğin kavramlar:** OAuth2 authorization code, redirect URI, ClientId/Secret, harici login → yerel kullanıcı eşleme.

Sırayı bozma: Google bitmeden Microsoft / GitHub / LinkedIn yok.

**Akış**

1. "Google ile giriş" → Google hesap seçimi
2. Google uygulamayı callback'e döndürür (`signin-google`)
3. Identity harici logini `AspNetUsers` kaydına bağlar
4. Uygulama cookie ve/veya JWT basar

ClientId / ClientSecret User Secrets'a gider; sohbete yapıştırılmaz. Google Cloud Console'da OAuth client sen oluşturursun.

**Başarı kriteri:** Google hesabıyla uçtan uca giriş.

---

## Faz 7 — Sağlayıcıları dinamikleştir

**Öğreneceğin kavramlar:** `IAuthenticationSchemeProvider`; "DB'ye satır eklemek her zaman yeterli değil."

`IdentityProviders` tablosu (özet): `DisplayName`, `Scheme`, `ClientId`, `ClientSecret` (secret store referansı, düz metin tercih değil), `Authority`, `Scopes`, `Enabled`.

Sonra aynı yapıya Microsoft, GitHub ve **LinkedIn** eklenir.

Dürüst not (README'ye de yazılacak): GitHub ve LinkedIn klasik OIDC discovery ile her zaman uymaz; Google/Microsoft OIDC'ye daha yakındır. Model "DB + handler tipi"dir, sihirli tek handler değil. Enabled=false olan buton görünmez.

**Başarı kriteri:** Dört sağlayıcı gerçek hesapla giriş; kapalı olan listede yok.

---

## Faz 8 — Temizlik, test, dokümantasyon

- Kullanılmayan Tabler/demo kalıntısı
- README: nasıl çalıştırılır, hangi secret'lar, migration
- Uçtan uca: kayıt, şifre login, 4 sağlayıcı, rol menü
- Teslim özeti + 3–5 dakikalık demo tıklama listesi

---

## Yapılmayacaklar

- Client secret'ı `appsettings.json`'a düz metin yazmak
- Tek "God Controller"
- Sadece frontend'de rol gizlemek
- Migration'ı elle SQL ile atlamak
- Kendi şifre hash fonksiyonunu yazmak
- Faz 6 bitmeden diğer OAuth sağlayıcılarına geçmek
