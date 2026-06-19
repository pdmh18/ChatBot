"""Pydantic schemas — định nghĩa request/response cho API."""
from pydantic import BaseModel, Field
from typing import Optional


class TaskRiskRequest(BaseModel):
    story_point: float = Field(..., ge=0, description="Độ lớn công việc (1,2,3,5,8,13)")
    do_phuc_tap: float = Field(..., ge=0, le=1, description="Độ phức tạp (0-1)")
    so_nguoi_phu_thuoc: float = Field(..., ge=0, description="Số task phụ thuộc")
    kinh_nghiem_nguoi_lam: float = Field(..., ge=0, le=10, description="Số năm kinh nghiệm")


class TaskRiskResponse(BaseModel):
    xac_suat_tre_han: float
    du_bao_tre_han: bool
    muc_do_rui_ro: str


class StaffMatchRequest(BaseModel):
    ky_nang_yeu_cau: str
    do_phuc_tap: float = Field(..., ge=0, le=1)
    top_n: int = 5


class StaffMatchResult(BaseModel):
    nhan_su_id: str
    diem_phu_hop: float


class BottleneckResult(BaseModel):
    task_id: str
    so_task_phu_thuoc_vao_no: int
    diem_trung_tam: float
    diem_bottleneck: float


class ChatRequest(BaseModel):
    message: str
    history: list[dict] = []


class ChatResponse(BaseModel):
    reply: str
