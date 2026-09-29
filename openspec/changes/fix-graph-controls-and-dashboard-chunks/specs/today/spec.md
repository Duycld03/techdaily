# Spec Delta

## ADDED Requirements

### Requirement: Authentic Learned Slices Metric in Domain Constellation Card

In the Home Bento Dashboard (`HomeBentoDashboard.vue`), the Domain Knowledge Constellation widget (`DomainConstellationCard.vue`) SHALL display the user's authentic cumulative count of learned and completed document slices (*Lát cắt đã học* / `stat_learned_chunks`), aggregated across the user's active and library document pacers (`Math.max(0, currentChunkOrder - 1)` for in-progress documents, and `totalChunks` for fully completed documents).

The dashboard SHALL NOT pass or display the total slice capacity of the largest document (e.g. `pacer.totalChunks`) as the learned slice count.

#### Scenario: Domain Constellation renders accurate learned slices count
- **WHEN** an authenticated user with active reading pacers views the Home Bento Dashboard
- **THEN** the Domain Constellation widget displays the authentic sum of completed slices across reading pacers
- **AND** the count reflects slices already read rather than the total slice count of the largest document.
