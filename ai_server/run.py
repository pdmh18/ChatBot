"""
Chạy toàn bộ pipeline AI:
  1. Tiền xử lý dữ liệu
  2. Train XGBoost
  3. Train Random Forest
  4. Train GNN
  5. Khởi động FastAPI server

Chạy: python run.py
"""

import subprocess
import sys
import os

def run_step(step_name: str, command: list):
    print(f"\n{'='*60}")
    print(f" {step_name}")
    print('='*60)
    result = subprocess.run(command, env=os.environ.copy())
    if result.returncode != 0:
        print(f"\nLỗi ở bước: {step_name}")
        print("Dừng pipeline.")
        sys.exit(1)
    print(f"Hoàn thành: {step_name}")


if __name__ == "__main__":
    python = sys.executable

    # Bước 1: Tiền xử lý dữ liệu
    run_step(
        "Tiền xử lý dữ liệu",
        [python, "-m", "src.preprocessing.clean_data"]
    )

    # Bước 2: Train XGBoost
    run_step(
        "Train XGBoost — Dự báo trễ hạn",
        [python, "-m", "src.models.train_risk_model"]
    )

    # Bước 3: Train Random Forest
    run_step(
        "Train Random Forest — Đề xuất giao việc",
        [python, "-m", "src.models.staff_matching"]
    )

    # Bước 4: Train GNN
    run_step(
        "Train GNN — Phát hiện điểm nghẽn",
        [python, "-m", "src.models.bottleneck_detector"]
    )

    # Bước 5: Khởi động FastAPI
    print(f"\n{'='*60}")
    print("Khởi động FastAPI Server")
    print('='*60)
    print("API docs: http://localhost:8000/docs")
    print("Nhấn CTRL+C để dừng server")
    print('='*60)

    subprocess.run([
        python, "-m", "uvicorn",
        "src.api.main:app",
        "--reload",
        "--port", "8000"
    ])