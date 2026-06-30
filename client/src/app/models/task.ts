export type TaskStatus = string;
export type TaskPriority = string;

export interface Task {
  id: number;
  code?: string | null;
  name: string;
  description?: string | null;
  projectId?: number | null;
  project: string;
  sprintId?: number | null;
  sprint: string;
  assigneeId?: number | null;
  assignee: string;
  creatorId?: number | null;
  creator?: string | null;
  status: TaskStatus;
  priority: TaskPriority;
  startDate?: string | null;
  deadline: string;
  completedDate?: string | null;
  estimatedHours?: number | null;
  actualHours?: number | null;
  progress?: number | null;
  riskScore: number;
  riskLevel?: string | null;
  blockedTasks?: number | null;
  overloadPercent?: number | null;
  dependencyBlockedCount?: number | null;
  aiReason?: string | null;
  featureImportance?: string[] | null;
}


export interface PagedResult<T> {
  items: T[];
  pageNumber: number;
  pageSize: number;
  totalItems: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}

export interface TaskQueryParams {
  search?: string;
  projectId?: number;
  sprintId?: number;
  assigneeId?: number;
  status?: string;
  priority?: string;
  pageNumber?: number;
  pageSize?: number;
}

export interface TaskListItem {
  maCongViec: number;
  maCongViecCode?: string | null;
  tenCongViec: string;
  maDuAn: number;
  tenDuAn: string;
  maSprint?: number | null;
  tenSprint?: string | null;
  maNguoiPhuTrach?: number | null;
  nguoiPhuTrach?: string | null;
  trangThai?: string | null;
  doUuTien?: string | null;
  ngayBatDau?: string | null;
  hanChot?: string | null;
  soGioUocTinh?: number | null;
  tienDo?: number | null;
  riskPercent: number;
  riskLevel: string;
  blockedTasks?: number | null;
  soTaskBiChan?: number | null;
  soTaskDangChan?: number | null;
  soTaskPhuThuoc?: number | null;
  overloadPercent?: number | null;
  dependencyBlockedCount?: number | null;
  aiReason?: string | null;
  nguyenNhanAI?: string | null;
  featureImportance?: string[] | string | null;
}

export interface TaskDetail extends TaskListItem {
  moTa?: string | null;
  maNguoiTao: number;
  nguoiTao: string;
  ngayHoanThanh?: string | null;
  soGioThucTe?: number | null;
  ngayTao?: string | null;
  ngayCapNhat?: string | null;
}

export interface CreateTaskRequest {
  maDuAn: number;
  maSprint?: number | null;
  maCongViecCode?: string | null;
  tenCongViec: string;
  moTa?: string | null;
  maNguoiTao: number;
  maNguoiPhuTrach?: number | null;
  doUuTien?: string | null;
  trangThai?: string | null;
  ngayBatDau?: string | null;
  hanChot?: string | null;
  soGioUocTinh?: number | null;
  tienDo?: number | null;
}

export interface UpdateTaskRequest extends CreateTaskRequest {
  ngayHoanThanh?: string | null;
  soGioThucTe?: number | null;
}

export interface UpdateTaskStatusRequest {
  trangThai: string;
  tienDo?: number | null;
}

export interface AssignTaskRequest {
  maNguoiPhuTrach?: number | null;
}

export const taskStatusLabelMap: Record<string, string> = {
  'Can lam': 'Cần làm',
  'Dang lam': 'Đang làm',
  'Cho kiem tra': 'Chờ kiểm tra',
  'Hoan thanh': 'Hoàn thành',
  'Bi chan': 'Bị chặn',
  'Cho duyet': 'Chờ duyệt',
  'Da duyet': 'Đã duyệt',
  'Bi tu choi': 'Bị từ chối',
  'Da huy': 'Đã hủy',
};

export const taskPriorityLabelMap: Record<string, string> = {
  Thap: 'Thấp',
  'Trung binh': 'Trung bình',
  Cao: 'Cao',
  'Khan cap': 'Khẩn cấp',
};

export function getTaskStatusLabel(value?: string | null): string {
  return value ? taskStatusLabelMap[value] ?? value : '';
}

export function getTaskPriorityLabel(value?: string | null): string {
  return value ? taskPriorityLabelMap[value] ?? value : '';
}
