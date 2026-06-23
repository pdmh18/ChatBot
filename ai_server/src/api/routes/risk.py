"""Routes cho AI — dự báo rủi ro & tối ưu nguồn lực."""
from fastapi import APIRouter, HTTPException
from src.api.schemas import (
    TaskRiskRequest, TaskRiskResponse,
    StaffMatchRequest, StaffMatchResponse,
    BottleneckResult,
)
from src.models.train_risk_model import predict_risk
from src.models.staff_matching import predict_staff_match
from src.models.bottleneck_detector import predict_bottleneck

router = APIRouter(prefix="/api", tags=["AI - Risk & Resource"])


# ============================================================
# ENDPOINT 1: XGBoost — Dự báo trễ hạn
# ============================================================

@router.post("/predict-risk", response_model=TaskRiskResponse)
def predict_task_risk(req: TaskRiskRequest):
    """
    Dự báo task có nguy cơ trễ hạn không.
    
    Input: thông tin task + nhân sự
    Output: xác suất trễ hạn + mức độ rủi ro
    """
    try:
        result = predict_risk(req.model_dump())
        return result
    except FileNotFoundError:
        raise HTTPException(
            status_code=503,
            detail="Model chưa train. Chạy: python -m src.models.train_risk_model"
        )
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))


# ============================================================
# ENDPOINT 2: Random Forest — Đề xuất giao việc
# ============================================================

@router.post("/match-staff", response_model=StaffMatchResponse)
def match_staff(req: StaffMatchRequest):
    """
    Dự báo việc giao task cho nhân sự có hiệu quả không.
    
    Input: thông tin task + thông tin nhân sự
    Output: xác suất hiệu quả + đề xuất giao/không giao
    """
    try:
        result = predict_staff_match(req.model_dump())
        return result
    except FileNotFoundError:
        raise HTTPException(
            status_code=503,
            detail="Model chưa train. Chạy: python -m src.models.staff_matching"
        )
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))


# ============================================================
# ENDPOINT 3: GNN — Phát hiện điểm nghẽn
# ============================================================

@router.get("/analyze-bottleneck", response_model=list[BottleneckResult])
def analyze_bottleneck(top_n: int = 10):
    """
    Phân tích đồ thị phụ thuộc công việc.
    Tìm và trả về top N task có nguy cơ là điểm nghẽn cao nhất.
    
    Output: danh sách task xếp hạng theo bottleneck_score
    """
    try:
        result = predict_bottleneck(top_n=top_n)
        return result
    except FileNotFoundError:
        raise HTTPException(
            status_code=503,
            detail="Model chưa train. Chạy: python -m src.models.bottleneck_detector"
        )
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))