import { AfterViewInit, Component } from '@angular/core';
import FrappeGantt from 'frappe-gantt';

@Component({
  selector: 'app-gantt',
  imports: [],
  templateUrl: './gantt.html',
  styleUrl: './gantt.scss',
})
export class Gantt implements AfterViewInit {
  ngAfterViewInit() {
    new FrappeGantt('#gantt', [
      {
        id: '1',
        name: 'Thiết kế database',
        start: '2026-06-20',
        end: '2026-06-25',
        progress: 60,
        dependencies: '',
      },
      {
        id: '2',
        name: 'API đăng nhập',
        start: '2026-06-24',
        end: '2026-06-28',
        progress: 40,
        dependencies: '1',
      },
      {
        id: '3',
        name: 'Màn hình Kanban',
        start: '2026-06-26',
        end: '2026-06-30',
        progress: 30,
        dependencies: '2',
      },
    ]);
  }
}