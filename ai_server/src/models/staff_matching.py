"""
Chấm điểm độ phù hợp giữa nhân sự và task — dùng dữ liệu thật từ SQL Server.
Có thể nâng cấp thành model học máy thật khi tích lũy đủ dữ liệu từ
view v_Dataset_DeXuatGiaoViec (đã có sẵn label Nhan_GiaoViecHieuQua).

Chạy: python -m src.models.staff_matching
"""
from email.policy import default

import pandas as pd
from pathlib import Path

PROCESSED_DIR = Path(__file__).resolve().parent.parent.parent / "data" / "processed"


def calculate_fit_score(nhan_su_row: dict, do_phuc_tap: float = 0.5) -> dict:
    """
    Công thức chấm điểm phù hợp (0-100) dựa trên dữ liệu bảng NguoiDung:
    - 50%: hiệu suất lịch sử (ước lượng qua kinh nghiệm vì chưa join điểm đánh giá)
    - 30%: kinh nghiệm phù hợp độ phức tạp task
    - 20%: nghịch đảo phần trăm tải hiện tại (workload thấp -> điểm cao)
    """
    # so_nam_kinh_nghiem = nhan_su_row.get("SoNamKinhNghiem") or 0
    # khoi_luong_hien_tai = nhan_su_row.get("KhoiLuongHienTai") or 0
    # khoi_luong_toi_da = nhan_su_row.get("KhoiLuongToiDa") or 1
    def numeric_or_default(value, default):
        return default if value is None or pd.isna(value) else value

    so_nam_kinh_nghiem = numeric_or_default(nhan_su_row.get("SoNamKinhNghiem"), 0)
    khoi_luong_hien_tai = numeric_or_default(nhan_su_row.get("KhoiLuongHienTai"), 0)
    khoi_luong_toi_da = numeric_or_default(nhan_su_row.get("KhoiLuongToiDa"), 1)


    exp_score = min(so_nam_kinh_nghiem / 10, 1.0) * (0.5 + do_phuc_tap * 0.5)
    phan_tram_tai = khoi_luong_hien_tai / khoi_luong_toi_da if khoi_luong_toi_da else 1
    workload_score = max(0, 1 - phan_tram_tai)

    fit_score = (
        0.50 * min(so_nam_kinh_nghiem / 10, 1.0)
        + 0.30 * exp_score
        + 0.20 * workload_score
    ) * 100

    return {
        "MaNguoiDung": nhan_su_row["MaNguoiDung"],
        "HoTen": nhan_su_row.get("HoTen", ""),
        "diem_phu_hop": round(fit_score, 2),
        "phan_tram_tai_hien_tai": round(phan_tram_tai * 100, 1),
    }


def rank_candidates(do_phuc_tap: float = 0.5, top_n: int = 5) -> list:
    """Xếp hạng top N nhân sự phù hợp nhất (đang hoạt động) cho 1 task."""
    nhan_su_df = pd.read_csv(PROCESSED_DIR / "nhan_su_clean.csv")

    scores = [
        calculate_fit_score(row.to_dict(), do_phuc_tap)
        for _, row in nhan_su_df.iterrows()
    ]
    scores_sorted = sorted(scores, key=lambda x: x["diem_phu_hop"], reverse=True)
    return scores_sorted[:top_n]


if __name__ == "__main__":
    result = rank_candidates(do_phuc_tap=0.7, top_n=5)
    for r in result:
        print(r)
