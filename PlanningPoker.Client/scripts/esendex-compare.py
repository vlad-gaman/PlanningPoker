"""Comparison sheets and measurements for the Esendex Fibonacci card art.

For each photographed card: find the card face in the photo, warp it upright to 5:7, and put it beside our light
render of the same card (photo left, render right). Also measure the illustration from the photo: its bounding box as
fractions of the card face, sampled colours, and simplified outline contours in the 100 by 140 art units that the SVG
uses. Judges compare the sheets; the contours are only a starting point for hand-drawn paths.

Usage (Python 3 with opencv-python-headless and numpy):
  python scripts/esendex-compare.py --photos "C:/Users/scien/Pictures/Esendex Planning Poker cards" \
      --renders "C:/Users/scien/AppData/Local/Temp/claude/esendex-renders" \
      --out "C:/Users/scien/AppData/Local/Temp/claude/esendex-renders/compare" \
      --contours "C:/Users/scien/AppData/Local/Temp/claude/esendex-contours"
Add --only 100 to rebuild one card.
"""

import argparse
import json
import os

import cv2
import numpy as np

# Photo file -> card value, from the photos as shot.
PHOTOS = {
    "IMG20261110130338.jpg": "1",
    "IMG20261110130342.jpg": "2",
    "IMG20261110130346.jpg": "3",
    "IMG20261110130352.jpg": "5",
    "IMG20261110130357.jpg": "8",
    "IMG20261110130401.jpg": "13",
    "IMG20261110130405.jpg": "30",
    "IMG20261110130412.jpg": "100",
    "IMG20261110130418.jpg": "?",
    "IMG20261110130429.jpg": "coffee",
}

CARD_W, CARD_H = 500, 700  # warped card face, 5:7
SHEET_H = 841  # height of each panel on the sheet, matching the render's height


def file_stem(value):
    return "question" if value == "?" else value


def find_card(img):
    """Warp the bright card face upright. The card is the largest bright, low-saturation blob."""
    hsv = cv2.cvtColor(img, cv2.COLOR_BGR2HSV)
    mask = ((hsv[:, :, 2] > 150) & (hsv[:, :, 1] < 90)).astype(np.uint8) * 255
    mask = cv2.morphologyEx(mask, cv2.MORPH_OPEN, np.ones((9, 9), np.uint8))
    contours, _ = cv2.findContours(mask, cv2.RETR_EXTERNAL, cv2.CHAIN_APPROX_SIMPLE)
    card = max(contours, key=cv2.contourArea)
    rect = cv2.minAreaRect(card)
    box = cv2.boxPoints(rect).astype(np.float32)
    # Order corners: top-left, top-right, bottom-right, bottom-left, with the long side vertical.
    s = box.sum(axis=1)
    d = np.diff(box, axis=1).ravel()
    ordered = np.array(
        [box[np.argmin(s)], box[np.argmin(d)], box[np.argmax(s)], box[np.argmax(d)]], dtype=np.float32
    )
    width = np.linalg.norm(ordered[1] - ordered[0])
    height = np.linalg.norm(ordered[3] - ordered[0])
    if width > height:
        ordered = np.roll(ordered, 1, axis=0)
    dst = np.array([[0, 0], [CARD_W, 0], [CARD_W, CARD_H], [0, CARD_H]], dtype=np.float32)
    matrix = cv2.getPerspectiveTransform(ordered, dst)
    warped = cv2.warpPerspective(img, matrix, (CARD_W, CARD_H))
    # Cut the mat that the bright-blob detection lets in along the edges, then scale back to the card size.
    m = int(CARD_W * 0.035)
    return cv2.resize(warped[m : CARD_H - m, m : CARD_W - m], (CARD_W, CARD_H), interpolation=cv2.INTER_CUBIC)


def illustration_mask(card):
    """Pixels that are not paper. The paper is a warm cream, so light grey parts of the illustration are told apart
    by their lack of yellow; coloured parts (purple, gold) by their saturation; shadows are left out by their
    closeness to the paper's yellow."""
    paper = np.median(card[int(CARD_H * 0.9) :, int(CARD_W * 0.1) : int(CARD_W * 0.9)].reshape(-1, 3), axis=0)
    lab = cv2.cvtColor(card, cv2.COLOR_BGR2LAB).astype(np.float32)
    paper_lab = np.median(
        cv2.cvtColor(card, cv2.COLOR_BGR2LAB)[int(CARD_H * 0.9) :, int(CARD_W * 0.1) : int(CARD_W * 0.9)].reshape(-1, 3),
        axis=0,
    ).astype(np.float32)
    # Lab b*: the cream paper sits at a warm (positive) b*; grey metal, shadow and white sit near zero or below.
    cooler = (lab[:, :, 2] - paper_lab[2]) < -6
    darker = lab[:, :, 0] < paper_lab[0] - 30
    sat = cv2.cvtColor(card, cv2.COLOR_BGR2HSV)[:, :, 1].astype(np.float32)
    coloured = sat > 70
    mask = (cooler | darker | coloured).astype(np.uint8) * 255
    mask = cv2.morphologyEx(mask, cv2.MORPH_OPEN, np.ones((5, 5), np.uint8))
    mask = cv2.morphologyEx(mask, cv2.MORPH_CLOSE, np.ones((9, 9), np.uint8))
    return mask, paper


def measure(card, mask, paper):
    # Corner number badges are not illustration: leave the four corner zones out of the bounding box.
    corners = np.zeros(mask.shape, bool)
    corners[: int(CARD_H * 0.16), : int(CARD_W * 0.2)] = True
    corners[: int(CARD_H * 0.16), int(CARD_W * 0.8) :] = True
    corners[int(CARD_H * 0.84) :, : int(CARD_W * 0.2)] = True
    corners[int(CARD_H * 0.84) :, int(CARD_W * 0.8) :] = True
    ys, xs = np.nonzero((mask > 0) & ~corners)
    if len(xs) == 0:
        return {}
    x0, x1, y0, y1 = xs.min(), xs.max(), ys.min(), ys.max()
    colours = {}
    hsv = cv2.cvtColor(card, cv2.COLOR_BGR2HSV)
    inside = mask > 0
    for name, sel in {
        "light": inside & (hsv[:, :, 2] > 215) & (hsv[:, :, 1] < 40),
        "grey": inside & (hsv[:, :, 1] < 45) & (hsv[:, :, 2] >= 120) & (hsv[:, :, 2] <= 215),
        "dark": inside & (hsv[:, :, 2] < 70),
    }.items():
        if sel.sum() > 50:
            b, g, r = np.median(card[sel], axis=0)
            colours[name] = "#%02x%02x%02x" % (int(r), int(g), int(b))
    # The brand purple: the corner badge, which is printed flat (no shading), sampled at its centre.
    cx, cy, rad = int(CARD_W * 0.15), int(CARD_H * 0.065), int(CARD_W * 0.02)
    badge = card[cy - rad : cy + rad, cx - rad : cx + rad].reshape(-1, 3)
    b, g, r = np.median(badge, axis=0)
    colours["badge_purple"] = "#%02x%02x%02x" % (int(r), int(g), int(b))
    return {
        "paper": "#%02x%02x%02x" % tuple(int(v) for v in paper[::-1]),
        "bbox_fraction": {
            "x0": round(x0 / CARD_W, 3),
            "x1": round(x1 / CARD_W, 3),
            "y0": round(y0 / CARD_H, 3),
            "y1": round(y1 / CARD_H, 3),
            "w": round((x1 - x0) / CARD_W, 3),
            "h": round((y1 - y0) / CARD_H, 3),
        },
        "colours": colours,
    }


def contours_in_art_units(mask, epsilon_fraction=0.006, min_area=40):
    """Outer contours of the illustration in the SVG's 100 by 140 units, simplified, largest first."""
    contours, _ = cv2.findContours(mask, cv2.RETR_EXTERNAL, cv2.CHAIN_APPROX_NONE)
    out = []
    for contour in sorted(contours, key=cv2.contourArea, reverse=True):
        area = cv2.contourArea(contour)
        if area < min_area:
            continue
        epsilon = epsilon_fraction * cv2.arcLength(contour, True)
        approx = cv2.approxPolyDP(contour, epsilon, True).reshape(-1, 2)
        points = [[round(x * 100 / CARD_W, 1), round(y * 140 / CARD_H, 1)] for x, y in approx]
        out.append({"area_fraction": round(area / (CARD_W * CARD_H), 4), "points": points})
    return out


def sheet(card, render_path, value, out_path):
    panel_w = int(CARD_W * SHEET_H / CARD_H)
    left = cv2.resize(card, (panel_w, SHEET_H), interpolation=cv2.INTER_AREA)
    render = cv2.imread(render_path)
    right = cv2.resize(render, (int(render.shape[1] * SHEET_H / render.shape[0]), SHEET_H), interpolation=cv2.INTER_AREA)
    gap = 24
    canvas = np.full((SHEET_H + 60, left.shape[1] + right.shape[1] + gap * 3, 3), 255, np.uint8)
    canvas[60 : 60 + SHEET_H, gap : gap + left.shape[1]] = left
    canvas[60 : 60 + SHEET_H, gap * 2 + left.shape[1] : gap * 2 + left.shape[1] + right.shape[1]] = right
    cv2.putText(canvas, "PHOTO  card " + value, (gap, 40), cv2.FONT_HERSHEY_SIMPLEX, 0.9, (0, 0, 0), 2)
    cv2.putText(canvas, "RENDER", (gap * 2 + left.shape[1], 40), cv2.FONT_HERSHEY_SIMPLEX, 0.9, (0, 0, 0), 2)
    cv2.imwrite(out_path, canvas)


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--photos", required=True)
    parser.add_argument("--renders", required=True)
    parser.add_argument("--out", required=True)
    parser.add_argument("--contours", required=True)
    parser.add_argument("--only")
    args = parser.parse_args()
    os.makedirs(args.out, exist_ok=True)
    os.makedirs(args.contours, exist_ok=True)

    for photo, value in PHOTOS.items():
        if args.only and value != args.only:
            continue
        img = cv2.imread(os.path.join(args.photos, photo))
        card = find_card(img)
        mask, paper = illustration_mask(card)
        stats = measure(card, mask, paper)
        stem = file_stem(value)
        cv2.imwrite(os.path.join(args.contours, f"mask-{stem}.png"), mask)
        with open(os.path.join(args.contours, f"{stem}.json"), "w", encoding="utf-8") as handle:
            json.dump({"measure": stats, "contours": contours_in_art_units(mask)}, handle, indent=1)
        light = os.path.join(args.renders, f"card-{stem}-light.png")
        dark = os.path.join(args.renders, f"card-{stem}-dark.png")
        sheet(card, light, value, os.path.join(args.out, f"compare-{stem}.png"))
        if os.path.exists(dark):
            cv2.imwrite(os.path.join(args.out, f"compare-{stem}-dark.png"), cv2.imread(dark))
        print(value, json.dumps(stats))


if __name__ == "__main__":
    main()
