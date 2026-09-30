# Spec Delta

## ADDED Requirements

### Requirement: Differentiated Dashboard Practice Navigation Targets
The Home Bento Dashboard SHALL provide distinct, non-redundant action targets for practice modalities:
1. The Daily Micro-Drill card (`Trắc Nghiệm Phản Xạ`) SHALL navigate to `/today?tab=challenge`, ensuring that on both desktop and mobile viewports, the challenge dock/tab is immediately activated and presented for practice evaluation.
2. The Senior Dilemma card (`Tình Huống Senior`) SHALL navigate to `/quiz` (Architecture Interview Arena), allowing the user to engage with senior-level architectural dilemma questions without redundant duplication of the daily drill session.

#### Scenario: User clicks Start Daily Micro-Drill
- **WHEN** the user clicks the action button on the Daily Micro-Drill card
- **THEN** the router navigates to `/today?tab=challenge` and automatically focuses the evaluation challenge pane

#### Scenario: User clicks Senior Architecture Dilemma CTA
- **WHEN** the user clicks the "Quyết Định Kiến Trúc" action button on the Senior Dilemma card
- **THEN** the router navigates to `/quiz` (Architecture Interview Arena)
