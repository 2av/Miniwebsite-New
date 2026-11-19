export const PROFILE_TYPES = [
  'Company / Agency',
  'Service Provider',
  'Trader / Retailer',
  'Freelancer / Consultant',
  'Manufacturer',
  'Home Business',
  'Street Vendor',
  'Gig Worker',
] as const

export const BUSINESS_MODELS = ['Product', 'Service', 'Hybrid (Product + Service)'] as const

export const OPERATION_AREAS = [
  'Local Area (Within City)',
  'Across India (Pan India Services)',
  'Selected Cities or States',
] as const

export const SELECTED_AREA = 'Selected Cities or States'

/** One category for now. The full list will be chosen later. */
export const BUSINESS_CATEGORIES = ['Digital Marketing'] as const

export const SAMPLE_WEBSITE = 'https://www.miniwebsite.in'

export const ABOUT_PLACEHOLDER =
  'Tell customers about your business, services, experience and what makes you unique'

export const WEEK_DAYS = ['Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday', 'Sunday'] as const

export const INDIA_LOCATIONS: Record<string, string[]> = {
  'Andhra Pradesh': ['Visakhapatnam', 'Vijayawada', 'Guntur', 'Nellore', 'Tirupati', 'Kakinada'],
  'Arunachal Pradesh': ['Itanagar', 'Naharlagun', 'Pasighat', 'Tawang'],
  Assam: ['Guwahati', 'Silchar', 'Dibrugarh', 'Jorhat', 'Tezpur', 'Nagaon'],
  Bihar: ['Patna', 'Gaya', 'Muzaffarpur', 'Bhagalpur', 'Darbhanga', 'Purnia'],
  Chhattisgarh: ['Raipur', 'Bhilai', 'Bilaspur', 'Korba', 'Durg', 'Raigarh'],
  Goa: ['Panaji', 'Margao', 'Vasco da Gama', 'Mapusa', 'Ponda'],
  Gujarat: ['Ahmedabad', 'Surat', 'Vadodara', 'Rajkot', 'Bhavnagar', 'Jamnagar'],
  Haryana: ['Gurugram', 'Faridabad', 'Panipat', 'Ambala', 'Karnal', 'Hisar'],
  'Himachal Pradesh': ['Shimla', 'Dharamshala', 'Mandi', 'Solan', 'Kullu', 'Hamirpur'],
  Jharkhand: ['Ranchi', 'Jamshedpur', 'Dhanbad', 'Bokaro', 'Hazaribagh', 'Deoghar'],
  Karnataka: ['Bengaluru', 'Mysuru', 'Mangaluru', 'Hubballi', 'Belagavi', 'Kalaburagi'],
  Kerala: ['Thiruvananthapuram', 'Kochi', 'Kozhikode', 'Thrissur', 'Kollam', 'Kannur'],
  'Madhya Pradesh': ['Bhopal', 'Indore', 'Gwalior', 'Jabalpur', 'Ujjain', 'Sagar'],
  Maharashtra: ['Mumbai', 'Pune', 'Nagpur', 'Nashik', 'Thane', 'Aurangabad'],
  Manipur: ['Imphal', 'Thoubal', 'Bishnupur', 'Churachandpur'],
  Meghalaya: ['Shillong', 'Tura', 'Jowai', 'Nongstoin'],
  Mizoram: ['Aizawl', 'Lunglei', 'Champhai', 'Serchhip'],
  Nagaland: ['Kohima', 'Dimapur', 'Mokokchung', 'Wokha'],
  Odisha: ['Bhubaneswar', 'Cuttack', 'Rourkela', 'Berhampur', 'Sambalpur', 'Puri'],
  Punjab: ['Ludhiana', 'Amritsar', 'Jalandhar', 'Patiala', 'Bathinda', 'Mohali'],
  Rajasthan: ['Jaipur', 'Jodhpur', 'Udaipur', 'Kota', 'Ajmer', 'Bikaner'],
  Sikkim: ['Gangtok', 'Namchi', 'Gyalshing', 'Mangan'],
  'Tamil Nadu': ['Chennai', 'Coimbatore', 'Madurai', 'Tiruchirappalli', 'Salem', 'Tirunelveli'],
  Telangana: ['Hyderabad', 'Warangal', 'Nizamabad', 'Karimnagar', 'Khammam', 'Ramagundam'],
  Tripura: ['Agartala', 'Udaipur', 'Dharmanagar', 'Kailashahar'],
  'Uttar Pradesh': ['Lucknow', 'Kanpur', 'Noida', 'Ghaziabad', 'Varanasi', 'Agra'],
  Uttarakhand: ['Dehradun', 'Haridwar', 'Haldwani', 'Roorkee', 'Rudrapur', 'Rishikesh'],
  'West Bengal': ['Kolkata', 'Howrah', 'Durgapur', 'Asansol', 'Siliguri', 'Kharagpur'],
  'Andaman and Nicobar Islands': ['Port Blair', 'Diglipur', 'Mayabunder', 'Rangat'],
  Chandigarh: ['Chandigarh'],
  'Dadra and Nagar Haveli and Daman and Diu': ['Daman', 'Diu', 'Silvassa'],
  Delhi: ['New Delhi', 'Dwarka', 'Rohini', 'Saket', 'Karol Bagh', 'Janakpuri'],
  'Jammu and Kashmir': ['Srinagar', 'Jammu', 'Anantnag', 'Baramulla', 'Udhampur'],
  Ladakh: ['Leh', 'Kargil'],
  Lakshadweep: ['Kavaratti', 'Agatti', 'Minicoy'],
  Puducherry: ['Puducherry', 'Karaikal', 'Mahe', 'Yanam'],
}

export type AreaSelection = { state: string; cities: string[] }

export type DaySchedule = { day: string; off: boolean; open: string; close: string }

const EMAIL_PATTERN = /^[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}$/
const GST_PATTERN = /^[0-9]{2}[A-Z]{5}[0-9]{4}[A-Z][1-9A-Z]Z[0-9A-Z]$/

export function lettersOnly(value: string) {
  return value.replace(/[^A-Za-z\s]/g, '').replace(/ {2,}/g, ' ')
}

export function mobileDigits(value: string) {
  return value.replace(/\D/g, '').slice(0, 10)
}

export function formatMobile(value: string) {
  const digits = mobileDigits(value)
  if (digits.length <= 5) return digits
  return `${digits.slice(0, 5)} ${digits.slice(5)}`
}

export function mobileError(value: string) {
  const digits = mobileDigits(value)
  if (!digits) return null
  if (digits.length !== 10) return 'Enter a valid 10-digit mobile number.'
  return null
}

export function emailError(value: string) {
  const text = value.trim()
  if (!text) return null
  if (!EMAIL_PATTERN.test(text)) return 'Invalid email format'
  return null
}

export function gstText(value: string) {
  return value.toUpperCase().replace(/[^0-9A-Z]/g, '').slice(0, 15)
}

export function gstError(value: string) {
  if (!value.trim()) return null
  if (!GST_PATTERN.test(gstText(value))) return 'GST number should be 15 characters'
  return null
}

export function pinDigits(value: string) {
  return value.replace(/\D/g, '').slice(0, 6)
}

export function pincodeError(value: string) {
  const digits = pinDigits(value)
  if (!digits) return null
  if (digits.length !== 6) return 'Enter valid 6 digit pin code'
  return null
}

export function aboutError(value: string) {
  const text = value.trim()
  if (!text) return null
  if (text.length < 30) return 'About us must be at least 30 characters.'
  if (text.length > 500) return 'About us can be at most 500 characters.'
  return null
}

export function nameError(value: string) {
  const text = value.trim()
  if (!text) return null
  if (!/^[A-Za-z]+(?: [A-Za-z]+)*$/.test(text)) return 'Only letters are allowed.'
  return null
}

export function parseAreaSelections(raw: string): AreaSelection[] {
  if (!raw.trim()) return []
  try {
    const parsed = JSON.parse(raw) as unknown
    if (!Array.isArray(parsed)) return []
    return parsed
      .map((item) => {
        const row = item as { state?: string; cities?: unknown }
        const state = String(row.state || '')
        const cities = Array.isArray(row.cities) ? row.cities.map((city) => String(city)).filter(Boolean).slice(0, 4) : []
        return state ? { state, cities } : null
      })
      .filter((item): item is AreaSelection => item !== null)
      .slice(0, 6)
  } catch {
    return []
  }
}

export function serializeAreaSelections(rows: AreaSelection[]) {
  return JSON.stringify(rows.filter((row) => row.state).slice(0, 6).map((row) => ({ state: row.state, cities: row.cities.slice(0, 4) })))
}

const TIME_CHOICES = Array.from({ length: 48 }, (_, index) => {
  const hour = Math.floor(index / 2)
  const minute = index % 2 === 0 ? '00' : '30'
  return `${String(hour).padStart(2, '0')}:${minute}`
})

export const TIME_OPTIONS = TIME_CHOICES.map((value) => ({ value, label: formatClock(value) }))

export function formatClock(value: string) {
  const [hourText, minute] = value.split(':')
  let hour = Number(hourText)
  const suffix = hour >= 12 ? 'PM' : 'AM'
  hour = hour % 12 || 12
  return `${hour}:${minute} ${suffix}`
}

function clockToValue(label: string) {
  const match = label.trim().match(/^(\d{1,2}):(\d{2})\s*(AM|PM)$/i)
  if (!match) return ''
  let hour = Number(match[1]) % 12
  if (match[3].toUpperCase() === 'PM') hour += 12
  return `${String(hour).padStart(2, '0')}:${match[2]}`
}

function defaultWeek(): DaySchedule[] {
  return WEEK_DAYS.map((day) => ({
    day,
    off: day === 'Sunday',
    open: '09:00',
    close: day === 'Saturday' ? '16:00' : '18:00',
  }))
}

export function parseSchedule(raw: string): DaySchedule[] {
  const week = defaultWeek()
  const text = raw.trim()
  if (!text) return week
  const entries: { days: string; hours: string }[] = []
  try {
    if (text.startsWith('[')) {
      const parsed = JSON.parse(text) as { days?: string; day?: string; hours?: string; time?: string }[]
      if (Array.isArray(parsed)) {
        for (const item of parsed) {
          const days = String(item.days || item.day || '').trim()
          const hours = String(item.hours || item.time || '').trim()
          if (days) entries.push({ days, hours })
        }
      }
    }
  } catch {
    /* use the line format below */
  }
  if (entries.length === 0) {
    for (const line of text.split(/\r?\n/)) {
      const parts = line.split(':')
      if (parts.length >= 2) entries.push({ days: parts[0].trim(), hours: parts.slice(1).join(':').trim() })
    }
  }
  if (entries.length === 0) return week
  const byDay = new Map(week.map((row) => [row.day, { ...row }]))
  for (const entry of entries) {
    const days = expandDays(entry.days)
    const closed = /weekly off|closed/i.test(entry.hours)
    const times = [...entry.hours.matchAll(/(\d{1,2}:\d{2}\s*(?:AM|PM))/gi)].map((match) => clockToValue(match[1]))
    for (const day of days) {
      const current = byDay.get(day)
      if (!current) continue
      current.off = closed
      if (!closed && times[0]) current.open = times[0]
      if (!closed && times[1]) current.close = times[1]
    }
  }
  return WEEK_DAYS.map((day) => byDay.get(day)!)
}

function expandDays(label: string) {
  const clean = label.replace(/\s+/g, ' ').trim()
  const range = clean.split(/\s+-\s+/)
  if (range.length === 2) {
    const start = WEEK_DAYS.indexOf(range[0] as (typeof WEEK_DAYS)[number])
    const end = WEEK_DAYS.indexOf(range[1] as (typeof WEEK_DAYS)[number])
    if (start >= 0 && end >= start) return WEEK_DAYS.slice(start, end + 1)
  }
  return WEEK_DAYS.includes(clean as (typeof WEEK_DAYS)[number]) ? [clean] : []
}

export function serializeSchedule(rows: DaySchedule[]) {
  return JSON.stringify(
    rows.map((row) => ({
      days: row.day,
      hours: row.off ? 'Weekly off' : `${formatClock(row.open)} - ${formatClock(row.close)}`,
    })),
  )
}

export const VIDEO_LINK_LIMIT = 20
export const VIDEO_LINK_PLACEHOLDER = 'Paste YouTube / Instagram / Facebook video link'
export const VIDEO_LINK_ERROR = 'Please enter valid video link'

export const UPI_PLACEHOLDER = 'Enter UPI ID (e.g. name@upi)'
export const UPI_ERROR = 'Invalid UPI ID format'

export function upiError(value: string) {
  const text = value.trim()
  if (!text) return null
  if (text.length > 200 || !/^[A-Za-z0-9][A-Za-z0-9._-]{1,255}@[A-Za-z][A-Za-z0-9]{1,63}$/.test(text)) return UPI_ERROR
  return null
}

export const SOCIAL_LINK_PLACEHOLDER = 'Paste your profile link'
export const SOCIAL_LINK_ERROR = 'Invalid link format'

export function socialLinkError(value: string) {
  const text = value.trim()
  if (!text) return null
  if (/\s/.test(text)) return SOCIAL_LINK_ERROR
  const withScheme = /^https?:\/\//i.test(text) ? text : `https://${text}`
  try {
    const url = new URL(withScheme)
    if (url.protocol !== 'http:' && url.protocol !== 'https:') return SOCIAL_LINK_ERROR
    if (!url.hostname.includes('.')) return SOCIAL_LINK_ERROR
    return null
  } catch {
    return SOCIAL_LINK_ERROR
  }
}

export function normalizeSocialLink(value: string) {
  const text = value.trim()
  if (!text || socialLinkError(text)) return text
  return /^https?:\/\//i.test(text) ? text : `https://${text}`
}

export function videoLinkError(value: string) {
  const text = value.trim()
  if (!text) return null
  if (text.length > 500) return VIDEO_LINK_ERROR
  try {
    const url = new URL(text)
    if (url.protocol !== 'http:' && url.protocol !== 'https:') return VIDEO_LINK_ERROR
    if (!url.hostname.includes('.')) return VIDEO_LINK_ERROR
    return null
  } catch {
    return VIDEO_LINK_ERROR
  }
}
