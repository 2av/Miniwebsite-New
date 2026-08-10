import { CKEditor } from '@ckeditor/ckeditor5-react'
import {
  Alignment,
  BlockQuote,
  Bold,
  ClassicEditor,
  Essentials,
  FontBackgroundColor,
  FontColor,
  FontFamily,
  FontSize,
  Heading,
  Indent,
  IndentBlock,
  Italic,
  Link,
  List,
  Paragraph,
  Table,
  TableToolbar,
  Underline,
  Undo,
} from 'ckeditor5'
import { useEffect, useMemo, useRef } from 'react'
import type { Editor } from 'ckeditor5'
import 'ckeditor5/ckeditor5.css'

const EDITOR_PLUGINS = [
  Essentials,
  Paragraph,
  Heading,
  Bold,
  Italic,
  Underline,
  List,
  Indent,
  IndentBlock,
  BlockQuote,
  Table,
  TableToolbar,
  Undo,
  Link,
  Alignment,
  FontSize,
  FontFamily,
  FontColor,
  FontBackgroundColor,
]

const EDITOR_TOOLBAR = [
  'heading',
  '|',
  'bold',
  'italic',
  'underline',
  '|',
  'bulletedList',
  'numberedList',
  '|',
  'outdent',
  'indent',
  '|',
  'blockQuote',
  'insertTable',
  '|',
  'undo',
  'redo',
  '|',
  'link',
  '|',
  'alignment',
  '|',
  'fontSize',
  'fontFamily',
  'fontColor',
  'fontBackgroundColor',
]

type HtmlRichEditorProps = {
  value: string
  onChange: (value: string) => void
  disabled?: boolean
}

export function HtmlRichEditor({ value, onChange, disabled }: HtmlRichEditorProps) {
  const editorRef = useRef<Editor | null>(null)
  const skipChangeRef = useRef(false)

  const config = useMemo(
    () => ({
      licenseKey: 'GPL' as const,
      plugins: EDITOR_PLUGINS,
      toolbar: EDITOR_TOOLBAR,
      table: {
        contentToolbar: ['tableColumn', 'tableRow', 'mergeTableCells'],
      },
    }),
    [],
  )

  useEffect(() => {
    const editor = editorRef.current
    if (!editor || editor.getData() === value) return
    skipChangeRef.current = true
    editor.setData(value)
    skipChangeRef.current = false
  }, [value])

  useEffect(() => {
    const editor = editorRef.current
    if (!editor) return
    if (disabled) editor.enableReadOnlyMode('manage-content')
    else editor.disableReadOnlyMode('manage-content')
  }, [disabled])

  return (
    <div className="html-rich-editor overflow-hidden rounded-lg border border-input bg-white">
      <CKEditor
        editor={ClassicEditor}
        config={config}
        data={value}
        disabled={disabled}
        onReady={(editor) => {
          editorRef.current = editor
        }}
        onChange={(_, editor) => {
          if (skipChangeRef.current) return
          onChange(editor.getData())
        }}
      />
    </div>
  )
}
