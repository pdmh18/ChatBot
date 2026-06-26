export interface DashboardSummary {
  tongCongViec: number;
  taskNguyCoTreHan: number;
  diemNghen: number;
  taskChuaPhanCong: number;
  taskHoanThanh: number;
  taskDangLam: number;
}

export interface DashboardWorkload {
  maNguoiDung: number;
  hoTen: string;
  soTask: number;
  tongGioUocTinh: number;
  phanTramTai: number;
  mucDoTai: string;
}

export interface DashboardTaskAlert {
  loaiCanhBao: string;
  maCongViec: number;
  maCongViecCode?: string | null;
  tenCongViec: string;
  maDuAn: number;
  maSprint?: number | null;
  maNguoiPhuTrach?: number | null;
  nguoiPhuTrach?: string | null;
  trangThai?: string | null;
  doUuTien?: string | null;
  hanChot?: string | null;
  tienDo?: number | null;
  soGioUocTinh?: number | null;
  riskPercent: number;
  riskLevel: string;
  nguyenNhan: string;
  khuyenNghi: string;
}

export const workloadLabelMap: Record<string, string> = {
  'Qua tai': 'Quá tải',
  Cao: 'Cao',
  'Trung binh': 'Trung bình',
  Thap: 'Thấp',
};

export function getWorkloadLabel(value?: string | null): string {
  return value ? workloadLabelMap[value] ?? value : '';
}
