export type TaskStatus = 'Todo' | 'In Progress' | 'Review' | 'Done';
export type TaskPriority = 'High' | 'Medium' | 'Low';

export interface Task {
  id: number;
  name: string;
  project: string;
  sprint: string;
  assignee: string;
  status: TaskStatus;
  priority: TaskPriority;
  deadline: string;
  riskScore: number;
}
