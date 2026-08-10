import { apiSend } from '@/shared/api/client'

export const INDIAN_STATES = [
  'Andhra Pradesh',
  'Arunachal Pradesh',
  'Assam',
  'Bihar',
  'Chhattisgarh',
  'Goa',
  'Gujarat',
  'Haryana',
  'Himachal Pradesh',
  'Jharkhand',
  'Karnataka',
  'Kerala',
  'Madhya Pradesh',
  'Maharashtra',
  'Manipur',
  'Meghalaya',
  'Mizoram',
  'Nagaland',
  'Odisha',
  'Punjab',
  'Rajasthan',
  'Sikkim',
  'Tamil Nadu',
  'Telangana',
  'Tripura',
  'Uttar Pradesh',
  'Uttarakhand',
  'West Bengal',
  'Andaman and Nicobar Islands',
  'Chandigarh',
  'Dadra and Nagar Haveli and Daman and Diu',
  'Delhi',
  'Jammu and Kashmir',
  'Ladakh',
  'Lakshadweep',
  'Puducherry',
] as const

export type RegistrationKind = 'customer' | 'franchisee'

export function startRegistration(kind: RegistrationKind, body: Record<string, unknown>) {
  return apiSend<{ email: string; message: string }>('post', `/api/v1/registration/${kind}`, body)
}

export function verifyRegistrationOtp(kind: RegistrationKind, email: string, otp: string) {
  return apiSend<{
    userId: number
    email: string
    name: string
    role: string
    referralCode: string
  }>('post', `/api/v1/registration/${kind}/verify-otp`, { email, otp })
}

export function resendRegistrationOtp(kind: RegistrationKind, email: string) {
  return apiSend('post', `/api/v1/registration/${kind}/resend-otp`, { email })
}
