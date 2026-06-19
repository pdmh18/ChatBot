"""
Tiền xử lý & chuẩn hóa dữ liệu trước khi đưa vào model.
Chạy: python -m src.preprocessing.clean_data
"""
import pandas as pd
from pathlib import Path
from sklearn.preprocessing import MinMaxScaler, LabelEncoder
import joblib

RAW_DIR = Path(__file__).resolve().parent.parent.parent / "data" / "raw"
PROCESSED_DIR = Path(__file__).resolve().parent.parent.parent / "data" / "processed"
MODEL_DIR = Path(__file__).resolve().parent.parent.parent / "models"
PROCESSED_DIR.mkdir(parents=True, exist_ok=True)
MODEL_DIR.mkdir(parents=True, exist_ok=True)

NUMERIC_COLS = ["story_point", "do_phuc_tap", "so_nguoi_phu_thuoc", "kinh_nghiem_nguoi_lam"]


def load_raw_data():
    cong_viec = pd.read_csv(RAW_DIR / "cong_viec.csv")
    nhan_su = pd.read_csv(RAW_DIR / "nhan_su.csv")
    phu_thuoc = pd.read_csv(RAW_DIR / "phu_thuoc_cong_viec.csv")
    return cong_viec, nhan_su, phu_thuoc


def clean_cong_viec(df: pd.DataFrame) -> pd.DataFrame:
    """Làm sạch: loại trùng, xử lý null."""
    df = df.drop_duplicates(subset="task_id")
    df = df.dropna(subset=["task_id", "bi_tre_han"])
    df[NUMERIC_COLS] = df[NUMERIC_COLS].fillna(df[NUMERIC_COLS].median())
    return df


def normalize_features(df: pd.DataFrame, fit=True, scaler_path=None):
    """Chuẩn hóa (Min-Max) các cột numeric về [0, 1]."""
    scaler_path = scaler_path or MODEL_DIR / "scaler.joblib"

    if fit:
        scaler = MinMaxScaler()
        df[NUMERIC_COLS] = scaler.fit_transform(df[NUMERIC_COLS])
        joblib.dump(scaler, scaler_path)
    else:
        scaler: MinMaxScaler = joblib.load(scaler_path)
        df[NUMERIC_COLS] = scaler.transform(df[NUMERIC_COLS])

    return df


def main():
    cong_viec, nhan_su, phu_thuoc = load_raw_data()

    cong_viec = clean_cong_viec(cong_viec)
    cong_viec_normalized = normalize_features(cong_viec.copy(), fit=True)

    cong_viec.to_csv(PROCESSED_DIR / "cong_viec_clean.csv", index=False)
    cong_viec_normalized.to_csv(PROCESSED_DIR / "cong_viec_normalized.csv", index=False)
    nhan_su.to_csv(PROCESSED_DIR / "nhan_su_clean.csv", index=False)
    phu_thuoc.to_csv(PROCESSED_DIR / "phu_thuoc_clean.csv", index=False)

    print(f"Tiền xử lý xong, lưu tại: {PROCESSED_DIR}")
    print(f"   Scaler lưu tại: {MODEL_DIR / 'scaler.joblib'}")


if __name__ == "__main__":
    main()
