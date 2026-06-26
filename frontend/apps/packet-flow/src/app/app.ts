import { Component } from '@angular/core';
import { RouterModule } from '@angular/router';
import { HeaderComponent } from './header/header.component';

@Component({
  imports: [RouterModule, HeaderComponent],
  selector: 'pf-root',
  templateUrl: './app.html',
  styleUrl: './app.css',
})
export class App {}
