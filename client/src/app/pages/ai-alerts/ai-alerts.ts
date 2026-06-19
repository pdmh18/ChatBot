import { Component } from '@angular/core';
import { AiService } from '../../services/ai';

@Component({
  selector: 'app-ai-alerts',
  imports: [],
  templateUrl: './ai-alerts.html',
  styleUrl: './ai-alerts.scss',
})
export class AiAlerts {
  lateRisks: any[] = [];
  bottlenecks: any[] = [];
  suggestions: any[] = [];

  constructor(private aiService: AiService) {
    this.lateRisks = this.aiService.getLateRisks();
    this.bottlenecks = this.aiService.getBottlenecks();
    this.suggestions = this.aiService.getAssigneeSuggestions();
  }
}