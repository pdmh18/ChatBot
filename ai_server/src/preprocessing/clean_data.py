"""
Tiền xử lý dữ liệu — đọc từ 3 file CSV trong data/raw/
Pipeline: MinMaxScaler + SMOTE

Chạy: python -m src.preprocessing.clean_data
"""

import pandas as pd
import numpy as np
from pathlib import Path
from sklearn.preprocessing import MinMaxScaler
from sklearn.model_selection import train_test_split
import joblib
from imblearn.over_sampling import SMOTE


# ============================================================
# ĐƯỜNG DẪN
# ============================================================
BASE_DIR      = Path(__file__).resolve().parent.parent.parent
RAW_DIR       = BASE_DIR / "data" / "raw"
PROCESSED_DIR = BASE_DIR / "data" / "processed"
MODEL_DIR     = BASE_DIR / "models"

PROCESSED_DIR.mkdir(parents=True, exist_ok=True)
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
# ĐỌC FILE CSV
# ============================================================

def doc_file_csv(ten_file: str, sep=",") -> pd.DataFrame:
    path = RAW_DIR / ten_file
    df = pd.read_csv(path, sep=sep)
    print(f"   Đọc {ten_file}: {len(df)} dòng | {len(df.columns)} cột")
    return df


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
    """Chuyển DoUuTien text → số theo thứ tự ưu tiên"""
    priority_map = {"Thap": 0, "Trung binh": 1, "Cao": 2, "Khan cap": 3}
    df["DoUuTien_Encoded"] = df["DoUuTien"].map(priority_map).fillna(1).astype(int)
    return df


def fill_null_features(df, cols):
    """Fill null bằng Median"""
    for col in cols:
        if col in df.columns:
            null_count = df[col].isnull().sum()
            if null_count > 0:
                median_val = df[col].median()
                df[col] = df[col].fillna(median_val)
                print(f"   Fill null [{col}]: {null_count} dòng → median={median_val:.2f}")
    return df


# ============================================================
# BƯỚC 2: LÀM GIÀU DỮ LIỆU
# ============================================================

def lam_giau_khoi_luong(df, col="KhoiLuongHienTaiNhanSu"):
    """Nếu cột workload toàn 0 → sinh ngẫu nhiên"""
    if col not in df.columns:
        return df
    zero_pct = (df[col] == 0).sum() / len(df)
    if zero_pct > 0.8:
        print(f"   [{col}] bị 0 {zero_pct:.0%} → sinh giá trị ngẫu nhiên...")
        np.random.seed(42)
        mask = df[col] == 0
        df.loc[mask, col] = np.random.uniform(0.1, 0.9, mask.sum()).round(2)
    return df


# ============================================================
# BƯỚC 3: SPLIT + NORMALIZE + SMOTE
# ============================================================

def chay_pipeline(df, feature_cols, label_col, scaler_name, test_size=0.2):
    """
    Thứ tự đúng:
    1. Split train/test (fix data leak)
    2. MinMaxScaler fit trên train
    3. SMOTE chỉ trên train
    """
    valid_cols = [c for c in feature_cols if c in df.columns]
    missing = [c for c in feature_cols if c not in df.columns]
    if missing:
        raise ValueError(f"Thiếu cột: {missing}")

    X = df[valid_cols].copy()
    y = df[label_col].copy()
    class_counts = y.value_counts()
    if len(class_counts) < 2:
        raise ValueError(f"{label_col} cần ít nhất 2 class để train")
    if class_counts.min() < 2:
        raise ValueError(
            f"{label_col} cần ít nhất 2 dòng mỗi class để stratified split: "
            f"{dict(class_counts)}"
        )
    # BƯỚC 1: SPLIT TRƯỚC — fix data leak
    X_train, X_test, y_train, y_test = train_test_split(
        X, y,
        test_size=test_size,
        random_state=42,
        stratify=y
    )
    print(f"   Split: Train {len(X_train)} | Test {len(X_test)}")
    print(f"   Train label trước SMOTE: {dict(y_train.value_counts())}")

    # BƯỚC 2: NORMALIZE — fit chỉ trên train
    scaler = MinMaxScaler()
    X_train_scaled = scaler.fit_transform(X_train)
    X_test_scaled  = scaler.transform(X_test)

    # Lưu scaler
    scaler_path = MODEL_DIR / scaler_name
    joblib.dump(scaler, scaler_path)
    print(f"   Scaler lưu: {scaler_path}")

    # BƯỚC 3: SMOTE — chỉ trên train
    ratio = y_train.value_counts().min() / y_train.value_counts().max()
    print(f"   Train label trước SMOTE: {dict(y_train.value_counts())}")
    print(f"   Tỉ lệ class train: {ratio:.0%} (SMOTE sẽ apply trong lúc train model)")

    X_train_df = pd.DataFrame(X_train_scaled, columns=valid_cols)
    X_train_df[label_col] = y_train.values if hasattr(y_train, 'values') else y_train

    X_test_df = pd.DataFrame(X_test_scaled, columns=valid_cols)
    X_test_df[label_col] = y_test.values
    return X_train_df, X_test_df


# ============================================================
# MAIN
# ============================================================

def main():
    print("=" * 60)
    print("TIỀN XỬ LÝ DỮ LIỆU — ĐỌC TỪ CSV")
    print("=" * 60)

    # --------------------------------------------------------
    # DATASET 1: DỰ BÁO TRỄ HẠN
    # --------------------------------------------------------
    print("\n📌 [1/3] Dataset Dự Báo Trễ Hạn")
    print("-" * 40)

    risk_df = doc_file_csv("dataset_du_bao_tre_han.csv")
    risk_df = lam_sach_co_ban(risk_df, "MaCongViec", RISK_LABEL_COL)
    risk_df = encode_do_uu_tien(risk_df)
    risk_df = lam_giau_khoi_luong(risk_df)
    risk_df = fill_null_features(risk_df, RISK_FEATURE_COLS)
    risk_df = xu_ly_outlier(risk_df, ["SoGioUocTinh", "SoNamKinhNghiemNhanSu"])

    X_train, X_test = chay_pipeline(
        risk_df, RISK_FEATURE_COLS, RISK_LABEL_COL,
        scaler_name="risk_pipeline_scaler.joblib"
    )
    X_train.to_csv(PROCESSED_DIR / "risk_train.csv", index=False)
    X_test.to_csv(PROCESSED_DIR  / "risk_test.csv",  index=False)
    print(f"   ✅ risk_train.csv ({len(X_train)}) | risk_test.csv ({len(X_test)})")

    # --------------------------------------------------------
    # DATASET 2: ĐỀ XUẤT GIAO VIỆC
    # --------------------------------------------------------
    print("\n📌 [2/3] Dataset Đề Xuất Giao Việc")
    print("-" * 40)

    assign_df = doc_file_csv("dataset_de_xuat_giao_viec.csv", sep="\t")
    assign_df = assign_df.dropna(subset=[ASSIGN_LABEL_COL])
    assign_df = lam_giau_khoi_luong(assign_df, col="PhanTramTaiNhanSu")
    assign_df = fill_null_features(assign_df, ASSIGN_FEATURE_COLS)

    X_train_a, X_test_a = chay_pipeline(
        assign_df, ASSIGN_FEATURE_COLS, ASSIGN_LABEL_COL,
        scaler_name="assignment_pipeline_scaler.joblib"
    )
    X_train_a.to_csv(PROCESSED_DIR / "assignment_train.csv", index=False)
    X_test_a.to_csv(PROCESSED_DIR  / "assignment_test.csv",  index=False)
    print(f"   ✅ assignment_train.csv ({len(X_train_a)}) | assignment_test.csv ({len(X_test_a)})")

    # --------------------------------------------------------
    # DATASET 3: ĐIỂM NGHẼN
    # --------------------------------------------------------
    print("\n📌 [3/3] Dataset Điểm Nghẽn")
    print("-" * 40)

    bottleneck_df = doc_file_csv("dataset_phat_hien_diem_nghen.csv")
    bottleneck_df = bottleneck_df.dropna(subset=["Nhan_GhiNhanDiemNghen"])
    bottleneck_df.to_csv(PROCESSED_DIR / "bottleneck_clean.csv", index=False)
    print(f"   ✅ bottleneck_clean.csv: {len(bottleneck_df)} dòng")

    # --------------------------------------------------------
    # TỔNG KẾT
    # --------------------------------------------------------
    print("\n" + "=" * 60)
    print("✅ TIỀN XỬ LÝ HOÀN TẤT")
    print(f"   File CSV lưu tại: {PROCESSED_DIR}")
    print(f"   Scaler lưu tại:   {MODEL_DIR}")
    print("=" * 60)


if __name__ == "__main__":
    main()