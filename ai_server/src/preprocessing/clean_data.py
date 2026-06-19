"""
Tiền xử lý dữ liệu dùng Pipeline — đọc từ SQL Server
Pipeline gom tất cả bước xử lý thành 1 chuỗi tuần tự:
  MinMaxScaler → SMOTE → sẵn sàng train

Chạy: python -m src.preprocessing.clean_data
"""

import pandas as pd
import numpy as np
from pathlib import Path
from sklearn.preprocessing import MinMaxScaler
from sklearn.model_selection import train_test_split
from imblearn.pipeline import Pipeline
from imblearn.over_sampling import SMOTE
import joblib

from src.preprocessing.db_connector import (
    load_dataset_du_bao_tre_han,
    load_dataset_de_xuat_giao_viec,
    load_dataset_phat_hien_diem_nghen,
    load_phu_thuoc_cong_viec,
    load_nguoi_dung,
)

# ============================================================
# ĐƯỜNG DẪN
# ============================================================
BASE_DIR      = Path(__file__).resolve().parent.parent.parent
PROCESSED_DIR = BASE_DIR / "data" / "processed"
RAW_DIR       = BASE_DIR / "data" / "raw"
MODEL_DIR     = BASE_DIR / "models"

PROCESSED_DIR.mkdir(parents=True, exist_ok=True)
RAW_DIR.mkdir(parents=True, exist_ok=True)
MODEL_DIR.mkdir(parents=True, exist_ok=True)

# ============================================================
# CẤU HÌNH CỘT
# ============================================================
RISK_FEATURE_COLS = [
    "SoGioUocTinh",
    "SoNamKinhNghiemNhanSu",
    "KhoiLuongHienTaiNhanSu",
    "SoCongViecPhuThuocTruoc",
    "DoUuTien_Encoded",
]
RISK_LABEL_COL = "Nhan_CoTreHan"

ASSIGN_FEATURE_COLS = [
    "SoGioUocTinh",
    "PhanTramTaiNhanSu",
    "DiemChatLuongTrungBinhLichSu",
]
ASSIGN_LABEL_COL = "Nhan_GiaoViecHieuQua"


# ============================================================
# BƯỚC 1: LÀM SẠCH CƠ BẢN
# ============================================================

def lam_sach_co_ban(df, id_col, label_col):
    ban_dau = len(df)
    df = df.drop_duplicates(subset=id_col)
    sau_duplicate = len(df)
    df = df.dropna(subset=[label_col])
    sau_null = len(df)
    print(f"   Duplicate bị loại:   {ban_dau - sau_duplicate} dòng")
    print(f"   Null label bị loại:  {sau_duplicate - sau_null} dòng")
    print(f"   Còn lại:             {sau_null} dòng")
    return df


def xu_ly_outlier(df, cols):
    truoc = len(df)
    for col in cols:
        if col in df.columns and df[col].dtype in ['float64', 'int64']:
            Q1  = df[col].quantile(0.25)
            Q3  = df[col].quantile(0.75)
            IQR = Q3 - Q1
            df  = df[(df[col] >= Q1 - 1.5*IQR) & (df[col] <= Q3 + 1.5*IQR)]
    print(f"   Outlier bị loại:     {truoc - len(df)} dòng")
    return df


def encode_do_uu_tien(df):
    priority_map = {"Thap": 0, "Trung binh": 1, "Cao": 2, "Khan cap": 3}
    df["DoUuTien_Encoded"] = df["DoUuTien"].map(priority_map).fillna(1).astype(int)
    return df


def fill_null_features(df, cols):
    for col in cols:
        if col in df.columns:
            null_count = df[col].isnull().sum()
            if null_count > 0:
                median_val = df[col].median()
                df[col]    = df[col].fillna(median_val)
                print(f"   Fill null [{col}]: {null_count} dòng → median={median_val:.2f}")
    return df


# ============================================================
# BƯỚC 2: LÀM GIÀU DỮ LIỆU
# ============================================================

def lam_giau_khoi_luong(df, col="KhoiLuongHienTaiNhanSu"):
    if col not in df.columns:
        return df
    zero_pct = (df[col] == 0).sum() / len(df)
    if zero_pct > 0.8:
        print(f"   [{col}] bị 0 {zero_pct:.0%} → sinh giá trị ngẫu nhiên...")
        np.random.seed(42)
        mask = df[col] == 0
        df.loc[mask, col] = np.random.uniform(0.1, 0.9, mask.sum()).round(2)
    return df


def lam_giau_nguoi_dung(df, so_luong_can=50):
    if len(df) >= 10:
        print(f"   NguoiDung đủ: {len(df)} người")
        return df
    print(f"   NguoiDung chỉ có {len(df)} người → sinh thêm {so_luong_can} người giả...")
    np.random.seed(42)
    synthetic = pd.DataFrame({
        "MaNguoiDung":            range(9000, 9000 + so_luong_can),
        "SoNamKinhNghiem":        np.random.randint(1, 15, so_luong_can),
        "KhoiLuongHienTai":       np.random.uniform(0.1, 0.9, so_luong_can).round(2),
        "MucLuongTheoGio":        np.random.randint(50000, 300000, so_luong_can),
        "DiemTrungBinhHieuSuat":  np.random.uniform(5.0, 10.0, so_luong_can).round(1),
    })
    common_cols  = [c for c in synthetic.columns if c in df.columns]
    df_combined  = pd.concat([df[common_cols], synthetic[common_cols]], ignore_index=True)
    print(f"   Sau làm giàu: {len(df_combined)} người")
    return df_combined


# ============================================================
# BƯỚC 3: TẠO PREPROCESSING PIPELINE
# ============================================================

def tao_preprocessing_pipeline():
    """
    Pipeline gồm 2 bước:
      1. MinMaxScaler  → chuẩn hóa feature về [0, 1]
      2. SMOTE         → cân bằng dữ liệu (chỉ áp dụng lúc fit)

    Dùng imblearn.Pipeline thay sklearn.Pipeline
    vì sklearn.Pipeline không hỗ trợ SMOTE
    """
    pipeline = Pipeline([
        ('scaler', MinMaxScaler()),
        ('smote',  SMOTE(random_state=42)),
    ])
    return pipeline


def chay_pipeline(df, feature_cols, label_col, pipeline_name, test_size=0.2):
    """
    Chạy toàn bộ pipeline:
      1. Split train/test (stratified, fix data leak)
      2. Fit pipeline trên train (scaler + SMOTE)
      3. Transform test (chỉ scaler, không SMOTE)
      4. Lưu pipeline
    """
    valid_cols = [c for c in feature_cols if c in df.columns]
    X = df[valid_cols].copy()
    y = df[label_col].copy()

    # SPLIT TRƯỚC — fix data leak
    X_train, X_test, y_train, y_test = train_test_split(
        X, y,
        test_size=test_size,
        random_state=42,
        stratify=y
    )
    print(f"   Split: Train {len(X_train)} | Test {len(X_test)}")

    # FIT PIPELINE TRÊN TRAIN
    pipeline = tao_preprocessing_pipeline()
    X_train_processed, y_train_processed = pipeline.fit_resample(X_train, y_train)

    print(f"   Trước SMOTE: {dict(y_train.value_counts())}")
    print(f"   Sau SMOTE:   {dict(pd.Series(y_train_processed).value_counts())}")
    print(f"   Train sau SMOTE: {len(X_train_processed)} dòng")

    # TRANSFORM TEST — chỉ dùng scaler, không SMOTE
    scaler     = pipeline.named_steps['scaler']
    X_test_scaled = scaler.transform(X_test)

    # Lưu pipeline (bao gồm cả scaler bên trong)
    pipeline_path = MODEL_DIR / pipeline_name
    joblib.dump(pipeline, pipeline_path)
    print(f"   Pipeline lưu tại: {pipeline_path}")

    # Tạo DataFrame để lưu CSV
    X_train_df = pd.DataFrame(X_train_processed, columns=valid_cols)
    X_train_df[label_col] = y_train_processed

    X_test_df  = pd.DataFrame(X_test_scaled, columns=valid_cols)
    X_test_df[label_col]  = y_test.values

    return X_train_df, X_test_df


# ============================================================
# MAIN
# ============================================================

def main():
    print("=" * 60)
    print("TIỀN XỬ LÝ DỮ LIỆU — PIPELINE")
    print("=" * 60)

    # --------------------------------------------------------
    # DATASET 1: DỰ BÁO TRỄ HẠN
    # --------------------------------------------------------
    print("\n📌 [1/3] Dataset Dự Báo Trễ Hạn")
    print("-" * 40)

    risk_df = load_dataset_du_bao_tre_han()
    print(f"   Raw: {len(risk_df)} dòng")

    risk_df = lam_sach_co_ban(risk_df, "MaCongViec", RISK_LABEL_COL)
    risk_df = encode_do_uu_tien(risk_df)
    risk_df = lam_giau_khoi_luong(risk_df)
    risk_df = fill_null_features(risk_df, RISK_FEATURE_COLS)
    risk_df = xu_ly_outlier(risk_df, ["SoGioUocTinh", "SoNamKinhNghiemNhanSu"])

    risk_df.to_csv(RAW_DIR / "risk_raw.csv", index=False)

    X_train, X_test = chay_pipeline(
        risk_df, RISK_FEATURE_COLS, RISK_LABEL_COL,
        pipeline_name="risk_pipeline.joblib"
    )

    X_train.to_csv(PROCESSED_DIR / "risk_train.csv", index=False)
    X_test.to_csv(PROCESSED_DIR  / "risk_test.csv",  index=False)
    print(f"   ✅ risk_train.csv ({len(X_train)}) | risk_test.csv ({len(X_test)})")

    # --------------------------------------------------------
    # DATASET 2: ĐỀ XUẤT GIAO VIỆC
    # --------------------------------------------------------
    print("\n📌 [2/3] Dataset Đề Xuất Giao Việc")
    print("-" * 40)

    assign_df = load_dataset_de_xuat_giao_viec()
    print(f"   Raw: {len(assign_df)} dòng")

    assign_df = assign_df.dropna(subset=[ASSIGN_LABEL_COL])
    assign_df = fill_null_features(assign_df, ASSIGN_FEATURE_COLS)

    assign_df.to_csv(RAW_DIR / "assignment_raw.csv", index=False)

    X_train_a, X_test_a = chay_pipeline(
        assign_df, ASSIGN_FEATURE_COLS, ASSIGN_LABEL_COL,
        pipeline_name="assignment_pipeline.joblib"
    )

    X_train_a.to_csv(PROCESSED_DIR / "assignment_train.csv", index=False)
    X_test_a.to_csv(PROCESSED_DIR  / "assignment_test.csv",  index=False)
    print(f"   ✅ assignment_train.csv ({len(X_train_a)}) | assignment_test.csv ({len(X_test_a)})")

    # --------------------------------------------------------
    # DATASET 3: ĐIỂM NGHẼN
    # --------------------------------------------------------
    print("\n📌 [3/3] Dataset Điểm Nghẽn + Phụ Thuộc")
    print("-" * 40)

    bottleneck_df = load_dataset_phat_hien_diem_nghen()
    bottleneck_df.to_csv(PROCESSED_DIR / "bottleneck_clean.csv", index=False)
    print(f"   ✅ bottleneck_clean.csv: {len(bottleneck_df)} dòng")

    phu_thuoc_df = load_phu_thuoc_cong_viec()
    phu_thuoc_df = phu_thuoc_df.dropna().drop_duplicates()
    phu_thuoc_df.to_csv(PROCESSED_DIR / "phu_thuoc_clean.csv", index=False)
    print(f"   ✅ phu_thuoc_clean.csv: {len(phu_thuoc_df)} cạnh đồ thị")

    nhan_su_df = load_nguoi_dung()
    nhan_su_df = lam_giau_nguoi_dung(nhan_su_df)
    nhan_su_df.to_csv(PROCESSED_DIR / "nhan_su_clean.csv", index=False)
    print(f"   ✅ nhan_su_clean.csv: {len(nhan_su_df)} người")

    # --------------------------------------------------------
    # TỔNG KẾT
    # --------------------------------------------------------
    print("\n" + "=" * 60)
    print("✅ TIỀN XỬ LÝ HOÀN TẤT")
    print(f"   Pipeline lưu tại: {MODEL_DIR}")
    print(f"     risk_pipeline.joblib       ← dùng khi predict task mới")
    print(f"     assignment_pipeline.joblib ← dùng khi predict giao việc")
    print("=" * 60)


if __name__ == "__main__":
    main()