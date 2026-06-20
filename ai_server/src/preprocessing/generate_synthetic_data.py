"""
Kết nối SQL Server (QuanLyDuAn_AI) và đọc dữ liệu từ 3 view có sẵn:
- v_Dataset_DuBaoTreHan        -> dùng cho model dự báo trễ hạn (XGBoost)
- v_Dataset_DeXuatGiaoViec     -> dùng cho model đề xuất giao việc
- v_Dataset_PhatHienDiemNghen  -> dùng cho model phát hiện điểm nghẽn (GNN/graph)

Ngoài ra còn hàm đọc bảng PhuThuocCongViec để dựng đồ thị (graph) phụ thuộc.

Yêu cầu: đã cài ODBC Driver 17 hoặc 18 for SQL Server.
Kiểm tra: powershell -> Get-OdbcDriver | Where-Object {$_.Name -like "*SQL Server*"}
"""
import os
import pandas as pd
import pyodbc
from dotenv import load_dotenv

load_dotenv()

# Đọc cấu hình từ .env (xem .env.example để biết format)
DB_SERVER = os.getenv("DB_SERVER", r"localhost\SQLEXPRESS")
DB_NAME = os.getenv("DB_NAME", "QuanLyDuAn_AI")
DB_DRIVER = os.getenv("DB_DRIVER", "ODBC Driver 18 for SQL Server")
DB_TRUSTED_CONNECTION = os.getenv("DB_TRUSTED_CONNECTION", "yes")  # yes = Windows Authentication
DB_USERNAME = os.getenv("DB_USERNAME", "")
DB_PASSWORD = os.getenv("DB_PASSWORD", "")
DB_TRUST_SERVER_CERTIFICATE = os.getenv("DB_TRUST_SERVER_CERTIFICATE", "no")



def get_connection_string() -> str:
    """
    Tạo connection string. Tự động chọn giữa Windows Authentication
    (mặc định, không cần user/pass) và SQL Server Authentication
    (nếu .env có khai báo DB_USERNAME + DB_PASSWORD).
    """
    base = (
        f"DRIVER={{{DB_DRIVER}}};"
        f"SERVER={DB_SERVER};"
        f"DATABASE={DB_NAME};"
    )

    if DB_USERNAME and DB_PASSWORD:
        # SQL Server Authentication
        base += f"UID={DB_USERNAME};PWD={DB_PASSWORD};"
    else:
        # Windows Authentication (mặc định khi chạy local)
        base += "Trusted_Connection=yes;"

    # Chỉ bật cho môi trường local/dev khi thật sự cần
    base += f"TrustServerCertificate={DB_TRUST_SERVER_CERTIFICATE};"
    return base


def get_connection() -> pyodbc.Connection:
    """Mở 1 connection mới tới SQL Server."""
    conn_str = get_connection_string()
    return pyodbc.connect(conn_str)


def query_to_dataframe(sql: str) -> pd.DataFrame:
    """Chạy 1 câu SQL bất kỳ, trả về pandas DataFrame."""
    with get_connection() as conn:
        return pd.read_sql(sql, conn)


# ===== Các hàm đọc đúng 3 view đã có sẵn trong DB =====

def load_dataset_du_bao_tre_han() -> pd.DataFrame:
    """Đọc view v_Dataset_DuBaoTreHan - dùng để train XGBoost dự báo trễ hạn."""
    return query_to_dataframe("SELECT * FROM v_Dataset_DuBaoTreHan")


def load_dataset_de_xuat_giao_viec() -> pd.DataFrame:
    """Đọc view v_Dataset_DeXuatGiaoViec - dùng để train model đề xuất giao việc."""
    return query_to_dataframe("SELECT * FROM v_Dataset_DeXuatGiaoViec")


def load_dataset_phat_hien_diem_nghen() -> pd.DataFrame:
    """Đọc view v_Dataset_PhatHienDiemNghen - dùng cho model phát hiện điểm nghẽn."""
    return query_to_dataframe("SELECT * FROM v_Dataset_PhatHienDiemNghen")


def load_phu_thuoc_cong_viec() -> pd.DataFrame:
    """
    Đọc bảng PhuThuocCongViec (quan hệ phụ thuộc giữa các task)
    Dùng để dựng đồ thị (graph) cho bottleneck_detector.py
    """
    return query_to_dataframe("""
        SELECT MaCongViecTruoc, MaCongViecSau, LoaiPhuThuoc
        FROM PhuThuocCongViec
    """)


def load_nguoi_dung() -> pd.DataFrame:
    """Đọc bảng NguoiDung (nhân sự) - dùng cho staff_matching.py"""
    return query_to_dataframe("""
        SELECT MaNguoiDung, HoTen, MaVaiTro, SoNamKinhNghiem,
               KhoiLuongHienTai, KhoiLuongToiDa, DangHoatDong
        FROM NguoiDung
        WHERE DangHoatDong = 1
    """)


def load_cong_viec() -> pd.DataFrame:
    """Đọc bảng CongViec đầy đủ (không qua view) - dùng khi cần thêm cột gốc."""
    return query_to_dataframe("SELECT * FROM CongViec")


if __name__ == "__main__":
    # Test nhanh kết nối
    print(f"Đang kết nối tới: {DB_SERVER} / DB: {DB_NAME}")
    try:
        df = load_dataset_du_bao_tre_han()
        print(f"✅ Kết nối thành công! v_Dataset_DuBaoTreHan: {len(df)} dòng")
        print(df.head())
    except Exception as e:
        print(f"❌ Lỗi kết nối: {e}")
