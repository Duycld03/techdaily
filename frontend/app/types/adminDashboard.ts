export interface ProcessingCounts {
  total: number
  pending: number
  processing: number
  ready: number
  failed: number
}

export interface AdminOverview {
  accountCount: number
  personalDocuments: ProcessingCounts
  catalogDocuments: {
    total: number
    draft: number
    published: number
    hidden: number
    processing: ProcessingCounts
  }
  observedAt: string
}

export interface ServiceObservation {
  status: 'healthy' | 'unhealthy' | 'unavailable'
  checkedAt: string
  latencyMs: number | null
  errorCode?: string | null
}

export interface AiObservation extends ServiceObservation {
  model: string | null
  dimension?: number | null
}

export interface AdminStatus {
  database: ServiceObservation
  observedAt: string
}

export interface AdminAiProbe {
  textGeneration: AiObservation
  embeddings: AiObservation
  checkedAt: string
  isHealthy: boolean
}
