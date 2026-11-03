let syncInProgress = false;
let syncHasStarted = false;
let syncInterval = null;
let progressPoll = null;

// Fungsi untuk menambah log
function addLog(message, type = "info") {
  const logContainer = document.getElementById("log-container");
  const timestamp = new Date().toLocaleString();
  const logClass =
    type === "error"
      ? "text-danger"
      : type === "success"
        ? "text-success"
        : "text-info";

  const logEntry = document.createElement("div");
  logEntry.innerHTML = `<span class="text-muted">[${timestamp}]</span> <span class="${logClass}">${message}</span>`;

  logContainer.appendChild(logEntry);
  logContainer.scrollTop = logContainer.scrollHeight;
}

// Fungsi untuk membersihkan log
function clearLog() {
  document.getElementById("log-container").innerHTML =
    '<div class="text-muted">Log dibersihkan...</div>';
}

function escapeHtml(value) {
  return String(value ?? "").replace(/[&<>"']/g, function (c) {
    return { "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" }[c];
  });
}

// Alert hasil sinkronisasi: tidak hilang sendiri, hanya ditutup manual.
// actionsHtml: HTML internal (bukan data pengguna) untuk tombol aksi.
function showSyncAlert(type, message, details, actionsHtml) {
  const box = document.getElementById("sync-alert");
  if (!box) return;
  const cls = type === "success" ? "alert-success" : type === "error" ? "alert-danger" : "alert-info";
  const icon = type === "success" ? "check-circle" : type === "error" ? "times-circle" : "info-circle";
  let html =
    '<div class="alert ' + cls + '" style="display:flex; gap:.55rem; align-items:flex-start;">' +
    '<i class="fas fa-' + icon + '" style="margin-top:.2rem;"></i>' +
    '<div style="flex:1; min-width:0;">' +
    '<div style="font-weight:700;">' + escapeHtml(message) + "</div>" +
    (details ? '<div style="font-size:.76rem; opacity:.85; margin-top:.25rem;">' + escapeHtml(details) + "</div>" : "") +
    (actionsHtml || "") +
    "</div>" +
    '<button type="button" aria-label="Tutup" onclick="document.getElementById(\'sync-alert\').style.display=\'none\'" ' +
    'style="background:none;border:none;color:inherit;cursor:pointer;font-size:1rem;line-height:1;padding:0;">&times;</button>' +
    "</div>";
  box.innerHTML = html;
  box.style.display = "block";
}

function actionLink(href, label) {
  if (!/^https:\/\//i.test(href)) href = "#";
  return (
    '<a href="' + escapeHtml(href) + '" target="_blank" rel="noopener" ' +
    'style="display:inline-block;margin:.4rem .6rem 0 0;font-weight:700;color:inherit;text-decoration:underline;">' +
    escapeHtml(label) + "</a>"
  );
}

function actionButton(onclick, label) {
  return (
    '<button type="button" onclick="' + escapeHtml(onclick) + '" ' +
    'style="display:inline-block;margin:.4rem .6rem 0 0;padding:.4rem .9rem;border:0;border-radius:8px;' +
    'background:#2563eb;color:#fff;font-weight:700;line-height:1.4;cursor:pointer;">' + escapeHtml(label) + "</button>"
  );
}

// Cek update online: server (update.php) yang membandingkan VERSION dengan repo.
function checkForUpdate() {
  const btn = document.getElementById("btn-check-update");
  if (btn) btn.disabled = true;

  showSyncAlert("info", "Memeriksa update...", "Menghubungi repo GitHub.");

  fetch("update.php?action=check", { cache: "no-store" })
    .then(function (r) {
      return r.json();
    })
    .then(function (data) {
      if (!data || !data.success) {
        throw new Error((data && data.message) || "Respons tidak valid");
      }
      const current = data.current || (window.SAE_APP && window.SAE_APP.version) || "0.0.0";
      const latest = data.latest || current;

      if (!data.update_available) {
        showSyncAlert("success", "SAE Feeder sudah versi terbaru (v" + current + ").");
        return;
      }

      const repo = (window.SAE_APP && window.SAE_APP.repo) || "aadul888/sae-feeder";
      const installerUrl = "https://github.com/" + repo + "/raw/main/dist/SAE-Feeder-Setup.exe";
      showSyncAlert(
        "info",
        "Update tersedia: v" + latest + " (terpasang v" + current + ").",
        "Update online hanya mengubah file aplikasi (PHP/JS/CSS). Token Dapodik dan API key SAE tidak tersentuh. Gunakan installer hanya jika diminta.",
        actionButton("applyOnlineUpdate()", "Update Online Sekarang") +
          actionLink(installerUrl, "Installer (offline)"),
      );
    })
    .catch(function (err) {
      showSyncAlert("error", "Gagal memeriksa update.", String((err && err.message) || err));
    })
    .then(function () {
      if (btn) btn.disabled = false;
    });
}

// Terapkan update online: unduh file sumber dari repo, cadangkan versi lama.
function applyOnlineUpdate() {
  if (syncInProgress) {
    showSyncAlert("error", "Sinkronisasi sedang berjalan.", "Tunggu sampai selesai sebelum update.");
    return;
  }
  showSyncAlert("info", "Mengunduh update...", "Jangan tutup halaman ini.");

  fetch("update.php?action=apply", { method: "POST" })
    .then(function (r) {
      return r.json();
    })
    .then(function (data) {
      if (data && data.success) {
        showSyncAlert(
          "success",
          data.message || "Update berhasil.",
          "Cadangan versi lama: " + (data.backup || "backup/") + ". Muat ulang halaman untuk memakai versi baru.",
          actionButton("location.reload()", "Muat Ulang Halaman"),
        );
      } else {
        showSyncAlert("error", "Update gagal.", (data && data.message) || "Tidak ada respons");
      }
    })
    .catch(function (err) {
      showSyncAlert("error", "Update gagal.", String((err && err.message) || err));
    });
}

function stopProgressPoll() {
  if (progressPoll) {
    clearInterval(progressPoll);
    progressPoll = null;
  }
}

function renderBreakdown(progress) {
  const box = document.getElementById("progress-breakdown");
  if (!box) return;
  const list = Array.isArray(progress.data_breakdown) ? progress.data_breakdown : [];
  if (!list.length) {
    box.innerHTML = "";
    return;
  }
  const active = progress.stage === "fetching" || progress.stage === "fetched";
  box.innerHTML = list
    .map(function (item) {
      const failed = item.error === true;
      const current = !failed && active && item.endpoint === progress.current_endpoint;
      const icon = failed
        ? '<i class="fas fa-times-circle"></i>'
        : current
          ? '<i class="fas fa-spinner fa-spin"></i>'
          : '<i class="fas fa-check-circle"></i>';
      const state = failed ? "error" : current ? "current" : "done";
      return (
        '<div class="progress-item ' + state + '">' +
        "<span>" + icon + " " + escapeHtml(item.label || item.endpoint) + "</span>" +
        "<span>" + (failed ? "gagal" : Number(item.count || 0) + " data") + "</span>" +
        "</div>"
      );
    })
    .join("");
}

// Poll progress from server
function pollProgress() {
  if (!syncInProgress) return;
  fetch("index.php?action=sync_progress")
    .then((r) => r.json())
    .then((data) => {
      if (!data.success || !data.progress) return;
      const p = data.progress;

      // Ignore stale 'done' or 'error' state from previous run before new run starts
      if ((p.stage === "done" || p.stage === "error") && !syncHasStarted) {
        return;
      }

      if (
        p.stage === "starting" ||
        p.stage === "fetching" ||
        p.stage === "fetched" ||
        p.stage === "pushing"
      ) {
        syncHasStarted = true;
      }

      const progressContainer = document.getElementById("progress-shell");
      const progressBar = document.querySelector(".progress-bar");
      const progressText = document.getElementById("progress-text");
      const detailText = document.getElementById("progress-detail");

      if (progressContainer) progressContainer.style.display = "block";

      // Calculate percentage
      let pct = 0;
      if (p.stage === "fetching" || p.stage === "fetched") {
        // During fetch: ep_index / total_endpoints * 80%
        pct = Math.round((p.ep_index / p.total_endpoints) * 80);
      } else if (p.stage === "pushing") {
        pct = 90;
      } else if (p.stage === "done") {
        pct = 100;
      } else if (p.stage === "error") {
        pct = 100;
      }

      progressBar.classList.remove("bg-danger");
      progressBar.style.background =
        p.stage === "error"
          ? "linear-gradient(90deg, #ef4444 0%, #dc2626 100%)"
          : p.stage === "done"
            ? "linear-gradient(90deg, #10b981 0%, #059669 100%)"
            : "";
      progressBar.style.width = pct + "%";
      progressText.textContent = p.message;
      if (detailText) {
        if (p.stage === "fetching") {
          detailText.textContent =
            "Mengambil " +
            (p.endpoint_label || p.current_endpoint || "") +
            " (" +
            p.ep_index +
            "/" +
            p.total_endpoints +
            ")";
        } else if (p.stage === "fetched") {
          detailText.textContent =
            "Didapat " +
            p.rows_done +
            " data dari " +
            (p.endpoint_label || p.current_endpoint || "") +
            " (" +
            p.ep_index +
            "/" +
            p.total_endpoints +
            ")";
        } else if (p.stage === "pushing") {
          detailText.textContent =
            "Mengirim " + p.rows_total + " data ke SAE...";
        } else if (p.stage === "done") {
          detailText.textContent = p.rows_done + " data berhasil dikirim";
        } else if (p.stage === "error") {
          detailText.textContent = "Terjadi kesalahan";
        }
      }
      renderBreakdown(p);

      if (p.stage === "done" || p.stage === "error") {
        stopProgressPoll();
      }
    })
    .catch(() => {});
}

// Selesaikan proses tanpa reload halaman: alert tetap tampil sampai ditutup manual.
function finishSync(success, message, details) {
  syncInProgress = false;
  syncHasStarted = false;
  stopProgressPoll();

  const btnSync = document.getElementById("btn-sync");
  if (btnSync) {
    btnSync.disabled = false;
    btnSync.className = success
      ? "btn btn-success btn-sync-main"
      : "btn btn-outline-danger btn-sync-main";
    btnSync.innerHTML = success
      ? '<i class="fas fa-check-circle"></i>Selesai — Kirim Ulang'
      : '<i class="fas fa-redo"></i>Coba Lagi';
    btnSync.onclick = startSync;
  }

  showSyncAlert(success ? "success" : "error", message, details);
}

// Fungsi untuk memulai kirim data
function startSync() {
  if (syncInProgress) {
    addLog("Kirim Data sedang berlangsung!", "error");
    return;
  }

  const endpoint = "all";
  const btnSync = document.getElementById("btn-sync");
  const progressContainer = document.getElementById("progress-shell");
  const progressBar = document.querySelector(".progress-bar");
  const progressText = document.getElementById("progress-text");

  syncInProgress = true;
  syncHasStarted = false;
  btnSync.disabled = true;
  btnSync.className = "btn btn-primary btn-sync-main";
  btnSync.innerHTML = '<i class="fas fa-spinner fa-spin"></i>Mengirim Data...';
  const alertBox = document.getElementById("sync-alert");
  if (alertBox) {
    alertBox.style.display = "none";
    alertBox.innerHTML = "";
  }
  const breakdownBox = document.getElementById("progress-breakdown");
  if (breakdownBox) breakdownBox.innerHTML = "";
  const detailBox = document.getElementById("progress-detail");
  if (detailBox) detailBox.textContent = "";
  if (progressContainer) progressContainer.style.display = "block";
  progressBar.style.width = "0%";
  progressBar.className = "progress-bar";
  progressBar.style.background = "";
  progressText.textContent = "Memulai proses Kirim Data...";

  addLog(`Memulai kirim data endpoint: ${endpoint}`, "info");

  // Start polling progress
  stopProgressPoll();
  pollProgress();
  progressPoll = setInterval(pollProgress, 1500);

  // AJAX request untuk kirim data
  fetch("index.php?action=sync", {
    method: "POST",
    headers: {
      "Content-Type": "application/x-www-form-urlencoded",
    },
    body: `endpoint=${encodeURIComponent(endpoint)}`,
  })
    .then((response) => response.json())
    .then((data) => {
      progressBar.style.width = "100%";
      const detail = data.details
        ? typeof data.details === "object"
          ? JSON.stringify(data.details)
          : data.details
        : "";

      if (data.success) {
        progressBar.style.background = "linear-gradient(90deg, #10b981 0%, #059669 100%)";
        progressText.innerHTML = '<span style="color:#10b981"><i class="fas fa-check-circle"></i> Berhasil!</span>';
        if (detailBox) detailBox.textContent = detail || "Semua data berhasil dikirim ke SAE.";
        addLog(`Kirim Data berhasil: ${data.message}`, "success");
        if (detail) addLog(`Detail: ${detail}`, "info");
        finishSync(true, "Sinkronisasi selesai. " + (data.message || ""), detail);
      } else {
        progressBar.style.background = "linear-gradient(90deg, #ef4444 0%, #dc2626 100%)";
        progressText.innerHTML = '<span style="color:#ef4444"><i class="fas fa-times-circle"></i> Gagal!</span>';
        if (detailBox) detailBox.textContent = detail || "Proses gagal. Lihat log aktivitas.";
        addLog(`Kirim Data gagal: ${data.message}`, "error");
        if (detail) addLog(`Detail: ${detail}`, "error");
        finishSync(false, "Sinkronisasi gagal. " + (data.message || ""), detail);
      }
    })
    .catch((error) => {
      progressBar.style.width = "100%";
      progressBar.style.background = "linear-gradient(90deg, #ef4444 0%, #dc2626 100%)";
      progressText.innerHTML = '<span style="color:#ef4444"><i class="fas fa-exclamation-triangle"></i> Error!</span>';
      addLog(`Error: ${error.message}`, "error");
      finishSync(false, "Koneksi ke server terputus. " + error.message, "");
    });
}

// Initialize
document.addEventListener("DOMContentLoaded", function () {
  addLog("Loader SAE siap digunakan (compact mode)", "success");
});

// ===== FUNGSI KONFIGURASI =====

function toggleEditDapodik() {
  const npsn = document.getElementById("dapodik_npsn");
  const token = document.getElementById("dapodik_token");
  const btn = document.getElementById("btn_save_dapodik");
  if (npsn && token) {
    npsn.disabled = false;
    token.disabled = false;
    if (btn) btn.style.display = "";
    npsn.focus();
  }
}

function toggleEditSae() {
  const url = document.getElementById("sae_base_url");
  const key = document.getElementById("sae_api_key");
  const btn = document.getElementById("btn_save_sae");
  if (url && key) {
    url.disabled = false;
    key.disabled = false;
    if (btn) btn.style.display = "";
    url.focus();
  }
}

function testDapodikConnection() {
  const token = document.getElementById("dapodik_token").value.trim();
  const npsn = document.getElementById("dapodik_npsn")
    ? document.getElementById("dapodik_npsn").value.trim()
    : "";
  if (!token || token === "***") {
    showConfigResult(false, "Token tidak boleh kosong");
    return;
  }
  if (!npsn) {
    showConfigResult(false, "NPSN tidak boleh kosong");
    return;
  }

  showConfigResult("loading", "Memverifikasi token & NPSN Dapodik...");

  const formData = new FormData();
  formData.append("action", "test_dapodik");
  formData.append("token", token);
  formData.append("npsn", npsn);

  fetch("index.php?action=test_dapodik", {
    method: "POST",
    body: formData,
  })
    .then((response) => response.json())
    .then((data) => {
      if (data.success) {
        const msg = data.sekolah
          ? "Koneksi Dapodik berhasil (" +
            data.sekolah +
            " - NPSN: " +
            (data.npsn || "N/A") +
            ")"
          : "Koneksi Dapodik berhasil";
        showConfigResult(true, msg);
        setTimeout(() => location.reload(), 2000);
      } else {
        showConfigResult(
          false,
          "Koneksi Dapodik gagal: " + (data.message || ""),
        );
      }
    })
    .catch((error) => {
      showConfigResult(false, "Error: " + error.message);
    });
}

function testSaeConnection() {
  const apiKey = document.getElementById("sae_api_key").value.trim();
  const baseUrl = document.getElementById("sae_base_url").value.trim();

  if (!apiKey || apiKey === "***") {
    showConfigResult(false, "API Key tidak boleh kosong");
    return;
  }

  showConfigResult("loading", "Memverifikasi koneksi SAE...");

  const formData = new FormData();
  formData.append("action", "test_sae");
  formData.append("api_key", apiKey);
  formData.append("base_url", baseUrl);

  fetch("index.php?action=test_sae", {
    method: "POST",
    body: formData,
  })
    .then((response) => response.json())
    .then((data) => {
      if (data.success) {
        showConfigResult(true, "Koneksi SAE berhasil");
        setTimeout(() => location.reload(), 2000);
      } else {
        showConfigResult(false, "Koneksi SAE gagal: " + (data.message || ""));
      }
    })
    .catch((error) => {
      showConfigResult(false, "Error: " + error.message);
    });
}

function testCurrentConfig() {
  showConfigResult("loading", "Mengtest konfigurasi saat ini...");

  const formData = new FormData();
  formData.append("action", "test_current_config");

  // Include current API Key and Base URL from inputs so test does not require saving
  const apiKeyInput = document.getElementById("sae_api_key");
  const baseUrlInput = document.getElementById("sae_base_url");
  if (apiKeyInput && apiKeyInput.value.trim() && apiKeyInput.value.trim() !== "***")
    formData.append("api_key", apiKeyInput.value.trim());
  if (baseUrlInput) formData.append("base_url", baseUrlInput.value.trim());

  fetch("index.php?action=test_current_config", {
    method: "POST",
    body: formData,
  })
    .then((response) => response.json())
    .then((data) => {
      let message = "Test Konfigurasi:<br>";
      message += `• Dapodik: ${data.dapodik?.status ? "✅" : "❌"} ${data.dapodik?.message || "Error"}<br>`;
      message += `• SAE v4: ${data.sae?.status ? "✅" : "❌"} ${data.sae?.message || "Error"}`;

      if (data.validation_errors && data.validation_errors.length > 0) {
        message += "<br>⚠️ " + data.validation_errors.join(", ");
      }

      showConfigResult(data.success, message);

      if (data.success) {
        setTimeout(() => location.reload(), 2000);
      }
    })
    .catch((error) => {
      showConfigResult(false, "Error: " + error.message);
    });
}

function showConfigResult(success, message) {
  const resultDiv = document.getElementById("config-result");

  if (success === "loading") {
    resultDiv.innerHTML = `
                    <div class="alert alert-info">
                        <i class="fas fa-spinner fa-spin me-2"></i>${message}
                    </div>
                `;
  } else {
    const alertClass = success ? "alert-success" : "alert-danger";
    const icon = success ? "check-circle" : "exclamation-triangle";

    resultDiv.innerHTML = `
                    <div class="alert ${alertClass}">
                        <i class="fas fa-${icon} me-2"></i>${message}
                    </div>
                `;
  }

  resultDiv.style.display = "block";
}

// ============================================================================
// AUTO-REFRESH EXPLICITLY DISABLED TO PROTECT DAPODIK
// ============================================================================
// DO NOT ADD setInterval(), setTimeout(), or any polling code here
// This is MANUAL MODE ONLY
// ============================================================================

console.log(
  "%c⚠️ LOADER SAE - MANUAL MODE ONLY",
  "color:red;font-size:14px;font-weight:bold",
);
console.log("Status check: Manual only (refresh page with F5)");
console.log("Data sync: Manual only (click 'Kirim Semua Data' button)");
