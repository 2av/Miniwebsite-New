import { useState, type FormEvent, type ReactNode } from 'react'
import type { CompanyForm } from '@/features/website/profileEditContext'
import { EditorActions, EditorDialog, EditorField } from '@/features/website/profileEditorUi'
import {
  ABOUT_PLACEHOLDER,
  BUSINESS_CATEGORIES,
  BUSINESS_MODELS,
  INDIA_LOCATIONS,
  OPERATION_AREAS,
  PROFILE_TYPES,
  SAMPLE_WEBSITE,
  SELECTED_AREA,
  aboutError,
  emailError,
  formatMobile,
  gstError,
  gstText,
  lettersOnly,
  mobileDigits,
  mobileError,
  nameError,
  parseAreaSelections,
  pinDigits,
  pincodeError,
  serializeAreaSelections,
  type AreaSelection,
} from '@/features/website/profileFieldRules'

const STATES = Object.keys(INDIA_LOCATIONS)

export function CompanyProfileDialog({
  company,
  busy,
  error,
  onClose,
  onSubmit,
}: {
  company: CompanyForm
  busy: boolean
  error: string | null
  onClose: () => void
  onSubmit: (next: CompanyForm) => void
}) {
  const [form, setForm] = useState<CompanyForm>({ ...company })
  const [areas, setAreas] = useState<AreaSelection[]>(() => parseAreaSelections(company.businessOperationLocations))
  const [localError, setLocalError] = useState<string | null>(null)
  const [fieldErrors, setFieldErrors] = useState<Record<string, string>>({})

  function set<K extends keyof CompanyForm>(key: K, value: CompanyForm[K]) {
    setForm((current) => ({ ...current, [key]: value }))
  }

  function save(event: FormEvent) {
    event.preventDefault()
    const next: CompanyForm = {
      ...form,
      firstName: lettersOnly(form.firstName).trim(),
      lastName: lettersOnly(form.lastName).trim(),
      contact: mobileDigits(form.contact),
      contact2: mobileDigits(form.contact2),
      whatsapp: mobileDigits(form.whatsapp),
      email: form.email.trim(),
      gstNumber: form.gstNumber.trim() ? gstText(form.gstNumber) : '',
      pincode: pinDigits(form.pincode),
      aboutUs: form.aboutUs.slice(0, 500),
      website: form.website.trim(),
      businessOperationLocations: form.businessOperationArea === SELECTED_AREA ? serializeAreaSelections(areas) : '',
    }
    const problems: Record<string, string> = {}
    const put = (key: string, message: string | null) => {
      if (message) problems[key] = message
    }
    put('firstName', nameError(next.firstName))
    put('lastName', nameError(next.lastName))
    put('contact', mobileError(next.contact))
    put('whatsapp', mobileError(next.whatsapp))
    put('contact2', mobileError(next.contact2))
    put('email', emailError(next.email))
    put('gstNumber', gstError(next.gstNumber))
    put('pincode', pincodeError(next.pincode))
    put('aboutUs', aboutError(next.aboutUs))
    if (next.businessOperationArea === SELECTED_AREA) {
      const picked = areas.filter((row) => row.state && row.cities.length > 0)
      if (picked.length === 0) problems.businessOperationArea = 'Select up to 6 states, and up to 4 cities in each state.'
      if (areas.length > 6 || areas.some((row) => row.cities.length > 4)) problems.businessOperationArea = 'Select up to 6 states, and up to 4 cities in each state.'
    }
    setFieldErrors(problems)
    if (Object.keys(problems).length > 0) {
      setLocalError('Fix the highlighted fields.')
      return
    }
    setLocalError(null)
    onSubmit(next)
  }

  function addState() {
    const nextState = STATES.find((state) => !areas.some((row) => row.state === state))
    if (!nextState || areas.length >= 6) return
    setAreas((current) => [...current, { state: nextState, cities: [] }])
  }

  return (
    <EditorDialog title="Business profile" onClose={onClose} busy={busy}>
      <form onSubmit={save}>
        {error || localError ? <p style={{ color: '#b42318' }}>{error || localError}</p> : null}
        <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 12 }}>
          <Field label="First Name" error={fieldErrors.firstName}>
            <input value={form.firstName} onChange={(e) => set('firstName', lettersOnly(e.target.value))} />
          </Field>
          <Field label="Last Name" error={fieldErrors.lastName}>
            <input value={form.lastName} onChange={(e) => set('lastName', lettersOnly(e.target.value))} />
          </Field>
        </div>
        <Choice label="Business Profile Type" value={form.businessProfileType} options={PROFILE_TYPES} onChange={(value) => set('businessProfileType', value)} />
        <Choice label="Business Model" value={form.businessType} options={BUSINESS_MODELS} onChange={(value) => set('businessType', value)} />
        <Choice
          label="Business Operation Area"
          value={form.businessOperationArea}
          options={OPERATION_AREAS}
          hint="Local area, across India, or selected cities and states. Selected cities: maximum 6 states, and 4 cities in each state."
          error={fieldErrors.businessOperationArea}
          onChange={(value) => set('businessOperationArea', value)}
        />
        {form.businessOperationArea === SELECTED_AREA ? (
          <div style={{ marginBottom: 12 }}>
            {areas.map((row, index) => {
              const cities = cityOptions(row.state, row.cities)
              return (
                <div key={`${row.state}-${index}`} style={{ border: '1px solid #e5e7eb', borderRadius: 8, padding: 10, marginBottom: 8 }}>
                  <EditorField label={`State ${index + 1}`}>
                    <select
                      value={row.state}
                      onChange={(e) =>
                        setAreas((current) => current.map((item, itemIndex) => (itemIndex === index ? { state: e.target.value, cities: [] } : item)))
                      }
                    >
                      {STATES.filter((state) => state === row.state || !areas.some((item) => item.state === state)).map((state) => (
                        <option key={state} value={state}>
                          {state}
                        </option>
                      ))}
                    </select>
                  </EditorField>
                  <EditorField label="Cities">
                    <select
                      value=""
                      disabled={row.cities.length >= 4}
                      onChange={(e) => {
                        const city = e.target.value
                        if (!city) return
                        setAreas((current) =>
                          current.map((item, itemIndex) =>
                            itemIndex === index && item.cities.length < 4 && !item.cities.includes(city)
                              ? { ...item, cities: [...item.cities, city] }
                              : item,
                          ),
                        )
                      }}
                    >
                      <option value="">{row.cities.length >= 4 ? '4 cities selected' : 'Add a city'}</option>
                      {cities.map((city) => (
                        <option key={city} value={city}>
                          {city}
                        </option>
                      ))}
                    </select>
                    <div style={{ display: 'flex', flexWrap: 'wrap', gap: 6, marginTop: 8 }}>
                      {row.cities.map((city) => (
                        <button
                          key={city}
                          type="button"
                          className="mw-editor-ghost"
                          style={{ minHeight: 28, padding: '0 8px' }}
                          onClick={() =>
                            setAreas((current) =>
                              current.map((item, itemIndex) =>
                                itemIndex === index ? { ...item, cities: item.cities.filter((name) => name !== city) } : item,
                              ),
                            )
                          }
                        >
                          {city} ×
                        </button>
                      ))}
                    </div>
                  </EditorField>
                  <button
                    type="button"
                    className="mw-editor-ghost"
                    onClick={() => setAreas((current) => current.filter((_, itemIndex) => itemIndex !== index))}
                  >
                    Remove state
                  </button>
                </div>
              )
            })}
            {areas.length < 6 ? (
              <button type="button" className="mw-editor-ghost" onClick={addState}>
                Add state
              </button>
            ) : (
              <p style={{ margin: 0, color: '#64748b', fontSize: 12 }}>Maximum 6 states selected.</p>
            )}
          </div>
        ) : null}
        <Choice label="Business Category (Primary)" value={form.positionPrimary} options={categoryOptions(form.positionPrimary)} onChange={(value) => set('positionPrimary', value)} />
        <Choice label="Business Category (Secondary)" value={form.positionSecondary} options={categoryOptions(form.positionSecondary)} onChange={(value) => set('positionSecondary', value)} />
        <Field label="Phone No." error={fieldErrors.contact}>
          <input inputMode="numeric" placeholder="10-digit mobile number" value={formatMobile(form.contact)} onChange={(e) => set('contact', mobileDigits(e.target.value))} />
        </Field>
        <Field label="WhatsApp No." error={fieldErrors.whatsapp}>
          <input inputMode="numeric" placeholder="10-digit mobile number" value={formatMobile(form.whatsapp)} onChange={(e) => set('whatsapp', mobileDigits(e.target.value))} />
        </Field>
        <Field label="Alternate No." error={fieldErrors.contact2}>
          <input inputMode="numeric" placeholder="10-digit mobile number" value={formatMobile(form.contact2)} onChange={(e) => set('contact2', mobileDigits(e.target.value))} />
        </Field>
        <Field label="Email Id" error={fieldErrors.email}>
          <input type="email" placeholder="Enter your email (optional)" value={form.email} onChange={(e) => set('email', e.target.value)} />
        </Field>
        <Field label="GST Identification Number" error={fieldErrors.gstNumber}>
          <input placeholder="Enter GST number (optional)" value={form.gstNumber} onChange={(e) => set('gstNumber', gstText(e.target.value))} />
        </Field>
        <EditorField label="Company Establishment Date">
          <input type="date" value={isoDate(form.compEstDate)} onChange={(e) => set('compEstDate', e.target.value)} />
        </EditorField>
        <EditorField label="Website Link">
          <input placeholder={SAMPLE_WEBSITE} value={form.website} onChange={(e) => set('website', e.target.value)} />
        </EditorField>
        <Field label="Address Line 1">
          <input value={form.address} onChange={(e) => set('address', e.target.value)} />
        </Field>
        <Field label="Area/Locality">
          <input value={form.address2} onChange={(e) => set('address2', e.target.value)} />
        </Field>
        <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 12 }}>
          <Field label="Country">
            <input value={form.country} onChange={(e) => set('country', e.target.value)} />
          </Field>
          <Field label="State">
            <input value={form.state} onChange={(e) => set('state', e.target.value)} />
          </Field>
          <Field label="City">
            <input value={form.city} onChange={(e) => set('city', e.target.value)} />
          </Field>
          <Field label="Pin code" error={fieldErrors.pincode}>
            <input inputMode="numeric" placeholder="6 digit pin code" value={form.pincode} onChange={(e) => set('pincode', pinDigits(e.target.value))} />
          </Field>
        </div>
        <Field label="About Us" error={fieldErrors.aboutUs}>
          <textarea
            rows={4}
            maxLength={500}
            placeholder={ABOUT_PLACEHOLDER}
            value={form.aboutUs}
            onChange={(e) => set('aboutUs', e.target.value.slice(0, 500))}
          />
          <span style={{ display: 'block', marginTop: 4, color: '#64748b', fontWeight: 500 }}>{form.aboutUs.trim().length}/500 · minimum 30</span>
        </Field>
        <EditorActions>
          <button className="mw-editor-primary" type="submit" disabled={busy}>
            {busy ? 'Saving...' : 'Save'}
          </button>
        </EditorActions>
      </form>
    </EditorDialog>
  )
}

function Field({ label, error, children }: { label: string; error?: string; children: ReactNode }) {
  return (
    <EditorField label={label}>
      {children}
      {error ? <span style={{ display: 'block', color: '#b42318', fontWeight: 500, marginTop: 4 }}>{error}</span> : null}
    </EditorField>
  )
}

function Choice({
  label,
  value,
  options,
  hint,
  error,
  onChange,
}: {
  label: string
  value: string
  options: readonly string[]
  hint?: string
  error?: string
  onChange: (value: string) => void
}) {
  const extra = value && !options.includes(value) ? [value] : []
  return (
    <Field label={label} error={error}>
      <select value={value} onChange={(e) => onChange(e.target.value)}>
        <option value="">Select</option>
        {[...extra, ...options].map((option) => (
          <option key={option} value={option}>
            {/^\d+$/.test(option) ? 'Current category' : option}
          </option>
        ))}
      </select>
      {hint ? <span style={{ display: 'block', marginTop: 4, color: '#64748b', fontWeight: 500 }}>{hint}</span> : null}
    </Field>
  )
}

function categoryOptions(current: string) {
  if (current && !BUSINESS_CATEGORIES.includes(current as (typeof BUSINESS_CATEGORIES)[number]) && !/^\d+$/.test(current)) {
    return [current, ...BUSINESS_CATEGORIES]
  }
  return [...BUSINESS_CATEGORIES]
}

function cityOptions(state: string, selected: string[]) {
  const known = INDIA_LOCATIONS[state] || []
  return [...known, ...selected.filter((city) => !known.includes(city))].filter((city) => !selected.includes(city))
}

function isoDate(value: string) {
  return /^\d{4}-\d{2}-\d{2}$/.test(value.trim()) ? value.trim() : ''
}
