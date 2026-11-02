export const ALL_STATES = ['home', 'home-dialog', 'join', 'room-lobby', 'room-voting', 'room-revealed', 'room-settings', 'credits'];
export const ALL_SIZES = ['desktop', 'phone'];
export const ALL_SCHEMES = ['light', 'dark'];
export const SIZES = { desktop: { width: 1280, height: 800 }, phone: { width: 375, height: 812 } };

const GUID = '[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}';

/** The room id differs per run: blank it in the page text so it never counts as a difference. */
export async function mask(page) {
  await page.evaluate((source) => {
    const re = new RegExp(source, 'g');
    const walker = document.createTreeWalker(document.body, NodeFilter.SHOW_TEXT);
    for (let node = walker.nextNode(); node; node = walker.nextNode()) {
      node.nodeValue = node.nodeValue.replace(re, 'ROOM-ID');
    }
  }, GUID);
}

/**
 * Runs in the page. One record per visible text, keyed by the normalised text, so the old and the new DOM pair up
 * despite different markup. Box, radius and padding come from the enclosing button when the text sits in one.
 */
export function probeFn() {
  const out = {};
  const backgroundOf = (el) => {
    for (let e = el; e; e = e.parentElement) {
      const colour = getComputedStyle(e).backgroundColor;
      if (colour && colour !== 'rgba(0, 0, 0, 0)' && colour !== 'transparent') {
        return colour;
      }
    }
    return 'rgba(0, 0, 0, 0)';
  };
  const walker = document.createTreeWalker(document.body, NodeFilter.SHOW_TEXT);
  for (let node = walker.nextNode(); node; node = walker.nextNode()) {
    const text = node.nodeValue.replace(/\s+/g, ' ').trim();
    const el = node.parentElement;
    if (!text || !el || /^(SCRIPT|STYLE|NOSCRIPT)$/.test(el.tagName)) {
      continue;
    }
    const rect = el.getBoundingClientRect();
    const style = getComputedStyle(el);
    if (rect.width === 0 || rect.height === 0 || style.visibility === 'hidden' || style.display === 'none') {
      continue;
    }
    const key = text.toLowerCase();
    if (out[key]) {
      continue;
    }
    const box = el.closest('button, .btn') ?? el;
    const boxStyle = getComputedStyle(box);
    const boxRect = box.getBoundingClientRect();
    out[key] = {
      tag: el.tagName.toLowerCase(),
      font: style.fontFamily.split(',')[0].replace(/["']/g, '').trim(),
      size: style.fontSize,
      weight: style.fontWeight,
      color: style.color,
      bg: backgroundOf(box),
      align: style.textAlign,
      deco: style.textDecorationLine,
      radius: boxStyle.borderRadius,
      pad: boxStyle.padding,
      x: Math.round(boxRect.x),
      y: Math.round(boxRect.y),
      w: Math.round(boxRect.width),
      h: Math.round(boxRect.height),
    };
  }
  return out;
}
