import { CardOption, GroupStatistics } from '../../../models/room';
import { ChartColors, buildChartConfig, chartSummary } from './chart-config';

const COLORS: ChartColors = { primary: '#007bff', highest: '#28a745', text: '#212529', border: '#e5e5e5' };
const CARDS: CardOption[] = [
  { value: '1', display: null },
  { value: '5', display: null },
  { value: 'coffee', display: '☕' },
  { value: 'two', display: 'two-finger-512x512.png' },
];

function group(overrides: Partial<GroupStatistics>): GroupStatistics {
  return { marks: [], highestMark: null, averageMark: null, ...overrides };
}

describe('buildChartConfig', () => {
  it('has a bar per cast mark, in the order given, with its percentage', () => {
    const config = buildChartConfig(
      group({
        marks: [
          { mark: '1', percentage: 33.3 },
          { mark: '5', percentage: 66.7 },
        ],
      }),
      CARDS,
      COLORS,
    );

    expect(config.data.labels).toEqual(['1', '5']);
    expect(config.data.datasets[0].data).toEqual([33.3, 66.7]);
  });

  it('labels a bar with the card display text, falling back to the mark', () => {
    const config = buildChartConfig(
      group({
        marks: [
          { mark: 'coffee', percentage: 50 },
          { mark: 'unknown', percentage: 50 },
        ],
      }),
      CARDS,
      COLORS,
    );

    expect(config.data.labels).toEqual(['☕', 'unknown']);
  });

  it('leaves the text label empty for an image card and reserves room for the image', () => {
    const config = buildChartConfig(
      group({ marks: [{ mark: 'two', percentage: 100 }] }),
      CARDS,
      COLORS,
    );

    expect(config.data.labels).toEqual(['']);
    expect(config.options?.layout?.padding).toEqual({ top: 20, bottom: 28 });
  });

  it('reserves no room when there is no image card', () => {
    const config = buildChartConfig(
      group({ marks: [{ mark: '1', percentage: 100 }] }),
      CARDS,
      COLORS,
    );

    expect(config.options?.layout?.padding).toEqual({ top: 20, bottom: 0 });
  });

  it('colours only the highest mark', () => {
    const config = buildChartConfig(
      group({
        highestMark: '5',
        marks: [
          { mark: '1', percentage: 40 },
          { mark: '5', percentage: 60 },
        ],
      }),
      CARDS,
      COLORS,
    );

    expect(config.data.datasets[0].backgroundColor).toEqual([COLORS.primary, COLORS.highest]);
  });

  it('colours every bar the same when the highest is tied', () => {
    const config = buildChartConfig(
      group({
        marks: [
          { mark: '1', percentage: 50 },
          { mark: '5', percentage: 50 },
        ],
      }),
      CARDS,
      COLORS,
    );

    expect(config.data.datasets[0].backgroundColor).toEqual([COLORS.primary, COLORS.primary]);
  });

  it('hides the y axis on a fixed 0 to 100 scale, shows no legend and formats the tooltip as a percentage', () => {
    const config = buildChartConfig(
      group({ marks: [{ mark: '1', percentage: 12.5 }] }),
      CARDS,
      COLORS,
    );

    const y = config.options?.scales?.['y'] as { min: number; max: number; display: boolean };
    expect([y.min, y.max, y.display]).toEqual([0, 100, false]);
    expect(config.options?.plugins?.legend?.display).toBe(false);
    const label = config.options?.plugins?.tooltip?.callbacks?.label as (item: unknown) => string;
    expect(label({ parsed: { y: 12.5 } })).toBe('12.5%');
  });

  it('uses 16px axis labels in the theme text colour', () => {
    const config = buildChartConfig(group({}), CARDS, COLORS);

    const x = config.options?.scales?.['x'] as { ticks: { color: string; font: { size: number } } };
    expect(x.ticks.color).toBe(COLORS.text);
    expect(x.ticks.font.size).toBe(16);
  });

  it('caps the bar thickness at 48 and rounds the corners by 4', () => {
    const config = buildChartConfig(group({ marks: [{ mark: '1', percentage: 100 }] }), CARDS, COLORS);

    const dataset = config.data.datasets[0] as { maxBarThickness: number; borderRadius: number };
    expect(dataset.maxBarThickness).toBe(48);
    expect(dataset.borderRadius).toBe(4);
  });

  it('draws the x-axis baseline in the border colour and keeps the grid hidden', () => {
    const config = buildChartConfig(group({}), CARDS, { ...COLORS, border: '#404040' });

    const x = config.options?.scales?.['x'] as {
      border: { display: boolean; color: string };
      grid: { display: boolean };
    };
    expect(x.border).toEqual({ display: true, color: '#404040' });
    expect(x.grid.display).toBe(false);
  });

  describe('percent labels', () => {
    it('draws "{percentage}%" above each bar, 14px in the text colour', () => {
      const config = buildChartConfig(
        group({
          marks: [
            { mark: '1', percentage: 33.3 },
            { mark: '5', percentage: 66.7 },
          ],
        }),
        CARDS,
        { ...COLORS, text: '#f8f9fa' },
      );
      const plugin = config.plugins?.find((p) => p.id === 'percentLabels');
      const ctx = {
        save: vi.fn(),
        restore: vi.fn(),
        fillText: vi.fn(),
        font: '',
        fillStyle: '',
        textAlign: '',
        textBaseline: '',
        fontAtDraw: '',
        colorAtDraw: '',
      };
      ctx.fillText.mockImplementation(() => {
        ctx.fontAtDraw = ctx.font;
        ctx.colorAtDraw = ctx.fillStyle;
      });
      const chart = {
        ctx,
        data: config.data,
        getDatasetMeta: () => ({
          data: [
            { x: 10, y: 80 },
            { x: 50, y: 30 },
          ],
        }),
      };

      plugin?.afterDatasetsDraw?.(chart as never, {} as never, {}, false);

      expect(plugin).toBeDefined();
      expect(ctx.fillText.mock.calls.map((call) => call[0])).toEqual(['33.3%', '66.7%']);
      expect(ctx.fillText.mock.calls[0][1]).toBe(10);
      expect(ctx.fillText.mock.calls[0][2]).toBeLessThan(80);
      expect(ctx.fontAtDraw).toContain('14px');
      expect(ctx.colorAtDraw).toBe('#f8f9fa');
    });
  });

  describe('image labels', () => {
    afterEach(() => {
      vi.unstubAllGlobals();
      vi.restoreAllMocks();
    });

    it('tints the label images with the theme text colour before drawing them', () => {
      class LoadedImage {
        complete = true;
        naturalWidth = 8;
        naturalHeight = 8;
        onload: (() => void) | null = null;
        src = '';
      }
      vi.stubGlobal('Image', LoadedImage);
      const tintContext = {
        drawImage: vi.fn(),
        fillRect: vi.fn(),
        globalCompositeOperation: 'source-over',
        fillStyle: '',
        operationAtFill: '',
        colorAtFill: '',
      };
      tintContext.fillRect.mockImplementation(() => {
        tintContext.operationAtFill = tintContext.globalCompositeOperation;
        tintContext.colorAtFill = tintContext.fillStyle;
      });
      const tintCanvas = { width: 0, height: 0, getContext: () => tintContext };
      vi.spyOn(document, 'createElement').mockReturnValue(tintCanvas as unknown as HTMLElement);
      const drawn = vi.fn();
      const config = buildChartConfig(
        group({ marks: [{ mark: 'two', percentage: 100 }] }),
        CARDS,
        { ...COLORS, text: '#f8f9fa' },
      );
      const plugin = config.plugins?.find((p) => p.id === 'cardImageLabels');
      const chart = {
        scales: { x: { top: 0, getPixelForTick: () => 50 } },
        ctx: { drawImage: drawn },
      };

      plugin?.afterDraw?.(chart as never, { cancelable: true } as never, {});
      plugin?.afterDraw?.(chart as never, { cancelable: true } as never, {});

      expect(tintContext.operationAtFill).toBe('source-in');
      expect(tintContext.colorAtFill).toBe('#f8f9fa');
      expect(drawn).toHaveBeenCalledTimes(2);
      expect(drawn.mock.calls[0][0]).toBe(tintCanvas);
      // The tinted copy is made once per image and colour, then reused.
      expect(tintContext.fillRect).toHaveBeenCalledTimes(1);
    });
  });
});

describe('chartSummary', () => {
  it('lists each mark with its share', () => {
    const summary = chartSummary(
      'All votes',
      group({
        marks: [
          { mark: '1', percentage: 50 },
          { mark: '5', percentage: 50 },
        ],
      }),
    );

    expect(summary).toBe('All votes: 1 50%, 5 50%');
  });

  it('says there are no votes for an empty group', () => {
    expect(chartSummary('Testers votes', group({}))).toBe('Testers votes: no votes');
  });
});
