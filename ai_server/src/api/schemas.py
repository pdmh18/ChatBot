"""Pydantic schemas — định nghĩa request/response cho API."""
from pydantic import BaseModel, Field
from typing import Optional


class TaskRiskRequest(BaseModel):
    SoGioUocTinh: float = Field(..., ge=0, description="Số giờ ước tính hoàn thành task")
    SoNamKinhNghiemNhanSu: float = Field(..., ge=0, le=50, description="Số năm kinh nghiệm người phụ trách")
    KhoiLuongHienTaiNhanSu: float = Field(..., ge=0, description="Khối lượng công việc hiện tại của nhân sự")
    SoCongViecPhuThuocTruoc: int = Field(..., ge=0, description="Số task phải hoàn thành trước task này")
    DoUuTien_Encoded: int = Field(..., ge=0, le=3, description="Độ ưu tiên đã mã hóa: 0=Thấp,1=Trung bình,2=Cao,3=Khẩn cấp")


class TaskRiskResponse(BaseModel):
    xac_suat_tre_han: float
    du_bao_tre_han: bool
    muc_do_rui_ro: str


class StaffMatchRequest(BaseModel):
    do_phuc_tap: float = Field(0.5, ge=0, le=1, description="Độ phức tạp ước lượng của task (0-1)")
    top_n: int = Field(5, ge=1, le=100, description="Số lượng nhân sự phù hợp nhất cần trả về")


class StaffMatchResult(BaseModel):
    MaNguoiDung: int
    HoTen: str
    diem_phu_hop: float
    phan_tram_tai_hien_tai: float


class BottleneckResult(BaseModel):
    MaCongViec: int
    trang_thai: Optional[str] = None
    so_task_phu_thuoc_vao_no: int
    diem_trung_tam: float
    diem_bottleneck: float


class ChatRequest(BaseModel):
    message: str
    history: list[dict] = []


class ChatResponse(BaseModel):
    reply: str
