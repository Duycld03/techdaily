# Spec Delta

## MODIFIED Requirements

### Requirement: Scenario Challenge Multiple-Choice Interface & Dock Layout Density
The Scenario Challenge multiple-choice interface (`InterviewChallengePane.vue`) on `/today` SHALL style option cards, score badges, and evaluation feedback according to Dev-Learning Studio design tokens while guaranteeing complete 1080p desktop viewport visibility:
1. **Challenge Heading Density**: The question title SHALL render at `text-base sm:text-lg md:text-xl font-bold` with `leading-snug`, preventing oversized header height from pushing options below the fold.
2. **Option Button Density**: Option cards SHALL use compact padding (`p-3 sm:p-3.5`) and balanced element gap (`gap-2.5 sm:gap-3`), with option text sized at `text-sm sm:text-base` (14px–16px) with comfortable leading.
3. **Zero-Scroll Desktop Choice Invariant**: On a 1080p desktop viewport (1920x1080) in side-by-side split view with browser chrome and OS taskbars, all four multiple-choice options (A, B, C, D) alongside the primary submit action button SHALL remain fully visible in the right Scenario Dock without requiring vertical scrolling.

#### Scenario: All multiple-choice options visible on 1080p desktop
- **WHEN** a user opens `/today` on a standard 1080p screen (1920x1080) with browser chrome (tabs, URL bar, bookmarks bar) and system taskbar visible
- **THEN** all 4 option cards (A, B, C, D) and the submission button are rendered completely above the bottom fold of the Scenario Dock without requiring scrolling.

#### Scenario: Option choice text remains readable and compact
- **WHEN** reading scenario descriptions and option trade-offs in `InterviewChallengePane.vue`
- **THEN** option text renders at `text-sm sm:text-base` (14px–16px), maintaining sharp typography while preventing bloated button heights.
