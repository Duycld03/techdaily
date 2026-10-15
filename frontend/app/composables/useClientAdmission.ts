/** Request-scoped admission state is serialized for identical SSR and first hydration markup. */
export function useClientAdmission() {
  const state = useState<'ready' | 'pending' | 'unavailable'>('client-session-admission', () => 'ready')
  return { state }
}
