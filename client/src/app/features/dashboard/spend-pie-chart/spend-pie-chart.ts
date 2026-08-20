import { Component, computed, input } from '@angular/core';
import { ChartConfiguration } from 'chart.js';
import { BaseChartDirective } from 'ng2-charts';
import type { CategorySlice } from '../../../core/services/dashboard.service';

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
  readonly slices = input<CategorySlice[]>([]);

  readonly total = computed(() => this.slices().reduce((sum, s) => sum + s.total, 0));

  readonly chartData = computed<ChartConfiguration<'pie'>['data']>(() => {
    const slices = this.slices();
    return {
      labels: slices.map((s) => s.category),
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

  readonly chartOptions: ChartConfiguration<'pie'>['options'] = {
    responsive: true,
    maintainAspectRatio: false,
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
  };
}
