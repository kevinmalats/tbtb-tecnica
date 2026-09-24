import { bootstrapApplication } from '@angular/platform-browser';
import { provideHttpClient } from '@angular/common/http';
import { provideRouter, Routes } from '@angular/router';
import { WorkspaceComponent } from './app/app.component';
import { RootComponent } from './app/root.component';

const routes: Routes = [
  { path: '', component: WorkspaceComponent },
  { path: 'patients', component: WorkspaceComponent },
  { path: 'patients/new', component: WorkspaceComponent },
  { path: 'patients/:id', component: WorkspaceComponent },
  { path: 'agenda', component: WorkspaceComponent },
  { path: 'contacts', component: WorkspaceComponent },
  { path: 'contacts/:id', component: WorkspaceComponent },
  { path: '**', redirectTo: '' }
];

bootstrapApplication(RootComponent, {
  providers: [provideHttpClient(), provideRouter(routes)]
}).catch((error: unknown) => console.error(error));
