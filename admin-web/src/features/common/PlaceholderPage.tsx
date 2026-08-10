import { Card } from '@/shared/ui/primitives'

export function PlaceholderPage({ title }: { title: string }) {
  return (
    <Card className="p-8">
      <h1 className="font-[family-name:var(--font-display)] text-2xl font-semibold">{title}</h1>
      <p className="mt-2 text-slate-500">This module will be migrated from PHP admin next.</p>
    </Card>
  )
}
