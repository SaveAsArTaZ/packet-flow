import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../auth.service';
import { TemplateService, TemplateSummary } from '../template.service';

type Tab = 'my' | 'public' | 'search';

@Component({
  selector: 'pf-dashboard',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule],
  templateUrl: './dashboard.component.html',
})
export class DashboardComponent implements OnInit {
  readonly auth = inject(AuthService);
  readonly templateSvc = inject(TemplateService);

  activeTab = signal<Tab>('my');
  searchQuery = signal('');
  searchResults = signal<TemplateSummary[]>([]);
  hasSearched = signal(false);

  ngOnInit(): void {
    this.loadMyTemplates();
  }

  switchTab(tab: Tab): void {
    this.activeTab.set(tab);
    this.hasSearched.set(false);
    if (tab === 'my') this.loadMyTemplates();
    else if (tab === 'public') this.loadPublic();
  }

  loadMyTemplates(): void {
    this.templateSvc.loadMyTemplates().subscribe();
  }

  loadPublic(): void {
    this.templateSvc.loadPublicTemplates().subscribe();
  }

  onSearch(): void {
    const q = this.searchQuery().trim();
    if (!q) return;
    this.hasSearched.set(true);
    this.templateSvc.search(q).subscribe({
      next: (data) => this.searchResults.set(data),
      error: () => this.searchResults.set([]),
    });
  }

  deleteTemplate(id: string): void {
    if (!confirm('Delete this template?')) return;
    this.templateSvc.delete(id).subscribe({
      next: () => this.loadMyTemplates(),
    });
  }

  cloneTemplate(id: string): void {
    this.templateSvc.clone(id).subscribe({
      next: () => this.loadMyTemplates(),
    });
  }

  get templateList(): TemplateSummary[] {
    if (this.activeTab() === 'search') return this.searchResults();
    return this.templateSvc.templates();
  }

  trackById(_: number, t: TemplateSummary): string {
    return t.id;
  }
}
