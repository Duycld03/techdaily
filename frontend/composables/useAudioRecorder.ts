import { ref } from 'vue'

export function useAudioRecorder() {
  const isRecording = ref(false)
  const isPaused = ref(false)
  const durationSeconds = ref(0)
  const audioBlob = ref<Blob | null>(null)
  const audioUrl = ref<string | null>(null)
  const error = ref<string | null>(null)

  let mediaRecorder: MediaRecorder | null = null
  let audioChunks: Blob[] = []
  let timerInterval: any = null

  async function startRecording(): Promise<boolean> {
    error.value = null
    audioChunks = []
    audioBlob.value = null
    if (audioUrl.value) {
      URL.revokeObjectURL(audioUrl.value)
      audioUrl.value = null
    }

    try {
      const stream = await navigator.mediaDevices.getUserMedia({ audio: true })
      
      const mimeType = MediaRecorder.isTypeSupported('audio/webm;codecs=opus')
        ? 'audio/webm;codecs=opus'
        : 'audio/webm'

      mediaRecorder = new MediaRecorder(stream, { mimeType })

      mediaRecorder.ondataavailable = (event) => {
        if (event.data && event.data.size > 0) {
          audioChunks.push(event.data)
        }
      }

      mediaRecorder.onstop = () => {
        const finalBlob = new Blob(audioChunks, { type: mimeType })
        audioBlob.value = finalBlob
        audioUrl.value = URL.createObjectURL(finalBlob)
        // Stop all tracks to release mic
        stream.getTracks().forEach((track) => track.stop())
      }

      mediaRecorder.start(200) // chunk every 200ms
      isRecording.value = true
      durationSeconds.value = 0

      timerInterval = setInterval(() => {
        durationSeconds.value++
      }, 1000)

      return true
    } catch (err: any) {
      error.value = err.message || 'Microphone access denied'
      isRecording.value = false
      return false
    }
  }

  function stopRecording() {
    if (mediaRecorder && isRecording.value) {
      mediaRecorder.stop()
      isRecording.value = false
      if (timerInterval) {
        clearInterval(timerInterval)
        timerInterval = null
      }
    }
  }

  function reset() {
    stopRecording()
    audioBlob.value = null
    if (audioUrl.value) {
      URL.revokeObjectURL(audioUrl.value)
      audioUrl.value = null
    }
    durationSeconds.value = 0
  }

  async function blobToBase64(blob: Blob): Promise<string> {
    return new Promise((resolve, reject) => {
      const reader = new FileReader()
      reader.onloadend = () => {
        const base64data = (reader.result as string).split(',')[1]
        resolve(base64data)
      }
      reader.onerror = reject
      reader.readAsDataURL(blob)
    })
  }

  return {
    isRecording,
    isPaused,
    durationSeconds,
    audioBlob,
    audioUrl,
    error,
    startRecording,
    stopRecording,
    reset,
    blobToBase64
  }
}
