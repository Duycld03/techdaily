# Tasks: Review Completion Card Generous Padding

## 1. Template & Styling Enhancements

- [X] 1.1 In `frontend/pages/review.vue`, expand the completion card container's maximum width constraint from `max-w-md` to `max-w-xl`.
- [X] 1.2 In `frontend/pages/review.vue`, replace compact padding `p-8 sm:p-10` with generous multi-tier padding `p-10 sm:p-12 md:p-14`.
- [X] 1.3 In `frontend/pages/review.vue`, relax vertical stack spacing from `space-y-4` to `space-y-6 sm:space-y-7` to provide distinct separation between icon, heading, copy, and action buttons.
- [X] 1.4 In `frontend/pages/review.vue`, adjust the action CTA container offset and spacing from `pt-2 gap-3` to `pt-4 sm:pt-6 gap-3.5 sm:gap-4`.
- [X] 1.5 In `frontend/pages/review.vue`, verify that responsive button stacking (`flex-col sm:flex-row`), action icons (`Library`, `Sparkles`), and reactive translations render without visual regression.

## 2. Unit & Integration Test Updates

- [X] 2.1 In `frontend/tests/pages/review.spec.ts`, update the completion state test case to assert the presence of expanded width class `max-w-xl` and generous padding classes (`p-10`, `sm:p-12`, `md:p-14`) on the completion card container.
- [X] 2.2 In `frontend/tests/pages/review.spec.ts`, assert that the completion card container contains relaxed vertical spacing class `space-y-6` (or `space-y-6 sm:space-y-7`).
- [X] 2.3 In `frontend/tests/pages/review.spec.ts`, assert that the CTA action row container has classes `pt-4 sm:pt-6` and `gap-3.5 sm:gap-4`.
- [X] 2.4 In `frontend/tests/pages/review.spec.ts`, verify that primary CTA click navigation to deck management (`activeTab = 'management'`) and deck queries continue to pass cleanly.

## 3. Validation & Visual Quality Assurance

- [X] 3.1 Execute `npm --prefix frontend test` to verify all frontend unit and integration tests pass without failures.
- [X] 3.2 Visually verify the review completion card across mobile (375px/390px), tablet (768px), and desktop (1280px+) viewports in both light and dark themes.
- [X] 3.3 Run `openspec validate --strict review-completion-card-generous-padding` and confirm 0 errors.
