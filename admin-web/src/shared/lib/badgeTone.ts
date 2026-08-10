/** Map legacy tone labels to shadcn Badge variants. */
export function badgeVariantFromTone(
  tone?: string | null,
): 'default' | 'secondary' | 'destructive' | 'outline' {
  switch (tone) {
    case 'ok':
      return 'default'
    case 'danger':
      return 'destructive'
    case 'warn':
      return 'secondary'
    default:
      return 'outline'
  }
}
