interface AudioMediaDeps {
  createAudio: () => HTMLAudioElement | null
  speed: () => number
  volume: () => number
  intendsPlay: () => boolean
  onTime: (time: number, duration: number) => void
  onBlocked: () => void
}

export interface AudioMediaSource {
  valid: () => boolean
  offset?: number
  globalStart?: number
  duration?: number
  segmentDuration?: number
  allowEnd?: boolean
  carrier?: boolean
  onReady?: (offset: number, restored: boolean) => void
  onEnded?: () => void
  onError?: (error: Error) => void
}

/** Source and playback revisions own listeners, metadata seeks, and play continuations. */
export function createAudioPlaybackMedia(deps: AudioMediaDeps) {
  let element: HTMLAudioElement | null = null
  let objectUrl: string | null = null
  let revision = 0
  let source: AudioMediaSource | null = null
  let ready = false
  let offset = 0
  let cleanup: (() => void) | null = null

  function retire(): void {
    revision++
    cleanup?.()
    cleanup = null
    source = null
    ready = false
    offset = 0
    element?.pause()
    if (objectUrl) URL.revokeObjectURL(objectUrl)
    objectUrl = null
  }

  function time(): number {
    if (!source) return 0
    return (source.globalStart ?? 0) + (ready ? element?.currentTime ?? offset : offset)
  }

  function updateTime(): void {
    if (!source || source.carrier || !source.valid()) return
    const total = source.duration ?? (Number.isFinite(element?.duration) ? element!.duration : 0)
    deps.onTime(time(), total)
  }

  function bind(): void {
    cleanup?.()
    const el = element
    const current = source
    if (!el || !current) return
    const operation = ++revision
    const valid = () => operation === revision && current.valid()
    const listeners: Array<[string, EventListener]> = []
    const listen = (name: string, handler: () => void) => {
      const listener = () => { if (valid()) handler() }
      listeners.push([name, listener])
      el.addEventListener(name, listener)
    }
    cleanup = () => {
      for (const [name, listener] of listeners) el.removeEventListener?.(name, listener)
    }
    listen('loadedmetadata', () => metadata(current, valid))
    listen('timeupdate', updateTime)
    listen('durationchange', updateTime)
    listen('ended', () => { if (!current.carrier && ready && deps.intendsPlay()) current.onEnded?.() })
    listen('error', () => {
      if (!current.carrier) current.onError?.(new Error(el.error?.message || 'Audio media could not be loaded'))
    })
  }

  function metadata(current: AudioMediaSource, valid: () => boolean): void {
    if (!valid() || ready || !element) return
    ready = true
    const total = current.segmentDuration ?? element.duration
    const requested = offset
    const inRange = Number.isFinite(total) && total > 0 && requested >= 0
      && (requested < total || (current.allowEnd && requested === total))
    offset = inRange ? requested : 0
    try { element.currentTime = offset } catch { offset = 0 }
    updateTime()
    current.onReady?.(offset, requested > 0 && offset === requested)
    if (valid() && deps.intendsPlay()) void play()
  }

  async function play(): Promise<void> {
    const current = source
    const el = element
    if (!el || !current?.valid() || !ready || !deps.intendsPlay()) return
    bind()
    const operation = revision
    try {
      await el.play()
      if ((operation !== revision || !current.valid()) && !deps.intendsPlay()) el.pause()
    } catch (error) {
      if (operation !== revision || !current.valid()) return
      if (error instanceof DOMException && error.name === 'AbortError') return
      // The optional System carrier must not govern native utterance intent.
      if (!current.carrier) deps.onBlocked()
    }
  }

  function mount(blob: Blob | string, options: AudioMediaSource): void {
    retire()
    element ??= deps.createAudio()
    const el = element
    if (!el || !options.valid()) return
    source = options
    offset = options.offset ?? 0
    if (typeof blob === 'string') el.src = blob
    else { objectUrl = URL.createObjectURL(blob); el.src = objectUrl }
    el.loop = !!options.carrier
    el.volume = options.carrier ? 0 : deps.volume()
    el.playbackRate = deps.speed()
    el.currentTime = 0
    bind()
    const operation = revision
    // A fresh real source has HAVE_NOTHING until its metadata is available.
    if (el.readyState >= 1 || options.carrier) metadata(options, () => operation === revision && options.valid())
    else updateTime()
  }

  function pause(): void { bind(); element?.pause() }

  function seek(seconds: number): boolean {
    if (!source?.valid() || !element || !ready || source.carrier) return false
    const total = source.segmentDuration ?? element.duration
    if (!Number.isFinite(seconds) || !Number.isFinite(total) || total <= 0) return false
    const local = seconds - (source.globalStart ?? 0)
    if (local < 0 || local > total) return false
    bind()
    element.currentTime = local
    offset = element.currentTime
    updateTime()
    return true
  }

  function setSpeed(value: number): void { if (element) element.playbackRate = value }
  function setVolume(value: number): void { if (element && !source?.carrier) element.volume = value }

  return { mount, play, pause, retire, time, seek, setSpeed, setVolume, element: () => element }
}
