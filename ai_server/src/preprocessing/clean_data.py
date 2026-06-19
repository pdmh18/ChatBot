"""
Tiền xử lý & chuẩn hóa dữ liệu — đọc trực tiếp từ SQL Server (DB thật)
thay vì dùng dữ liệu giả lập.

Chạy: python -m src.preprocessing.clean_data
"""
import pandas as pd
from pathlib import Path
from sklearn.preprocessing import MinMaxScaler
import joblib

from src.preprocessing.db_connector import (
    load_dataset_du_bao_tre_han,
    load_dataset_de_xuat_giao_viec,
    load_dataset_phat_hien_diem_nghen,
    load_phu_thuoc_cong_viec,
    load_nguoi_dung,
)

PROCESSED_DIR = Path(__file__).resolve().parent.parent.parent / "data" / "processed"
MODEL_DIR = Path(__file__).resolve().parent.parent.parent / "models"
PROCESSED_DIR.mkdir(parents=True, exist_ok=True)
MODEL_DIR.mkdir(parents=True, exist_ok=True)

# Các cột số dùng để train model dự báo trễ hạn (khớp đúng tên cột trong view SQL)
RISK_FEATURE_COLS = [
    "SoGioUocTinh",
    "SoNamKinhNghiemNhanSu",
    "KhoiLuongHienTaiNhanSu",
    "SoCongViecPhuThuocTruoc",
]
RISK_LABEL_COL = "Nhan_CoTreHan"


def clean_risk_dataset(df: pd.DataFrame) -> pd.DataFrame:
    """Làm sạch dữ liệu dự báo trễ hạn: loại trùng, xử lý null."""
    df = df.drop_duplicates(subset="MaCongViec")
    df = df.dropna(subset=[RISK_LABEL_COL])

    # Encode DoUuTien (categorical) thành số
    priority_map = {"Thap": 0, "Trung binh": 1, "Cao": 2, "Khan cap": 3}
    df["DoUuTien_Encoded"] = df["DoUuTien"].map(priority_map).fillna(1)

    df[RISK_FEATURE_COLS] = df[RISK_FEATURE_COLS].fillna(df[RISK_FEATURE_COLS].median())
    return df


def normalize_features(df: pd.DataFrame, cols: list, fit=True, scaler_name="scaler.joblib"):
    """Chuẩn hóa (Min-Max) các cột numeric về [0, 1]."""
    scaler_path = MODEL_DIR / scaler_name

    if fit:
        scaler = MinMaxScaler()
        df[cols] = scaler.fit_transform(df[cols])
        joblib.dump(scaler, scaler_path)
    else:
        scaler: MinMaxScaler = joblib.load(scaler_path)
        df[cols] = scaler.transform(df[cols])

    return df


def main():
    print("📡 Đang đọc dữ liệu từ SQL Server...")

    # 1. Dataset dự báo trễ hạn
    risk_df = load_dataset_du_bao_tre_han()
    risk_df = clean_risk_dataset(risk_df)
    risk_cols_to_normalize = RISK_FEATURE_COLS + ["DoUuTien_Encoded"]
    risk_normalized = normalize_features(risk_df.copy(), risk_cols_to_normalize, fit=True, scaler_name="risk_scaler.joblib")

    risk_df.to_csv(PROCESSED_DIR / "risk_clean.csv", index=False)
    risk_normalized.to_csv(PROCESSED_DIR / "risk_normalized.csv", index=False)
    print(f"   ✅ v_Dataset_DuBaoTreHan: {len(risk_df)} dòng -> {PROCESSED_DIR / 'risk_normalized.csv'}")

    # 2. Dataset đề xuất giao việc
    assign_df = load_dataset_de_xuat_giao_viec()
    assign_df = assign_df.dropna(subset=["Nhan_GiaoViecHieuQua"])
    assign_df.to_csv(PROCESSED_DIR / "assignment_clean.csv", index=False)
    print(f"v_Dataset_DeXuatGiaoViec: {len(assign_df)} dòng -> {PROCESSED_DIR / 'assignment_clean.csv'}")

    # 3. Dataset phát hiện điểm nghẽn
    bottleneck_df = load_dataset_phat_hien_diem_nghen()
    bottleneck_df.to_csv(PROCESSED_DIR / "bottleneck_clean.csv", index=False)
    print(f" v_Dataset_PhatHienDiemNghen: {len(bottleneck_df)} dòng -> {PROCESSED_DIR / 'bottleneck_clean.csv'}")

    # 4. Bảng phụ thuộc công việc (cho graph/GNN)
    phu_thuoc_df = load_phu_thuoc_cong_viec()
    phu_thuoc_df.to_csv(PROCESSED_DIR / "phu_thuoc_clean.csv", index=False)
    print(f" PhuThuocCongViec: {len(phu_thuoc_df)} dòng -> {PROCESSED_DIR / 'phu_thuoc_clean.csv'}")

    # 5. Bảng nhân sự (cho staff matching)
    nhan_su_df = load_nguoi_dung()
    nhan_su_df.to_csv(PROCESSED_DIR / "nhan_su_clean.csv", index=False)
    print(f" NguoiDung: {len(nhan_su_df)} dòng -> {PROCESSED_DIR / 'nhan_su_clean.csv'}")

    print(f"\nTiền xử lý hoàn tất. Scaler lưu tại: {MODEL_DIR / 'risk_scaler.joblib'}")


if __name__ == "__main__":
    main()
