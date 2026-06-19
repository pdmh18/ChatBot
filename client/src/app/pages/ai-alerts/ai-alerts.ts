import { Component, OnInit } from '@angular/core';
import { AssigneeSuggestion, Bottleneck, LateRisk } from '../../models/ai';
import { AiService } from '../../services/ai';

@Component({
  selector: 'app-ai-alerts',
  imports: [],
  templateUrl: './ai-alerts.html',
  styleUrl: './ai-alerts.scss',
})
export class AiAlerts implements OnInit {
  lateRisks: LateRisk[] = [];
  bottlenecks: Bottleneck[] = [];
  suggestions: AssigneeSuggestion[] = [];
  selectedBottleneck: Bottleneck | null = null;

  constructor(private aiService: AiService) {}

  ngOnInit(): void {
    this.lateRisks = this.aiService.getLateRisks();
    this.bottlenecks = this.aiService.getBottlenecks();
    this.suggestions = this.aiService.getAssigneeSuggestions();
  }

  openBottleneckAlert(item: Bottleneck): void {
    this.selectedBottleneck = item;
  }

  closeBottleneckAlert(): void {
    this.selectedBottleneck = null;
  }
}
