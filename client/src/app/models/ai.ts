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
  task: string;
  developer: string;
  score: number;
}
