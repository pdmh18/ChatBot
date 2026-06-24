import { ChangeDetectorRef, Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { forkJoin, of } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { BottleneckResult } from '../../models/ai';
import { Task, getTaskPriorityLabel, getTaskStatusLabel } from '../../models/task';
import { AiService } from '../../services/ai';
import { TaskService } from '../../services/task';

interface ChatMessage {
  id: number;
  role: 'bot' | 'user';
  content: string;
}

@Component({
  selector: 'app-floating-ai-bot',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './floating-ai-bot.html',
  styleUrl: './floating-ai-bot.scss',
})
export class FloatingAiBot {
  private messageId = 0;

  isOpen = false;
  userInput = '';
  isTyping = false;

  quickQuestions = [
    'Task nào có nguy cơ trễ hạn?',
    'Điểm nghẽn lớn nhất là gì?',
    'Nên ưu tiên task nào?',
  ];

  messages: ChatMessage[] = [
    {
      id: this.messageId++,
      role: 'bot',
      content:
        'Xin chào, tôi là AI Bot hỗ trợ PM phân tích rủi ro, điểm nghẽn và gợi ý phân công task theo dữ liệu hiện tại của dự án.',
    },
  ];

  constructor(
    private taskService: TaskService,
    private aiService: AiService,
    private cdr: ChangeDetectorRef
  ) {}

  toggleChat(): void {
    this.isOpen = !this.isOpen;

    if (!this.isOpen) {
      this.isTyping = false;
    }
  }

  askQuick(question: string): void {
    if (this.isTyping) {
      return;
    }

    this.userInput = question;
    this.sendMessage();
  }

  sendMessage(): void {
    const text = this.userInput.trim();

    if (!text || this.isTyping) {
      return;
    }

    this.messages.push({
      id: this.messageId++,
      role: 'user',
      content: text,
    });

    this.userInput = '';
    this.isTyping = true;
    this.cdr.detectChanges();

    forkJoin({
      tasks: this.taskService.getTaskViews({ pageNumber: 1, pageSize: 100 }).pipe(
        catchError(() => of([] as Task[]))
      ),
      bottlenecks: this.aiService.analyzeBottlenecks(5).pipe(
        catchError(() => of([] as BottleneckResult[]))
      ),
    }).subscribe(({ tasks, bottlenecks }) => {
      this.messages.push({
        id: this.messageId++,
        role: 'bot',
        content: this.buildLiveReply(text, tasks, bottlenecks),
      });

      this.isTyping = false;
      this.cdr.detectChanges();
    });
  }

  private buildLiveReply(question: string, tasks: Task[], bottlenecks: BottleneckResult[]): string {
    const lowerQuestion = this.normalizeText(question);

    if (!tasks.length) {
      return 'Tôi chưa đọc được danh sách task từ backend. Bạn kiểm tra backend .NET có đang chạy ở port 49261 không nhé.';
    }

    if (this.hasAny(lowerQuestion, ['tre', 'risk', 'rui ro', 'nguy co'])) {
      return this.buildRiskReply(tasks);
    }

    if (this.hasAny(lowerQuestion, ['nghen', 'bottleneck', 'chan', 'phu thuoc'])) {
      return this.buildBottleneckReply(bottlenecks);
    }

    if (this.hasAny(lowerQuestion, ['giao', 'phan cong', 'nhan su', 'lap trinh vien', 'dev'])) {
      return this.buildAssignmentReply(tasks);
    }

    if (this.hasAny(lowerQuestion, ['uu tien', 'lam truoc', 'deadline', 'han chot'])) {
      return this.buildPriorityReply(tasks);
    }

    return this.buildProjectSummaryReply(tasks, bottlenecks);
  }

  private buildRiskReply(tasks: Task[]): string {
    const riskyTasks = [...tasks]
      .filter((task) => (task.riskScore ?? 0) >= 50 || this.isUrgent(task.priority))
      .sort((a, b) => (b.riskScore ?? 0) - (a.riskScore ?? 0))
      .slice(0, 3);

    if (!riskyTasks.length) {
      return `Hiện có ${tasks.length} task và chưa thấy task nào có risk score cao. PM nên tiếp tục theo dõi các task deadline gần.`;
    }

    const lines = riskyTasks.map(
      (task, index) =>
        `${index + 1}. ${task.name} - risk ${task.riskScore ?? 0}%, trạng thái ${getTaskStatusLabel(task.status)}, phụ trách ${task.assignee || 'chưa phân công'}.`
    );

    return `Các task nên chú ý theo dữ liệu hiện tại:\n${lines.join('\n')}`;
  }

  private buildBottleneckReply(bottlenecks: BottleneckResult[]): string {
    if (!bottlenecks.length) {
      return 'Tôi chưa nhận được dữ liệu điểm nghẽn từ AI server. Nếu cần phân tích bottleneck, hãy kiểm tra backend và AI server đang chạy đủ.';
    }

    const top = bottlenecks[0];
    return `Điểm nghẽn đáng chú ý nhất là công việc #${top.maCongViec ?? 'N/A'} (${top.mucDoNghiemTrong}). Nguyên nhân: ${top.nguyenNhan || 'AI chưa trả mô tả chi tiết'}. Khuyến nghị: ${top.khuyenNghiAI || 'PM nên kiểm tra task phụ thuộc và ưu tiên xử lý sớm.'}`;
  }

  private buildAssignmentReply(tasks: Task[]): string {
    const activeTasks = tasks.filter((task) => !this.isDone(task.status));
    const byAssignee = new Map<string, number>();

    activeTasks.forEach((task) => {
      const assignee = task.assignee || 'Chưa phân công';
      byAssignee.set(assignee, (byAssignee.get(assignee) ?? 0) + 1);
    });

    const leastBusy = [...byAssignee.entries()].sort((a, b) => a[1] - b[1])[0];
    const highRisk = [...activeTasks].sort((a, b) => (b.riskScore ?? 0) - (a.riskScore ?? 0))[0];

    if (!leastBusy || !highRisk) {
      return 'Hiện chưa có đủ task đang mở để đề xuất phân công. Bạn có thể thêm task mới rồi bấm Gợi ý AI ở màn Tasks.';
    }

    return `Theo dữ liệu hiện tại, người đang ít task mở nhất là ${leastBusy[0]} (${leastBusy[1]} task). Task nên xem xét phân công/kiểm tra trước là ${highRisk.name} vì risk ${highRisk.riskScore ?? 0}%. Để có điểm AI chính xác từng người, dùng nút “Gợi ý AI” ở màn Tasks.`;
  }

  private buildPriorityReply(tasks: Task[]): string {
    const candidates = [...tasks]
      .filter((task) => !this.isDone(task.status))
      .sort((a, b) => {
        const riskDiff = (b.riskScore ?? 0) - (a.riskScore ?? 0);
        if (riskDiff !== 0) return riskDiff;
        return new Date(a.deadline || '2999-12-31').getTime() - new Date(b.deadline || '2999-12-31').getTime();
      })
      .slice(0, 3);

    if (!candidates.length) {
      return 'Các task hiện tại phần lớn đã hoàn thành. PM có thể kiểm tra Gantt để xác nhận tiến độ theo mốc thời gian.';
    }

    const lines = candidates.map(
      (task, index) =>
        `${index + 1}. ${task.name} - deadline ${task.deadline || 'chưa có'}, ưu tiên ${getTaskPriorityLabel(task.priority)}, risk ${task.riskScore ?? 0}%.`
    );

    return `Nên ưu tiên theo thứ tự:\n${lines.join('\n')}`;
  }

  private buildProjectSummaryReply(tasks: Task[], bottlenecks: BottleneckResult[]): string {
    const total = tasks.length;
    const done = tasks.filter((task) => this.isDone(task.status)).length;
    const highRisk = tasks.filter((task) => (task.riskScore ?? 0) >= 70).length;
    const active = total - done;

    return `Tổng quan hiện tại: ${total} task, ${done} đã hoàn thành, ${active} đang mở, ${highRisk} task risk cao. AI bottleneck đang ghi nhận ${bottlenecks.length} điểm cần theo dõi. Bạn có thể hỏi “task nào rủi ro”, “điểm nghẽn lớn nhất” hoặc “nên ưu tiên task nào”.`;
  }

  private hasAny(text: string, keywords: string[]): boolean {
    return keywords.some((keyword) => text.includes(keyword));
  }

  private normalizeText(value: string): string {
    return value
      .toLowerCase()
      .normalize('NFD')
      .replace(/[\u0300-\u036f]/g, '')
      .replace(/đ/g, 'd');
  }

  private isDone(status?: string | null): boolean {
    const normalized = this.normalizeText(status ?? '');
    return normalized.includes('hoan thanh') || normalized.includes('done');
  }

  private isUrgent(priority?: string | null): boolean {
    const normalized = this.normalizeText(priority ?? '');
    return normalized.includes('khan cap') || normalized.includes('cao') || normalized.includes('high');
  }
}
