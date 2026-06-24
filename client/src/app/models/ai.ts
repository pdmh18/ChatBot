export interface LateRisk {
  id: number;
  task: string;
  risk: number;
  reason: string;
}

export interface Bottleneck {
  id: number;
  task: string;
  blockedTasks: number;
}

export interface AssigneeSuggestion {
  id: number;
  task: string;
  developer: string;
  score: number;
}

export interface StaffMatchResult {
  maDeXuat: number;
  maCongViec: number;
  maNguoiDuocDeXuat: number;
  hoTenNguoiDuocDeXuat: string;
  tenMoHinh: string;
  diemPhuHop: number;
  diemKyNang?: number | null;
  diemKhoiLuong?: number | null;
  diemKinhNghiem?: number | null;
  lyDo?: string | null;
  daChapNhan?: boolean | null;
  ngayTao?: string | null;
}

export interface RiskPredictionResult {
  maDuBao: number;
  maDuAn: number;
  maCongViec?: number | null;
  maLoaiRuiRo: number;
  tenMoHinh: string;
  xacSuatRuiRo: number;
  duBaoTreHan: boolean;
  mucDoRuiRo: string;
  tacDongDuBao?: string | null;
  khuyenNghi?: string | null;
  ngayDuBao?: string | null;
}

export interface BottleneckResult {
  maDiemNghen: number;
  maDuAn: number;
  maCongViec?: number | null;
  khuVucPhatHien: string;
  nguyenNhan?: string | null;
  mucDoNghiemTrong: string;
  soNgayTreDuBao?: number | null;
  khuyenNghiAI?: string | null;
  ngayPhatHien?: string | null;
}

export interface ApiErrorResponse {
  message: string;
  detail?: string;
}
