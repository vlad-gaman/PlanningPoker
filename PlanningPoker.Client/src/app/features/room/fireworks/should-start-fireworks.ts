export interface FireworksPolicyInput {
  /** The reveal count went up since the last look: the user watched this reveal happen, it was not there on joining. */
  isNewReveal: boolean;
  /** The revealed votes ended in consensus. */
  consensus: boolean;
  /** The room has fireworks turned on. */
  showFireworks: boolean;
}

/** Whether to launch fireworks: only for a live reveal that ended in consensus, in a room that has them on. */
export function shouldStartFireworks({ isNewReveal, consensus, showFireworks }: FireworksPolicyInput): boolean {
  return isNewReveal && consensus && showFireworks;
}
