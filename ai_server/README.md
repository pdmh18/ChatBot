# AI Server — Dự Báo Rủi Ro & Tối Ưu Nguồn Lực Dự Án

API AI gồm 3 model:
- **XGBoost** — Dự báo task có nguy cơ trễ hạn
- **Random Forest** — Chấm điểm độ phù hợp nhân sự
- **GNN** — Phát hiện điểm nghẽn trong đồ thị công việc

---

## Yêu cầu hệ thống

- Python **3.10+**
- Windows 10/11 (đã test)
- RAM tối thiểu 4GB

---

## Cài đặt

### Bước 1 — Clone project và tạo môi trường ảo

```bash
cd ai_server
python -m venv .venv
```

Kích hoạt môi trường ảo:
```bash
# Windows
.venv\Scripts\activate

# Mac/Linux
source .venv/bin/activate
```

### Bước 2 — Cài thư viện cơ bản

```bash
pip install -r requirements.txt
```

### Bước 3 — Cài thêm PyTorch (CPU)

```bash
pip install torch --index-url https://download.pytorch.org/whl/cpu
```

### Bước 4 — Cài PyTorch Geometric

```bash
pip install torch-geometric
pip install torch-scatter torch-sparse -f https://data.pyg.org/whl/torch-2.12.1+cpu.html
```

### Bước 5 — Cài thêm thư viện còn lại

```bash
pip install imbalanced-learn matplotlib
```

---

## Cấu hình

Tạo file `.env` trong thư mục `ai_server/` (copy từ `.env.example`):

```env
DB_SERVER=localhost\SQLEXPRESS
DB_NAME=QuanLyDuAn_AI
DB_DRIVER=ODBC Driver 18 for SQL Server
DB_TRUSTED_CONNECTION=yes
DB_TRUST_SERVER_CERTIFICATE=yes
DB_USERNAME=
DB_PASSWORD=
```

---

## Chuẩn bị dữ liệu

Copy 3 file CSV vào thư mục `data/raw/`:

```
data/raw/
  ├── dataset_du_bao_tre_han.csv
  ├── dataset_de_xuat_giao_viec.csv
  └── dataset_phat_hien_diem_nghen.csv
```

---

## Chạy theo thứ tự

### Bước 1 — Tiền xử lý dữ liệu
```bash
python -m src.preprocessing.clean_data
```

Kết quả: sinh ra các file CSV trong `data/processed/` và scaler trong `models/`

### Bước 2 — Train XGBoost (dự báo trễ hạn)
```bash
python -m src.models.train_risk_model
```

Kết quả: `models/risk_model.joblib`

### Bước 3 — Train Random Forest (đề xuất giao việc)
```bash
python -m src.models.staff_matching
```

Kết quả: `models/staff_matching_model.joblib`

### Bước 4 — Train GNN (phát hiện điểm nghẽn)
```bash
python -m src.models.bottleneck_detector
```

Kết quả: `models/gnn_model.pt`

### Bước 5 — Chạy API Server
```bash
uvicorn src.api.main:app --reload --port 8000
```

Truy cập docs tại: `http://localhost:8000/docs`

---

## API Endpoints

| Method | Endpoint | Mô tả |
|--------|----------|-------|
| POST | `/api/predict-risk` | Dự báo task trễ hạn (XGBoost) |
| POST | `/api/match-staff` | Đề xuất giao việc (Random Forest) |
| GET | `/api/analyze-bottleneck` | Phát hiện điểm nghẽn (GNN) |
| POST | `/api/chat` | Chatbot hỗ trợ |
| GET | `/health` | Kiểm tra server |

---

## Ví dụ gọi API từ Backend C#

### Dự báo trễ hạn
```csharp
var response = await httpClient.PostAsJsonAsync(
    "http://localhost:8000/api/predict-risk",
    new {
        SoGioUocTinh = 35,
        SoNamKinhNghiemNhanSu = 3,
        KhoiLuongHienTaiNhanSu = 0.7,
        SoCongViecPhuThuocTruoc = 2,
        DoUuTien_Encoded = 2
    }
);
// Kết quả: { xac_suat_tre_han: 0.74, du_bao_tre_han: true, muc_do_rui_ro: "Cao" }
```

### Đề xuất giao việc
```csharp
var response = await httpClient.PostAsJsonAsync(
    "http://localhost:8000/api/match-staff",
    new {
        SoGioUocTinh = 16,
        PhanTramTaiNhanSu = 0.4,
        DiemChatLuongTrungBinhLichSu = 8.5
    }
);
// Kết quả: { xac_suat_hieu_qua: 0.54, de_xuat_giao_viec: true, muc_do_phu_hop: "Trung bình" }
```

### Phát hiện điểm nghẽn
```csharp
var response = await httpClient.GetAsync(
    "http://localhost:8000/api/analyze-bottleneck?top_n=10"
);
// Kết quả: danh sách 10 task có bottleneck_score cao nhất
```

---

## Cấu trúc thư mục

```
ai_server/
├── data/
│   ├── raw/          ← 3 file CSV đầu vào
│   └── processed/    ← CSV đã tiền xử lý (tự sinh)
├── models/           ← model đã train (tự sinh)
├── src/
│   ├── preprocessing/
│   │   ├── clean_data.py        ← tiền xử lý dữ liệu
│   │   └── db_connector.py      ← kết nối SQL Server
│   ├── models/
│   │   ├── train_risk_model.py  ← XGBoost
│   │   ├── staff_matching.py    ← Random Forest
│   │   └── bottleneck_detector.py ← GNN
│   └── api/
│       ├── main.py              ← FastAPI entry point
│       ├── schemas.py           ← request/response models
│       └── routes/
│           ├── risk.py          ← AI endpoints
│           └── chat.py          ← chat endpoint
├── .env                         ← cấu hình DB (tự tạo)
├── .env.example                 ← mẫu cấu hình
├── requirements.txt             ← thư viện cơ bản
└── README.md
```

---

## Lưu ý

- Phải chạy **đúng thứ tự** Bước 1 → 2 → 3 → 4 → 5
- Nếu thay đổi dữ liệu CSV → chạy lại từ Bước 1
- File `.env` không được commit lên Git