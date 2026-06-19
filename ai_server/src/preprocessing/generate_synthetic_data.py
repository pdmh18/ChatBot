"""
Sinh dữ liệu giả lập (synthetic) cho 3 bảng:
- nhan_su (developers)
- cong_viec (tasks)
- phu_thuoc_cong_viec (task dependencies) -> dùng cho GNN

Chạy: python -m src.preprocessing.generate_synthetic_data
"""
import numpy as np
import pandas as pd
from pathlib import Path

np.random.seed(42)

RAW_DIR = Path(__file__).resolve().parent.parent.parent / "data" / "raw"
RAW_DIR.mkdir(parents=True, exist_ok=True)

SKILLS = ["Backend", "Frontend", "DevOps", "AI/ML", "QA", "Mobile"]


def generate_nhan_su(n=20) -> pd.DataFrame:
    """Bảng nhân sự: năng lực, khối lượng công việc hiện tại."""
    rows = []
    for i in range(n):
        rows.append({
            "nhan_su_id": f"NS{i+1:03d}",
            "ten": f"Developer_{i+1}",
            "ky_nang_chinh": np.random.choice(SKILLS),
            "nam_kinh_nghiem": np.random.randint(0, 10),
            "diem_hieu_suat": round(np.random.uniform(0.5, 1.0), 2),  # historical performance score
            "so_task_dang_lam": np.random.randint(0, 6),  # current workload
        })
    return pd.DataFrame(rows)


def generate_cong_viec(n=80, nhan_su_ids=None) -> pd.DataFrame:
    """Bảng công việc: thông tin task + nhãn trễ hạn (label để train)."""
    rows = []
    for i in range(n):
        story_point = np.random.choice([1, 2, 3, 5, 8, 13])
        do_phuc_tap = np.random.uniform(0.1, 1.0)
        so_nguoi_phu_thuoc = np.random.randint(0, 4)
        kinh_nghiem_nguoi_lam = np.random.uniform(0, 10)

        # luật giả lập để tạo nhãn "trễ hạn" có ý nghĩa thống kê (không hoàn toàn random)
        risk_score = (
            0.35 * (story_point / 13)
            + 0.25 * do_phuc_tap
            + 0.20 * (so_nguoi_phu_thuoc / 3)
            - 0.20 * (kinh_nghiem_nguoi_lam / 10)
        )
        bi_tre_han = 1 if risk_score + np.random.normal(0, 0.1) > 0.45 else 0

        rows.append({
            "task_id": f"TASK{i+1:04d}",
            "ten_task": f"Task {i+1}",
            "story_point": story_point,
            "do_phuc_tap": round(do_phuc_tap, 2),
            "so_nguoi_phu_thuoc": so_nguoi_phu_thuoc,
            "kinh_nghiem_nguoi_lam": round(kinh_nghiem_nguoi_lam, 2),
            "nhan_su_id": np.random.choice(nhan_su_ids) if nhan_su_ids is not None else None,
            "bi_tre_han": bi_tre_han,  # label: 1 = trễ, 0 = đúng hạn
        })
    return pd.DataFrame(rows)


def generate_phu_thuoc_cong_viec(task_ids, n_edges=60) -> pd.DataFrame:
    """
    Bảng phụ thuộc công việc: task A phải xong trước task B mới được làm.
    Dùng để xây đồ thị (graph) cho GNN tìm bottleneck.
    """
    edges = set()
    while len(edges) < n_edges:
        a, b = np.random.choice(task_ids, 2, replace=False)
        edges.add((a, b))

    rows = [{"task_truoc": a, "task_sau": b} for a, b in edges]
    return pd.DataFrame(rows)


def main():
    nhan_su_df = generate_nhan_su(n=20)
    cong_viec_df = generate_cong_viec(n=80, nhan_su_ids=nhan_su_df["nhan_su_id"].tolist())
    phu_thuoc_df = generate_phu_thuoc_cong_viec(cong_viec_df["task_id"].tolist(), n_edges=60)

    nhan_su_df.to_csv(RAW_DIR / "nhan_su.csv", index=False)
    cong_viec_df.to_csv(RAW_DIR / "cong_viec.csv", index=False)
    phu_thuoc_df.to_csv(RAW_DIR / "phu_thuoc_cong_viec.csv", index=False)

    print(f" Đã sinh dữ liệu giả lập tại: {RAW_DIR}")
    print(f"   - nhan_su.csv: {len(nhan_su_df)} dòng")
    print(f"   - cong_viec.csv: {len(cong_viec_df)} dòng")
    print(f"   - phu_thuoc_cong_viec.csv: {len(phu_thuoc_df)} dòng")


if __name__ == "__main__":
    main()
