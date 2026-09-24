# Technical Design: Stepper & Direct-Input Architecture for AppTimePicker

## Context

`frontend/components/common/AppTimePicker.vue` previously used a 3-column scrollable well (Hour, Minute, Period) with overflow tracks. While compact, scroll wheels can feel clumsy for quick adjustments on desktop and laptop trackpads, and restrict users from freely typing custom minutes.

## Goals / Non-Goals

**Goals:**
- Replace scroll wheels with 3 digital stepper segments (Hour, Minute, Period).
- Support direct keyboard typing with instant or on-blur validation ($1\le h \le 12$, $0 \le m \le 59$).
- Support increment/decrement via Up/Down buttons and keyboard Arrow keys.
- Include quick preset chips (`08:00 AM`, `12:00 PM`, `08:00 PM`, `10:00 PM`).
- Eliminate all scrollbars and reduce total popover height to $\sim 175\text{px}$.

**Non-Goals:**
- Removing the popover pattern altogether (it remains a floating dialog for consistency).
- Breaking existing 24-hour model string format (`HH:mm`).

## Component Layout Architecture

```
+------------------------------------------------+
| (clock) Select Time                  08:00 AM  |
+------------------------------------------------+
|      [ ▲ ]          [ ▲ ]          [ ▲ ]       |
|   +---------+    +---------+    +---------+    |
|   |   08    |  : |   00    |    |   AM    |    |
|   +---------+    +---------+    +---------+    |
|      [ ▼ ]          [ ▼ ]          [ ▼ ]       |
|      HOUR           MINUTE         PERIOD      |
+------------------------------------------------+
|  [ 08:00 AM ]  [ 12:00 PM ]  [ 08:00 PM ]      |
+------------------------------------------------+
|                    [ Done ]                    |
+------------------------------------------------+
```

## Key Decisions

1. **Direct Input Binding**:
   - Local state `tempHour` and `tempMinute` synced with `parsedTime`.
   - On change/blur, sanitize and clamp: `Math.min(12, Math.max(1, parseInt(val) || 12))`.
2. **Stepper Logic**:
   - `incrementHour()`: `(currentHour12 % 12) + 1`
   - `decrementHour()`: `currentHour12 === 1 ? 12 : currentHour12 - 1`
   - `incrementMinute(step = 5)`: `(Math.floor(currentMinute / 5) * 5 + 5) % 60`
   - `decrementMinute(step = 5)`: `(Math.floor(currentMinute / 5) * 5 - 5 + 60) % 60`
   - `togglePeriod()`: `currentPeriod === 'AM' ? 'PM' : 'AM'`
3. **Accessibility**:
   - Inputs have `aria-label="Hour"` and `aria-label="Minute"`.
   - Steppers have `aria-label="Increase Hour"`, etc.
   - ArrowUp and ArrowDown mapped on `@keydown.up.prevent` and `@keydown.down.prevent`.

## Risks / Trade-offs

- **Risk**: Typing invalid characters (letters).
  - **Mitigation**: Filter input to numeric-only digits on `@input` (`e.target.value.replace(/\D/g, '')`) and clamp on blur.
