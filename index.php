<?php

/**
 * SAE FEEDER - Frontend
 */

// Let the built-in PHP server serve real static assets directly; never bypass .php requests (breaks AJAX POSTs)
if (php_sapi_name() === 'cli-server') {
    $filePath = __DIR__ . parse_url($_SERVER['REQUEST_URI'], PHP_URL_PATH);
    if (is_file($filePath) && strtolower(pathinfo($filePath, PATHINFO_EXTENSION)) !== 'php') {
        return false;
    }
}

// Load backend logic
require_once __DIR__ . '/proses.php';

$config = new ConfigManager();
$dapodik_status = $dapodik_status ?? ['status' => false, 'sekolah' => '', 'npsn' => ''];
$sae_status = $sae_status ?? ['status' => false];

// Nilai form: token & API key disensor (***) saat sudah tersimpan agar tidak bocor di HTML
$dapodik_npsn_val  = $dapodik_status['status'] ? htmlspecialchars($config->get('dapodik.npsn'))      : '';
$dapodik_token_val = $dapodik_status['status'] ? '***'                                               : '';
$sae_url_val       = $sae_status['status']     ? htmlspecialchars($config->get('sae.base_url'))      : '';
$sae_key_val       = $sae_status['status']     ? '***'                                               : '';

// Versi aplikasi untuk cek update (dibaca dari file VERSION)
$app_version = trim(@file_get_contents(__DIR__ . '/VERSION') ?: '0.0.0');
$app_update_repo = 'aadul888/sae-feeder';
?>
<!DOCTYPE html>
<html lang="id">

<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>SAE Feeder — Dapodik Data Synchronizer</title>
    <link rel="icon" type="image/png" href="sae-logo.png">
    <link rel="preconnect" href="https://fonts.googleapis.com">
    <link rel="preconnect" href="https://fonts.gstatic.com" crossorigin>
    <link href="https://fonts.googleapis.com/css2?family=Plus+Jakarta+Sans:wght@400;500;600;700;800&display=swap" rel="stylesheet">
    <link href="https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.5.1/css/all.min.css" rel="stylesheet">
    <link rel="stylesheet" href="assets/css/loader.css">
</head>

<body>
    <div class="ambient-glow"></div>
    <div class="ambient-glow-2"></div>

    <div class="app-shell">
        <div class="compact-layout">

            <!-- Header -->
            <div class="app-header">
                <img src="sae-logo.png" alt="SAE Logo">
                <div class="titles">
                    <h2 class="section-title">SAE Feeder</h2>
                    <p class="text-muted">Sinkronisasi Data Dapodik ke SAE</p>
                </div>
                <button class="btn btn-outline-secondary btn-sm" id="btn-check-update" onclick="checkForUpdate()"
                    style="margin-left: auto; white-space: nowrap;" title="Versi <?php echo htmlspecialchars($app_version); ?>">
                    <i class="fas fa-cloud-download-alt"></i> Cek Update
                </button>
            </div>

            <!-- Status Bar -->
            <div class="status-bar">
                <span id="dapodik-status-badge" class="mini-badge <?php echo $dapodik_status['status'] ? 'success' : 'error'; ?>">
                    <i class="fas fa-database me-1"></i>Dapodik: <?php echo $dapodik_status['status'] ? 'Terhubung' : 'Terputus'; ?>
                </span>
                <span id="sae-status-badge" class="mini-badge <?php echo $sae_status['status'] ? 'success' : 'error'; ?>">
                    <i class="fas fa-cloud me-1"></i>SAE: <?php echo $sae_status['status'] ? 'Terhubung' : 'Terputus'; ?>
                </span>
            </div>

            <?php if ($dapodik_status['status'] && !empty($dapodik_status['sekolah'])): ?>
            <div class="school-alert">
                <i class="fas fa-school"></i>
                <strong><?php echo htmlspecialchars($dapodik_status['sekolah']); ?></strong>
                <?php if (!empty($dapodik_status['npsn'])): ?>
                <span style="color: var(--text-muted); margin-left: 0.35rem;">(NPSN: <?php echo htmlspecialchars($dapodik_status['npsn']); ?>)</span>
                <?php endif; ?>
            </div>
            <?php endif; ?>

            <!-- Config Result -->
            <div id="config-result" style="display: none;"></div>

            <!-- Form Grid: Dapodik & SAE side by side -->
            <div class="form-grid">
                <!-- Dapodik -->
                <div class="step-card">
                    <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 0.5rem;">
                        <div class="field-label" style="margin: 0;"><i class="fas fa-server me-1"></i>Dapodik WS</div>
                        <?php if ($dapodik_status['status']): ?>
                        <button class="btn btn-outline-secondary btn-sm" style="padding: 2px 8px; font-size: 0.72rem; border-radius: 6px; height: 24px;" onclick="toggleEditDapodik()">
                            <i class="fas fa-edit"></i> Edit
                        </button>
                        <?php endif; ?>
                    </div>
                    <div style="display: flex; flex-direction: column; gap: 0.45rem;">
                        <input type="text" class="form-control" id="dapodik_npsn"
                            value="<?php echo $dapodik_npsn_val; ?>"
                            placeholder="NPSN (mis: 20252031)"
                            <?php echo $dapodik_status['status'] ? 'disabled' : ''; ?>>
                        <div style="display: flex; gap: 0;">
                            <input type="text" class="form-control" id="dapodik_token"
                                value="<?php echo $dapodik_token_val; ?>"
                                placeholder="Token Web Service"
                                style="border-radius: 10px <?php echo !$dapodik_status['status'] ? '0 0 10px' : '10px'; ?> !important; flex: 1;"
                                <?php echo $dapodik_status['status'] ? 'disabled' : ''; ?>>
                            <?php if (!$dapodik_status['status']): ?>
                            <button class="btn btn-primary" id="btn_save_dapodik" onclick="testDapodikConnection()" style="border-radius: 0 10px 10px 0 !important; height: 38px; padding: 0 0.85rem; white-space: nowrap;">
                                <i class="fas fa-plug"></i>
                            </button>
                            <?php endif; ?>
                        </div>
                        <?php if ($dapodik_status['status']): ?>
                        <button class="btn btn-primary" id="btn_save_dapodik" onclick="testDapodikConnection()" style="display: none; height: 34px; font-size: 0.82rem; border-radius: 8px; padding: 0 0.85rem;">
                            <i class="fas fa-save me-1"></i>Simpan
                        </button>
                        <?php endif; ?>
                    </div>
                </div>

                <!-- SAE -->
                <div class="step-card">
                    <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 0.5rem;">
                        <div class="field-label" style="margin: 0;"><i class="fas fa-globe me-1"></i>Aplikasi SAE</div>
                        <?php if ($sae_status['status']): ?>
                        <button class="btn btn-outline-secondary btn-sm" style="padding: 2px 8px; font-size: 0.72rem; border-radius: 6px; height: 24px;" onclick="toggleEditSae()">
                            <i class="fas fa-edit"></i> Edit
                        </button>
                        <?php endif; ?>
                    </div>
                    <div style="display: flex; flex-direction: column; gap: 0.45rem;">
                        <input type="text" class="form-control" id="sae_base_url"
                            value="<?php echo $sae_url_val; ?>"
                            placeholder="http://localhost/sae"
                            <?php echo $sae_status['status'] ? 'disabled' : ''; ?>>
                        <div style="display: flex; gap: 0;">
                            <input type="text" class="form-control" id="sae_api_key"
                                value="<?php echo $sae_key_val; ?>"
                                placeholder="API Key SAE"
                                style="border-radius: 10px <?php echo !$sae_status['status'] ? '0 0 10px' : '10px'; ?> !important; flex: 1;"
                                <?php echo $sae_status['status'] ? 'disabled' : ''; ?>>
                            <?php if (!$sae_status['status']): ?>
                            <button class="btn btn-primary" id="btn_save_sae" onclick="testSaeConnection()" style="border-radius: 0 10px 10px 0 !important; height: 38px; padding: 0 0.85rem; white-space: nowrap;">
                                <i class="fas fa-plug"></i>
                            </button>
                            <?php endif; ?>
                        </div>
                        <?php if ($sae_status['status']): ?>
                        <button class="btn btn-primary" id="btn_save_sae" onclick="testSaeConnection()" style="display: none; height: 34px; font-size: 0.82rem; border-radius: 8px; padding: 0 0.85rem;">
                            <i class="fas fa-save me-1"></i>Simpan
                        </button>
                        <?php endif; ?>
                    </div>
                </div>
            </div>

            <!-- Sync Row -->
            <div class="sync-row">
                <button class="btn btn-primary btn-sync-main" type="button" id="btn-sync" onclick="startSync()"
                    <?php echo (!($dapodik_status['status'] && $sae_status['status'])) ? 'disabled' : ''; ?>>
                    <i class="fas fa-bolt"></i>Kirim Semua Data
                </button>
            </div>

            <!-- Alert hasil sinkronisasi (tetap tampil sampai ditutup) -->
            <div id="sync-alert" class="sync-alert" style="display: none;"></div>

            <!-- Progress (hidden by default) -->
            <div class="progress-shell progress-container" id="progress-shell" style="margin-bottom: 0.75rem; display: none;">
                <div class="progress">
                    <div class="progress-bar" role="progressbar" style="width: 0%"></div>
                </div>
                <div style="margin-top: 0.45rem; font-size: 0.78rem; text-align: center; color: var(--text-muted);">
                    <span id="progress-text">Mempersiapkan...</span>
                    <div id="progress-detail" class="mt-1" style="font-size: 0.72rem;"></div>
                    <div id="progress-breakdown" class="progress-breakdown"></div>
                </div>
            </div>

            <!-- Log Box -->
            <div class="log-card">
                <div class="log-header">
                    <span><i class="fas fa-terminal me-1"></i>Aktivitas Sinkronisasi</span>
                    <button onclick="clearLog()"><i class="fas fa-trash-alt me-1"></i>Hapus</button>
                </div>
                <div class="log-container" id="log-container" style="max-height: 130px;">
                    <div class="text-muted">Menunggu instruksi sinkronisasi...</div>
                </div>
            </div>

        </div>
    </div>

    <!-- Scripts -->
    <script>
        window.SAE_APP = {
            version: <?php echo json_encode($app_version); ?>,
            repo: <?php echo json_encode($app_update_repo); ?>
        };
    </script>
    <script src="assets/js/loader.js"></script>
</body>

</html>