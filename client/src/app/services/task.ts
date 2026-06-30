import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, map, shareReplay, tap } from 'rxjs';
import { environment } from '../../environments/environment';
import {
  AssignTaskRequest,
  CreateTaskRequest,
  Task,
  TaskDetail,
  TaskListItem,
  TaskQueryParams,
  PagedResult,
  UpdateTaskRequest,
  UpdateTaskStatusRequest,
} from '../models/task';

interface ApiListResponse<T> {
  items?: T[];
  Items?: T[];
  data?: T[];
  Data?: T[];
  value?: T[];
  Value?: T[];
  result?: T[];
  Result?: T[];
  pageNumber?: number;
  PageNumber?: number;
  pageSize?: number;
  PageSize?: number;
  totalItems?: number;
  TotalItems?: number;
  totalPages?: number;
  TotalPages?: number;
  hasPreviousPage?: boolean;
  HasPreviousPage?: boolean;
  hasNextPage?: boolean;
  HasNextPage?: boolean;
  count?: number;
  Count?: number;
  totalCount?: number;
  TotalCount?: number;
  total?: number;
  Total?: number;
  totalRecords?: number;
  TotalRecords?: number;
}

export interface TaskPage {
  items: Task[];
  pageNumber: number;
  pageSize: number;
  totalItems: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
  // alias kept for old component code
  totalCount: number;
}

@Injectable({
  providedIn: 'root',
})
export class TaskService {
  private readonly apiUrl = `${environment.apiUrl}/tasks`;
  private cachedTaskViews: Task[] = [];
  private taskViewsCache = new Map<string, Observable<Task[]>>();
  private taskPageCache = new Map<string, Observable<TaskPage>>();

  constructor(private http: HttpClient) {}

  getTasks(params?: TaskQueryParams): Observable<TaskListItem[]> {
    return this.http
      .get<TaskListItem[] | ApiListResponse<TaskListItem> | PagedResult<TaskListItem>>(this.apiUrl, {
        params: this.buildParams(params),
      })
      .pipe(map((response) => this.unwrapList(response)));
  }

  getTaskViews(params?: TaskQueryParams): Observable<Task[]> {
    const cacheKey = JSON.stringify(params ?? {});
    const cached = this.taskViewsCache.get(cacheKey);
    if (cached) return cached;

    const request$ = this.getTasks(params).pipe(
      map((items) => items.map((item) => this.mapListItemToTask(item))),
      tap((tasks) => {
        if (tasks.length) {
          this.cachedTaskViews = tasks;
        }
      }),
      shareReplay({ bufferSize: 1, refCount: false })
    );

    this.taskViewsCache.set(cacheKey, request$);
    return request$;
  }


  getTaskViewsPage(params?: TaskQueryParams): Observable<TaskPage> {
    // Không cache API phân trang ở đây để tránh giữ lại metadata cũ
    // khi backend vừa đổi từ array sang object { items, totalItems, totalPages, ... }.
    return this.http
      .get<TaskListItem[] | ApiListResponse<TaskListItem> | PagedResult<TaskListItem>>(this.apiUrl, {
        params: this.buildParams(params),
      })
      .pipe(
        map((response) => {
          const rawItems = this.unwrapList<TaskListItem>(response);
          const items = rawItems.map((item) => this.mapListItemToTask(item));
          const pageNumber = this.unwrapNumber(response, 'pageNumber', params?.pageNumber ?? 1);
          const pageSize = this.unwrapNumber(response, 'pageSize', params?.pageSize ?? (items.length || 20));
          const fallbackTotalItems = Math.max(items.length, ((pageNumber - 1) * pageSize) + items.length);
          const totalItems = this.unwrapCount(response, fallbackTotalItems);
          const totalPages = this.unwrapNumber(
            response,
            'totalPages',
            Math.max(1, Math.ceil(totalItems / Math.max(pageSize, 1)))
          );
          const hasPreviousPage = this.unwrapBoolean(response, 'hasPreviousPage', pageNumber > 1);
          const hasNextPage = this.unwrapBoolean(response, 'hasNextPage', pageNumber < totalPages);

          if (items.length) {
            this.cachedTaskViews = items;
          }

          return {
            items,
            pageNumber,
            pageSize,
            totalItems,
            totalPages,
            hasPreviousPage,
            hasNextPage,
            totalCount: totalItems,
          };
        })
      );
  }

  getCachedTaskViews(): Task[] {
    return [...this.cachedTaskViews];
  }

  getTaskById(id: number): Observable<TaskDetail> {
    return this.http.get<TaskDetail>(`${this.apiUrl}/${id}`);
  }

  getTaskViewById(id: number): Observable<Task> {
    return this.getTaskById(id).pipe(
      map((detail) => this.mapDetailToTask(detail))
    );
  }

  createTask(payload: CreateTaskRequest): Observable<{ maCongViec: number; message: string }> {
    return this.http.post<{ maCongViec: number; message: string }>(this.apiUrl, payload);
  }

  updateTask(id: number, payload: UpdateTaskRequest): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/${id}`, payload);
  }

  updateTaskStatus(id: number, payload: UpdateTaskStatusRequest): Observable<void> {
    return this.http.patch<void>(`${this.apiUrl}/${id}/status`, payload);
  }

  assignTask(id: number, payload: AssignTaskRequest): Observable<void> {
    return this.http.patch<void>(`${this.apiUrl}/${id}/assign`, payload);
  }

  deleteTask(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }

  mapListItemToTask(item: TaskListItem): Task {
    return {
      id: item.maCongViec,
      code: item.maCongViecCode,
      name: item.tenCongViec,
      projectId: item.maDuAn,
      project: item.tenDuAn,
      sprintId: item.maSprint ?? null,
      sprint: item.tenSprint ?? 'Chưa có sprint',
      assigneeId: item.maNguoiPhuTrach ?? null,
      assignee: item.nguoiPhuTrach ?? 'Chưa phân công',
      status: item.trangThai ?? 'Can lam',
      priority: item.doUuTien ?? 'Trung binh',
      startDate: item.ngayBatDau ?? null,
      deadline: item.hanChot ?? '',
      estimatedHours: item.soGioUocTinh ?? null,
      progress: item.tienDo ?? 0,
      riskScore: item.riskPercent ?? 0,
      riskLevel: item.riskLevel,
      blockedTasks: item.blockedTasks ?? item.soTaskBiChan ?? item.soTaskDangChan ?? item.soTaskPhuThuoc ?? null,
      overloadPercent: item.overloadPercent ?? null,
      dependencyBlockedCount: item.dependencyBlockedCount ?? item.soTaskPhuThuoc ?? null,
      aiReason: item.aiReason ?? item.nguyenNhanAI ?? null,
      featureImportance: this.normalizeFeatureImportance(item.featureImportance),
    };
  }

  mapDetailToTask(detail: TaskDetail): Task {
    return {
      ...this.mapListItemToTask(detail),
      description: detail.moTa ?? null,
      creatorId: detail.maNguoiTao,
      creator: detail.nguoiTao,
      completedDate: detail.ngayHoanThanh ?? null,
      actualHours: detail.soGioThucTe ?? null,
    };
  }

  private unwrapList<T>(response: T[] | ApiListResponse<T> | PagedResult<T>): T[] {
    if (Array.isArray(response)) return response;

    const anyResponse = response as any;
    return (
      anyResponse.items ??
      anyResponse.Items ??
      anyResponse.data ??
      anyResponse.Data ??
      anyResponse.value ??
      anyResponse.Value ??
      anyResponse.result ??
      anyResponse.Result ??
      []
    );
  }

  private unwrapCount<T>(response: T[] | ApiListResponse<T> | PagedResult<T>, fallback: number): number {
    if (Array.isArray(response)) return fallback;

    const anyResponse = response as any;
    return Number(
      anyResponse.totalItems ??
      anyResponse.TotalItems ??
      anyResponse.totalCount ??
      anyResponse.TotalCount ??
      anyResponse.totalRecords ??
      anyResponse.TotalRecords ??
      anyResponse.totalRows ??
      anyResponse.TotalRows ??
      anyResponse.count ??
      anyResponse.Count ??
      anyResponse.total ??
      anyResponse.Total ??
      anyResponse.pagination?.totalItems ??
      anyResponse.pagination?.totalCount ??
      anyResponse.Pagination?.TotalItems ??
      anyResponse.Pagination?.TotalCount ??
      fallback
    );
  }

  private unwrapNumber<T>(
    response: T[] | ApiListResponse<T> | PagedResult<T>,
    key: 'pageNumber' | 'pageSize' | 'totalPages',
    fallback: number
  ): number {
    if (Array.isArray(response)) return fallback;

    const anyResponse = response as any;
    const pascalKey = key.charAt(0).toUpperCase() + key.slice(1);
    const value = anyResponse[key] ?? anyResponse[pascalKey] ?? anyResponse.pagination?.[key] ?? anyResponse.Pagination?.[pascalKey];
    const parsed = Number(value);
    return Number.isFinite(parsed) && parsed > 0 ? parsed : fallback;
  }

  private unwrapBoolean<T>(
    response: T[] | ApiListResponse<T> | PagedResult<T>,
    key: 'hasPreviousPage' | 'hasNextPage',
    fallback: boolean
  ): boolean {
    if (Array.isArray(response)) return fallback;

    const anyResponse = response as any;
    const pascalKey = key.charAt(0).toUpperCase() + key.slice(1);
    const value = anyResponse[key] ?? anyResponse[pascalKey] ?? anyResponse.pagination?.[key] ?? anyResponse.Pagination?.[pascalKey];
    return typeof value === 'boolean' ? value : fallback;
  }

  private normalizeFeatureImportance(value?: string[] | string | null): string[] | null {
    if (Array.isArray(value)) return value;
    if (!value) return null;

    return value
      .split(/[;,]/)
      .map((item) => item.trim())
      .filter(Boolean);
  }

  private buildParams(params?: TaskQueryParams): HttpParams {
    let httpParams = new HttpParams();

    if (!params) return httpParams;

    Object.entries(params).forEach(([key, value]) => {
      if (value !== undefined && value !== null && value !== '') {
        httpParams = httpParams.set(key, String(value));
      }
    });

    return httpParams;
  }
}
