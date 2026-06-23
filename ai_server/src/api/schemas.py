"""Pydantic schemas — định nghĩa request/response cho API."""
from pydantic import BaseModel, Field
from typing import Optional


# ============================================================
# XGBoost — Dự báo trễ hạn
# ============================================================

class TaskRiskRequest(BaseModel):
    SoGioUocTinh: float = Field(..., ge=0, description="Số giờ ước tính hoàn thành task")
    SoNamKinhNghiemNhanSu: float = Field(..., ge=0, le=50, description="Số năm kinh nghiệm người phụ trách")
    KhoiLuongHienTaiNhanSu: float = Field(..., ge=0, le=1, description="Khối lượng hiện tại (0-1)")
    SoCongViecPhuThuocTruoc: int = Field(..., ge=0, description="Số task phải xong trước task này")
    DoUuTien_Encoded: int = Field(..., ge=0, le=3, description="Độ ưu tiên: 0=Thấp, 1=TB, 2=Cao, 3=Khẩn")

class TaskRiskResponse(BaseModel):
    xac_suat_tre_han: float
    du_bao_tre_han: bool
    muc_do_rui_ro: str


# ============================================================
# Random Forest — Đề xuất giao việc
# ============================================================

class StaffMatchRequest(BaseModel):
    SoGioUocTinh: float = Field(..., ge=0, description="Số giờ ước tính task")
    PhanTramTaiNhanSu: float = Field(..., ge=0, le=1, description="% khối lượng nhân sự đang bận (0-1)")
    DiemChatLuongTrungBinhLichSu: float = Field(..., ge=0, le=10, description="Điểm chất lượng trung bình lịch sử (0-10)")

class StaffMatchResponse(BaseModel):
    xac_suat_hieu_qua: float
    de_xuat_giao_viec: bool
    muc_do_phu_hop: str


# ============================================================
# GNN — Phát hiện điểm nghẽn
# ============================================================

class BottleneckResult(BaseModel):
    MaCongViec: int
    SoTaskBiAnhHuongPhiaSau: int
    bottleneck_score: float


# ============================================================
# Chat
# ============================================================

class ChatRequest(BaseModel):
    message: str
    history: list[dict] = []

class ChatResponse(BaseModel):
    reply: str