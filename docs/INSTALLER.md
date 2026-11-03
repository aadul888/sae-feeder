# Panduan Distribusi & Build Installer SAE Feeder

File executable installer mandiri untuk **SAE Feeder** telah berhasil dibuat dan siap didistribusikan ke komputer operator sekolah.

---

## 1. File Installer Siap Pakai

- **File**: `dist\SAE-Feeder-Setup.exe`
- **Ukuran**: ~8.76 MB
- **Arsitektur**: Windows 64-bit (Windows 10, Windows 11, Windows 7/8 x64)
- **Kemandirian**: 100% Standalone (sudah memuat runtime PHP 8.3 portabel teroptimasi). Komputer target **TIDAK PERLU** menginstal Laragon, Composer, atau PHP sebelumnya.

---

## 2. Fitur Unggulan Installer & Launcher

1. **Wizard Instalasi Visual (GUI)**:
   - Header bertema gelap modern dengan Logo Resmi SAE.
   - Pilihan direktori tujuan (default `%LOCALAPPDATA%\SAE-Feeder` yang bebas hak administrator UAC, atau folder custom seperti `C:\SAE-Feeder`).
   - Opsi pembuatan shortcut di **Desktop** dan **Start Menu**.
   - **Proteksi Konfigurasi**: Berkas `dynamic_config.json` tidak akan tertimpa saat pengguna melakukan update atau reinstall sehingga token Dapodik dan API key SAE tetap aman.
   - Opsi otomatis *"Jalankan SAE Feeder setelah selesai"*.
2. **Native Launcher (`SAE-Feeder.exe`)**:
   - Berjalan di background tanpa jendela console hitam (CMD prompt).
   - Menjalankan server PHP built-in di port `4215` (`http://localhost:4215`).
   - Otomatis membuka peramban web (browser default) ke antarmuka SAE Feeder.
   - Ikon baki sistem (**System Tray**) di pojok kanan bawah taskbar Windows dengan menu:
     - 🌐 **Buka di Browser**
     - 🔄 **Restart Server**
     - 📁 **Buka Folder Aplikasi**
     - ❌ **Keluar** (menghentikan server PHP secara bersih tanpa menyisakan proses yatim/orphan).
3. **Uninstaller Resmi Windows (`Uninstall.exe`)**:
   - Terdaftar secara otomatis di Windows **Settings > Apps > Installed apps** (atau *Programs and Features*).
   - Memastikan proses server dihentikan, menghapus shortcut Desktop/Start Menu, menghapus entri registry, dan membersihkan folder aplikasi.

---

## 3. Cara Membangun Ulang Installer (Rebuild)

Jika terdapat pembaruan kode PHP, CSS, atau aset pada `sae-feeder`, Anda dapat membuat ulang installer kapan saja dengan satu perintah:

```powershell
powershell -ExecutionPolicy Bypass -File c:\laragon\www\sae-feeder\build-installer.ps1
```

Skrip ini akan secara otomatis:
1. Memvalidasi dan mengemas runtime PHP portabel minimalis.
2. Mengompilasi `SAE-Feeder.exe` (Launcher) dan `Uninstall.exe`.
3. Memadatkan payload aplikasi ke dalam ZIP terenkapsulasi.
4. Mengompilasi `dist\SAE-Feeder-Setup.exe` baru via `csc.exe` bawaan Windows.

---

## 4. Update Aplikasi (Online, Tanpa Installer)

1. Buka SAE Feeder, klik tombol **Cek Update** di header.
2. Server (`update.php`) membandingkan `VERSION` lokal dengan `VERSION` branch `main`.
3. Jika ada versi lebih baru, muncul **Update Online Sekarang**. Klik untuk mengunduh `index.php`, `proses.php`, `safety.php`, `.htaccess`, aset CSS/JS, dan `VERSION` langsung dari repo.
4. File lama dicadangkan ke `backup/YYYYmmdd_HHMMSS/`. `dynamic_config.json` tidak tersentuh, jadi token Dapodik & API key SAE tetap aman.
5. Setelah selesai, klik **Muat Ulang Halaman**.

Catatan:
- Update online hanya untuk file aplikasi. Perubahan runtime PHP, Launcher, atau installer tetap butuh `SAE-Feeder-Setup.exe` (tautan *Installer (offline)* pada alert).
- Tombol update hanya berfungsi dari komputer server (remote address `127.0.0.1`).
- Jika tombol tidak muncul/hilang, terapkan manual: klik **Cek Update** → **Installer (offline)**.

## 5. Rilis Versi Baru (Maintainer)

Set token GitHub: `$env:GITHUB_TOKEN = "ghp_..."` lalu jalankan:

```powershell
powershell -ExecutionPolicy Bypass -File c:\laragon\www\sae-feeder\release.ps1 -Version 1.2.0 -Message "Perbaikan progres sinkronisasi"
```

Skrip akan: menaikkan `VERSION`, menyinkronkan versi Inno Setup, membangun installer, commit, tag `v1.2.0`, push `main` + tag, lalu membuat GitHub Release dan mengunggah installer bila `GITHUB_TOKEN` tersedia. Tanpa token, unggah manual di halaman Releases.

Untuk rilis online saja (tanpa ganti installer), cukup ubah `VERSION` dan push ke `main`; tombol **Update Online** di klien akan menariknya.

## 6. Opsi Alternatif: Inno Setup 6

Tersedia juga skrip Inno Setup standar di `sae-feeder-setup.iss` jika ingin dikompilasi menggunakan GUI Inno Setup Compiler.
Cukup buka file `sae-feeder-setup.iss` di Inno Setup dan tekan **Compile (Ctrl + F9)**.

## 7. Metode Portable (Tanpa Installer)

Untuk PC yang sudah menjalankan Dapodik, metode paling simpel: ekstrak `dist/SAE-Feeder-Portable.zip` ke folder mana saja, lalu jalankan `run_feeder.bat`.

- `run_feeder.bat` mencari PHP berurutan: `php\` di folder yang sama → `C:\Program Files (x86)\Dapodik\php\php.exe` → `C:\Program Files\Dapodik\php\php.exe` → `php` di PATH.
- Server berjalan di `http://localhost:4215` dan browser dibuka otomatis.
- Tidak butuh hak admin; hapus folder = uninstall.
- Build paket: `powershell -ExecutionPolicy Bypass -File build-portable.ps1` (tambah `-BundlePhp` untuk menyertakan runtime PHP bila tidak mau bergantung PHP Dapodik).

