import { ChangeDetectorRef, Component } from '@angular/core';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-floating-ai-bot',
  imports: [FormsModule],
  templateUrl: './floating-ai-bot.html',
  styleUrl: './floating-ai-bot.scss',
})
export class FloatingAiBot {
  isOpen = false;
  userInput = '';
  isTyping = false;

  quickQuestions = [
    'Task nào có nguy cơ trễ hạn?',
    'Điểm nghẽn lớn nhất là gì?',
    'Nên giao API đăng nhập cho ai?',
  ];

  messages = [
    {
      role: 'bot',
      content:
        'Xin chào, tôi là AI Bot hỗ trợ PM phân tích rủi ro, điểm nghẽn và gợi ý phân công task.',
    },
  ];

  constructor(private cdr: ChangeDetectorRef) {}

  toggleChat() {
    this.isOpen = !this.isOpen;

    if (!this.isOpen) {
      this.isTyping = false;
    }
  }

  askQuick(question: string) {
    if (this.isTyping) {
      return;
    }

    this.userInput = question;
    this.sendMessage();
  }

  sendMessage() {
    const text = this.userInput.trim();

    if (!text || this.isTyping) {
      return;
    }

    this.messages.push({
      role: 'user',
      content: text,
    });

    this.userInput = '';
    this.isTyping = true;

    setTimeout(() => {
      this.messages.push({
        role: 'bot',
        content: this.getMockReply(text),
      });

      this.isTyping = false;
      this.cdr.detectChanges();
    }, 500);
  }

  getMockReply(question: string) {
    const lowerQuestion = question.toLowerCase();

    if (lowerQuestion.includes('trễ') || lowerQuestion.includes('tre')) {
      return 'Tôi phát hiện 2 task có nguy cơ trễ hạn: Thiết kế database 82% và Màn hình Kanban 68%. PM nên ưu tiên kiểm tra deadline và dependency.';
    }

    if (lowerQuestion.includes('nghẽn') || lowerQuestion.includes('nghen')) {
      return 'Điểm nghẽn lớn nhất là Database Schema vì đang chặn 5 task khác. Nếu task này chậm, tiến độ sprint có thể bị ảnh hưởng.';
    }

    if (
      lowerQuestion.includes('giao') ||
      lowerQuestion.includes('phân công') ||
      lowerQuestion.includes('api đăng nhập')
    ) {
      return 'AI gợi ý giao task API đăng nhập cho Bình vì độ phù hợp 91% và workload hiện tại khoảng 55%.';
    }

    if (lowerQuestion.includes('quá tải') || lowerQuestion.includes('workload')) {
      return 'Hiện chưa phát hiện lập trình viên quá tải nghiêm trọng. Tuy nhiên An có workload 80%, nên hạn chế giao thêm task High priority.';
    }

    return 'Dự án hiện có rủi ro trung bình. Bạn nên ưu tiên task có deadline gần, risk score trên 70% và nhiều task phụ thuộc.';
  }
}