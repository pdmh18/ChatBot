"""
Train mô hình phân loại dự báo task trễ hạn (XGBoost).
Chạy: python -m src.models.train_risk_model
"""
import pandas as pd
import joblib
from pathlib import Path
from sklearn.model_selection import train_test_split
from sklearn.metrics import classification_report, roc_auc_score
from xgboost import XGBClassifier

PROCESSED_DIR = Path(__file__).resolve().parent.parent.parent / "data" / "processed"
MODEL_DIR = Path(__file__).resolve().parent.parent.parent / "models"
MODEL_DIR.mkdir(parents=True, exist_ok=True)

FEATURE_COLS = ["story_point", "do_phuc_tap", "so_nguoi_phu_thuoc", "kinh_nghiem_nguoi_lam"]
LABEL_COL = "bi_tre_han"


def train():
    df = pd.read_csv(PROCESSED_DIR / "cong_viec_normalized.csv")

    X = df[FEATURE_COLS]
    y = df[LABEL_COL]

    X_train, X_test, y_train, y_test = train_test_split(
        X, y, test_size=0.2, random_state=42, stratify=y
    )

    model = XGBClassifier(
        n_estimators=200,
        max_depth=4,
        learning_rate=0.1,
        eval_metric="logloss",
        random_state=42,
    )
    model.fit(X_train, y_train)

    y_pred = model.predict(X_test)
    y_proba = model.predict_proba(X_test)[:, 1]

    print("=== Đánh giá mô hình dự báo trễ hạn ===")
    print(classification_report(y_test, y_pred, target_names=["Đúng hạn", "Trễ hạn"]))
    print(f"ROC-AUC: {roc_auc_score(y_test, y_proba):.3f}")

    model_path = MODEL_DIR / "risk_model.joblib"
    joblib.dump(model, model_path)
    print(f" Đã lưu model tại: {model_path}")

    return model


def predict_risk(task_features: dict, model=None) -> dict:
    """
    task_features: dict với 4 keys trong FEATURE_COLS (đã normalize hoặc raw tùy use-case)
    Trả về: xác suất trễ hạn + nhãn dự đoán
    """
    if model is None:
        model = joblib.load(MODEL_DIR / "risk_model.joblib")

    X = pd.DataFrame([task_features])[FEATURE_COLS]
    proba = model.predict_proba(X)[0, 1]
    label = int(proba > 0.5)

    return {
        "xac_suat_tre_han": round(float(proba), 4),
        "du_bao_tre_han": bool(label),
        "muc_do_rui_ro": "Cao" if proba > 0.7 else ("Trung bình" if proba > 0.4 else "Thấp"),
    }


if __name__ == "__main__":
    train()
