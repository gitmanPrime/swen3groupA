import { Component } from '@angular/core';

@Component({
  selector: 'app-login',
  template: `
    <section class="rounded-lg bg-white p-6 shadow-sm">
      <h1 class="text-xl font-semibold">Sign in to DMS</h1>
      <p class="mt-2 text-slate-600">
        Use the Log in button in the header to access your documents.
      </p>
    </section>
  `
})
export class Login { }
