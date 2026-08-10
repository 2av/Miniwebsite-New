import { Link } from 'react-router-dom'
import { Button, Card, Input } from '@/shared/ui/primitives'

export function LoginPage() {
  return (
    <div className="flex min-h-full items-center justify-center p-6">
      <Card className="w-full max-w-md p-8">
        <div className="text-xs tracking-[0.2em] text-rose-600 uppercase">MiniWebsite Admin</div>
        <h1 className="mt-2 font-[family-name:var(--font-display)] text-3xl font-semibold">Sign in</h1>
        <p className="mt-2 text-sm text-slate-500">
          Auth is temporarily open on the API. This screen is ready to wire to JWT login later.
        </p>
        <form
          className="mt-6 space-y-3"
          onSubmit={(e) => {
            e.preventDefault()
            window.location.href = '/'
          }}
        >
          <Input type="email" placeholder="Admin email" required />
          <Input type="password" placeholder="Password" required />
          <Button type="submit" className="w-full">
            Continue to dashboard
          </Button>
        </form>
        <Link to="/" className="mt-4 inline-block text-sm text-rose-600 hover:underline">
          Skip for now →
        </Link>
      </Card>
    </div>
  )
}
