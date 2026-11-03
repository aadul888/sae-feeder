# SAE Feeder

Aplikasi satu arah untuk mengambil data dari **Dapodik** (via WebService/token resmi) dan mengirimkannya ke **Aplikasi SAE**. Berjalan lokal di komputer operator sekolah, tanpa perlu Laragon/Composer/PHP terpasang.

- Repo: https://github.com/aadul888/sae-feeder
- Versi terpasang dibaca dari file [`VERSION`](VERSION)
- Data contoh Dapodik tidak disertakan (folder `get/` diabaikan)

## Metode Pemasangan

Tiga cara, pilih yang paling simpel untuk PC operator:

1. **Portable (tanpa install, disandingkan dengan PC Dapodik)** — unduh `dist/SAE-Feeder-Portable.zip`, ekstrak ke folder mana saja (mis. `C:\SAE-Feeder`), lalu klik dua kali `run_feeder.bat`. PHP otomatis diambil dari instalasi Dapodik (`C:\Program Files (x86)\Dapodik\php\php.exe`) atau dari PATH. Tidak ada installer, tidak ada registry, tidak perlu hak admin.
2. **Installer EXE** — `dist/SAE-Feeder-Setup.exe` untuk PC tanpa Dapodik/PHP (runtime PHP sudah dibundel).
3. **Tempel di webserver Dapodik** — salin folder aplikasi ke bawah `C:\Program Files (x86)\Dapodik\dataweb\apps\` dan akses lewat Apache Dapodik.

Semua metode membaca konfigurasi dari `dynamic_config.json` di folder aplikasi. Update aplikasi (PHP/JS/CSS) bisa dilakukan online dari tombol **Cek Update**, tanpa install ulang.

## Cara Pakai Cepat

1. Unduh `dist/SAE-Feeder-Setup.exe`, jalankan. Default install di `%LOCALAPPDATA%\SAE-Feeder`.
2. Launcher berjalan di tray dan membuka `http://localhost:4215`.
3. Isi **NPSN**, **Token Web Service Dapodik**, **URL SAE**, dan **API Key SAE** di halaman.
4. Klik **Kirim Semua Data**. Progres per jenis data, alert hasil, dan log tampil di halaman.

Token dan API key disensor (`***`) setelah tersimpan dan tidak pernah dirender ke HTML.

## Update

- **Online (tanpa installer):** klik **Cek Update** di header → **Update Online Sekarang**. File aplikasi (PHP/JS/CSS) diunduh langsung dari repo, versi lama dicadangkan ke `backup/`. `dynamic_config.json` tidak tersentuh.
- **Installer:** tautan *Installer (offline)* pada alert, untuk perubahan runtime PHP/Launcher.

Detail: [docs/INSTALLER.md](docs/INSTALLER.md).

## Struktur

```
index.php          UI
proses.php         logika sync Dapodik -> SAE
safety.php         rate limit, circuit breaker, validasi token
update.php         updater online (khusus localhost)
VERSION            versi aplikasi
assets/            CSS & JS UI
installer_builder/ sumber C# install menu, Launcher, Uninstaller
dist/              installer jadi (SAE-Feeder-Setup.exe)
docs/              dokumentasi
build-installer.ps1 / build-portable.ps1 / release.ps1 / verify_build.ps1   skrip build & rilis
```

## Build & Rilis (Maintainer)

```powershell
# build installer saja
powershell -ExecutionPolicy Bypass -File build-installer.ps1

# paket portable (tanpa installer)
powershell -ExecutionPolicy Bypass -File build-portable.ps1
# sertakan runtime PHP di dalam zip
powershell -ExecutionPolicy Bypass -File build-portable.ps1 -BundlePhp

# rilis versi baru: bump VERSION, build, commit, tag, push, (opsional) upload Release
$env:GITHUB_TOKEN = "ghp_xxx"
powershell -ExecutionPolicy Bypass -File release.ps1 -Version 1.2.0 -Message "Catatan rilis"
```

## Keamanan

- `dynamic_config.json` (token & API key asli) tidak dilacak git; jangan pernah di-commit.
- `update.php` menolak request selain dari `127.0.0.1`.
- Endpoint sync dibatasi rate limit dan circuit breaker (lihat [docs/SAFETY.md](docs/SAFETY.md)).
