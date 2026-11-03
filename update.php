<?php

/**
 * SAE Feeder - Online Updater
 * Update source aplikasi langsung dari repo GitHub, tanpa installer/exe.
 * Hanya bisa dijalankan dari komputer server (localhost).
 */

header('Content-Type: application/json');

$repo    = 'aadul888/sae-feeder';
$branch  = 'main';
$rawBase = "https://raw.githubusercontent.com/$repo/$branch/";
$appDir  = __DIR__;

// File yang disinkronkan. Update.php sendiri tidak diikutkan agar aman saat berjalan.
$manifest = [
    'index.php',
    'proses.php',
    'safety.php',
    '.htaccess',
    'favicon.png',
    'sae-logo.png',
    'assets/css/loader.css',
    'assets/js/loader.js',
    'VERSION'
];

function json_out($data)
{
    echo json_encode($data);
    exit;
}

function http_get($url)
{
    $ch = curl_init($url);
    curl_setopt_array($ch, [
        CURLOPT_RETURNTRANSFER => true,
        CURLOPT_FOLLOWLOCATION => true,
        CURLOPT_TIMEOUT        => 60,
        CURLOPT_CONNECTTIMEOUT => 15,
        CURLOPT_SSL_VERIFYPEER => false,
        CURLOPT_SSL_VERIFYHOST => false,
        CURLOPT_USERAGENT      => 'SAE-Feeder-Updater/1.0'
    ]);
    $body = curl_exec($ch);
    $code = curl_getinfo($ch, CURLINFO_HTTP_CODE);
    $err  = curl_error($ch);
    curl_close($ch);
    return [$code, $body, $err];
}

// Proteksi: hanya dari komputer tempat server berjalan
$ip = $_SERVER['REMOTE_ADDR'] ?? '';
if (!in_array($ip, ['127.0.0.1', '::1', '::ffff:127.0.0.1'], true)) {
    json_out(['success' => false, 'message' => 'Update hanya bisa dijalankan dari komputer server SAE Feeder.']);
}

$action       = $_REQUEST['action'] ?? 'check';
$localVersion = trim(@file_get_contents($appDir . '/VERSION') ?: '0.0.0');
$cacheBuster  = '?t=' . time();

if ($action === 'check') {
    [$code, $body, $err] = http_get($rawBase . 'VERSION' . $cacheBuster);
    if ($code !== 200 || $body === false) {
        json_out(['success' => false, 'message' => 'Gagal cek versi terbaru.' . ($err ? ' ' . $err : '')]);
    }
    $latest = trim($body);
    json_out([
        'success'          => true,
        'current'          => $localVersion,
        'latest'           => $latest,
        'update_available' => version_compare($latest, $localVersion, '>')
    ]);
}

if ($action === 'apply') {
    if (($_SERVER['REQUEST_METHOD'] ?? '') !== 'POST') {
        json_out(['success' => false, 'message' => 'Gunakan metode POST.']);
    }

    [$code, $body, $err] = http_get($rawBase . 'VERSION' . $cacheBuster);
    if ($code !== 200 || $body === false) {
        json_out(['success' => false, 'message' => 'Gagal mengambil VERSION terbaru.' . ($err ? ' ' . $err : '')]);
    }
    $latest = trim($body);
    if (!version_compare($latest, $localVersion, '>')) {
        json_out(['success' => true, 'message' => 'Sudah versi terbaru.', 'version' => $localVersion]);
    }

    $backupDir = $appDir . '/backup/' . date('Ymd_His');
    @mkdir($backupDir, 0777, true);

    $updated = [];
    $failed  = [];

    foreach ($manifest as $rel) {
        if ($rel === 'VERSION') continue; // ditulis terakhir setelah semua sukses

        [$c, $content] = http_get($rawBase . $rel . $cacheBuster);
        if ($c !== 200 || $content === false || $content === '') {
            $failed[] = $rel;
            continue;
        }

        $target = $appDir . '/' . $rel;
        @mkdir(dirname($target), 0777, true);

        if (is_file($target)) {
            @copy($target, $backupDir . '/' . str_replace(['/', '\\'], '_', $rel));
        }

        $tmp = $target . '.new';
        if (@file_put_contents($tmp, $content) === false || !@rename($tmp, $target)) {
            @unlink($tmp);
            $failed[] = $rel;
            continue;
        }
        $updated[] = $rel;
    }

    if (empty($failed)) {
        @file_put_contents($appDir . '/VERSION', $latest);
        @unlink($appDir . '/sync_progress.json');
    }

    if (function_exists('opcache_reset')) {
        @opcache_reset();
    }

    json_out([
        'success' => empty($failed),
        'message' => empty($failed) ? "Berhasil update ke v$latest" : 'Sebagian file gagal diunduh/ditulis.',
        'version' => empty($failed) ? $latest : $localVersion,
        'updated' => $updated,
        'failed'  => $failed,
        'backup'  => str_replace('\\', '/', substr($backupDir, strlen($appDir) + 1))
    ]);
}

json_out(['success' => false, 'message' => 'Action tidak dikenal.']);
