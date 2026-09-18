# Spec Delta

## MODIFIED Requirements

### Requirement: Clean Library Card Presentation
The `/library` page SHALL serve as the technical document catalog, displaying books, bookmarks, and curation states adhering to the **Dev-Learning Studio** visual theme:

1. **Obsidian Studio Canvas & Glass Controls:**
   - The `/library` page container SHALL render over `dark:bg-canvas` (`#09090b` obsidian base) instead of legacy slate backgrounds.
   - Category filter pills and the search bar SHALL render using glass styling (`bg-white dark:bg-canvas-subtle border-slate-200/80 dark:border-white/[0.08]`), highlighting active category pills with Iris Violet styling (`bg-slate-100 dark:bg-canvas-elevated text-brand-600 dark:text-brand-400 border-slate-300 dark:border-white/[0.12]`).
   - The "Import Document" action button SHALL feature Iris Violet styling (`bg-brand-600 hover:bg-brand-500 text-white shadow-brand-500/20`).

2. **Glass Card Document Catalog:**
   - Document cards in the library grid SHALL render as `.glass-card` containers with translucent hairline borders (`border-white/[0.08]`), elevating on hover (`hover:border-white/[0.16]`).
   - In-progress reading progress bars SHALL use Iris Violet gradient fills (`bg-gradient-to-r from-brand-600 to-brand-500`) over subtle dark track backgrounds (`dark:bg-canvas-elevated`).
   - Bookmark resume badges SHALL display translucent brand accents (`bg-brand-500/10 text-brand-400 border border-brand-500/20`).
   - Action buttons on book cards (Resume, Read, Delete, Export) SHALL use glass styling without legacy hardcoded background colors.

3. **Import Modal Modernization:**
   - The 3-tab import modal (Markdown Series, PDF Upload, Web Crawler) SHALL render using `.glass-panel` elevation with a backdrop blur overlay (`bg-black/60 backdrop-blur-sm`).
   - Drag-and-drop PDF dropzone SHALL use subtle dark glass styling with active drag state highlighted in Iris Violet (`border-brand-500 bg-brand-500/5`).
   - Modal action buttons and confirmation dialogs SHALL adhere to Dev-Learning Studio tokens.

4. **Status & Curation Clarity:**
   - The `/library` page SHALL display book status as `Ready` without rendering an ongoing background curation progress bar once initial slices are curated.

#### Scenario: User views library card
- **WHEN** user views book list on `/library`
- **THEN** ready books display "Ready" status and clean metadata
- **AND** cards render as `.glass-card` elements over a dark canvas with hairline borders
- **AND** do not display the ongoing background curation progress bar once initial slices are ready.
