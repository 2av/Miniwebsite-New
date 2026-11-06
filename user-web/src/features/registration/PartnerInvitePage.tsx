import { useEffect, useState } from 'react'
import { useSearchParams } from 'react-router-dom'
import { apiGet, apiSend, ApiError } from '@/shared/api/client'
import { useSiteStyles } from '@/shared/site/useSiteStyles'

export function PartnerInvitePage() {
  useSiteStyles()
  const [params] = useSearchParams()
  const token = params.get('token') || ''
  const [email, setEmail] = useState('')
  const [roleName, setRoleName] = useState('Partner')
  const [firstName, setFirstName] = useState('')
  const [lastName, setLastName] = useState('')
  const [phone, setPhone] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [done, setDone] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)

  useEffect(() => {
    if (!token) {
      setError('This registration link is missing a token.')
      return
    }
    let cancelled = false
    ;(async () => {
      try {
        const raw = await apiGet<Record<string, unknown>>('/api/v1/registration/partner', { token })
        if (cancelled) return
        setEmail(String(raw.email ?? raw.Email ?? ''))
        setRoleName(String(raw.roleName ?? raw.RoleName ?? 'Partner'))
      } catch (e) {
        if (!cancelled) setError(e instanceof Error ? e.message : 'This registration link is not valid.')
      }
    })()
    return () => {
      cancelled = true
    }
  }, [token])

  async function submit(e: React.FormEvent) {
    e.preventDefault()
    setBusy(true)
    setError(null)
    try {
      const res = await apiSend('post', '/api/v1/registration/partner', {
        token,
        firstName,
        lastName,
        phone,
        password,
      })
      setDone(res.message || 'Account created. Check your email for login credentials.')
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Registration failed.')
    } finally {
      setBusy(false)
    }
  }

  return (
    <main className="mx-auto max-w-lg p-6">
      <h1 className="mb-2 text-2xl font-semibold">Create {roleName} account</h1>
      <p className="mb-4 text-sm text-[#4B5563]">{email}</p>
      {error && <p className="mb-3 text-red-600">{error}</p>}
      {done ? (
        <p>{done}</p>
      ) : (
        <form className="grid gap-3" onSubmit={(e) => void submit(e)}>
          <input className="h-11 rounded-lg border px-3" placeholder="First name" value={firstName} onChange={(e) => setFirstName(e.target.value)} required />
          <input className="h-11 rounded-lg border px-3" placeholder="Last name" value={lastName} onChange={(e) => setLastName(e.target.value)} />
          <input className="h-11 rounded-lg border px-3" placeholder="Phone" value={phone} onChange={(e) => setPhone(e.target.value)} required />
          <input className="h-11 rounded-lg border px-3" placeholder="Password" type="password" value={password} onChange={(e) => setPassword(e.target.value)} minLength={6} required />
          <button type="submit" className="sb-btn-primary" disabled={busy || !token}>
            {busy ? 'Please wait…' : 'Create account'}
          </button>
        </form>
      )}
    </main>
  )
}
