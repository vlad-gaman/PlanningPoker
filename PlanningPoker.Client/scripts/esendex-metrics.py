"""Objective metrics for the Esendex Fibonacci card art: how close each render is to the real card.

Compares, for each card, the straightened real cutout with our light (and dark) render, on the same footing:
same 600 x 840 frame (5:7), the corner number badges masked out of both, and each illustration mask taken by its
colour distance from the card's paper. Writes metrics.json, metrics.md and one diagnostic image per card.

Usage (Python 3 with numpy, opencv-python-headless, scikit-image and scipy; nothing of this is in the app):
  python scripts/esendex-metrics.py --cutouts <folder with cutout-<stem>.png> --renders <folder with
      card-<stem>-light.png and card-<stem>-dark.png> --versions <folder with traced-/hand-<stem>-light.png> --out <folder>

Definitions are in the module docstring of each metric below and summarised in metrics.md.
"""

import argparse
import json
import os

import cv2
import numpy as np
from scipy.optimize import linear_sum_assignment
from scipy.ndimage import distance_transform_edt
from skimage.color import deltaE_ciede2000, rgb2lab
from skimage.metrics import structural_similarity

W, H = 600, 840  # the common frame: 5:7
STEMS = ["1", "2", "3", "5", "8", "13", "30", "100", "question", "coffee"]
NO_BADGES = {"coffee"}  # the coffee card's corner cups are art, not number badges
PAPER_DE = 12.0  # CIE76 distance from the paper that makes a pixel illustration
EDGE_TOL = 3  # pixels: an edge counts as matched within this tolerance
CHAMFER_SCALE = 12.0  # pixels: a mean edge distance of 12 px scores 1/e

# Composite weights (sum to 1). Silhouette and edges weigh most; colour and tone less.
WEIGHTS = {
    "silhouette_iou": 0.25,
    "edge_f": 0.20,
    "chamfer": 0.05,
    "bbox": 0.10,
    "area": 0.05,
    "palette": 0.10,
    "histogram": 0.05,
    "ssim": 0.10,
    "ms_ssim": 0.05,
    "tone": 0.05,
}
DARK_WEIGHTS = {"silhouette_iou": 0.30, "edge_f": 0.30, "chamfer": 0.10, "ssim": 0.15, "ms_ssim": 0.15}


def load(path, size=(W, H)):
    img = cv2.imread(path)
    if img is None:
        raise SystemExit(f"cannot read {path}")
    return cv2.resize(img, size, interpolation=cv2.INTER_AREA)


def valid_region(stem):
    """All of the frame except the two corner number badges (top-left and bottom-right), unless the card has none."""
    valid = np.ones((H, W), bool)
    if stem not in NO_BADGES:
        valid[: int(H * 0.16), : int(W * 0.2)] = False
        valid[int(H * 0.84) :, int(W * 0.8) :] = False
    return valid


def to_lab(img_bgr):
    return rgb2lab(img_bgr[:, :, ::-1].astype(np.float64) / 255.0)


def paper_lab(lab):
    """The card's paper colour: the median over a band near the bottom, clear of the illustration."""
    band = lab[int(H * 0.9) : int(H * 0.98), int(W * 0.1) : int(W * 0.9)].reshape(-1, 3)
    return np.median(band, axis=0)


def object_mask(img_bgr, valid):
    """Illustration = pixels at least PAPER_DE (CIE76) from the local paper, opened and closed, specks dropped.

    The paper is not one colour in a photo (lighting makes one side darker), so the reference is the local
    background: a 151 px median blur of the image, which the illustration covers only where it is much larger than
    the object. Soft shadows are not separated out: a shadow darker than PAPER_DE counts as object in both the real
    card and the render, so the rule treats the two alike. Light objects closer to the paper than PAPER_DE are missed."""
    blurred = cv2.GaussianBlur(img_bgr, (3, 3), 0)
    lab = to_lab(blurred)
    background = to_lab(cv2.medianBlur(blurred, 151))
    de = np.sqrt(((lab - background) ** 2).sum(axis=2))
    mask = ((de > PAPER_DE) & valid).astype(np.uint8)
    mask = cv2.morphologyEx(mask, cv2.MORPH_OPEN, np.ones((3, 3), np.uint8))
    mask = cv2.morphologyEx(mask, cv2.MORPH_CLOSE, np.ones((5, 5), np.uint8))
    n, labels, stats, _ = cv2.connectedComponentsWithStats(mask, 8)
    keep = np.zeros(n, bool)
    keep[1:] = stats[1:, cv2.CC_STAT_AREA] >= 0.0005 * W * H
    return keep[labels] & valid


def edges(img_bgr, valid):
    gray = cv2.cvtColor(img_bgr, cv2.COLOR_BGR2GRAY)
    gray = cv2.GaussianBlur(gray, (5, 5), 1.2)
    return (cv2.Canny(gray, 30, 90) > 0) & valid


def bbox_of(mask):
    ys, xs = np.nonzero(mask)
    if len(xs) == 0:
        return None
    return xs.min(), xs.max(), ys.min(), ys.max(), xs.mean(), ys.mean()


def palette(lab, mask, k=6):
    pts = lab[mask].astype(np.float32)
    if len(pts) > 20000:
        pts = pts[np.random.default_rng(0).choice(len(pts), 20000, replace=False)]
    criteria = (cv2.TERM_CRITERIA_EPS + cv2.TERM_CRITERIA_MAX_ITER, 50, 0.5)
    cv2.setRNGSeed(0)  # the k-means start is seeded, so the same pixels always give the same palette
    _, _, centres = cv2.kmeans(pts, k, None, criteria, 3, cv2.KMEANS_PP_CENTERS)
    return centres.astype(np.float64)


def ssim_map(gray_a, gray_b):
    a = cv2.GaussianBlur(gray_a.astype(np.float64), (0, 0), 1.5)
    b = cv2.GaussianBlur(gray_b.astype(np.float64), (0, 0), 1.5)
    return structural_similarity(a, b, data_range=255.0, full=True)


def ms_ssim_approx(gray_r, gray_o, union, scales=(1, 2, 4)):
    """Mean SSIM over three scales inside the union of the masks: an approximation of MS-SSIM (no torch needed)."""
    values = []
    for s in scales:
        if s == 1:
            a, b, u = gray_r, gray_o, union
        else:
            size = (W // s, H // s)
            a = cv2.resize(gray_r, size, interpolation=cv2.INTER_AREA)
            b = cv2.resize(gray_o, size, interpolation=cv2.INTER_AREA)
            u = cv2.resize(union.astype(np.float32), size, interpolation=cv2.INTER_AREA) > 0.5
        _, smap = ssim_map(a, b)
        if u.sum():
            values.append(float(smap[u].mean()))
    return float(np.mean(values)) if values else 0.0


def compare(stem, real, ours, valid, real_name="real", ours_name="render"):
    lab_r, lab_o = to_lab(real), to_lab(ours)
    mr, mo = object_mask(real, valid), object_mask(ours, valid)
    er, eo = edges(real, valid), edges(ours, valid)
    gray_r = cv2.cvtColor(real, cv2.COLOR_BGR2GRAY)
    gray_o = cv2.cvtColor(ours, cv2.COLOR_BGR2GRAY)
    union = mr | mo
    out = {}

    # 1. Silhouette
    inter = (mr & mo).sum()
    out["iou"] = float(inter / max(union.sum(), 1))
    out["dice"] = float(2 * inter / max(mr.sum() + mo.sum(), 1))

    # 2. Bounding box and centroid (fractions of the card)
    br, bo = bbox_of(mr), bbox_of(mo)
    if br is None or bo is None:
        out.update({"bbox_w_ratio": 0.0, "bbox_h_ratio": 0.0, "centre_dx": 0.0, "centre_dy": 0.0,
                    "centroid_dx": 0.0, "centroid_dy": 0.0, "bbox_score": 0.0})
    else:
        wr, hr = (br[1] - br[0]) / W, (br[3] - br[2]) / H
        wo, ho = (bo[1] - bo[0]) / W, (bo[3] - bo[2]) / H
        rw, rh = wo / max(wr, 1e-6), ho / max(hr, 1e-6)
        dx = ((bo[0] + bo[1]) - (br[0] + br[1])) / (2 * W)
        dy = ((bo[2] + bo[3]) - (br[2] + br[3])) / (2 * H)
        cdx, cdy = (bo[4] - br[4]) / W, (bo[5] - br[5]) / H
        size_score = (min(rw, 1 / rw) + min(rh, 1 / rh)) / 2
        pos_score = 1 - min(1.0, float(np.hypot(dx, dy)) / 0.1)
        out.update({"bbox_w_ratio": float(rw), "bbox_h_ratio": float(rh), "centre_dx": float(dx),
                    "centre_dy": float(dy), "centroid_dx": float(cdx), "centroid_dy": float(cdy),
                    "bbox_score": float(0.5 * size_score + 0.5 * pos_score)})

    # 3. Area coverage
    cov_r, cov_o = mr.sum() / (W * H), mo.sum() / (W * H)
    out.update({"coverage_real": float(cov_r), "coverage_render": float(cov_o),
                "area_ratio": float(cov_o / max(cov_r, 1e-6)),
                "area_score": float(min(cov_o / max(cov_r, 1e-6), max(cov_r, 1e-6) / max(cov_o, 1e-6)))})

    # 4. Edges: F-score within EDGE_TOL pixels, and a symmetric chamfer distance
    kernel = cv2.getStructuringElement(cv2.MORPH_ELLIPSE, (2 * EDGE_TOL + 1, 2 * EDGE_TOL + 1))
    dil_r = cv2.dilate(er.astype(np.uint8), kernel) > 0
    dil_o = cv2.dilate(eo.astype(np.uint8), kernel) > 0
    precision = (eo & dil_r).sum() / max(eo.sum(), 1)
    recall = (er & dil_o).sum() / max(er.sum(), 1)
    out["edge_precision"] = float(precision)
    out["edge_recall"] = float(recall)
    out["edge_f"] = float(2 * precision * recall / max(precision + recall, 1e-9))
    if er.sum() and eo.sum():
        d_r = distance_transform_edt(~er)
        d_o = distance_transform_edt(~eo)
        cham = 0.5 * (d_r[eo].mean() + d_o[er].mean())
    else:
        cham = float(max(W, H))
    out["chamfer_px"] = float(cham)
    out["chamfer_score"] = float(np.exp(-cham / CHAMFER_SCALE))

    # 5. Colour: palette matched in Lab with deltaE2000, and a Bhattacharyya histogram overlap inside the masks
    if mr.sum() and mo.sum():
        pr, po = palette(lab_r, mr), palette(lab_o, mo)
        cost = np.array([[float(deltaE_ciede2000(a, b)) for b in po] for a in pr])
        rows, cols = linear_sum_assignment(cost)
        mean_de = float(cost[rows, cols].mean())
        hist_r, _ = np.histogramdd(lab_r[mr], bins=(8, 8, 8), range=((0, 100), (-128, 127), (-128, 127)))
        hist_o, _ = np.histogramdd(lab_o[mo], bins=(8, 8, 8), range=((0, 100), (-128, 127), (-128, 127)))
        hist_r, hist_o = hist_r / hist_r.sum(), hist_o / hist_o.sum()
        bc = float(np.sqrt(hist_r * hist_o).sum())
    else:
        mean_de, bc = 100.0, 0.0
    out["palette_mean_de2000"] = mean_de
    out["palette_score"] = float(max(0.0, 1 - mean_de / 25.0))
    out["histogram_bhattacharyya"] = bc

    # 6. Structure: SSIM inside the union of the masks, and a three-scale MS-SSIM approximation
    smap_mean, smap = ssim_map(gray_r, gray_o)
    out["ssim_global"] = float(smap_mean)
    out["ssim"] = float(smap[union].mean()) if union.sum() else 0.0
    out["ms_ssim"] = ms_ssim_approx(gray_r.astype(np.float64), gray_o.astype(np.float64), union)

    # 7. Tone: mean lightness inside the masks and the contrast (standard deviation) ratio
    if mr.sum() and mo.sum():
        lr, lo = lab_r[..., 0][mr], lab_o[..., 0][mo]
        dl = float(lo.mean() - lr.mean())
        cr = float(lo.std() / max(lr.std(), 1e-6))
        out.update({"lightness_real": float(lr.mean()), "lightness_render": float(lo.mean()),
                    "lightness_delta": dl, "contrast_ratio": cr,
                    "tone_score": float(0.5 * max(0.0, 1 - abs(dl) / 30.0) + 0.5 * min(cr, 1 / max(cr, 1e-6)))})
    else:
        out.update({"lightness_real": 0.0, "lightness_render": 0.0, "lightness_delta": 0.0,
                    "contrast_ratio": 0.0, "tone_score": 0.0})

    scores = {
        "silhouette_iou": out["iou"],
        "edge_f": out["edge_f"],
        "chamfer": out["chamfer_score"],
        "bbox": out["bbox_score"],
        "area": out["area_score"],
        "palette": out["palette_score"],
        "histogram": out["histogram_bhattacharyya"],
        "ssim": out["ssim"],
        "ms_ssim": out["ms_ssim"],
        "tone": out["tone_score"],
    }
    out["scores"] = {k: float(v) for k, v in scores.items()}
    out["composite_10"] = float(10 * sum(WEIGHTS[k] * scores[k] for k in WEIGHTS))
    out["dark_structural_10"] = float(
        10 * sum(DARK_WEIGHTS[k] * scores[k] for k in DARK_WEIGHTS)
    )
    out["_masks"] = (mr, mo, er, eo, smap, union)  # for the diagnostics only; dropped before JSON
    return out


def diagnostic(stem, real, ours, result, path):
    mr, mo, er, eo, smap, union = result["_masks"]
    panel_a = np.full((H, W, 3), 255, np.uint8)
    panel_a[mr & mo] = (128, 128, 128)
    panel_a[mr & ~mo] = (0, 0, 255)  # only in the real card: red (BGR)
    panel_a[mo & ~mr] = (255, 0, 0)  # only in our render: blue
    panel_b = np.full((H, W, 3), 255, np.uint8)
    panel_b[er & eo] = (128, 128, 128)
    panel_b[er & ~eo] = (0, 0, 255)
    panel_b[eo & ~er] = (255, 0, 0)
    heat = np.full((H, W, 3), 255, np.uint8)
    diff = np.clip(1 - smap, 0, 1)
    coloured = cv2.applyColorMap((diff * 255).astype(np.uint8), cv2.COLORMAP_JET)
    heat[union] = coloured[union]
    head = np.full((70, 3 * W + 40, 3), 255, np.uint8)
    cv2.putText(head, f"{stem}: composite {result['composite_10']:.2f}/10", (10, 30),
                cv2.FONT_HERSHEY_SIMPLEX, 0.9, (0, 0, 0), 2)
    cv2.putText(head, "A masks: red only real, blue only render, grey both   B edges (3px)   C SSIM diff (red = worst)",
                (10, 60), cv2.FONT_HERSHEY_SIMPLEX, 0.55, (0, 0, 0), 1)
    gap = np.full((H, 20, 3), 255, np.uint8)
    body = np.hstack([panel_a, gap, panel_b, gap, heat])
    cv2.imwrite(path, np.vstack([head, body]))


def validation(cutouts, versions, valid_fn):
    """Traced versus hand: does the metric prefer the one the judges preferred?"""
    judges = {  # judges' scores (traced, hand), light theme
        "1": (4, 6), "2": (6, 4), "3": (5, 7), "5": (2, 7), "8": (6, 8), "13": (3, 6), "30": (5, 7),
        "100": (6, 4), "question": (2, 8), "coffee": (4, 7),
    }
    rows = {}
    for stem in STEMS:
        real = load(os.path.join(cutouts, f"cutout-{stem}.png"))
        traced_p = os.path.join(versions, f"traced-{stem}-light.png")
        hand_p = os.path.join(versions, f"hand-{stem}-light.png")
        if not (os.path.exists(traced_p) and os.path.exists(hand_p)):
            continue
        valid = valid_fn(stem)
        t = compare(stem, real, load(traced_p), valid)
        h = compare(stem, real, load(hand_p), valid)
        jt, jh = judges[stem]
        metric_pref = "traced" if t["composite_10"] > h["composite_10"] else "hand"
        judge_pref = "traced" if jt > jh else "hand"
        rows[stem] = {"traced_composite": t["composite_10"], "hand_composite": h["composite_10"],
                      "judges_traced": jt, "judges_hand": jh, "metric_prefers": metric_pref,
                      "judges_prefer": judge_pref, "agree": metric_pref == judge_pref}
    agree = sum(1 for r in rows.values() if r["agree"])
    return {"rows": rows, "agree": agree, "total": len(rows)}


def self_check(cutouts, stem, valid):
    """Sanity: the real card against itself should score 10; against itself shifted and blurred, lower."""
    real = load(os.path.join(cutouts, f"cutout-{stem}.png"))
    same = compare(stem, real, real, valid)
    shifted = np.roll(cv2.GaussianBlur(real, (0, 0), 2.0), (14, 10), axis=(0, 1))
    off = compare(stem, real, shifted, valid)
    return {"identical": same["composite_10"], "shifted_blurred": off["composite_10"]}


def weak_spots(result):
    comps = result["scores"]
    worst = sorted(comps.items(), key=lambda kv: kv[1])[:2]
    notes = []
    for name, value in worst:
        notes.append(f"{name} {value:.2f}")
    return "; ".join(notes)


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--cutouts", required=True)
    parser.add_argument("--renders", required=True)
    parser.add_argument("--versions", required=True)
    parser.add_argument("--out", required=True)
    args = parser.parse_args()
    os.makedirs(args.out, exist_ok=True)

    cards, dark = {}, {}
    for stem in STEMS:
        real = load(os.path.join(args.cutouts, f"cutout-{stem}.png"))
        valid = valid_region(stem)
        light_img = load(os.path.join(args.renders, f"card-{stem}-light.png"))
        result = compare(stem, real, light_img, valid)
        diagnostic(stem, real, light_img, result, os.path.join(args.out, f"metrics-{stem}.png"))
        cards[stem] = result
        dark_path = os.path.join(args.renders, f"card-{stem}-dark.png")
        if os.path.exists(dark_path):
            dark[stem] = compare(stem, real, load(dark_path), valid)
        print(stem, round(result["composite_10"], 2), "dark", round(dark.get(stem, {}).get("dark_structural_10", 0), 2))

    val = validation(args.cutouts, args.versions, valid_region)
    check = {stem: self_check(args.cutouts, stem, valid_region(stem)) for stem in ["1", "100"]}

    for result in list(cards.values()) + list(dark.values()):
        result.pop("_masks", None)
    summary = {
        "frame": [W, H],
        "paper_threshold_de": PAPER_DE,
        "edge_tolerance_px": EDGE_TOL,
        "chamfer_scale_px": CHAMFER_SCALE,
        "weights": WEIGHTS,
        "dark_weights": DARK_WEIGHTS,
        "light": cards,
        "dark_structural": dark,
        "validation_traced_vs_hand": val,
        "self_check": check,
    }
    with open(os.path.join(args.out, "metrics.json"), "w", encoding="utf-8") as handle:
        json.dump(summary, handle, indent=1)
    write_markdown(summary, os.path.join(args.out, "metrics.md"))
    print("wrote", args.out)


def write_markdown(s, path):
    lines = ["# Esendex card metrics", "",
             "Objective, repeatable comparison of each light render with its real card (cutout), computed before any "
             "judge looks. Every number is reproducible with `scripts/esendex-metrics.py`.", "",
             "## Composite score (0-10)", "",
             "| Card | Composite | Silhouette IoU | Dice | Edge F (3 px) | Chamfer (px) | BBox score | Area ratio | "
             "Palette dE00 | Histogram | SSIM | MS-SSIM | Tone | Weakest two |",
             "|---|---|---|---|---|---|---|---|---|---|---|---|---|---|"]
    for stem, r in s["light"].items():
        lines.append(
            f"| {stem} | **{r['composite_10']:.2f}** | {r['iou']:.3f} | {r['dice']:.3f} | {r['edge_f']:.3f} | "
            f"{r['chamfer_px']:.1f} | {r['bbox_score']:.3f} | {r['area_ratio']:.2f} | {r['palette_mean_de2000']:.1f} | "
            f"{r['histogram_bhattacharyya']:.3f} | {r['ssim']:.3f} | {r['ms_ssim']:.3f} | {r['tone_score']:.3f} | "
            f"{weak_spots(r)} |")
    lines += ["", "## Bounding box, area and centroid (raw)", "",
              "| Card | Width ratio | Height ratio | Centre dx | Centre dy | Centroid dx | Centroid dy | "
              "Coverage real | Coverage render |", "|---|---|---|---|---|---|---|---|---|"]
    for stem, r in s["light"].items():
        lines.append(f"| {stem} | {r['bbox_w_ratio']:.2f} | {r['bbox_h_ratio']:.2f} | {r['centre_dx']:+.3f} | "
                     f"{r['centre_dy']:+.3f} | {r['centroid_dx']:+.3f} | {r['centroid_dy']:+.3f} | "
                     f"{r['coverage_real']:.3f} | {r['coverage_render']:.3f} |")
    lines += ["", "## Dark theme, structural metrics only (render vs the same real cutout)", "",
              "| Card | Dark structural score | IoU | Edge F | Chamfer (px) | SSIM | MS-SSIM |", "|---|---|---|---|---|---|---|"]
    for stem, r in s["dark_structural"].items():
        lines.append(f"| {stem} | **{r['dark_structural_10']:.2f}** | {r['iou']:.3f} | {r['edge_f']:.3f} | "
                     f"{r['chamfer_px']:.1f} | {r['ssim']:.3f} | {r['ms_ssim']:.3f} |")
    v = s["validation_traced_vs_hand"]
    lines += ["", "## Calibration: does the metric agree with the judges (traced versus hand, light)?", "",
              f"Agreement on direction: **{v['agree']} of {v['total']}** cards.", "",
              "| Card | Traced composite | Hand composite | Metric prefers | Judges (traced/hand) | Judges prefer | Agree |",
              "|---|---|---|---|---|---|---|"]
    for stem, r in v["rows"].items():
        lines.append(f"| {stem} | {r['traced_composite']:.2f} | {r['hand_composite']:.2f} | {r['metric_prefers']} | "
                     f"{r['judges_traced']}/{r['judges_hand']} | {r['judges_prefer']} | "
                     f"{'yes' if r['agree'] else 'NO'} |")
    c = s["self_check"]
    lines += ["", "## Self-check", "",
              "A card compared with itself must score 10; the same card shifted 14 px and blurred must score lower.", ""]
    for stem, r in c.items():
        lines.append(f"- {stem}: identical {r['identical']:.2f}, shifted and blurred {r['shifted_blurred']:.2f}")
    lines += ["", "## How to read this", "",
              "- **Composite (0-10)**: a weighted sum of the scores below, each scaled 0-1 (1 = identical). Silhouette "
              "IoU (0.25) and edge F-score (0.20) weigh most; bounding box and palette 0.10 each; SSIM 0.10; chamfer, "
              "area, histogram, MS-SSIM and tone 0.05 each.",
              "- **Silhouette IoU / Dice**: how much of the illustration's outline the render covers, from the masks. "
              "1 means the same pixels. The masks are the pixels at least 12 CIE76 units from the card's paper.",
              "- **Bounding box**: width and height ratios (render over real; 1 is the same size), and the centre offset "
              "as a fraction of the card. A 0.1 offset costs the whole position score.",
              "- **Area ratio**: the render's illustration area over the real card's; 1 is equal.",
              "- **Edge F (3 px)**: Canny edges of both; an edge counts as matched if the other has one within 3 px. "
              "**Chamfer** is the mean distance (px) from each edge to the nearest edge of the other, averaged; "
              "scored as exp(-d/12).",
              "- **Palette dE00**: six dominant Lab colours of each card, matched optimally; the mean CIEDE2000 "
              "difference. 0 is identical; 25 or more scores 0.",
              "- **Histogram**: the Bhattacharyya overlap of 8x8x8 Lab colour histograms inside the masks; 1 is identical.",
              "- **SSIM / MS-SSIM**: structural similarity on lightly blurred greyscale, inside the union of the masks. "
              "MS-SSIM here is a three-scale approximation (no torch, so no LPIPS).",
              "- **Tone**: how close the mean lightness and contrast inside the masks are.",
              "- **Limits**: shadows darker than 12 CIE76 units count as illustration in both images. Light objects "
              "close to the paper (a white cube or pin) can be missed by the mask; check the diagnostic image.",
              "- **Diagnostic images** `metrics-<card>.png`: panel A shows the masks (red = only real, blue = only "
              "render, grey = both); panel B the edges (same colours); panel C the SSIM difference (red = worst)."]
    with open(path, "w", encoding="utf-8") as handle:
        handle.write("\n".join(lines) + "\n")


if __name__ == "__main__":
    main()
