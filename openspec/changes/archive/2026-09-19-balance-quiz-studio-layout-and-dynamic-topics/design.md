# Design: Balanced Quiz Studio Layout, Dynamic Topics & Pacing

## Context

See `proposal.md` for motivation.

Following initial user feedback and visual inspection on a standard 1080p desktop display, the current Quiz Generation interface suffers from:
1. An overly narrow container (`max-w-5xl`) resulting in broad empty black borders on wide viewports.
2. A vertical height mismatch caused by a 7/5 column split with a 4-card vertical stack on the right (~530px) versus a short left column (~390px).
3. A static hardcoded array of topic chips that ignores user documentation in `/library` and career targets in `/profile`.
4. A rigid binary question count (5 vs 10) that omits a fast concept check option (3 questions).

## Goals / Non-Goals

**Goals:**
- Upgrade the container width to `max-w-6xl` to harmonize with `/library`, `/profile`, and `/roadmap`.
- Rebalance the Bento layout into an equal 50/50 grid on desktop (`grid grid-cols-1 lg:grid-cols-2 gap-6 items-stretch`).
- Restructure the 4 seniority level cards into a balanced $2 \times 2$ grid (`grid grid-cols-1 sm:grid-cols-2 gap-3`), equalizing card heights (~440px each).
- Introduce a reactive `computedQuickTopics` pipeline that prioritizes active library books and target career roles before falling back to core engineering pillars.
- Expand question count selection into a 3-tier segmented pill bar (`3`, `5`, `10` questions).
- Maintain 100% bilingual parity (EN/VI) and preserve all existing test IDs and event bindings.

**Non-Goals:**
- Modifying the Arena runner, Review queue, or Stats tabs.
- Backend API contract alterations (`POST /api/v1/quiz/generate` already accepts any count between 1 and 10).
- Adding complex autocomplete dropdowns or external AI topic generation requests.

## Decisions

### 1. Standard Studio Width (`max-w-6xl`)

**Decision**: Change the root container from `max-w-5xl mx-auto px-4 sm:px-6 py-6 space-y-6` to `max-w-6xl mx-auto px-4 sm:px-6 py-6 space-y-6`.

*Rationale*: Consistency across the TechDaily Dev-Learning Studio. `/library`, `/profile`, and `/roadmap` all employ `max-w-6xl`, which balances wide-screen desktop readability with comfortable edge margins.

### 2. Equal 50/50 Bento Grid & $2 \times 2$ Seniority Matrix

**Decision**:
- Replace `lg:grid-cols-12` (7/5 split) with `grid grid-cols-1 lg:grid-cols-2 gap-6 items-stretch`.
- Inside the Right Column, render the 4 seniority level cards in a $2 \times 2$ grid (`grid grid-cols-1 sm:grid-cols-2 gap-3`):
  - Row 1: Fresher / Entry | Junior
  - Row 2: Mid-Level | Senior / Staff
- Both the Left Container and Right Container will reach approximately ~440px height on desktop, achieving visual harmony.

*Rationale*: A $2 \times 2$ matrix cuts the vertical height of the level cards in half while still providing ample width for title, active dot, and readable scope descriptions (`text-xs sm:text-sm`).

### 3. Context-Aware Dynamic Topics Pipeline (`computedQuickTopics`)

**Decision**: Replace static `quickTopics` with a computed property that derives 6 to 8 relevant chips:
```typescript
const computedQuickTopics = computed(() => {
  const list: string[] = []
  
  // 1. Prioritize active technical books from user's library
  if (libraryStore.books && libraryStore.books.length > 0) {
    libraryStore.books.slice(0, 3).forEach(b => {
      if (b.title && !list.includes(b.title)) {
        list.push(b.title)
      }
    })
  }

  // 2. Prioritize target role specialization if set in profile
  const role = profileStore.profile?.targetRole?.toLowerCase() || ''
  if (role.includes('backend') || role.includes('.net') || role.includes('c#')) {
    list.push('.NET 10 Runtime & Memory', 'PostgreSQL MVCC & Indexing', 'Distributed Systems & Raft')
  } else if (role.includes('frontend') || role.includes('react') || role.includes('web')) {
    list.push('React 19 Concurrency', 'CSS Engine & Layout Performance', 'Web Vitals & Browser Runtime')
  } else if (role.includes('devops') || role.includes('cloud') || role.includes('infra')) {
    list.push('Kubernetes Scheduling & Pods', 'Docker Storage Drivers', 'Linux Kernel & cgroups')
  }

  // 3. Fallback to foundational senior engineering topics
  const fallbacks = [
    'PostgreSQL MVCC & Indexing',
    'Distributed Consensus & Raft',
    'Redis Caching & Lock Strategies',
    'Docker & Kubernetes Architecture',
    '.NET 10 Internals & Memory'
  ]
  for (const item of fallbacks) {
    if (list.length >= 7) break
    if (!list.includes(item)) {
      list.push(item)
    }
  }

  return list.slice(0, 7)
})
```

*Rationale*: Connects the Quiz Arena directly with the user's active reading and professional objectives without requiring backend round-trips or slow network fetches.

### 4. 3-Tier Question Count Segmented Controller (`3` | `5` | `10`)

**Decision**: Update the segmented pill container to feature 3 options:
- `3 Questions` (`quiz.count_3`): "3 Questions" / "3 Câu"
- `5 Questions` (`quiz.count_5`): "5 Questions" / "5 Câu"
- `10 Questions` (`quiz.count_10`): "10 Questions" / "10 Câu"

*Rationale*: 3 questions fulfills the need for a micro-session (<3 mins), 5 is standard (~5 mins), and 10 is deep dive (~10-12 mins). The backend validator already enforces `Math.Clamp(apiRequest.Count, 1, 10)`, so this is 100% backward-compatible.

## Component Anatomy

```
+-------------------------------------------------------------------------------------------------+
| STUDIO HEADER: [Icon] Quiz Arena Studio — AI Scenario Drill & Spaced Mastery   [Tabs Switcher]  |
+-------------------------------------------------------------------------------------------------+
| BENTO GRID 50/50 (max-w-6xl mx-auto, lg:grid-cols-2 gap-6 items-stretch)                        |
|                                                                                                 |
|  LEFT BENTO (50% Width)                         RIGHT BENTO (50% Width)                         |
|  +-------------------------------------------+  +---------------------------------------------+ |
|  | Topic Input                               |  | Seniority Level (2x2 Grid)                  | |
|  | [ Text: .NET 10 Memory, MVCC...         ] |  | +--------------------+--------------------+ | |
|  |                                           |  | | Fresher / Entry(•) | Junior         (•) | | |
|  | Suggested Topics (Dynamic & Contextual)   |  | | Core syntax, OOP   | Framework APIs     | | |
|  | [From Reading Book] [Role Topic] [Chip]... |  | +--------------------+--------------------+ | |
|  |                                           |  | | Mid-Level      (•) | Senior / Staff (•) | | |
|  | Grounded in Book Mode (Toggle)            |  | | Patterns, SQL tune | Runtime, Memory    | | |
|  | +---------------------------------------+ |  | +--------------------+--------------------+ | |
|  | | [BookOpen] Grounded in Book       [X] | |  |                                             | |
|  | | [ AppSelect: Choose document...     ] | |  | Question Count (3-Tier Segmented Pills)     | |
|  | +---------------------------------------+ |  | [ 3 Questions ] [ 5 Questions ] [ 10 Ques ] | |
|  |                                           |  |                                             | |
|  |                                           |  | [ Button: ✨ Generate Interview Quiz      ] | |
|  +-------------------------------------------+  +---------------------------------------------+ |
|  (Equal vertical height ~440px on desktop)                                                      |
+-------------------------------------------------------------------------------------------------+
```

## Risks / Trade-offs

- **Risk**: Dynamic topics could overflow if book titles are excessively long.
  - **Mitigation**: Truncate individual book title chips using CSS `max-w-[200px] truncate` and `whitespace-nowrap shrink-0`.
- **Risk**: 3 questions might produce a faster session that completes before the user settles into study flow.
  - **Mitigation**: 3 questions is explicitly labeled as Quick Drill; the default remains 5 questions (`selectedCount = ref(5)`).
