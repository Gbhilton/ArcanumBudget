import { Component } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-landing',
  imports: [RouterLink, MatButtonModule],
  templateUrl: './landing.html',
  styleUrl: './landing.scss',
})
export class Landing {}
