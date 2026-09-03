# Cấu hình Git cho dự án LẠC. Mỗi máy chạy MỘT LẦN, ngay sau khi clone.
#
#     powershell -ExecutionPolicy Bypass -File tools\setup-git.ps1
#
# Script cài merge driver cho file scene và prefab của Unity. Không có nó thì
# hai người cùng sửa một scene sẽ tạo ra xung đột mà Git không gỡ được, và cách
# duy nhất là một người vứt bỏ công việc của mình.

$ErrorActionPreference = 'Stop'

Write-Host "Cau hinh Git cho du an LAC" -ForegroundColor Cyan
Write-Host ""

# --- 1. Tim UnityYAMLMerge.exe -----------------------------------------------
# Cong cu di kem Unity Editor. Tim theo phien ban chinh xac cua du an truoc,
# roi moi noi long ra cac phien ban khac.

$version = "6000.5.6f1"
$candidates = @()

foreach ($root in @("C:\Program Files\Unity\Hub\Editor",
                    "D:\Unity\Hub\Editor",
                    "C:\Unity\Hub\Editor",
                    "$env:LOCALAPPDATA\Unity\Hub\Editor")) {
    $candidates += "$root\$version\Editor\Data\Tools\UnityYAMLMerge.exe"
}

$exe = $candidates | Where-Object { Test-Path $_ } | Select-Object -First 1

if (-not $exe) {
    Write-Host "Khong tim thay ban $version o cac vi tri thong thuong, dang do rong..." -ForegroundColor Yellow
    foreach ($root in @("C:\Program Files\Unity\Hub\Editor", "D:\Unity\Hub\Editor",
                        "C:\Unity\Hub\Editor", "$env:LOCALAPPDATA\Unity\Hub\Editor")) {
        if (Test-Path $root) {
            $found = Get-ChildItem $root -Directory -ErrorAction SilentlyContinue |
                     ForEach-Object { Join-Path $_.FullName "Editor\Data\Tools\UnityYAMLMerge.exe" } |
                     Where-Object { Test-Path $_ } | Select-Object -First 1
            if ($found) { $exe = $found; break }
        }
    }
}

if (-not $exe) {
    Write-Host "KHONG TIM THAY UnityYAMLMerge.exe" -ForegroundColor Red
    Write-Host "No nam trong thu muc cai Unity, duong dan dang:"
    Write-Host "  <Unity>\$version\Editor\Data\Tools\UnityYAMLMerge.exe"
    Write-Host "Tim file do roi chay lai voi duong dan tu nhap:"
    Write-Host "  powershell -File tools\setup-git.ps1 -UnityYamlMerge 'D:\...\UnityYAMLMerge.exe'"
    exit 1
}

$exe = $exe -replace '\\', '/'
Write-Host "Tim thay: $exe" -ForegroundColor Green

# --- 2. Merge driver ---------------------------------------------------------
# .gitattributes da khai bao 'merge=unityyamlmerge' cho file .unity, .prefab,
# .asset... Phan con thieu la dinh nghia cua driver do, va no phai nam o tung
# may vi duong dan Unity moi may moi khac.

git config --local unityyamlmerge.path "$exe"
git config --local merge.unityyamlmerge.name "Unity SmartMerge"
git config --local merge.unityyamlmerge.driver './tools/unityyamlmerge.sh %O %B %A %P'
git config --local merge.unityyamlmerge.recursive binary

# Duong xu ly tay khi driver bo cuoc: git mergetool
git config --local merge.tool unityyamlmerge
git config --local mergetool.unityyamlmerge.trustExitCode false
git config --local mergetool.unityyamlmerge.cmd './tools/unityyamlmerge-tool.sh $BASE $REMOTE $LOCAL $MERGED'
git config --local mergetool.keepBackup false

Write-Host "Da cai merge driver cho file scene va prefab" -ForegroundColor Green

# --- 3. Git LFS --------------------------------------------------------------
# Anh va am thanh di qua LFS, khai bao trong .gitattributes.

$lfs = (git lfs version 2>$null)
if ($LASTEXITCODE -eq 0) {
    git lfs install --local | Out-Null
    Write-Host "Git LFS: $lfs" -ForegroundColor Green
} else {
    Write-Host "CHUA CAI Git LFS. Tai tai https://git-lfs.github.com roi chay lai script nay." -ForegroundColor Red
    Write-Host "Khong co LFS thi file anh se tai ve duoi dang van ban vo nghia."
}

# --- 4. Kiem tra lai ---------------------------------------------------------
Write-Host ""
Write-Host "Kiem tra:" -ForegroundColor Cyan
git config --local --get-regexp '^(merge|mergetool|unityyamlmerge)\.' | ForEach-Object { Write-Host "  $_" }

Write-Host ""
Write-Host "Xong. Doc docs/WORKFLOW.md truoc khi commit lan dau." -ForegroundColor Cyan
