/** What a synth did to a recorded context: the nodes it made and when its sources started and stopped. */
export interface RecordedNode {
  kind: 'oscillator' | 'gain' | 'filter' | 'buffer-source' | 'other';
  /** The `type` of an oscillator or filter, when it has one. */
  type?: string;
  startedAt: number | null;
  stoppedAt: number | null;
}

class FakeParam {
  value = 0;
  setValueAtTime = vi.fn().mockReturnThis();
  linearRampToValueAtTime = vi.fn().mockReturnThis();
  exponentialRampToValueAtTime = vi.fn().mockReturnThis();
  setTargetAtTime = vi.fn().mockReturnThis();
  cancelScheduledValues = vi.fn().mockReturnThis();
  setValueCurveAtTime = vi.fn().mockReturnThis();
}

/**
 * A stand-in for an AudioContext that records what a synth builds, so a spec can check a sound's shape (it starts and
 * stops its sources, inside the time it reports) without a browser's audio. It cannot say how a sound sounds.
 */
export function fakeAudioContext() {
  const nodes: RecordedNode[] = [];

  function node(kind: RecordedNode['kind'], extra: Record<string, unknown> = {}) {
    const record: RecordedNode = { kind, startedAt: null, stoppedAt: null };
    nodes.push(record);
    const base = {
      connect: vi.fn((target: unknown) => target),
      disconnect: vi.fn(),
      start: vi.fn((when = 0) => {
        record.startedAt = when;
      }),
      stop: vi.fn((when = 0) => {
        record.stoppedAt = when;
      }),
      addEventListener: vi.fn(),
    };
    const result = Object.assign(base, extra);
    Object.defineProperty(result, 'type', {
      get: () => record.type,
      set: (value: string) => {
        record.type = value;
      },
    });
    return result;
  }

  const context = {
    currentTime: 0,
    sampleRate: 44100,
    state: 'running',
    destination: { connect: vi.fn() },
    createOscillator: vi.fn(() => node('oscillator', { frequency: new FakeParam(), detune: new FakeParam(), setPeriodicWave: vi.fn() })),
    createGain: vi.fn(() => node('gain', { gain: new FakeParam() })),
    createBiquadFilter: vi.fn(() => node('filter', { frequency: new FakeParam(), Q: new FakeParam(), gain: new FakeParam(), detune: new FakeParam() })),
    createBufferSource: vi.fn(() => node('buffer-source', { buffer: null, playbackRate: new FakeParam(), loop: false })),
    createBuffer: vi.fn((channels: number, length: number) => ({
      numberOfChannels: channels,
      length,
      sampleRate: 44100,
      getChannelData: () => new Float32Array(length),
    })),
    createPeriodicWave: vi.fn(() => ({})),
    createStereoPanner: vi.fn(() => node('other', { pan: new FakeParam() })),
    createDynamicsCompressor: vi.fn(() => node('other', { threshold: new FakeParam(), knee: new FakeParam(), ratio: new FakeParam(), attack: new FakeParam(), release: new FakeParam() })),
    createWaveShaper: vi.fn(() => node('other', { curve: null, oversample: 'none' })),
    createDelay: vi.fn(() => node('other', { delayTime: new FakeParam() })),
    close: vi.fn().mockResolvedValue(undefined),
    resume: vi.fn().mockResolvedValue(undefined),
  };
  const destination = node('other');
  return {
    context: context as unknown as BaseAudioContext,
    destination: destination as unknown as AudioNode,
    nodes,
    sources: () => nodes.filter((n) => n.kind === 'oscillator' || n.kind === 'buffer-source'),
  };
}
