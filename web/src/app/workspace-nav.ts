import { Component, model } from '@angular/core';
@Component({
  selector: 'app-workspace-nav',
  templateUrl: './workspace-nav.html',
  styleUrl: './workspace-nav.scss',
})
export class WorkspaceNav {
  readonly section = model('Overview');
  readonly sections = ['Overview', 'Players', 'Clan', 'Wars', 'Collection', 'My profile'];
}
