declare module 'frappe-gantt' {
  interface GanttTask {
    id: string;
    name: string;
    start: string;
    end: string;
    progress: number;
    dependencies?: string;
  }

  export default class FrappeGantt {
    constructor(
      selector: string | HTMLElement | SVGElement,
      tasks: GanttTask[],
      options?: Record<string, unknown>
    );
  }
}