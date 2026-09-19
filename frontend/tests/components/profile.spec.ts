import { describe, it, expect } from 'vitest'
import { mount } from '@vue/test-utils'
import EngineerIdentityPassport from '~/components/profile/EngineerIdentityPassport.vue'
import EngineerMilestonesCard from '~/components/profile/EngineerMilestonesCard.vue'
import DomainGoalTracker from '~/components/profile/DomainGoalTracker.vue'
import type { UserProfile, UserLearningStats } from '~/stores/useProfileStore'
import type { QuizStats } from '~/stores/useInterviewQuizStore'

describe('EngineerIdentityPassport.vue', () => {
  const mockProfile: UserProfile = {
    id: 'user-123',
    email: 'architect@techdaily.dev',
    name: 'Alice Architect',
    avatarUrl: 'https://example.com/avatar.jpg',
    preferredLocale: 'en',
    targetRole: 'Principal Architect',
    dailyGoalMinutes: 15,
    hasPassword: true,
    isGoogleLinked: true
  }

  const mockStats: UserLearningStats = {
    currentStreak: 12,
    longestStreak: 25,
    freezeCreditsRemaining: 3,
    totalDrillsCompleted: 48,
    averageScore: 9.2,
    totalCardsInDeck: 120,
    totalHighlightsSaved: 35,
    memberSince: '2026-01-01'
  }

  it('renders user details, badges, and tenure with full data', () => {
    const wrapper = mount(EngineerIdentityPassport, {
      props: {
        profile: mockProfile,
        stats: mockStats
      }
    })

    // Identity assertions
    expect(wrapper.text()).toContain('Alice Architect')
    expect(wrapper.text()).toContain('architect@techdaily.dev')
    expect(wrapper.text()).toContain('Principal Architect')
    expect(wrapper.text()).toContain('profile.google_linked')
    expect(wrapper.text()).toContain('profile.longest_streak_record')
    expect(wrapper.text()).toContain('profile.member_since')
    // Check avatar image
    const img = wrapper.find('img')
    expect(img.exists()).toBe(true)
    expect(img.attributes('src')).toBe('https://example.com/avatar.jpg')
  })

  it('renders fallback monogram and standard email account when no avatar and not Google-linked', () => {
    const standardProfile: UserProfile = {
      ...mockProfile,
      name: 'Bob Builder',
      avatarUrl: undefined,
      isGoogleLinked: false
    }

    const wrapper = mount(EngineerIdentityPassport, {
      props: {
        profile: standardProfile,
        stats: mockStats
      }
    })

    expect(wrapper.find('img').exists()).toBe(false)
    expect(wrapper.text()).toContain('B') // Initial
    expect(wrapper.text()).toContain('profile.standard_account')
  })

  it('handles null props gracefully without crashing', () => {
    const wrapper = mount(EngineerIdentityPassport, {
      props: {
        profile: null,
        stats: null
      }
    })

    expect(wrapper.text()).toContain('Engineer')
    expect(wrapper.text()).toContain('U')
  })
})

describe('EngineerMilestonesCard.vue', () => {
  const mockStats: UserLearningStats = {
    currentStreak: 12,
    longestStreak: 25,
    freezeCreditsRemaining: 3,
    totalDrillsCompleted: 48,
    averageScore: 9.2,
    totalCardsInDeck: 120,
    totalHighlightsSaved: 35,
    memberSince: '2026-01-01'
  }

  const mockQuizStats: QuizStats = {
    totalAnswered: 50,
    masteredCount: 42,
    reviewQueueCount: 8,
    accuracyRate: 84,
    levelBreakdown: [],
    topicBreakdown: []
  }

  it('renders 4 cumulative milestone cells with full data', () => {
    const wrapper = mount(EngineerMilestonesCard, {
      props: {
        stats: mockStats,
        quizStats: mockQuizStats
      }
    })

    // Milestones Bento assertions
    expect(wrapper.text()).toContain('profile.milestones_title')
    expect(wrapper.text()).toContain('48') // Drills completed
    expect(wrapper.text()).toContain('9.2/10') // Average score
    expect(wrapper.text()).toContain('84%') // Quiz accuracy
    expect(wrapper.text()).toContain('120') // SM-2 Memory Vault concepts
    expect(wrapper.text()).toContain('35') // Highlights saved
    expect(wrapper.text()).toContain('profile.memory_vault')
    expect(wrapper.text()).toContain('profile.highlights_vault')
  })

  it('handles null props gracefully without crashing', () => {
    const wrapper = mount(EngineerMilestonesCard, {
      props: {
        stats: null,
        quizStats: null
      }
    })

    expect(wrapper.text()).toContain('profile.milestones_title')
    expect(wrapper.text()).toContain('0')
  })
})

describe('DomainGoalTracker.vue', () => {
  it('renders all 4 curriculum technical pillars with defaults when no data passed', () => {
    const wrapper = mount(DomainGoalTracker, {
      props: {
        topicBreakdown: {}
      }
    })

    // Titles or title keys
    expect(wrapper.text()).toContain('profile.domain_backend_runtime')
    expect(wrapper.text()).toContain('profile.domain_data_storage')
    expect(wrapper.text()).toContain('profile.domain_system_design')
    expect(wrapper.text()).toContain('profile.domain_frontend')
    // 0% percentages for all 4
    const text = wrapper.text()
    const matches = text.match(/0%/g)
    expect(matches).not.toBeNull()
    expect(matches!.length).toBeGreaterThanOrEqual(4)
  })

  it('calculates progress accurately from object-based topicBreakdown', () => {
    const objectBreakdown = {
      'Category.BackendDotNet': { total: 10, correct: 8 },
      'PostgreSQL Storage': { total: 10, correct: 5 },
      'Distributed Systems': { total: 10, correct: 9 },
      'Frontend Browser': { total: 10, correct: 6 }
    }

    const wrapper = mount(DomainGoalTracker, {
      props: {
        topicBreakdown: objectBreakdown
      }
    })

    expect(wrapper.text()).toContain('80%') // Backend .NET: 8 / 10
    expect(wrapper.text()).toContain('50%') // Postgres: 5 / 10
    expect(wrapper.text()).toContain('90%') // System Design: 9 / 10
    expect(wrapper.text()).toContain('60%') // Frontend: 6 / 10
  })

  it('calculates progress accurately from array-based topicBreakdown (Quiz TopicStat[])', () => {
    const arrayBreakdown = [
      { topic: '.NET GC & Concurrency', answeredCount: 8, masteredCount: 6, accuracyRate: 75 },
      { topic: 'PostgreSQL WAL & Indexes', answeredCount: 7, masteredCount: 7, accuracyRate: 100 },
      { topic: 'Distributed Outbox', answeredCount: 8, masteredCount: 4, accuracyRate: 50 },
      { topic: 'Vue 3 Reactivity & Browser', answeredCount: 7, masteredCount: 3, accuracyRate: 42.8 }
    ]

    const wrapper = mount(DomainGoalTracker, {
      props: {
        topicBreakdown: arrayBreakdown
      }
    })

    expect(wrapper.text()).toContain('75%')  // 6 / 8 = 75%
    expect(wrapper.text()).toContain('100%') // 7 / 7 = 100%
    expect(wrapper.text()).toContain('50%')  // 4 / 8 = 50%
    expect(wrapper.text()).toContain('43%')  // 3 / 7 = 43%
  })

  it('calculates progress accurately for multi-stack keywords (NestJS, Mongo, Kafka, React)', () => {
    const multiStackBreakdown = [
      { topic: 'NestJS Event Loop & V8', answeredCount: 10, masteredCount: 8 },
      { topic: 'MongoDB Indexes & Transactions', answeredCount: 10, masteredCount: 7 },
      { topic: 'Kafka Event-Driven Architecture', answeredCount: 10, masteredCount: 9 },
      { topic: 'React Server Components & DOM', answeredCount: 10, masteredCount: 6 }
    ]

    const wrapper = mount(DomainGoalTracker, {
      props: {
        topicBreakdown: multiStackBreakdown
      }
    })

    expect(wrapper.text()).toContain('80%') // Backend Runtime (Nest): 8 / 10
    expect(wrapper.text()).toContain('70%') // Data Storage (Mongo): 7 / 10
    expect(wrapper.text()).toContain('90%') // Distributed Systems (Kafka): 9 / 10
    expect(wrapper.text()).toContain('60%') // Frontend (React): 6 / 10
  })
})
