<script setup lang="ts">
import { computed } from 'vue'

const props = withDefaults(
  defineProps<{
    size?: 'xs' | 'sm' | 'md' | 'lg' | 'xl' | number
    ariaLabel?: string
    idPrefix?: string
  }>(),
  {
    size: 'md',
    ariaLabel: 'TechDaily',
    idPrefix: 'td-logo'
  }
)

const dimension = computed(() => {
  if (typeof props.size === 'number') {
    return `${props.size}px`
  }
  switch (props.size) {
    case 'xs':
      return '20px'
    case 'sm':
      return '28px'
    case 'md':
      return '32px'
    case 'lg':
      return '40px'
    case 'xl':
      return '48px'
    default:
      return '32px'
  }
})

// Scoped gradient and filter IDs to prevent DOM duplicate collisions
const bgGradId = computed(() => `${props.idPrefix}-bgGrad`)
const squircleBorderId = computed(() => `${props.idPrefix}-squircleBorder`)
const symmHexGradId = computed(() => `${props.idPrefix}-symmHexGrad`)
const leftBracketGradId = computed(() => `${props.idPrefix}-leftBracketGrad`)
const rightBracketGradId = computed(() => `${props.idPrefix}-rightBracketGrad`)
const centerGlowId = computed(() => `${props.idPrefix}-centerGlow`)
const nodeGlowId = computed(() => `${props.idPrefix}-nodeGlow`)
const softGlowId = computed(() => `${props.idPrefix}-softGlow`)
</script>

<template>
  <svg
    xmlns="http://www.w3.org/2000/svg"
    viewBox="0 0 512 512"
    :width="dimension"
    :height="dimension"
    :style="{ width: dimension, height: dimension }"
    :aria-label="ariaLabel"
    role="img"
    class="shrink-0 select-none transition-transform duration-200"
  >
    <defs>
      <!-- Background Gradient -->
      <linearGradient :id="bgGradId" x1="0%" y1="0%" x2="100%" y2="100%">
        <stop offset="0%" stop-color="#151419" />
        <stop offset="50%" stop-color="#0f0f13" />
        <stop offset="100%" stop-color="#08080a" />
      </linearGradient>

      <!-- Border Stroke Gradient -->
      <linearGradient :id="squircleBorderId" x1="0%" y1="0%" x2="100%" y2="100%">
        <stop offset="0%" stop-color="rgba(167, 139, 250, 0.4)" />
        <stop offset="50%" stop-color="rgba(124, 58, 237, 0.15)" />
        <stop offset="100%" stop-color="rgba(255, 255, 255, 0.05)" />
      </linearGradient>

      <!-- Perfectly Symmetrical Hexagon Gradient (Vertical) -->
      <linearGradient :id="symmHexGradId" x1="0%" y1="0%" x2="0%" y2="100%">
        <stop offset="0%" stop-color="#c4b5fd" />
        <stop offset="35%" stop-color="#8b5cf6" />
        <stop offset="70%" stop-color="#7c3aed" />
        <stop offset="100%" stop-color="#5b21b6" />
      </linearGradient>

      <!-- Left Bracket Gradient (Electric Violet-Cyan balance) -->
      <linearGradient :id="leftBracketGradId" x1="0%" y1="0%" x2="100%" y2="100%">
        <stop offset="0%" stop-color="#a78bfa" />
        <stop offset="100%" stop-color="#7c3aed" />
      </linearGradient>

      <!-- Right Bracket Gradient (Symmetrical Electric Violet-Cyan balance) -->
      <linearGradient :id="rightBracketGradId" x1="100%" y1="0%" x2="0%" y2="100%">
        <stop offset="0%" stop-color="#a78bfa" />
        <stop offset="100%" stop-color="#7c3aed" />
      </linearGradient>

      <!-- Central Symmetrical Radial Core Glow -->
      <radialGradient :id="centerGlowId" cx="50%" cy="50%" r="50%">
        <stop offset="0%" stop-color="#7c3aed" stop-opacity="0.35" />
        <stop offset="60%" stop-color="#7c3aed" stop-opacity="0.1" />
        <stop offset="100%" stop-color="#7c3aed" stop-opacity="0" />
      </radialGradient>

      <radialGradient :id="nodeGlowId" cx="50%" cy="50%" r="50%">
        <stop offset="0%" stop-color="#ffffff" />
        <stop offset="40%" stop-color="#c4b5fd" />
        <stop offset="100%" stop-color="#7c3aed" />
      </radialGradient>

      <!-- Glow Filter -->
      <filter :id="softGlowId" x="-30%" y="-30%" width="160%" height="160%">
        <feGaussianBlur stdDeviation="20" result="blur" />
        <feComposite in="SourceGraphic" in2="blur" operator="over" />
      </filter>
    </defs>

    <!-- Container Squircle Canvas -->
    <rect
      x="24"
      y="24"
      width="464"
      height="464"
      rx="104"
      :fill="`url(#${bgGradId})`"
      :stroke="`url(#${squircleBorderId})`"
      stroke-width="3"
    />

    <!-- Center Ambient Radial Glow (Pure radial symmetry) -->
    <circle cx="256" cy="256" r="140" :fill="`url(#${centerGlowId})`" />

    <!-- Subtle Inner Dark Disc -->
    <circle cx="256" cy="256" r="132" fill="#131218" opacity="0.65" />

    <!-- Symmetrical Regular Hexagon Outer Ring -->
    <polygon
      points="256,100 391,178 391,334 256,412 121,334 121,178"
      fill="none"
      :stroke="`url(#${symmHexGradId})`"
      stroke-width="18"
      stroke-linecap="round"
      stroke-linejoin="round"
    />

    <!-- Inner Concentric Hairline Hexagon for Precision / Architecture aesthetic -->
    <polygon
      points="256,128 367,192 367,320 256,384 145,320 145,192"
      fill="none"
      stroke="rgba(196, 181, 253, 0.25)"
      stroke-width="2"
      stroke-linejoin="round"
    />

    <!-- Perfectly Symmetrical Left Code Bracket < -->
    <path
      d="M 224 204 L 172 256 L 224 308"
      fill="none"
      stroke="#ffffff"
      stroke-width="22"
      stroke-linecap="round"
      stroke-linejoin="round"
    />

    <!-- Perfectly Symmetrical Right Code Bracket > -->
    <path
      d="M 288 204 L 340 256 L 288 308"
      fill="none"
      stroke="#ffffff"
      stroke-width="22"
      stroke-linecap="round"
      stroke-linejoin="round"
    />

    <!-- Central Symmetric Core Node (Memory & System Nucleus) -->
    <circle cx="256" cy="256" r="22" fill="#7c3aed" opacity="0.45" :filter="`url(#${softGlowId})`" />
    <circle cx="256" cy="256" r="14" :fill="`url(#${symmHexGradId})`" />
    <circle cx="256" cy="256" r="7" fill="#ffffff" />

    <!-- Subtle Symmetrical Balance Accent Dots (Top & Bottom Vertical Axis) -->
    <circle cx="256" cy="156" r="4.5" fill="#a78bfa" opacity="0.8" />
    <circle cx="256" cy="356" r="4.5" fill="#a78bfa" opacity="0.8" />
  </svg>
</template>
