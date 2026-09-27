<script setup lang="ts">
import ReaderAudioPlayer from '~/components/reader/ReaderAudioPlayer.vue'
import type { ChunkSummary } from '~/stores/useLibraryStore'

// Isolated visual harness for the reader audio narration control bar.
// Exercises the AI-formatted (EN + VI voice) and not-AI-formatted states, plus a
// locale toggle so the bilingual button/label layout can be inspected.
const { locale, setLocale } = useI18n()

const base: ChunkSummary = {
  id: 'demo-en',
  chunkOrder: 4,
  chapterTitle: 'Dependency Injection in .NET 10',
  summaryMarkdown: '',
  originalTextMarkdown: 'Dependency injection decouples construction from use. First sentence. Second sentence.',
  keyTakeaways: [],
  estimatedReadMinutes: 4,
  isAiFormatted: true,
  language: 'en'
}
const enChunk = base
const longViMarkdown = [
  'Kiến trúc phần mềm phân tán đòi hỏi sự cân bằng giữa hiệu năng và độ tin cậy.',
  'Mỗi dịch vụ cần có ranh giới nghiệp vụ rõ ràng để giảm thiểu phụ thuộc.',
  'Hàng đợi thông điệp giúp phân tách các thành phần xử lý không đồng bộ.',
  'Bộ nhớ đệm phân tán giảm tải đáng kể cho tầng cơ sở dữ liệu quan hệ.',
  'Chiến lược ghi dữ liệu cần tính đến tính nhất quán cuối cùng giữa các nút mạng.',
  'Việc giám sát hệ thống theo thời gian thực giúp phát hiện sự cố sớm nhất.',
  'Nhật ký tập trung và truy vết phân tán là chìa khóa để chẩn đoán lỗi phức tạp.',
  'Kiểm thử tích hợp tự động đảm bảo các luồng giao tiếp giữa các dịch vụ ổn định.',
  'Bảo mật theo chiều sâu cần áp dụng xác thực mã thông báo và mã hóa dữ liệu.',
  'Triển khai liên tục giúp rút ngắn chu kỳ phản hồi từ người dùng thực tế.',
  'Khả năng tự phục hồi của cụm máy chủ giữ cho dịch vụ luôn sẵn sàng cao.',
  'Việc phân vùng dữ liệu hợp lý giúp mở rộng quy mô theo chiều ngang dễ dàng.',
  'Quản lý cấu hình tập trung giúp đồng bộ môi trường triển khai nhanh chóng.',
  'Giới hạn tốc độ gọi API ngăn chặn các cuộc tấn công từ chối dịch vụ.',
  'Tối ưu hóa truy vấn chỉ mục giúp tăng tốc độ phản hồi đáng kể cho người dùng.',
  'Sao lưu định kỳ dữ liệu đảm bảo khả năng khôi phục sau thảm họa.',
  'Thiết kế giao diện người dùng cần ưu tiên khả năng tiếp cận và độ mượt mà.',
  'Xử lý lỗi phía máy khách phải cung cấp hướng dẫn rõ ràng cho người dùng.',
  'Tài liệu hóa kiến trúc là tài sản quý giá cho các thành viên mới của nhóm.',
  'Đánh giá mã nguồn định kỳ giúp duy trì tiêu chuẩn chất lượng kỹ thuật cao.',
].join(' ')
const viChunk: ChunkSummary = { ...base, id: 'demo-vi', chapterTitle: 'Trích Đoạn Tài Liệu Gốc', originalTextMarkdown: longViMarkdown, language: 'vi' }
const rawChunk: ChunkSummary = { ...base, id: 'demo-raw', isAiFormatted: false }
</script>
<template>
  <div class="min-h-screen bg-slate-50 dark:bg-canvas text-slate-900 dark:text-white">
    <div class="mx-auto w-full max-w-3xl px-4 sm:px-6 py-10 space-y-8">
      <header class="space-y-3">
        <h1 class="text-xl sm:text-2xl font-extrabold tracking-tight">
          Reader Audio Narration — Playground
        </h1>
        <div class="flex items-center gap-2">
          <button
            type="button"
            class="rounded-lg border border-slate-300 dark:border-white/10 px-3 py-1.5 text-sm font-semibold"
            @click="setLocale('en')"
          >
            EN
          </button>
          <button
            type="button"
            class="rounded-lg border border-slate-300 dark:border-white/10 px-3 py-1.5 text-sm font-semibold"
            @click="setLocale('vi')"
          >
            VI
          </button>
          <span class="text-sm text-slate-500 dark:text-slate-400">active locale: {{ locale }}</span>
        </div>
      </header>

      <section class="space-y-3">
        <p class="text-sm font-bold uppercase tracking-wider text-brand-500">
          AI-formatted slice — English voice
        </p>
        <ReaderAudioPlayer :chunk="enChunk" />
      </section>

      <section class="space-y-3">
        <p class="text-sm font-bold uppercase tracking-wider text-brand-500">
          AI-formatted slice — Vietnamese voice
        </p>
        <ReaderAudioPlayer :chunk="viChunk" />
      </section>

      <section class="space-y-3">
        <p class="text-sm font-bold uppercase tracking-wider text-brand-500">
          Not AI-formatted — narration control is hidden
        </p>
        <ReaderAudioPlayer :chunk="rawChunk" />
        <p class="text-sm text-slate-500 dark:text-slate-400">
          Nothing renders above: narration is gated on AI-formatted slices.
        </p>
      </section>
    </div>
  </div>
</template>
