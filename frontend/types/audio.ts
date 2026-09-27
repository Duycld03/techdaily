export interface AudioQuotaInfo {
  monthlyLimit: number
  usedCharacters: number
  remainingCharacters: number
  isNearLimit: boolean
  isExhausted: boolean
}
