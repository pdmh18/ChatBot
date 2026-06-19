# AI Server — Dự báo rủi ro & tối ưu nguồn lực dự án phần mềm

## 🚀 Hướng dẫn cho người mới clone về (làm theo đúng thứ tự)

### Yêu cầu trước
- Đã cài **Python 3.12** (không dùng 3.13/3.14 vì pandas/xgboost chưa có wheel sẵn, sẽ lỗi build)
- Kiểm tra: `py -3.12 --version`

### Bước 1 — Vào đúng thư mục
```powershell
cd ai_server
```

### Bước 2 — Tạo môi trường ảo (venv)
```powershell
py -3.12 -m venv .venv
.venv\Scripts\activate
```
> Sau khi activate, đầu dòng lệnh sẽ hiện `(.venv)` — nếu không thấy nghĩa là chưa activate đúng.

### Bước 3 — Cài thư viện
```powershell
pip install -r requirements.txt
```

### Bước 4 — Sinh dữ liệu giả lập (synthetic data)
```powershell
python -m src.preprocessing.generate_synthetic_data
```
Tạo ra 3 file CSV trong `data/raw/`: `nhan_su.csv`, `cong_viec.csv`, `phu_thuoc_cong_viec.csv`

### Bước 5 — Tiền xử lý & chuẩn hóa dữ liệu
```powershell
python -m src.preprocessing.clean_data
```
Làm sạch + chuẩn hóa (Min-Max) → lưu vào `data/processed/`, lưu `scaler.joblib` vào `models/`

### Bước 6 — Train model dự báo trễ hạn (XGBoost)
```powershell
python -m src.models.train_risk_model
```
In ra báo cáo độ chính xác (precision/recall/ROC-AUC) → lưu `risk_model.joblib` vào `models/`

### Bước 7 — (Tùy chọn) Test riêng từng module AI
```powershell
python -m src.models.staff_matching        # chấm điểm phù hợp nhân sự
python -m src.models.bottleneck_detector    # tìm điểm nghẽn dự án
```

### Bước 8 — Chạy API server
```powershell
uvicorn src.api.main:app --reload --port 8000
```

Vào trình duyệt: **http://127.0.0.1:8000/docs** để xem & test toàn bộ API qua Swagger UI.

---

## ⚠️ Lưu ý quan trọng
- **Bước 4, 5, 6 bắt buộc chạy trước** khi gọi API `/api/predict-risk` và `/api/analyze-bottleneck`, nếu không sẽ báo lỗi thiếu model/data.
- Mỗi lần mở terminal mới để chạy lại project, phải `activate` venv lại:
  ```powershell
  .venv\Scripts\activate
  ```
- File trong `data/raw/`, `data/processed/`, `models/*.joblib` **không được commit lên Git** (đã có trong `.gitignore`) vì là dữ liệu sinh ra, người khác clone về tự chạy lại bước 4-6 là có.

## Cấu trúc

```
ai_server/
├── data/
│   ├── raw/            ← dữ liệu thô (synthetic hoặc thật)
│   └── processed/      ← dữ liệu đã làm sạch + chuẩn hóa
├── src/
│   ├── preprocessing/  ← sinh data giả + clean/normalize
│   ├── models/         ← train + inference (risk, staff matching, bottleneck/GNN)
│   ├── api/             ← FastAPI app, routes, schemas
│   └── utils/
├── models/              ← model đã train (.joblib) — không commit lên git
└── notebooks/            ← Jupyter để EDA / thử nghiệm
```

## API Endpoints

| Method | Endpoint | Mô tả |
|---|---|---|
| POST | `/api/predict-risk` | Dự báo xác suất task trễ hạn |
| POST | `/api/match-staff` | Xếp hạng nhân sự phù hợp cho task |
| GET | `/api/analyze-bottleneck` | Phân tích điểm nghẽn dự án (graph) |
| POST | `/api/chat` | Chat placeholder |
| GET | `/health` | Health check |

## Roadmap nâng cấp

- [ ] Thay synthetic data bằng dữ liệu thật từ Jira/GitHub
- [ ] Nâng `bottleneck_detector.py` từ graph-centrality lên GNN thực sự (PyTorch Geometric)
- [ ] Thêm model RandomForest để so sánh với XGBoost
- [ ] Tích hợp LLM thật vào `/api/chat`
