import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatToolbarModule } from '@angular/material/toolbar';
import { Router, RouterLink } from '@angular/router';
import { forkJoin } from 'rxjs';
import { AccountsService, LinkedAccount } from '../../../core/services/accounts.service';
import { AuthService } from '../../../core/services/auth.service';
import { CategorySlice, DashboardService, MerchantSlice } from '../../../core/services/dashboard.service';
import { HouseholdService, HouseholdSummary } from '../../../core/services/household.service';
import { PlaidLinkService } from '../../../core/services/plaid-link.service';
import { PlaidService } from '../../../core/services/plaid.service';
import { daysAgo, toDateOnlyString } from '../../../core/date-range';
import { AccountToggleList } from '../account-toggle-list/account-toggle-list';
import { HouseholdToggle } from '../household-toggle/household-toggle';
import { PieSlice, SpendPieChart } from '../spend-pie-chart/spend-pie-chart';

@Component({
  selector: 'app-dashboard',
  imports: [
    MatToolbarModule,
    MatButtonModule,
    MatProgressSpinnerModule,
    MatFormFieldModule,
    MatInputModule,
    MatDatepickerModule,
    MatIconModule,
    RouterLink,
    AccountToggleList,
    HouseholdToggle,
    SpendPieChart,
  ],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.scss',
})
export class Dashboard implements OnInit {
  private readonly accountsApi = inject(AccountsService);
  private readonly householdApi = inject(HouseholdService);
  private readonly dashboardApi = inject(DashboardService);
  private readonly plaidApi = inject(PlaidService);
  private readonly plaidLink = inject(PlaidLinkService);
  private readonly router = inject(Router);
  readonly auth = inject(AuthService);

  readonly loading = signal(true);
  readonly connecting = signal(false);
  readonly connectError = signal<string | null>(null);
  readonly accounts = signal<LinkedAccount[]>([]);
  readonly household = signal<HouseholdSummary | null>(null);
  readonly includeHousehold = signal(true);
  readonly checkedIds = signal<ReadonlySet<number>>(new Set());
  readonly slices = signal<CategorySlice[]>([]);
  readonly drilldownCategory = signal<string | null>(null);
  readonly merchantSlices = signal<MerchantSlice[]>([]);
  readonly startDate = signal<Date>(daysAgo(30));
  readonly endDate = signal<Date>(new Date());

  readonly today = new Date();

  readonly rangeLabel = computed(() => {
    const fmt = (d: Date) => d.toLocaleDateString(undefined, { month: 'short', day: 'numeric', year: 'numeric' });
    return `${fmt(this.startDate())} – ${fmt(this.endDate())}`;
  });

  readonly pieSlices = computed<PieSlice[]>(() => {
    const category = this.drilldownCategory();
    if (category !== null) {
      return this.merchantSlices().map((m) => ({
        label: m.merchant,
        total: m.total,
        transactionCount: m.transactionCount,
      }));
    }
    return this.slices().map((s) => ({
      label: s.category,
      total: s.total,
      transactionCount: s.transactionCount,
    }));
  });

  readonly visibleAccounts = computed(() =>
    this.includeHousehold() ? this.accounts() : this.accounts().filter((a) => a.isMine),
  );

  readonly effectiveAccountIds = computed(() => {
    const checked = this.checkedIds();
    return this.visibleAccounts()
      .map((a) => a.accountId)
      .filter((id) => checked.has(id));
  });

  ngOnInit(): void {
    this.loadAccountsAndHousehold(() => this.loading.set(false));
  }

  onAccountToggled(accountId: number): void {
    const next = new Set(this.checkedIds());
    if (next.has(accountId)) {
      next.delete(accountId);
    } else {
      next.add(accountId);
    }
    this.checkedIds.set(next);
    this.refreshChart();
  }

  onIncludeHouseholdChanged(value: boolean): void {
    this.includeHousehold.set(value);
    this.refreshChart();
  }

  onStartDateChanged(date: Date | null): void {
    if (!date) return;
    this.startDate.set(date);
    this.refreshChart();
  }

  onEndDateChanged(date: Date | null): void {
    if (!date) return;
    this.endDate.set(date);
    this.refreshChart();
  }

  onCategoryClick(category: string): void {
    this.drilldownCategory.set(category);
    this.refreshChart();
  }

  backToCategories(): void {
    this.drilldownCategory.set(null);
    this.merchantSlices.set([]);
    this.refreshChart();
  }

  connectBank(): void {
    this.connecting.set(true);
    this.connectError.set(null);

    this.plaidApi.createLinkToken().subscribe({
      next: ({ linkToken }) => {
        this.plaidLink.open(
          linkToken,
          (publicToken, institutionName) => {
            this.plaidApi.exchangePublicToken(publicToken, institutionName).subscribe({
              next: () => this.loadAccountsAndHousehold(() => this.connecting.set(false)),
              error: () => {
                this.connectError.set('Could not connect that account. Please try again.');
                this.connecting.set(false);
              },
            });
          },
          () => this.connecting.set(false), // user closed the Plaid Link widget without finishing
        );
      },
      error: () => {
        this.connectError.set('Could not start the bank connection. Please try again.');
        this.connecting.set(false);
      },
    });
  }

  logout(): void {
    this.auth.logout();
    this.router.navigate(['/']);
  }

  private loadAccountsAndHousehold(onDone: () => void): void {
    forkJoin({
      accounts: this.accountsApi.list(),
      household: this.householdApi.getMyHousehold(),
    }).subscribe(({ accounts, household }) => {
      const existingIds = new Set(this.accounts().map((a) => a.accountId));
      const nextChecked = new Set(this.checkedIds());
      for (const a of accounts) {
        if (!existingIds.has(a.accountId)) nextChecked.add(a.accountId); // newly linked accounts default to checked
      }

      this.accounts.set(accounts);
      this.household.set(household);
      this.includeHousehold.set(household.hasHousehold);
      this.checkedIds.set(nextChecked);
      this.refreshChart();
      onDone();
    });
  }

  private refreshChart(): void {
    const ids = this.effectiveAccountIds();
    if (ids.length === 0) {
      this.slices.set([]);
      this.merchantSlices.set([]);
      return;
    }
    const from = toDateOnlyString(this.startDate());
    const to = toDateOnlyString(this.endDate());
    const category = this.drilldownCategory();

    if (category !== null) {
      this.dashboardApi
        .spendByMerchant(category, ids, from, to)
        .subscribe((slices) => this.merchantSlices.set(slices));
    } else {
      this.dashboardApi.spendByCategory(ids, from, to).subscribe((slices) => this.slices.set(slices));
    }
  }
}
