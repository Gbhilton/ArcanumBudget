import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatToolbarModule } from '@angular/material/toolbar';
import { Router } from '@angular/router';
import { forkJoin } from 'rxjs';
import { AccountsService, LinkedAccount } from '../../../core/services/accounts.service';
import { AuthService } from '../../../core/services/auth.service';
import { CategorySlice, DashboardService } from '../../../core/services/dashboard.service';
import { HouseholdService, HouseholdSummary } from '../../../core/services/household.service';
import { AccountToggleList } from '../account-toggle-list/account-toggle-list';
import { HouseholdToggle } from '../household-toggle/household-toggle';
import { SpendPieChart } from '../spend-pie-chart/spend-pie-chart';

@Component({
  selector: 'app-dashboard',
  imports: [MatToolbarModule, MatButtonModule, AccountToggleList, HouseholdToggle, SpendPieChart],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.scss',
})
export class Dashboard implements OnInit {
  private readonly accountsApi = inject(AccountsService);
  private readonly householdApi = inject(HouseholdService);
  private readonly dashboardApi = inject(DashboardService);
  private readonly router = inject(Router);
  readonly auth = inject(AuthService);

  readonly loading = signal(true);
  readonly accounts = signal<LinkedAccount[]>([]);
  readonly household = signal<HouseholdSummary | null>(null);
  readonly includeHousehold = signal(true);
  readonly checkedIds = signal<ReadonlySet<number>>(new Set());
  readonly slices = signal<CategorySlice[]>([]);

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
    forkJoin({
      accounts: this.accountsApi.list(),
      household: this.householdApi.getMyHousehold(),
    }).subscribe(({ accounts, household }) => {
      this.accounts.set(accounts);
      this.household.set(household);
      this.includeHousehold.set(household.hasHousehold);
      this.checkedIds.set(new Set(accounts.map((a) => a.accountId)));
      this.loading.set(false);
      this.refreshChart();
    });
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

  logout(): void {
    this.auth.logout();
    this.router.navigate(['/']);
  }

  private refreshChart(): void {
    const ids = this.effectiveAccountIds();
    if (ids.length === 0) {
      this.slices.set([]);
      return;
    }
    this.dashboardApi.spendByCategory(ids).subscribe((slices) => this.slices.set(slices));
  }
}
