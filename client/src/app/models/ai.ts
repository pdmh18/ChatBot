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
  taskId?: number;
  task: string;
  developer: string;
  score: number;
  model?: string;
  reason?: string;
  skillScore?: number | null;
  workloadScore?: number | null;
  expScore?: number | null;
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
  giaiThich?: string | null;
  featureImportance?: string[] | string | null;
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
  nguyenNhan?: string | null;
  giaiThich?: string | null;
  featureImportance?: string[] | string | null;
  ngayDuBao?: string | null;
}

export interface BottleneckResult {
  maDiemNghen: number;
  maDuAn: number;
  maCongViec?: number | null;
  tenCongViec?: string | null;
  khuVucPhatHien: string;
  nguyenNhan?: string | null;
  giaiThich?: string | null;
  mucDoNghiemTrong: string;
  soNgayTreDuBao?: number | null;
  soTaskBiAnhHuong?: number | null;
  soTaskDangChan?: number | null;
  soTaskBiAnhHuongPhiaSau?: number | null;
  bottleneckScore?: number | null;
  blockedTasks?: number | null;
  khuyenNghiAI?: string | null;
  ngayPhatHien?: string | null;
}

export interface ApiErrorResponse {
  message: string;
  detail?: string;
}



