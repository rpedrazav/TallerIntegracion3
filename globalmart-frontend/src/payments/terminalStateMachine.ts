export const TERMINAL_STATES = [
  'available',
  'processing',
  'approved',
  'declined',
  'uncertain',
  'cancelled',
] as const;

export type TerminalState = (typeof TERMINAL_STATES)[number];

export type TerminalEvent =
  | 'START'
  | 'APPROVE'
  | 'DECLINE'
  | 'MARK_UNCERTAIN'
  | 'VERIFY'
  | 'RETRY'
  | 'CANCEL';

const transitions: Record<TerminalState, Partial<Record<TerminalEvent, TerminalState>>> = {
  available: { START: 'processing' },
  processing: {
    APPROVE: 'approved',
    DECLINE: 'declined',
    MARK_UNCERTAIN: 'uncertain',
    CANCEL: 'cancelled',
  },
  approved: {},
  declined: { RETRY: 'available' },
  uncertain: { VERIFY: 'processing', CANCEL: 'cancelled' },
  cancelled: {},
};

export class InvalidTerminalTransitionError extends Error {
  constructor(
    public readonly state: TerminalState,
    public readonly event: TerminalEvent,
  ) {
    super(`Transición de terminal inválida: ${state} + ${event}`);
    this.name = 'InvalidTerminalTransitionError';
  }
}

export function canTransitionTerminal(state: TerminalState, event: TerminalEvent): boolean {
  return transitions[state][event] !== undefined;
}

export function transitionTerminalState(state: TerminalState, event: TerminalEvent): TerminalState {
  const nextState = transitions[state][event];
  if (!nextState) throw new InvalidTerminalTransitionError(state, event);
  return nextState;
}
