import { parseSettings } from './preferences';
describe('Preferences', () => {
  it('recovers from corrupt local storage', () =>
    expect(parseSettings('{bad').theme).toBe('system'));
  it('rejects unsupported values', () => {
    const p = parseSettings(
      JSON.stringify({ theme: 'wrong', historyDays: 123, primaryTag: 'bad' }),
    );
    expect(p.historyDays).toBe(14);
    expect(p.primaryTag).toBe('');
  });
  it('retains valid preferences', () =>
    expect(
      parseSettings(JSON.stringify({ theme: 'dark', historyDays: 90, primaryTag: '#P0Y28' })).theme,
    ).toBe('dark'));
});
