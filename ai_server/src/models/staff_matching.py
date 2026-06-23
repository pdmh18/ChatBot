"""
Train mô hình Random Forest chấm điểm độ phù hợp nhân sự.
Dùng dữ liệu đã xử lý từ clean_data.py (assignment_train.csv, assignment_test.csv)

Chạy: python -m src.models.staff_matching
"""

import pandas as pd
import joblib
import numpy as np
from pathlib import Path
from sklearn.ensemble import RandomForestClassifier
from sklearn.metrics import (
    classification_report,
    roc_auc_score,
    roc_curve,
)
from sklearn.model_selection import StratifiedKFold, cross_val_score

# ============================================================
# ĐƯỜNG DẪN
# ============================================================
PROCESSED_DIR = Path(__file__).resolve().parent.parent.parent / "data" / "processed"
MODEL_DIR     = Path(__file__).resolve().parent.parent.parent / "models"
MODEL_DIR.mkdir(parents=True, exist_ok=True)

# ============================================================
# CẤU HÌNH
# ============================================================
FEATURE_COLS = [
    "SoGioUocTinh",
    "PhanTramTaiNhanSu",
    "DiemChatLuongTrungBinhLichSu",
]
LABEL_COL = "Nhan_GiaoViecHieuQua"


# ============================================================
# TRAIN MODEL
# ============================================================

def train():
    print("=" * 60)
    print("TRAIN MODEL — Random Forest Đề Xuất Giao Việc")
    print("=" * 60)

    # --------------------------------------------------------
    # 1. ĐỌC DỮ LIỆU
    # --------------------------------------------------------
    print("\n📌 [1/4] Đọc dữ liệu...")
    train_df = pd.read_csv(PROCESSED_DIR / "assignment_train.csv")
    test_df  = pd.read_csv(PROCESSED_DIR / "assignment_test.csv")

    X_train = train_df[FEATURE_COLS]
    y_train = train_df[LABEL_COL]
    X_test  = test_df[FEATURE_COLS]
    y_test  = test_df[LABEL_COL]

    print(f"   Train: {len(X_train)} dòng | Test: {len(X_test)} dòng")
    print(f"   Train label: {dict(y_train.value_counts())}")
    print(f"   Test label:  {dict(y_test.value_counts())}")

    # --------------------------------------------------------
    # 2. ĐỊNH NGHĨA MODEL RANDOM FOREST
    # --------------------------------------------------------
    print("\n📌 [2/4] Cấu hình Random Forest...")

    """
    Giải thích tham số:
    - n_estimators:   số cây quyết định (200 cây)
    - max_depth:      độ sâu tối đa mỗi cây (tránh overfitting)
    - min_samples_split: tối thiểu bao nhiêu mẫu để chia nhánh
    - min_samples_leaf:  tối thiểu bao nhiêu mẫu ở lá
    - max_features:   số feature xem xét mỗi lần chia ('sqrt' = căn bậc 2)
    - class_weight:   tự động cân bằng class theo tỉ lệ
    - random_state:   cố định kết quả
    - n_jobs:         dùng tất cả CPU core
    """
    model = RandomForestClassifier(
        n_estimators     = 200,
        max_depth        = 6,
        min_samples_split= 5,
        min_samples_leaf = 2,
        max_features     = 'sqrt',
        class_weight     = 'balanced',
        random_state     = 42,
        n_jobs           = -1,
    )

    print("   Tham số Random Forest:")
    print("     n_estimators:      200")
    print("     max_depth:         6")
    print("     min_samples_split: 5")
    print("     min_samples_leaf:  2")
    print("     max_features:      sqrt")
    print("     class_weight:      balanced")

    # --------------------------------------------------------
    # 3. CROSS-VALIDATION
    # --------------------------------------------------------
    print("\n📌 [3/4] Cross-Validation (5-fold)...")

    skf = StratifiedKFold(n_splits=5, shuffle=True, random_state=42)
    cv_scores = cross_val_score(
        model, X_train, y_train,
        cv=skf, scoring="roc_auc", n_jobs=-1
    )

    print(f"   ROC-AUC mỗi fold: {[round(s, 3) for s in cv_scores]}")
    print(f"   ROC-AUC trung bình: {cv_scores.mean():.3f} ± {cv_scores.std():.3f}")

    # --------------------------------------------------------
    # 4. TRAIN CHÍNH THỨC + ĐÁNH GIÁ
    # --------------------------------------------------------
    print("\n📌 [4/4] Train chính thức...")
    model.fit(X_train, y_train)

    y_pred  = model.predict(X_test)
    y_proba = model.predict_proba(X_test)[:, 1]

    print("\n=== KẾT QUẢ ĐÁNH GIÁ ===")
    print(classification_report(
        y_test, y_pred,
        target_names=["Không hiệu quả", "Hiệu quả"]
    ))
    roc_auc = roc_auc_score(y_test, y_proba)
    print(f"ROC-AUC: {roc_auc:.3f}")

    # Đánh giá chất lượng
    if roc_auc >= 0.85:
        print("✅ Model TỐT — ROC-AUC ≥ 0.85")
    elif roc_auc >= 0.70:
        print("⚠️  Model TRUNG BÌNH — ROC-AUC 0.70-0.85")
    else:
        print("❌ Model YẾU — ROC-AUC < 0.70")

    # Tìm ngưỡng tối ưu
    fpr, tpr, thresholds = roc_curve(y_test, y_proba)
    optimal_idx       = np.argmax(tpr - fpr)
    optimal_threshold = thresholds[optimal_idx]
    print(f"\nNgưỡng tối ưu: {optimal_threshold:.3f}")

    y_pred_optimal = (y_proba >= optimal_threshold).astype(int)
    print("\n=== KẾT QUẢ VỚI NGƯỠNG TỐI ƯU ===")
    print(classification_report(
        y_test, y_pred_optimal,
        target_names=["Không hiệu quả", "Hiệu quả"]
    ))

    # --------------------------------------------------------
    # 5. FEATURE IMPORTANCE
    # --------------------------------------------------------
    print("\n=== MỨC ĐỘ QUAN TRỌNG TỪNG ĐẶC TRƯNG ===")
    importance = sorted(
        zip(FEATURE_COLS, model.feature_importances_),
        key=lambda x: -x[1]
    )
    for name, score in importance:
        bar = "█" * int(score * 50)
        print(f"  {name:<40} {bar} {score:.3f}")

    # --------------------------------------------------------
    # 6. LƯU MODEL
    # --------------------------------------------------------
    model_path    = MODEL_DIR / "staff_matching_model.joblib"
    metadata_path = MODEL_DIR / "staff_matching_metadata.joblib"

    joblib.dump(model, model_path)
    print(f"\n✅ Model lưu tại: {model_path}")

    metadata = {
        "feature_cols":      FEATURE_COLS,
        "label_col":         LABEL_COL,
        "roc_auc":           round(roc_auc, 4),
        "cv_roc_auc_mean":   round(cv_scores.mean(), 4),
        "cv_roc_auc_std":    round(cv_scores.std(), 4),
        "optimal_threshold": round(float(optimal_threshold), 4),
        "n_train":           len(X_train),
        "n_test":            len(X_test),
    }
    joblib.dump(metadata, metadata_path)
    print(f"✅ Metadata lưu tại: {metadata_path}")

    return model, optimal_threshold


# ============================================================
# PREDICT — dùng khi API gọi
# ============================================================

def predict_staff_match(task_features: dict, model=None, threshold=None) -> dict:
    """
    Dự báo việc giao task cho dev có hiệu quả không.

    Input: dict với các key trong FEATURE_COLS
    Ví dụ:
      {
        "SoGioUocTinh": 16,
        "PhanTramTaiNhanSu": 0.4,
        "DiemChatLuongTrungBinhLichSu": 8.5
      }

    Output: dict với xác suất và đề xuất
    """
    # Load scaler
    scaler_path = MODEL_DIR / "assignment_pipeline_scaler.joblib"
    scaler      = joblib.load(scaler_path)

    # Load model + metadata
    if model is None:
        model = joblib.load(MODEL_DIR / "staff_matching_model.joblib")

    metadata = joblib.load(MODEL_DIR / "staff_matching_metadata.joblib")
    if threshold is None:
        threshold = metadata["optimal_threshold"]

    # Chuẩn hóa input
    X_raw    = pd.DataFrame([task_features])[FEATURE_COLS]
    X_scaled = scaler.transform(X_raw)
    X        = pd.DataFrame(X_scaled, columns=FEATURE_COLS)

    proba = model.predict_proba(X)[0, 1]
    label = int(proba >= threshold)
    muc_do_phu_hop = "Cao" if label else (
        "Trung bình" if proba >= threshold * 0.8 else "Thấp"
    )

    return {
        "xac_suat_hieu_qua":  round(float(proba), 4),
        "de_xuat_giao_viec":  bool(label),
        "muc_do_phu_hop":     muc_do_phu_hop,
    }

if __name__ == "__main__":
    train()