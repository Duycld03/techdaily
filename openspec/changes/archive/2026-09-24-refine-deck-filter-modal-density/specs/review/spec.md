# Spec Delta: review (Advanced Filter Modal Density)

## ADDED Requirements

### Requirement: Advanced Filter Modal Layout & Sort Options Density
The Advanced Filter Modal SHALL render all filter and sorting option groups ("Knowledge Source" / "Nguồn kiến thức", "Mastery Stage" / "Giai đoạn thành thạo", "Urgency Priority" / "Mức độ ưu tiên", and "Sort By" / "Sắp xếp theo") using unified, compact pill chips with natural content-sized boundaries and responsive auto-wrapping (`flex flex-wrap items-center gap-2`).

The modal SHALL prohibit rigid multi-column grid layouts in the sorting section that cause sort buttons to stretch across disproportionate horizontal widths. All chips across all four sections MUST share consistent border radius (`rounded-xl`), vertical and horizontal padding (`px-3.5 py-2`), and active/inactive visual states.

#### Scenario: Inspecting sort options in Advanced Filter Modal
- **WHEN** the user opens the Advanced Filter Modal in Deck Management (`/review` Tab 2)
- **THEN** the four sort options ("Ngày ôn gần nhất", "Ngày ôn xa nhất", "Thẻ khó nhất trước", "Mới tạo gần đây") MUST render as individual, content-sized pill chips aligned with the chip architecture of the preceding filter sections
- **AND** MUST NOT stretch into elongated horizontal bars spanning half the modal width.

#### Scenario: Selecting and applying sort options in Advanced Filter Modal
- **WHEN** the user clicks any sort option chip
- **THEN** the clicked chip MUST immediately transition to active state (`bg-brand-600 text-white font-bold border-transparent shadow-sm`) while previously selected sort chips return to inactive state
- **AND** clicking "Áp dụng bộ lọc" MUST dispatch the active sort criteria to update deck cards.
