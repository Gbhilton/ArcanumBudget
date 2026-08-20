import { Component, computed, input, output } from '@angular/core';
import { MatCheckboxModule } from '@angular/material/checkbox';
import type { LinkedAccount } from '../../../core/services/accounts.service';

interface OwnerGroup {
  ownerDisplayName: string;
  accounts: LinkedAccount[];
}

@Component({
  selector: 'app-account-toggle-list',
  imports: [MatCheckboxModule],
  templateUrl: './account-toggle-list.html',
  styleUrl: './account-toggle-list.scss',
})
export class AccountToggleList {
  readonly accounts = input<LinkedAccount[]>([]);
  readonly checkedIds = input<ReadonlySet<number>>(new Set());
  readonly toggled = output<number>();

  readonly showOwnerLabels = computed(
    () => new Set(this.accounts().map((a) => a.ownerUserId)).size > 1,
  );

  readonly groups = computed<OwnerGroup[]>(() => {
    const byOwner = new Map<string, OwnerGroup>();
    for (const account of this.accounts()) {
      const group = byOwner.get(account.ownerUserId);
      if (group) {
        group.accounts.push(account);
      } else {
        byOwner.set(account.ownerUserId, {
          ownerDisplayName: account.ownerDisplayName,
          accounts: [account],
        });
      }
    }
    return [...byOwner.values()];
  });

  isChecked(accountId: number): boolean {
    return this.checkedIds().has(accountId);
  }
}
