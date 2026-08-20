import { Component, computed, inject, input, output } from '@angular/core';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { AuthService } from '../../../core/services/auth.service';
import type { HouseholdSummary } from '../../../core/services/household.service';

@Component({
  selector: 'app-household-toggle',
  imports: [MatSlideToggleModule],
  templateUrl: './household-toggle.html',
  styleUrl: './household-toggle.scss',
})
export class HouseholdToggle {
  private readonly auth = inject(AuthService);

  readonly household = input<HouseholdSummary | null>(null);
  readonly includeHousehold = input(true);
  readonly includeHouseholdChange = output<boolean>();

  readonly otherMemberNames = computed(() => {
    const myUserId = this.auth.currentUser()?.userId;
    return (this.household()?.members ?? [])
      .filter((m) => m.status === 'Verified' && m.userId !== myUserId)
      .map((m) => m.displayName);
  });
}
