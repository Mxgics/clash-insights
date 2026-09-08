import { chartPath } from './history-chart';
describe('Observed chart', () => {
  it('breaks paths across collection gaps and missing values', () => {
    const p = chartPath(
      [
        { at: '2026-09-01T00:00:00Z', trophies: 100, donations: 0 },
        { at: '2026-09-01T01:00:00Z', trophies: 120, donations: 1 },
        { at: '2026-09-02T00:00:00Z', trophies: 140, donations: 2 },
      ],
      60,
    );
    expect((p.match(/M/g) || []).length).toBe(2);
    expect((p.match(/L/g) || []).length).toBe(1);
  });
  it('does not invent points for unknown trophies', () =>
    expect(chartPath([{ at: '2026-09-01', trophies: null, donations: 0 }], 60)).toBe(''));
});
