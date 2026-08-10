import type { ReactNode } from 'react'

type Props = {
  label: string
  children: ReactNode
  noWrap?: boolean
}

export function FormField({ label, children, noWrap = false }: Props) {
  return (
    <div className="space-y-1.5">
      <label
        className={`text-xs font-medium text-muted-foreground${noWrap ? ' whitespace-nowrap' : ''}`}
      >
        {label}
      </label>
      {children}
    </div>
  )
}
