# Proposal: Multiple-Choice Senior Scenario Interview Drills

## 1. Why (Problem & Motivation)
The current Daily Focus Hub (`/today`) features an open-ended essay and voice recording editor for Senior interview questions. While comprehensive, this poses several friction points for daily learners:
1. **Time & Energy Barrier:** Busy engineers completing their daily 5-minute morning micro-learning session often skip long free-text essay typing or voice recordings, breaking their daily learning habit.
2. **Ambiguous Evaluation:** Free-text LLM evaluations can occasionally be noisy or slow (3–5 seconds), whereas clear multiple-choice questions evaluating precise architectural trade-offs provide instant, deterministic, and unambiguous feedback.
3. **Focused Senior Trade-off Assessment:** Multiple-choice scenario questions (with plausible senior-level distractor options covering common anti-patterns, naive solutions, and suboptimal architectures) test deep technical decision-making and nuance more effectively in short intervals.

Transitioning/enhancing the daily interview challenge into **Multiple-Choice Senior Scenario Drills** allows engineers to quickly evaluate complex architectural trade-offs, receive instant verification with detailed senior-level explanations, and maintain consistent daily streaks.

---

## 2. What (Scope & Deliverables)
Transform the Daily Interview Challenge into a scenario-based multiple-choice format across the entire stack:
- **Domain & Schema Update:**
  - Update `InterviewQuestion` to support multiple options (`Options: List<string>`), the designated correct option (`CorrectOptionIndex: int`), and comprehensive trade-off rationale (`ExplanationMarkdown: string`).
  - Update `DailyDrill` to track the user's selected choice (`SelectedOptionIndex: int?`), correctness (`IsCorrect: bool`), and instant score.
- **Application & Submission Flow:**
  - Update `SubmitDailyDrillHandler` to accept `selectedOptionIndex`, validate correctness against the question entity, assign an instant score (10/10 for correct, 0/10 for incorrect), increment streaks, and automatically schedule SM-2 Spaced Repetition review cards for missed questions.
- **Frontend UI (`InterviewChallengePane.vue`):**
  - Replace the text/voice editor with a scenario multiple-choice interface:
    - Display scenario question and 4 interactive choice cards (A, B, C, D) with distinct hover, focus, and selection states.
    - "Submit Answer" button with confirmation check.
    - Post-submission state displaying immediate Correct/Incorrect feedback badges, highlighting the correct option, and presenting the deep-dive architectural trade-off explanation.
    - Confetti celebration upon correct submission.
- **Internationalization (i18n):**
  - Update English (`en.json`) and Vietnamese (`vi.json`) translations for all multiple-choice drill UI elements.
- **Automated Tests:**
  - Unit tests for `SubmitDailyDrillHandler` validating correct and incorrect option submissions.
  - Component tests for `InterviewChallengePane.vue`.

---

## 3. Impact & Non-Goals
- **Impact:** Greatly reduces friction in the daily learning loop, increases daily active drill completions, and guarantees deterministic grading and instant feedback.
- **Non-Goals:** Building a timed standardized exam engine or deprecating Spaced Repetition (SM-2) review decks.
