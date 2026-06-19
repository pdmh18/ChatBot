"""
Chấm điểm độ phù hợp giữa nhân sự và task (rule-based + thống kê).
Có thể nâng cấp thành model học máy khi có dữ liệu lịch sử phân công thực tế.
Chạy: python -m src.models.staff_matching
"""
import pandas as pd
from pathlib import Path

PROCESSED_DIR = Path(__file__).resolve().parent.parent.parent / "data" / "processed"


def calculate_fit_score(nhan_su_row: dict, task_ky_nang_yeu_cau: str, do_phuc_tap: float) -> dict:
    """
    Công thức chấm điểm phù hợp (0-100):
    - 40%: khớp kỹ năng chính
    - 30%: hiệu suất lịch sử
    - 20%: kinh nghiệm phù hợp độ phức tạp task
    - 10%: nghịch đảo khối lượng công việc hiện tại (workload thấp -> điểm cao)
    """
    skill_match = 1.0 if nhan_su_row["ky_nang_chinh"] == task_ky_nang_yeu_cau else 0.3
    perf_score = nhan_su_row["diem_hieu_suat"]
    exp_score = min(nhan_su_row["nam_kinh_nghiem"] / 10, 1.0) * (0.5 + do_phuc_tap * 0.5)
    workload_score = max(0, 1 - nhan_su_row["so_task_dang_lam"] / 6)

    fit_score = (
        0.40 * skill_match
        + 0.30 * perf_score
        + 0.20 * exp_score
        + 0.10 * workload_score
    ) * 100

    return {
        "nhan_su_id": nhan_su_row["nhan_su_id"],
        "diem_phu_hop": round(fit_score, 2),
    }


def rank_candidates(task_ky_nang_yeu_cau: str, do_phuc_tap: float, top_n: int = 5) -> list:
    """Xếp hạng top N nhân sự phù hợp nhất cho 1 task."""
    nhan_su_df = pd.read_csv(PROCESSED_DIR / "nhan_su_clean.csv")

    scores = [
        calculate_fit_score(row.to_dict(), task_ky_nang_yeu_cau, do_phuc_tap)
        for _, row in nhan_su_df.iterrows()
    ]
    scores_sorted = sorted(scores, key=lambda x: x["diem_phu_hop"], reverse=True)
    return scores_sorted[:top_n]


if __name__ == "__main__":
    result = rank_candidates(task_ky_nang_yeu_cau="Backend", do_phuc_tap=0.7, top_n=5)
    for r in result:
        print(r)
