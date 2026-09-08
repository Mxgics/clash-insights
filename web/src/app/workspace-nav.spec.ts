import { TestBed } from '@angular/core/testing';
import { WorkspaceNav } from './workspace-nav';
describe('Navigation', () => {
  it('marks the selected section', () => {
    const f = TestBed.createComponent(WorkspaceNav);
    f.componentInstance.section.set('Wars');
    f.detectChanges();
    expect(f.nativeElement.querySelector('[aria-current="page"]').textContent).toContain('Wars');
  });
});
