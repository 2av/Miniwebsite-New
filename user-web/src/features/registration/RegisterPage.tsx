import { useMemo, useState } from 'react'
import { Link, useNavigate, useSearchParams } from 'react-router-dom'
import { ApiError } from '@/shared/api/client'
import { loginApi } from '@/features/auth/api'
import { setAuthSession } from '@/shared/auth/session'
import type { UserRole } from '@/shared/types/api'
import { useSiteStyles } from '@/shared/site/useSiteStyles'
import {
  INDIAN_STATES,
  resendRegistrationOtp,
  startRegistration,
  verifyRegistrationOtp,
  type RegistrationKind,
} from '@/features/registration/api'

type Props = { kind: RegistrationKind }

export function RegisterPage({ kind }: Props) {
  useSiteStyles()
  const navigate = useNavigate()
  const [params] = useSearchParams()
  const refFromUrl = params.get('ref') || ''

  const meta = useMemo(
    () =>
      kind === 'customer'
        ? {
            title: 'Create An Account',
            subtitle: 'Create an account to create your ShareBizz',
            backTo: '/login',
            successPath: '/registration/customer/success',
            role: 'CUSTOMER' as UserRole,
            autoLogin: true,
          }
        : {
            title: 'Franchisee Registration',
            subtitle: 'Create an account to become a franchise partner',
            backTo: '/login',
            successPath: '/registration/franchisee/success',
            role: 'FRANCHISEE' as UserRole,
            autoLogin: false,
          },
    [kind],
  )

  const [step, setStep] = useState<'form' | 'otp'>('form')
  const [firstName, setFirstName] = useState('')
  const [lastName, setLastName] = useState('')
  const [email, setEmail] = useState('')
  const [phone, setPhone] = useState('')
  const [state, setState] = useState('')
  const [password, setPassword] = useState('')
  const [showPassword, setShowPassword] = useState(false)
  const [referralCode, setReferralCode] = useState(refFromUrl)
  const [otp, setOtp] = useState('')
  const [errors, setErrors] = useState<string[]>([])
  const [info, setInfo] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)

  async function onRegister(e: React.FormEvent) {
    e.preventDefault()
    setErrors([])
    setInfo(null)

    const errs: string[] = []
    if (!firstName.trim()) errs.push('First Name is required.')
    if (!email.trim() || !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email.trim())) errs.push('Valid email is required.')
    if (!/^[0-9]{10}$/.test(phone.trim())) errs.push('Valid 10-digit mobile number is required.')
    if (kind === 'customer' && !INDIAN_STATES.includes(state as (typeof INDIAN_STATES)[number])) {
      errs.push('Please select a valid State.')
    }
    if (password.length < 6) errs.push('Password must be at least 6 characters long.')
    if (errs.length) {
      setErrors(errs)
      return
    }

    setBusy(true)
    try {
      const body: Record<string, unknown> = {
        firstName: firstName.trim(),
        lastName: lastName.trim(),
        email: email.trim(),
        phone: phone.trim(),
        password,
        referralCode: referralCode.trim() || null,
      }
      if (kind === 'customer') body.state = state
      const res = await startRegistration(kind, body)
      setInfo(res.message || 'OTP has been sent to your email.')
      setStep('otp')
    } catch (err) {
      setErrors([err instanceof ApiError ? err.message : 'Registration failed.'])
    } finally {
      setBusy(false)
    }
  }

  async function onVerify(e: React.FormEvent) {
    e.preventDefault()
    setErrors([])
    setBusy(true)
    try {
      const res = await verifyRegistrationOtp(kind, email.trim(), otp.trim())
      const data = res.data
      if (meta.autoLogin && data) {
        try {
          const auth = await loginApi(email.trim(), password, meta.role)
          setAuthSession({
            token: auth.accessToken,
            refreshToken: auth.refreshToken,
            role: (auth.role as UserRole) || meta.role,
            email: auth.email,
            name: auth.name,
            userId: auth.userId,
            collaborationEnabled: auth.collaborationEnabled === 'YES',
            saleskitEnabled: auth.saleskitEnabled === 'YES',
          })
          navigate('/dashboard', { replace: true })
          return
        } catch {
          // fall through to success page
        }
      }
      sessionStorage.setItem(
        'mw_registration_success',
        JSON.stringify({ kind, email: data?.email || email, name: data?.name || `${firstName} ${lastName}`.trim() }),
      )
      navigate(meta.successPath, { replace: true })
    } catch (err) {
      setErrors([err instanceof ApiError ? err.message : 'OTP verification failed.'])
    } finally {
      setBusy(false)
    }
  }

  async function onResend() {
    setErrors([])
    setBusy(true)
    try {
      const res = await resendRegistrationOtp(kind, email.trim())
      setInfo(res.message || 'OTP resent.')
    } catch (err) {
      setErrors([err instanceof ApiError ? err.message : 'Could not resend OTP.'])
    } finally {
      setBusy(false)
    }
  }

  return (
    <div className="login-wrap">
      <div className="login-container">
        <h2 className="heading">
          <Link to={meta.backTo}>
            <i className="fa fa-angle-left" aria-hidden="true" />
          </Link>
          {meta.title}
        </h2>
        <p className="text-white">{meta.subtitle}</p>

        {step === 'otp' ? (
          <form onSubmit={onVerify} autoComplete="off">
            <div className="mb-4">
              <input
                type="text"
                className="form-control"
                placeholder="Enter OTP*"
                value={otp}
                onChange={(e) => setOtp(e.target.value)}
                required
              />
            </div>
            <input type="submit" className="btn btn-login" value={busy ? 'VERIFYING...' : 'VERIFY OTP'} disabled={busy} />
            <button type="button" className="btn btn-login" style={{ marginTop: 10 }} onClick={() => void onResend()} disabled={busy}>
              Resend OTP
            </button>
          </form>
        ) : (
          <form onSubmit={onRegister} autoComplete="off">
            <div className="mb-3" style={{ display: 'flex', gap: 10 }}>
              <input
                type="text"
                className="form-control"
                placeholder="First Name*"
                value={firstName}
                onChange={(e) => setFirstName(e.target.value)}
                required
              />
              <input
                type="text"
                className="form-control"
                placeholder="Last Name"
                value={lastName}
                onChange={(e) => setLastName(e.target.value)}
              />
            </div>
            <div className="mb-3">
              <input
                type="email"
                className="form-control"
                placeholder="Email*"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                required
              />
            </div>
            <div className="mb-3">
              <input
                type="tel"
                className="form-control"
                placeholder="Mobile Number*"
                value={phone}
                onChange={(e) => setPhone(e.target.value)}
                required
              />
            </div>
            {kind === 'customer' && (
              <div className="mb-3">
                <select className="form-control" value={state} onChange={(e) => setState(e.target.value)} required>
                  <option value="">Select State*</option>
                  {INDIAN_STATES.map((s) => (
                    <option key={s} value={s}>
                      {s}
                    </option>
                  ))}
                </select>
              </div>
            )}
            <div className="mb-3">
              <div className="password-field">
                <input
                  type={showPassword ? 'text' : 'password'}
                  className="form-control"
                  placeholder="Password*"
                  value={password}
                  onChange={(e) => setPassword(e.target.value)}
                  required
                />
                <button
                  type="button"
                  className="password-toggle"
                  onClick={() => setShowPassword((v) => !v)}
                  title={showPassword ? 'Hide Password' : 'Show Password'}
                >
                  <i className={`fa ${showPassword ? 'fa-eye' : 'fa-eye-slash'}`} aria-hidden="true" />
                </button>
              </div>
            </div>
            <div className="mb-3">
              <input
                type="text"
                className="form-control"
                placeholder="Referral Code (Optional)"
                value={referralCode}
                onChange={(e) => setReferralCode(e.target.value)}
                readOnly={Boolean(refFromUrl)}
              />
            </div>
            <input
              type="submit"
              className="btn btn-login"
              value={busy ? 'PLEASE WAIT...' : 'CREATE ACCOUNT'}
              disabled={busy}
            />
          </form>
        )}

        {info && <div className="alert success" style={{ marginTop: 12 }}>{info}</div>}
        {errors.length > 0 && (
          <div className="alert danger text-danger" style={{ marginTop: 12 }}>
            <ul>
              {errors.map((err) => (
                <li key={err}>{err}</li>
              ))}
            </ul>
          </div>
        )}

        <div className="CreateAccount-wrap">
          <span className="text-white">
            Already have an account? &nbsp;
            <Link to={meta.backTo} className="create-account">
              Login
            </Link>
          </span>
        </div>
      </div>
    </div>
  )
}
