import type { CSSProperties } from 'react';

export interface AttentionColorOptions {
  /** The category or column hue, expressed as a CSS hue angle. */
  hue: number;
  /** Semantic urgency on a continuous 0–10 scale. */
  intensity: number;
}

export interface AttentionColorResult {
  /** The mixed category-to-danger color used for markers and labels. */
  accent: string;
  /** Percentage of the surface traversed by the gradient. */
  coverage: number;
  /** Clamped 0–10 intensity. */
  intensity: number;
  /** Percentage of red in the accent color. */
  redMix: number;
  /** Ready-to-apply surface styles. */
  style: CSSProperties;
}

function clamp(value: number, min: number, max: number): number {
  return Math.min(max, Math.max(min, value));
}

/**
 * Builds a calm-to-urgent surface treatment without erasing category identity.
 * An intensity of 2 mixes 80% category hue with 20% red; an intensity of 8
 * mixes 20% category hue with 80% red. Gradient coverage grows continuously.
 */
export function createAttentionColor({ hue, intensity: rawIntensity }: AttentionColorOptions): AttentionColorResult {
  const normalizedHue = ((Math.round(Number.isFinite(hue) ? hue : 210) % 360) + 360) % 360;
  const intensity = clamp(Number.isFinite(rawIntensity) ? rawIntensity : 0, 0, 10);
  const redMix = Math.round(intensity * 10);
  const categoryMix = 100 - redMix;
  const coverage = 24 + intensity * 7.2;
  const colorStrength = Math.round(10 + intensity * 6.2);
  const accent = `color-mix(in oklab, hsl(${normalizedHue} 66% 52%) ${categoryMix}%, hsl(6 78% 56%) ${redMix}%)`;

  return {
    accent,
    coverage,
    intensity,
    redMix,
    style: {
      background: `linear-gradient(45deg, color-mix(in oklab, ${accent} ${colorStrength}%, rgba(17, 30, 41, 0.94)) 0%, color-mix(in oklab, ${accent} ${Math.round(colorStrength * 0.66)}%, rgba(17, 30, 41, 0.94)) ${(coverage * 0.58).toFixed(1)}%, rgba(17, 30, 41, 0.94) ${coverage.toFixed(1)}%)`,
      borderColor: `color-mix(in oklab, ${accent} ${Math.round(12 + intensity * 2.4)}%, rgba(255, 255, 255, 0.06))`
    }
  };
}
