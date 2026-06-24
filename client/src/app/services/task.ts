import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { environment } from '../../environments/environment';
import {
  AssignTaskRequest,
  CreateTaskRequest,
  Task,
  TaskDetail,
  TaskListItem,
  TaskQueryParams,
  UpdateTaskRequest,
  UpdateTaskStatusRequest,
} from '../models/task';

@Injectable({
  providedIn: 'root',
})
export class TaskService {
  private readonly apiUrl = `${environment.apiUrl}/tasks`;

  constructor(private http: HttpClient) {}

  getTasks(params?: TaskQueryParams): Observable<TaskListItem[]> {
    return this.http.get<TaskListItem[]>(this.apiUrl, {
      params: this.buildParams(params),
    });
  }

  getTaskViews(params?: TaskQueryParams): Observable<Task[]> {
    return this.getTasks(params).pipe(
      map((items) => items.map((item) => this.mapListItemToTask(item)))
    );
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
