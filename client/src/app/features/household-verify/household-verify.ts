import { Component, OnInit, inject, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { HouseholdService } from '../../core/services/household.service';

type VerifyState = 'verifying' | 'success' | 'error';

@Component({
  selector: 'app-household-verify',
  imports: [RouterLink, MatButtonModule, MatProgressSpinnerModule],
  templateUrl: './household-verify.html',
  styleUrl: './household-verify.scss',
})
export class HouseholdVerify implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly householdApi = inject(HouseholdService);

  readonly state = signal<VerifyState>('verifying');

  ngOnInit(): void {
    const token = this.route.snapshot.queryParamMap.get('token');
    if (!token) {
      this.state.set('error');
      return;
    }

    this.householdApi.verify(token).subscribe({
      next: (res) => this.state.set(res.verified ? 'success' : 'error'),
      error: () => this.state.set('error'),
    });
  }
}
