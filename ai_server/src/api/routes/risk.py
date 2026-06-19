"""Routes liên quan đến dự báo rủi ro & tối ưu nguồn lực."""
from fastapi import APIRouter, HTTPException
from src.api.schemas import (
    TaskRiskRequest, TaskRiskResponse,
    StaffMatchRequest, StaffMatchResult,
    BottleneckResult,
)
from src.models.train_risk_model import predict_risk
from src.models.staff_matching import rank_candidates
from src.models.bottleneck_detector import build_graph, detect_bottlenecks

router = APIRouter(prefix="/api", tags=["AI - Risk & Resource"])


@router.post("/predict-risk", response_model=TaskRiskResponse)
def predict_task_risk(req: TaskRiskRequest):
    """Dự báo xác suất 1 task bị trễ hạn (dùng cho khi PM tạo task mới)."""
    try:
        result = predict_risk(req.model_dump())
        return result
    except FileNotFoundError:
        raise HTTPException(
            status_code=503,
            detail="Model chưa được train. Chạy: python -m src.models.train_risk_model"
        )


@router.post("/match-staff", response_model=list[StaffMatchResult])
def match_staff(req: StaffMatchRequest):
    """Xếp hạng nhân sự đang hoạt động phù hợp nhất cho 1 task."""
    return rank_candidates(req.do_phuc_tap, req.top_n)


@router.get("/analyze-bottleneck", response_model=list[BottleneckResult])
def analyze_bottleneck(top_n: int = 10):
    """Phân tích đồ thị phụ thuộc công việc (PhuThuocCongViec), tìm điểm nghẽn dự án."""
    G = build_graph()
    return detect_bottlenecks(G, top_n=top_n)
