<script setup lang="ts">
import { ref, onMounted, onBeforeUnmount, watch } from 'vue'
import { EditorView, basicSetup } from 'codemirror'
import { EditorState } from '@codemirror/state'
import { markdown } from '@codemirror/lang-markdown'
import { oneDark } from '@codemirror/theme-one-dark'

const props = defineProps<{
  modelValue: string
  placeholder?: string
  disabled?: boolean
}>()

const emit = defineEmits<{
  (e: 'update:modelValue', value: string): void
}>()

const editorContainer = ref<HTMLDivElement | null>(null)
let editorView: EditorView | null = null

onMounted(() => {
  if (!editorContainer.value) return

  const state = EditorState.create({
    doc: props.modelValue,
    extensions: [
      basicSetup,
      markdown(),
      oneDark,
      EditorView.lineWrapping,
      EditorView.updateListener.of((update) => {
        if (update.docChanged) {
          const docString = update.state.doc.toString()
          emit('update:modelValue', docString)
        }
      }),
      EditorState.readOnly.of(!!props.disabled)
    ]
  })

  editorView = new EditorView({
    state,
    parent: editorContainer.value
  })
})

watch(
  () => props.modelValue,
  (newValue) => {
    if (editorView && newValue !== editorView.state.doc.toString()) {
      editorView.dispatch({
        changes: { from: 0, to: editorView.state.doc.length, insert: newValue }
      })
    }
  }
)

watch(
  () => props.disabled,
  (newDisabled) => {
    if (editorView) {
      editorView.dispatch({
        effects: []
      })
    }
  }
)

onBeforeUnmount(() => {
  if (editorView) {
    editorView.destroy()
  }
})
</script>

<template>
  <div class="relative w-full h-full flex flex-col rounded-xl overflow-hidden border border-slate-800 bg-[#282c34] font-mono text-sm shadow-inner">
    <div ref="editorContainer" class="w-full h-full overflow-y-auto codemirror-wrapper"></div>
  </div>
</template>

<style>
.codemirror-wrapper .cm-editor {
  height: 100%;
  font-family: 'JetBrains Mono', 'Fira Code', monospace;
  font-size: 13.5px;
}
.codemirror-wrapper .cm-scroller {
  padding: 12px 4px;
}
</style>
