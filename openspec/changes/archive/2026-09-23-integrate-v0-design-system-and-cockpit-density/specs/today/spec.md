# Spec Delta: today

## MODIFIED Requirements

### Requirement: Scenario Challenge Multiple-Choice Interface & Dock Layout Density
The Scenario Challenge multiple-choice interface (`InterviewChallengePane.vue`) on `/today` SHALL style option cards, score badges, and evaluation feedback according to the standardized `OptionCard` design primitive while guaranteeing complete 1080p desktop viewport visibility:
1. **Compact Option Primitive**: Option cards SHALL use `OptionCard.vue` with `px-3 py-2.5 rounded-lg border text-sm` (~44px height), letter badge `h-7 w-7 text-xs font-bold`, and 4 discrete states (`default`, `selected`, `correct`, `incorrect`).
2. **Dock Container Spacing**: The right Scenario Dock container padding SHALL standardize to `p-3.5 sm:p-4 md:p-5` (eliminating `md:p-8`), and internal section spacing SHALL standardize to `space-y-3 sm:space-y-4`.
3. **Zero-Scroll Desktop Choice Invariant**: On a 1080p desktop viewport (1920x1080) in side-by-side split view with browser chrome and OS taskbars, all four multiple-choice options (A, B, C, D) alongside the primary submit action button SHALL remain fully visible in the right Scenario Dock without requiring vertical scrolling.

#### Scenario: All multiple-choice options visible on 1080p desktop
- **WHEN** a user opens `/today` on a standard 1080p screen (1920x1080) with browser chrome (tabs, URL bar, bookmarks bar) and system taskbar visible
- **THEN** all 4 option cards (A, B, C, D) and the submission button are rendered completely above the bottom fold of the Scenario Dock without requiring scrolling
- **AND** option cards render at approximately 44px height with `px-3 py-2.5 rounded-lg`.
