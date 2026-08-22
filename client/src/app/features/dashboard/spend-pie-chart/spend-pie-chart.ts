import { Component, computed, input, output } from '@angular/core';
import { ActiveElement, ChartConfiguration, ChartEvent } from 'chart.js';
import { BaseChartDirective } from 'ng2-charts';

export interface PieSlice {
  label: string;
  total: number;
  transactionCount: number;
}

// Cool violet/indigo/teal family, ordered so adjacent pie slices stay
// visually distinct even with several categories.
const SLICE_COLORS = [
  '#a684f2',
  '#22d3ee',
  '#6d28d9',
  '#f472b6',
  '#38bdf8',
  '#c084fc',
  '#4c1d95',
  '#7dd3fc',
];

@Component({
  selector: 'app-spend-pie-chart',
  imports: [BaseChartDirective],
  templateUrl: './spend-pie-chart.html',
  styleUrl: './spend-pie-chart.scss',
})
export class SpendPieChart {
  readonly slices = input<PieSlice[]>([]);
  readonly clickable = input(true);
  readonly sliceClick = output<string>();

  readonly total = computed(() => this.slices().reduce((sum, s) => sum + s.total, 0));

  readonly chartData = computed<ChartConfiguration<'pie'>['data']>(() => {
    const slices = this.slices();
    return {
      labels: slices.map((s) => s.label.replace(/_/g, ' ')),
      datasets: [
        {
          data: slices.map((s) => s.total),
          backgroundColor: slices.map((_, i) => SLICE_COLORS[i % SLICE_COLORS.length]),
          borderColor: '#130e1f',
          borderWidth: 2,
          hoverOffset: 8,
        },
      ],
    };
  });

  readonly chartOptions = computed<ChartConfiguration<'pie'>['options']>(() => ({
    responsive: true,
    maintainAspectRatio: false,
    onHover: (event, elements) => {
      const target = event.native?.target as HTMLElement | undefined;
      if (target) target.style.cursor = this.clickable() && elements.length ? 'pointer' : 'default';
    },
    plugins: {
      legend: {
        position: 'right',
        labels: { color: '#e9e4f5', boxWidth: 12, padding: 16 },
      },
      tooltip: {
        callbacks: {
          label: (ctx) => {
            const value = ctx.parsed as number;
            return ` ${ctx.label}: $${value.toFixed(2)}`;
          },
        },
      },
    },
  }));

  onChartClick(event: { event?: ChartEvent; active?: object[] }): void {
    if (!this.clickable()) return;
    const active = event.active as ActiveElement[] | undefined;
    const index = active?.[0]?.index;
    if (index === undefined) return;
    const slice = this.slices()[index];
    if (slice) this.sliceClick.emit(slice.label);
  }
}
