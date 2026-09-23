# Spec Delta

## MODIFIED Requirements

### Requirement: Document Library Grid & Card Layout Density
The Document Library interface (`pages/library.vue`) SHALL present technical publications with compact, balanced card geometry and responsive grid spacing:
1. **Document Card Dimensions & Padding**: Individual book cards SHALL render with compact padding (`p-4 sm:p-5 rounded-2xl`) and balanced internal vertical spacing (`space-y-3`), eliminating excessive padding (`p-6 sm:p-7 rounded-3xl`).
2. **Grid Spacing**: The responsive book grid SHALL use a balanced gap (`gap-4 sm:gap-5`), preserving clean multi-column layouts across desktop screen sizes without unnecessary vertical stretching.
3. **Card Content Proportions**: Book title text (`text-base sm:text-lg font-bold line-clamp-2`), status indicators, and action buttons (`Continue Reading`, `Delete`, `Export`) SHALL fit compactly within each card.

#### Scenario: Library cards display balanced whitespace
- **WHEN** browsing technical publications on the `/library` route
- **THEN** book cards display compact vertical height and clean padding, allowing more cards to remain visible above the fold on desktop screens.
