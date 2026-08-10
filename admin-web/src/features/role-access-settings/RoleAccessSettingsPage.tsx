import { useQuery, useQueryClient } from '@tanstack/react-query'
import { Info, Pencil, RefreshCw, ShieldCheck } from 'lucide-react'
import { Fragment, useMemo, useState } from 'react'
import { fetchRoleAccessMatrix, updateRoleAccessSetting } from '@/features/role-access-settings/api'
import type { RoleAccessCell, RoleAccessFeature, RoleAccessProfile } from '@/shared/types/api'
import { ApiError } from '@/shared/api/client'
import { useToast } from '@/shared/ui/Toast'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Card, CardContent } from '@/components/ui/card'
import { Checkbox } from '@/components/ui/checkbox'
import {
  Dialog,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import { Textarea } from '@/components/ui/textarea'
import { cn } from '@/lib/utils'

function displayLabel(fieldType: string, value: string) {
  if (fieldType === 'yes_no') {
    return value.trim().toUpperCase() === 'YES' ? 'Yes' : 'No'
  }
  const v = value.trim()
  if (!v) return '—'
  return v.length <= 40 ? v : `${v.slice(0, 37)}…`
}

function profileNote(profile: RoleAccessProfile) {
  const parts = [profile.baseRole]
  if (profile.requiresCollaboration !== 'ANY') parts.push(`Collab ${profile.requiresCollaboration}`)
  if (profile.requiresInfluencer !== 'ANY') parts.push(`Influencer ${profile.requiresInfluencer}`)
  return parts.join(' · ')
}

type EditTarget = {
  settingId: number
  profileLabel: string
  featureLabel: string
  fieldType: string
  isNotApplicable: boolean
  settingValue: string
}

export function RoleAccessSettingsPage() {
  const toast = useToast()
  const qc = useQueryClient()
  const [edit, setEdit] = useState<EditTarget | null>(null)
  const [isNa, setIsNa] = useState(false)
  const [value, setValue] = useState('')
  const [busy, setBusy] = useState(false)

  const query = useQuery({
    queryKey: ['role-access-matrix'],
    queryFn: fetchRoleAccessMatrix,
  })

  const matrix = query.data
  const cellMap = useMemo(() => {
    const map = new Map<string, RoleAccessCell>()
    for (const cell of matrix?.cells ?? []) {
      map.set(`${cell.profileKey}:${cell.featureKey}`, cell)
    }
    return map
  }, [matrix?.cells])

  const openEdit = (
    cell: RoleAccessCell,
    profile: RoleAccessProfile,
    feature: RoleAccessFeature,
  ) => {
    if (cell.settingId <= 0) return
    setEdit({
      settingId: cell.settingId,
      profileLabel: profile.profileLabel,
      featureLabel: feature.featureLabel,
      fieldType: feature.fieldType,
      isNotApplicable: cell.isNotApplicable,
      settingValue: cell.settingValue,
    })
    setIsNa(cell.isNotApplicable)
    setValue(cell.settingValue)
  }

  const onSave = async () => {
    if (!edit) return
    setBusy(true)
    try {
      const res = await updateRoleAccessSetting(edit.settingId, {
        isNotApplicable: isNa,
        settingValue: value,
        updatedBy: 'admin',
      })
      toast.push(res.message || 'Saved', 'success')
      setEdit(null)
      await qc.invalidateQueries({ queryKey: ['role-access-matrix'] })
    } catch (e) {
      toast.push(e instanceof ApiError ? e.message : 'Save failed', 'error')
    } finally {
      setBusy(false)
    }
  }

  return (
    <div className="flex h-full min-h-0 min-w-0 max-w-full flex-col gap-3">
      <div className="flex shrink-0 flex-wrap items-end justify-between gap-3">
        <div>
          <h1 className="font-[family-name:var(--font-display)] flex items-center gap-2 text-3xl font-semibold tracking-tight">
            <ShieldCheck size={28} className="text-rose-600" />
            Role Access Settings
          </h1>
          <p className="mt-1 text-sm text-muted-foreground">
            Role-wise visibility and access configuration matrix
          </p>
        </div>
        <Button variant="outline" onClick={() => query.refetch()} disabled={query.isFetching}>
          <RefreshCw size={16} className={cn(query.isFetching && 'animate-spin')} />
          Refresh
        </Button>
      </div>

      {!query.isLoading && matrix && !matrix.tablesExist && (
        <Card className="border-amber-200 bg-amber-50">
          <CardContent className="py-4 text-sm text-amber-900">
            Role access tables are not set up yet. Run{' '}
            <code className="rounded bg-white px-1.5 py-0.5">admin/create_role_access_settings_table.php</code>{' '}
            on the PHP site first.
          </CardContent>
        </Card>
      )}

      {matrix?.tablesExist && (
        <>
          <div className="flex shrink-0 flex-wrap gap-4 rounded-xl border bg-slate-50 px-4 py-3 text-xs text-muted-foreground">
            <span className="inline-flex items-center gap-2">
              <span className="h-5 w-5 rounded border bg-slate-100" />
              Not applicable
            </span>
            <span className="inline-flex items-center gap-2">
              <span className="inline-flex h-5 w-5 items-center justify-center rounded border border-blue-200 bg-blue-50 text-blue-600">
                <Info size={12} />
              </span>
              Configured — hover for details
            </span>
            <span className="inline-flex items-center gap-2">
              <Pencil size={12} />
              Hover cell to edit
            </span>
          </div>

          <Card className="min-h-0 flex-1 overflow-hidden py-0">
            <CardContent className="min-h-0 flex-1 overflow-auto p-0">
              <table className="w-max min-w-full border-collapse text-sm">
                <thead>
                  <tr className="bg-slate-900 text-left text-xs font-semibold tracking-wide text-slate-200 uppercase">
                    <th className="sticky top-0 left-0 z-30 min-w-[240px] border-r border-slate-700 bg-slate-950 px-4 py-3">
                      Feature
                    </th>
                    {matrix.profiles.map((profile) => (
                      <th
                        key={profile.id}
                        className="sticky top-0 z-20 min-w-[160px] border-r border-slate-700 px-3 py-3 align-top"
                      >
                        <div>{profile.profileLabel}</div>
                        <div className="mt-1 text-[10px] font-normal tracking-normal text-slate-400 normal-case">
                          {profileNote(profile)}
                        </div>
                      </th>
                    ))}
                  </tr>
                </thead>
                <tbody>
                  {matrix.featureGroups.map((group) => (
                    <Fragment key={group.name}>
                      <tr className="bg-slate-100">
                        <td
                          colSpan={matrix.profiles.length + 1}
                          className="px-4 py-2 text-[11px] font-bold tracking-wider text-slate-500 uppercase"
                        >
                          {group.name}
                        </td>
                      </tr>
                      {group.features.map((feature) => (
                        <tr key={feature.id} className="border-b border-slate-200">
                          <td className="sticky left-0 z-10 min-w-[240px] border-r bg-slate-50 px-4 py-3 align-top">
                            <div className="font-semibold text-slate-800">{feature.featureLabel}</div>
                            <div className="text-[11px] text-slate-400">{feature.featureKey}</div>
                          </td>
                          {matrix.profiles.map((profile) => {
                            const cell = cellMap.get(`${profile.profileKey}:${feature.featureKey}`) ?? {
                              settingId: 0,
                              profileKey: profile.profileKey,
                              featureKey: feature.featureKey,
                              isNotApplicable: true,
                              settingValue: '',
                            }
                            const applicable = !cell.isNotApplicable && cell.settingId > 0
                            const tooltip = cell.settingValue.trim()

                            return (
                              <td
                                key={`${profile.id}-${feature.id}`}
                                className={cn(
                                  'relative min-w-[160px] border-r p-0 align-middle',
                                  cell.isNotApplicable ? 'bg-slate-100' : 'bg-white',
                                )}
                              >
                                <div className="group flex min-h-14 items-center justify-center px-2 py-2">
                                  {applicable && (
                                    <>
                                      {feature.fieldType === 'yes_no' ? (
                                        <Badge
                                          variant="outline"
                                          className={cn(
                                            'max-w-full truncate',
                                            cell.settingValue.trim().toUpperCase() === 'YES'
                                              ? 'border-green-200 bg-green-50 text-green-800'
                                              : 'border-slate-200 bg-slate-50 text-slate-600',
                                          )}
                                          title={tooltip || undefined}
                                        >
                                          {displayLabel(feature.fieldType, cell.settingValue)}
                                        </Badge>
                                      ) : (
                                        <span
                                          className="inline-flex h-8 w-8 items-center justify-center rounded-lg border border-blue-200 bg-blue-50 text-blue-600"
                                          title={tooltip || undefined}
                                        >
                                          <Info size={16} />
                                        </span>
                                      )}
                                    </>
                                  )}
                                  {cell.settingId > 0 && (
                                    <Button
                                      type="button"
                                      size="icon"
                                      variant="ghost"
                                      className="absolute top-1 right-1 h-7 w-7 opacity-0 transition group-hover:opacity-100"
                                      onClick={() => openEdit(cell, profile, feature)}
                                    >
                                      <Pencil size={13} />
                                    </Button>
                                  )}
                                </div>
                              </td>
                            )
                          })}
                        </tr>
                      ))}
                    </Fragment>
                  ))}
                </tbody>
              </table>
            </CardContent>
          </Card>
        </>
      )}

      {query.isLoading && (
        <p className="py-10 text-center text-muted-foreground">Loading role access matrix…</p>
      )}

      <Dialog open={edit !== null} onOpenChange={(o) => !o && setEdit(null)}>
        <DialogContent className="max-w-md">
          <DialogHeader>
            <DialogTitle>Edit setting</DialogTitle>
          </DialogHeader>
          {edit && (
            <div className="space-y-4">
              <div className="rounded-lg border bg-slate-50 px-3 py-2 text-sm text-muted-foreground">
                <strong className="text-slate-900">{edit.profileLabel}</strong>
                <br />
                {edit.featureLabel}
              </div>
              <div className="flex items-center gap-2">
                <Checkbox
                  id="ras-na"
                  checked={isNa}
                  onCheckedChange={(c) => setIsNa(c === true)}
                />
                <label htmlFor="ras-na" className="text-sm font-normal">
                  Mark as not applicable
                </label>
              </div>
              <div className={cn('space-y-2', isNa && 'pointer-events-none opacity-45')}>
                <span className="text-sm font-medium">Value</span>
                {edit.fieldType === 'yes_no' ? (
                  <Select value={value.toUpperCase() === 'YES' ? 'YES' : 'NO'} onValueChange={setValue}>
                    <SelectTrigger>
                      <SelectValue />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="YES">Yes</SelectItem>
                      <SelectItem value="NO">No</SelectItem>
                    </SelectContent>
                  </Select>
                ) : (
                  <Textarea
                    value={value}
                    onChange={(e) => setValue(e.target.value)}
                    rows={6}
                    className="min-h-[120px] resize-y"
                  />
                )}
              </div>
            </div>
          )}
          <DialogFooter>
            <Button variant="outline" onClick={() => setEdit(null)}>
              Cancel
            </Button>
            <Button disabled={busy} onClick={() => void onSave()}>
              Save changes
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  )
}
