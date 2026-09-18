# Design

## Context

See `proposal.md` for background and motivation.

Current state:
1. `HomeBentoDashboard.vue` hardcodes `/ 30` in the welcome banner badge:
   ```vue
   {{ $t('dashboard.curriculum_day') }} {{ topic?.dayOrder || 1 }} / 30
   ```
   And `AppHeader.vue` displays:
   ```vue
   Day {{ focusStore.data.topic.dayOrder }} / 30
   ```
   This breaks the platform invariant that TechDaily is tech-agnostic and documentation-agnostic, supporting any book with arbitrary slice counts (e.g. 23 slices for ASP.NET Core documentation).
2. In `DomainConstellationCard.vue`:
   ```vue
   <circle v-if="n.pulse" class="animate-ping origin-center" ... />
   ```
   SVG viewports calculate CSS `transform: scale()` against the SVG canvas root `(0, 0)` rather than the SVG node's `(cx, cy)` center, blowing up into an off-center bubble animation. Additionally, `<NuxtLink class="... group-hover:translate-x-0.5">` shifts the "Mở Vũ Trụ 3D" header text horizontally whenever the mouse enters the card.
3. Both action buttons in `HomeBentoDashboard.vue` currently route to `/today`:
   ```ts
   function handleStartReading() { navigateTo('/today') }
   function handleStartScenario() { navigateTo('/today') }
   ```

## Goals / Non-Goals

**Goals:**
- Dynamically format slice badges using real Pacer bounds (`Slice {current} / {total}` / `Lát cắt {current} / {total}`) with graceful fallback when no Pacer is active.
- Fix SVG constellation animation to pulse in place without bubble scaling artifacts, and eliminate text jitter on card hover.
- Differentiate button routing: "Continue Reading" opens the GitBook reader at the active slice (`/read/[bookId]?slice=[order]`), while "Solve Challenge" opens the Focus Studio scenario challenge (`/today`).

**Non-Goals:**
- Altering the backend API contract or database schemas.
- Changing reading controls inside the reader itself.

## Decisions

### 1. Dynamic Slice Progress Formatting
- In `HomeBentoDashboard.vue`, create a computed helper `sliceBadgeText`:
  ```ts
  const sliceBadgeText = computed(() => {
    if (pacer.value && pacer.value.totalChunks > 0) {
      return `${t('dashboard.active_slice_badge', { current: pacer.value.currentChunkOrder, total: pacer.value.totalChunks })}`
    }
    return `${t('dashboard.curriculum_day')} ${topic.value?.dayOrder || 1}`
  })
  ```
- In `i18n`:
  - `en.json`: `"active_slice_badge": "Slice {current} / {total}"`
  - `vi.json`: `"active_slice_badge": "Lát cắt {current} / {total}"`
- In `AppHeader.vue`, display `${pacer.currentChunkOrder} / ${pacer.totalChunks}` when pacer data exists, or `Day ${topic.dayOrder}` as a fallback without `/ 30`.

### 2. SVG Constellation Stabilization
- In `DomainConstellationCard.vue`:
  - Replace `animate-ping origin-center` with a stationary SVG aura ring:
    ```vue
    <circle
      v-if="n.pulse"
      :cx="n.x"
      :cy="n.y"
      r="8"
      :stroke="n.color"
      stroke-width="1"
      fill="none"
      opacity="0.5"
      class="animate-pulse"
    />
    ```
    This modulates opacity smoothly without coordinate scaling.
  - Remove `group-hover:translate-x-0.5` from the `<NuxtLink>` title.

### 3. Differentiated Action Navigation
- Update `handleStartReading`:
  ```ts
  function handleStartReading() {
    emit('startReading')
    if (pacer.value?.bookId && pacer.value?.currentChunkOrder) {
      navigateTo({
        path: `/read/${pacer.value.bookId}`,
        query: { slice: pacer.value.currentChunkOrder.toString() }
      })
    } else {
      navigateTo('/today')
    }
  }
  ```
- Update `handleStartScenario`:
  ```ts
  function handleStartScenario() {
    emit('startScenario')
    navigateTo('/today')
  }
  ```

## Risks / Trade-offs

- **Risk**: User clicks "Continue Reading" when `pacer.bookId` is not yet loaded.
  - *Mitigation*: Fall back safely to `/today`.
- **Risk**: Translation key missing in existing test stubs.
  - *Mitigation*: Fall back to template interpolation if `$t` is unmocked.
