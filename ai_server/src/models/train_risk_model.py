"""
Train mô hình XGBoost dự báo task trễ hạn.
Dùng dữ liệu đã xử lý từ clean_data.py (risk_train.csv, risk_test.csv)

Chạy: python -m src.models.train_risk_model
"""

import pandas as pd
import joblib
import numpy as np
from pathlib import Path
from sklearn.metrics import (
    classification_report,
    roc_auc_score,
    confusion_matrix,
    ConfusionMatrixDisplay,
    roc_curve
)
from sklearn.model_selection import StratifiedKFold, cross_val_score, train_test_split
from xgboost import XGBClassifier
import matplotlib.pyplot as plt
from imblearn.over_sampling import SMOTE



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
    "SoNamKinhNghiemNhanSu",
    "KhoiLuongHienTaiNhanSu",
    "SoCongViecPhuThuocTruoc",
    "DoUuTien_Encoded",
]
LABEL_COL = "Nhan_CoTreHan"


# ============================================================
# TRAIN MODEL
# ============================================================

def train():
    print("=" * 60)
    print("TRAIN MODEL — XGBoost Dự Báo Trễ Hạn")
    print("=" * 60)

    # --------------------------------------------------------
    # 1. ĐỌC DỮ LIỆU ĐÃ XỬ LÝ TỪ clean_data.py
    # --------------------------------------------------------
    print("\n📌 [1/4] Đọc dữ liệu...")
    train_df = pd.read_csv(PROCESSED_DIR / "risk_train.csv")
    test_df  = pd.read_csv(PROCESSED_DIR / "risk_test.csv")

    X_train = train_df[FEATURE_COLS]
    y_train = train_df[LABEL_COL]
    X_test  = test_df[FEATURE_COLS]
    y_test  = test_df[LABEL_COL]

    print(f"   Train: {len(X_train)} dòng | Test: {len(X_test)} dòng")
    print(f"   Train label: {dict(y_train.value_counts())}")
    print(f"   Test label:  {dict(y_test.value_counts())}")

    #2.	Áp dụng SMOTE cân bằng nhãn trên tập train (tỉ lệ 0/1 chênh > 20%).
    ratio = y_train.value_counts().min() / y_train.value_counts().max()
    if ratio < 0.8:
        print("\n   Áp dụng SMOTE để cân bằng dữ liệu train...")
        smote = SMOTE(random_state=42)
        X_train, y_train = smote.fit_resample(X_train, y_train)
        print(f"   Train sau SMOTE: {len(X_train)} dòng | {dict(pd.Series(y_train).value_counts())}")
    else:
        print(f"   Dữ liệu cân bằng ({ratio:.0%}) → không cần SMOTE")

    # --------------------------------------------------------
    # 2. ĐỊNH NGHĨA MODEL XGBOOST
    # --------------------------------------------------------
    print("\n📌 [2/4] Cấu hình XGBoost...")

    """
    Giải thích các tham số:
    - n_estimators:   số cây quyết định (200 cây)
    - max_depth:      độ sâu tối đa mỗi cây (tránh overfitting)
    - learning_rate:  tốc độ học (0.1 = học chậm, chính xác hơn)
    - subsample:      dùng 80% dữ liệu mỗi lần train 1 cây
    - colsample_bytree: dùng 80% feature mỗi lần train 1 cây
    - scale_pos_weight: cân bằng class (số class 0 / số class 1)
                        → quan trọng vì dữ liệu gốc mất cân bằng
    - reg_alpha:      L1 regularization (tránh overfitting)
    - reg_lambda:     L2 regularization (tránh overfitting)
    - eval_metric:    dùng logloss để đánh giá trong quá trình train
    - early_stopping_rounds: dừng sớm nếu không cải thiện sau 5 vòng
    """
    model = XGBClassifier(
        n_estimators        = 100,
        max_depth           = 4,
        learning_rate       = 0.05,
        subsample           = 0.8,
        colsample_bytree    = 0.8,
        reg_alpha           = 0.1,
        reg_lambda          = 1.0,
        eval_metric         = "logloss",
        early_stopping_rounds = 5,
        random_state        = 42,
        n_jobs              = -1,
    )

    print("   Tham số XGBoost:")
    print(f"     n_estimators:   100")
    print(f"     max_depth:      4")
    print(f"     learning_rate:  0.05")
    print(f"     subsample:      0.8")
    print(f"     colsample_bytree: 0.8")
    print(f"     reg_alpha (L1): 0.1")
    print(f"     reg_lambda (L2): 1.0")

    # --------------------------------------------------------
    # 3. Cross-Validation 5-fold đánh giá độ ổn định trước khi train chính thức.
    # --------------------------------------------------------
    print("\n📌 [3/4] Cross-Validation (5-fold)...")
    """
    StratifiedKFold: chia 5 fold, mỗi fold giữ tỉ lệ class
    → Đánh giá model khách quan hơn train/test split đơn giản
    → Phát hiện overfitting sớm
    """
    cv_model = XGBClassifier(
        n_estimators     = 100, #•	Số cây: 100 
        max_depth        = 4, #Độ sâu tối đa: 4 
        learning_rate    = 0.05, #Tốc độ học: 0.05
        subsample        = 0.8, #Sử dụng 80% dữ liệu mỗi lần train 1 cây
        colsample_bytree = 0.8, #Sử dụng 80% feature mỗi lần train 1 cây
        reg_alpha        = 0.1, #L1 regularization (tránh overfitting)
        reg_lambda       = 1.0, #L2 regularization (tránh overfitting)
        eval_metric      = "logloss", #Dùng logloss để đánh giá trong quá trình train
        random_state     = 42, 
        n_jobs           = -1,
    )

    skf = StratifiedKFold(n_splits=5, shuffle=True, random_state=42)
    cv_scores = cross_val_score(
        cv_model, X_train, y_train,
        cv=skf, scoring="roc_auc", n_jobs=-1
    )

    print(f"   ROC-AUC mỗi fold: {[round(s, 3) for s in cv_scores]}")
    print(f"   ROC-AUC trung bình: {cv_scores.mean():.3f} ± {cv_scores.std():.3f}")

    # --------------------------------------------------------
    # 4. TRAIN CHÍNH THỨC + ĐÁNH GIÁ
    # --------------------------------------------------------
    print("\n📌 [4/4] Train chính thức...")
    X_fit, X_val, y_fit, y_val = train_test_split(
        X_train,
        y_train,
        test_size=0.2,
        random_state=42,
        stratify=y_train,
    )
    model.fit(
        X_fit, y_fit,
        eval_set=[(X_val, y_val)],
        verbose=True,
    )

    y_pred  = model.predict(X_test)
    y_proba = model.predict_proba(X_test)[:, 1]

    print("\n=== KẾT QUẢ ĐÁNH GIÁ ===")
    print(classification_report(
        y_test, y_pred,
        target_names=["Đúng hạn", "Trễ hạn"]
    ))
    roc_auc = roc_auc_score(y_test, y_proba)
    print(f"ROC-AUC: {roc_auc:.3f}")

    # 5.	Tìm ngưỡng phân loại tối ưu bằng ROC Curve thay vì dùng mặc định 0.5.

    fpr, tpr, thresholds = roc_curve(y_test, y_proba)
    optimal_idx = np.argmax(tpr - fpr)
    optimal_threshold = thresholds[optimal_idx]
    print(f"\nNgưỡng tối ưu: {optimal_threshold:.3f}")

    # Predict lại với ngưỡng tối ưu
    y_pred_optimal = (y_proba >= optimal_threshold).astype(int)
    print("\n=== KẾT QUẢ VỚI NGƯỠNG TỐI ƯU ===")
    print(classification_report(y_test, y_pred_optimal,
        target_names=["Đúng hạn", "Trễ hạn"]))
    # Đánh giá chất lượng model
    if roc_auc >= 0.85:
        print("✅ Model TỐT — ROC-AUC ≥ 0.85")
    elif roc_auc >= 0.70:
        print("⚠️  Model TRUNG BÌNH — ROC-AUC 0.70-0.85")
    else:
        print("❌ Model YẾU — ROC-AUC < 0.70, cần xem lại dữ liệu")

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
        print(f"  {name:<35} {bar} {score:.3f}")

    # --------------------------------------------------------
    # 6. LƯU MODEL
    # --------------------------------------------------------
    model_path = MODEL_DIR / "risk_model.joblib"
    joblib.dump(model, model_path)
    print(f"\n✅ Model lưu tại: {model_path}")

    # Lưu thêm metadata
    metadata = {
        "feature_cols":     FEATURE_COLS,
        "label_col":        LABEL_COL,
        "roc_auc":          round(roc_auc, 4),
        "optimal_threshold": round(float(optimal_threshold), 4),
        "cv_roc_auc_mean":  round(cv_scores.mean(), 4),
        "cv_roc_auc_std":   round(cv_scores.std(), 4),
        "n_train":          len(X_train),
        "n_test":           len(X_test),
    }
    joblib.dump(metadata, MODEL_DIR / "risk_model_metadata.joblib")
    print(f"✅ Metadata lưu tại: {MODEL_DIR / 'risk_model_metadata.joblib'}")

    return model


# ============================================================
# PREDICT — dùng khi API gọi
# ============================================================


def predict_risk(task_features: dict, model=None, threshold=None) -> dict:
    """
    Dự báo task có nguy cơ trễ hạn không.
    Thêm trường nguyen_nhan dựa trên Feature Importance.
 
    Input: dict với các key trong FEATURE_COLS
    Output: dict với xác suất, dự báo, mức độ rủi ro và nguyên nhân
    """
    import pandas as pd
    import joblib
    from pathlib import Path
 
    MODEL_DIR = Path(__file__).resolve().parent.parent.parent / "models"
    FEATURE_COLS = [
        "SoGioUocTinh",
        "SoNamKinhNghiemNhanSu",
        "KhoiLuongHienTaiNhanSu",
        "SoCongViecPhuThuocTruoc",
        "DoUuTien_Encoded",
    ]
 
    # Load scaler + model + metadata
    scaler   = joblib.load(MODEL_DIR / "risk_pipeline_scaler.joblib")
    if model is None:
        model = joblib.load(MODEL_DIR / "risk_model.joblib")
    metadata = joblib.load(MODEL_DIR / "risk_model_metadata.joblib")
    feature_cols = metadata["feature_cols"]  # ← đọc từ metadata
    if threshold is None:
        threshold = metadata["optimal_threshold"]
 
    # Chuẩn hóa input
    X_raw = pd.DataFrame([task_features])[feature_cols]
    X_scaled = scaler.transform(X_raw)
    X        = pd.DataFrame(X_scaled, columns=feature_cols)
 
    proba = model.predict_proba(X)[0, 1]
    label = int(proba >= threshold)
 
    # --------------------------------------------------------
    # THÊM MỚI: Tính nguyên nhân từ Feature Importance + giá trị thực
    # --------------------------------------------------------
    reasons = []
 
    so_gio             = task_features.get("SoGioUocTinh", 0)
    phu_thuoc          = task_features.get("SoCongViecPhuThuocTruoc", 0)
    khoi_luong         = task_features.get("KhoiLuongHienTaiNhanSu", 0)
    kinh_nghiem        = task_features.get("SoNamKinhNghiemNhanSu", 0)
    do_uu_tien         = task_features.get("DoUuTien_Encoded", 0)
 
    # Feature quan trọng nhất: SoCongViecPhuThuocTruoc (77.7%)
    if phu_thuoc >= 2:
        reasons.append(f"Phụ thuộc {int(phu_thuoc)} task trước chưa hoàn thành")
    elif phu_thuoc == 1:
        reasons.append("Phụ thuộc 1 task trước chưa hoàn thành")
 
    # Feature quan trọng thứ 2: SoGioUocTinh (14.1%)
    if so_gio > 32:
        reasons.append(f"Ước tính {int(so_gio)}h — vượt ngưỡng rủi ro (>32h)")
    elif so_gio > 24:
        reasons.append(f"Ước tính {int(so_gio)}h — tương đối lớn")
 
    # KhoiLuongHienTaiNhanSu
    if khoi_luong >= 0.8:
        reasons.append(f"Nhân sự đang quá tải ({int(khoi_luong*100)}%)")
    elif khoi_luong >= 0.6:
        reasons.append(f"Nhân sự đang bận ({int(khoi_luong*100)}%)")
 
    # SoNamKinhNghiemNhanSu
    if kinh_nghiem <= 1:
        reasons.append("Nhân sự ít kinh nghiệm (≤1 năm)")
 
    # DoUuTien
    if do_uu_tien == 3:
        reasons.append("Task khẩn cấp — áp lực deadline cao")
 
    # Nếu không có lý do cụ thể
    if not reasons:
        if label == 1:
            reasons.append("Kết hợp nhiều yếu tố rủi ro nhỏ")
        else:
            reasons.append("Không có yếu tố rủi ro đáng kể")
 
    nguyen_nhan = " & ".join(reasons)
 
    if proba >= max(threshold, 0.7):
        muc_do_rui_ro = "Cao"
    elif proba >= threshold:
        muc_do_rui_ro = "Trung bình"
    else:
        muc_do_rui_ro = "Thấp"

    return {
        "xac_suat_tre_han": round(float(proba), 4),
        "du_bao_tre_han":   bool(label),
        "muc_do_rui_ro":    muc_do_rui_ro,
        "nguyen_nhan":      nguyen_nhan,
    }


if __name__ == "__main__":
    train()