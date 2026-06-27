@echo off
chcp 65001 > nul
title AI Server — Khoi dong he thong

echo.
echo ========================================================
echo  HE THONG AI SERVER — QUAN LY DU AN
echo  XGBoost + Random Forest + GNN + FastAPI
echo ========================================================
echo.

:: ── Kiem tra Python ──────────────────────────────────────
python --version > nul 2>&1
IF %ERRORLEVEL% NEQ 0 (
    echo [LOI] Khong tim thay Python. Vui long cai Python 3.10+ truoc.
    pause
    exit /b 1
)

:: ── Tao .venv neu chua co ────────────────────────────────
IF NOT EXIST ".venv" (
    echo [1/6] Tao moi truong ao .venv ...
    python -m venv .venv
    IF %ERRORLEVEL% NEQ 0 (
        echo [LOI] Khong tao duoc .venv
        pause
        exit /b 1
    )
    echo       Tao .venv thanh cong!
) ELSE (
    echo [1/6] Moi truong ao .venv da ton tai, bo qua.
)
echo.

:: ── Cai thu vien ─────────────────────────────────────────
echo [2/6] Cai dat thu vien (co the mat vai phut lan dau)...

.venv\Scripts\python.exe -m pip install --upgrade pip --quiet

:: Core requirements
.venv\Scripts\pip.exe install -r requirements.txt --quiet
IF %ERRORLEVEL% NEQ 0 (
    echo [LOI] Cai requirements.txt that bai
    pause
    exit /b 1
)

:: Fix xung dot version
.venv\Scripts\pip.exe install "scikit-learn==1.5.2" "numpy<2" "scipy<1.14" --quiet

:: PyTorch CPU
.venv\Scripts\pip.exe install torch --index-url https://download.pytorch.org/whl/cpu --quiet

:: PyTorch Geometric
.venv\Scripts\pip.exe install torch-geometric --quiet

:: imbalanced-learn va matplotlib
.venv\Scripts\pip.exe install imbalanced-learn matplotlib --quiet

:: XGBoost moi nhat tuong thich
.venv\Scripts\pip.exe install "xgboost>=2.0" --quiet

echo       Cai dat hoan tat!
echo.

:: ── Kiem tra file CSV ─────────────────────────────────────
echo [3/6] Kiem tra file du lieu CSV...
IF NOT EXIST "data\raw\dataset_du_bao_tre_han.csv" (
    echo [LOI] Thieu file: data\raw\dataset_du_bao_tre_han.csv
    pause
    exit /b 1
)
IF NOT EXIST "data\raw\dataset_de_xuat_giao_viec.csv" (
    echo [LOI] Thieu file: data\raw\dataset_de_xuat_giao_viec.csv
    pause
    exit /b 1
)
IF NOT EXIST "data\raw\dataset_phat_hien_diem_nghen.csv" (
    echo [LOI] Thieu file: data\raw\dataset_phat_hien_diem_nghen.csv
    pause
    exit /b 1
)
echo       3 file CSV hop le!
echo.

:: ── Tien xu ly du lieu ───────────────────────────────────
echo [4/6] Tien xu ly du lieu...
.venv\Scripts\python.exe -m src.preprocessing.clean_data
IF %ERRORLEVEL% NEQ 0 (
    echo [LOI] Tien xu ly du lieu that bai
    pause
    exit /b 1
)
echo.

:: ── Train 3 model ────────────────────────────────────────
echo [5/6] Huan luyen 3 mo hinh AI...
echo.

echo --- [5a] XGBoost: Du bao tre han ---
.venv\Scripts\python.exe -m src.models.train_risk_model
IF %ERRORLEVEL% NEQ 0 (
    echo [LOI] Train XGBoost that bai
    pause
    exit /b 1
)
echo.

echo --- [5b] Random Forest: De xuat giao viec ---
.venv\Scripts\python.exe -m src.models.staff_matching
IF %ERRORLEVEL% NEQ 0 (
    echo [LOI] Train Random Forest that bai
    pause
    exit /b 1
)
echo.

echo --- [5c] GNN: Phat hien diem nghen ---
.venv\Scripts\python.exe -m src.models.bottleneck_detector
IF %ERRORLEVEL% NEQ 0 (
    echo [LOI] Train GNN that bai
    pause
    exit /b 1
)
echo.

:: ── Khoi dong FastAPI ─────────────────────────────────────
echo [6/6] Khoi dong FastAPI Server...
echo.
echo ========================================================
echo  SERVER DANG CHAY
echo  API Docs : http://localhost:8000/docs
echo  Health   : http://localhost:8000/health
echo  Nhan Ctrl+C de tat server
echo ========================================================
echo.

.venv\Scripts\python.exe -m uvicorn src.api.main:app --reload --port 8000

pause