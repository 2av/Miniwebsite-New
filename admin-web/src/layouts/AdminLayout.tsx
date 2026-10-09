import { NavLink, Outlet, useLocation, useNavigate } from 'react-router-dom'
import {
  LayoutDashboard,
  Users,
  Globe2,
  LogOut,
  Menu,
  CreditCard,
  Delete as DeleteIcon,
  ChevronDown,
  UserRound,
  Briefcase,
  UsersRound,
  Share2,
  Tag,
  BatteryCharging,
  CircleHelp,
  Scale,
  Shield,
  Handshake,
  Store,
  ScrollText,
  ClipboardList,
  FolderTree,
  BookOpen,
  Folders,
  Images,
  Wrench,
  Megaphone,
  Sparkles,
  Monitor,
  FileSignature,
  FileText,
  Mail,
  Network,
  ShieldCheck,
  Search,
  X,
  BadgeIndianRupee,
} from 'lucide-react'
import { useMemo, useState, type ComponentType } from 'react'
import { cn } from '@/lib/utils'
import { Input } from '@/components/ui/input'
import {
  SALES_CHANNEL_PARTNER_BASE,
  SALES_CHANNEL_PARTNERS,
} from '@/features/sales-channel-partners/partners'

type NavIcon = ComponentType<{ size?: number; className?: string }>

type NavLinkItem = {
  kind: 'link'
  to: string
  label: string
  icon: NavIcon
  end?: boolean
}

type NavGroupItem = {
  kind: 'group'
  id: string
  label: string
  icon: NavIcon
  matchPrefixes: string[]
  children: NavNode[]
}

type NavNode = NavLinkItem | NavGroupItem

type FlatNavItem = {
  to: string
  label: string
  breadcrumb: string
  icon: NavIcon
  end?: boolean
}

function flattenNavLinks(nodes: NavNode[], ancestors: string[] = []): FlatNavItem[] {
  const items: FlatNavItem[] = []
  for (const node of nodes) {
    if (node.kind === 'link') {
      items.push({
        to: node.to,
        label: node.label,
        breadcrumb: ancestors.join(' › '),
        icon: node.icon,
        end: node.end,
      })
    } else {
      items.push(...flattenNavLinks(node.children, [...ancestors, node.label]))
    }
  }
  return items
}

const nav: NavNode[] = [
  { kind: 'link', to: '/', label: 'Dashboard', icon: LayoutDashboard, end: true },
  { kind: 'link', to: '/sb-user-details', label: 'SB User Details', icon: Users },
  { kind: 'link', to: '/sb-profile-details', label: 'SB Profile Details', icon: Globe2 },
  
  {
    kind: 'group',
    id: 'sales-channel-partner',
    label: 'Sales Channel Partner',
    icon: Network,
    matchPrefixes: [SALES_CHANNEL_PARTNER_BASE],
    children: SALES_CHANNEL_PARTNERS.map((p) => ({
      kind: 'link' as const,
      to: `${SALES_CHANNEL_PARTNER_BASE}/${p.slug}`,
      label: p.label,
      icon: p.icon,
    })),
  },
  { kind: 'link', to: '/all-orders', label: 'All Orders', icon: CreditCard },
  {
    kind: 'group',
    id: 'marketing-management',
    label: 'Marketing Management',
    icon: Megaphone,
    matchPrefixes: ['/grow-with-mw', '/kit-management', '/documentation'],
    children: [
      {
        kind: 'group',
        id: 'grow-with-mw',
        label: 'Grow with SB',
        icon: BookOpen,
        matchPrefixes: ['/grow-with-mw', '/documentation'],
        children: [
          { kind: 'link', to: '/grow-with-mw', label: 'Pages', icon: BookOpen, end: true },
          { kind: 'link', to: '/grow-with-mw/sections', label: 'Sections', icon: Folders },
          { kind: 'link', to: '/grow-with-mw/media', label: 'Media', icon: Images },
        ],
      },
      {
        kind: 'group',
        id: 'kit-management',
        label: 'Kit Management',
        icon: Wrench,
        matchPrefixes: ['/kit-management'],
        children: [
          { kind: 'link', to: '/kit-management/sales', label: 'SB Sales Kit', icon: Briefcase },
          { kind: 'link', to: '/kit-management/marketing', label: 'Creator Kit', icon: Megaphone },
          { kind: 'link', to: '/kit-management/franchise-sales', label: 'Franchise Sales Kit', icon: Handshake },
        ],
      },
    ],
  },
  {
    kind: 'group',
    id: 'content-management',
    label: 'Content Management',
    icon: FileText,
    matchPrefixes: ['/manage-content', '/manage-faqs', '/manage-faq', '/email-templates', '/manage-email-templates'],
    children: [
      { kind: 'link', to: '/manage-content/terms-conditions', label: 'Terms & Conditions', icon: Scale },
      { kind: 'link', to: '/manage-content/privacy-policy', label: 'Privacy Policy', icon: Shield },
      { kind: 'link', to: '/manage-faqs', label: 'FAQ Management', icon: CircleHelp },
      { kind: 'link', to: '/email-templates', label: 'Email Templates', icon: Mail },
      {
        kind: 'group',
        id: 'sales-channel-agreement',
        label: 'Sales Channel Agreement',
        icon: FileSignature,
        matchPrefixes: [
          '/manage-content/franchisee-agreement',
          '/manage-content/mw-full-franchise-agreement',
          '/manage-content/mw-franchisee-operation-policy',
          '/manage-content/franchisee-distributer',
          '/manage-content/influencer-partner-agreement',
          '/manage-content/digital-marketing-partner-agreement',
        ],
        children: [
          { kind: 'link', to: '/manage-content/franchisee-agreement', label: 'Franchise Agreement', icon: Handshake },
          {
            kind: 'link',
            to: '/manage-content/mw-full-franchise-agreement',
            label: 'Full Franchise Partner Agreement',
            icon: ScrollText,
          },
          {
            kind: 'link',
            to: '/manage-content/mw-franchisee-operation-policy',
            label: 'Franchise Operation Policy',
            icon: ClipboardList,
          },
          {
            kind: 'link',
            to: '/manage-content/franchisee-distributer',
            label: 'Freelance Sales Partner Agreement',
            icon: Store,
          },
          {
            kind: 'link',
            to: '/manage-content/influencer-partner-agreement',
            label: 'Influencer Partner Agreement',
            icon: Sparkles,
          },
          {
            kind: 'link',
            to: '/manage-content/digital-marketing-partner-agreement',
            label: 'Digital Marketing Partner Agreement',
            icon: Monitor,
          },
        ],
      },
    ],
  },
  {
    kind: 'group',
    id: 'user-deletion',
    label: 'User Deletion',
    icon: DeleteIcon,
    matchPrefixes: ['/user-deletion'],
    children: [
      { kind: 'link', to: '/user-deletion/customer', label: 'Customer', icon: UserRound },
      { kind: 'link', to: '/user-deletion/franchise', label: 'Franchise Partner', icon: Briefcase },
      { kind: 'link', to: '/user-deletion/team', label: 'Team', icon: UsersRound },
    ],
  },
  { kind: 'link', to: '/manage-referrals', label: 'Manage Referrals', icon: Share2 },
  { kind: 'link', to: '/manage-deals', label: 'Manage Deals', icon: Tag },
  { kind: 'link', to: '/manage-pricing', label: 'Manage Pricing', icon: BadgeIndianRupee },
  { kind: 'link', to: '/recharge-wallet', label: 'Recharge Wallet', icon: BatteryCharging },
  { kind: 'link', to: '/manage-categories', label: 'Category Management', icon: FolderTree },
  { kind: 'link', to: '/role-management', label: 'Role Management', icon: Shield },
  { kind: 'link', to: '/role-access-settings', label: 'Role Access Settings', icon: ShieldCheck },
]

function collectGroupIds(nodes: NavNode[]): string[] {
  const ids: string[] = []
  const walk = (items: NavNode[]) => {
    for (const node of items) {
      if (node.kind === 'group') {
        ids.push(node.id)
        walk(node.children)
      }
    }
  }
  walk(nodes)
  return ids
}

function findGroup(nodes: NavNode[], id: string): NavGroupItem | null {
  for (const node of nodes) {
    if (node.kind === 'group') {
      if (node.id === id) return node
      const nested = findGroup(node.children, id)
      if (nested) return nested
    }
  }
  return null
}

function isGroupActive(group: NavGroupItem, pathname: string) {
  return group.matchPrefixes.some((p) => pathname.startsWith(p))
}

function isLinkActive(to: string, pathname: string, end?: boolean) {
  if (end) return pathname === to
  return pathname === to || pathname.startsWith(`${to}/`)
}

function NavGroup({
  group,
  depth,
  expanded,
  onToggle,
  pathname,
  onNavigate,
  expandedSections,
  toggleSection,
}: {
  group: NavGroupItem
  depth: number
  expanded: boolean
  onToggle: () => void
  pathname: string
  onNavigate: () => void
  expandedSections: Record<string, boolean>
  toggleSection: (id: string) => void
}) {
  const active = isGroupActive(group, pathname)
  const topLevel = depth === 0

  return (
    <div className="space-y-1">
      <button
        type="button"
        onClick={onToggle}
        className={cn(
          'flex w-full items-center gap-3 rounded-xl px-3 py-2.5 text-sm font-medium transition',
          topLevel
            ? active
              ? 'bg-rose-500/20 text-white ring-1 ring-rose-400/30'
              : 'text-slate-300 hover:bg-white/5 hover:text-white'
            : active
              ? 'bg-white/10 font-medium text-white'
              : 'text-slate-400 hover:bg-white/5 hover:text-white',
        )}
      >
        <group.icon size={topLevel ? 18 : 15} />
        <span className="flex-1 text-left">{group.label}</span>
        <ChevronDown size={16} className={cn('shrink-0 transition', expanded && 'rotate-180')} />
      </button>
      {expanded && (
        <div className="ml-3 space-y-1 border-l border-white/10 pl-2">
          {group.children.map((child) => (
            <NavNodeView
              key={child.kind === 'link' ? child.to : child.id}
              node={child}
              depth={depth + 1}
              expandedSections={expandedSections}
              toggleSection={toggleSection}
              pathname={pathname}
              onNavigate={onNavigate}
            />
          ))}
        </div>
      )}
    </div>
  )
}

function NavNodeView({
  node,
  depth,
  expandedSections,
  toggleSection,
  pathname,
  onNavigate,
}: {
  node: NavNode
  depth: number
  expandedSections: Record<string, boolean>
  toggleSection: (id: string) => void
  pathname: string
  onNavigate: () => void
}) {
  if (node.kind === 'link') {
    const active = isLinkActive(node.to, pathname, node.end)
    const topLevel = depth === 0

    if (topLevel) {
      return (
        <NavLink
          to={node.to}
          end={node.end}
          onClick={onNavigate}
          className={({ isActive }) =>
            cn(
              'flex items-center gap-3 rounded-xl px-3 py-2.5 text-sm font-medium transition',
              isActive
                ? 'bg-rose-500/20 text-white ring-1 ring-rose-400/30'
                : 'text-slate-300 hover:bg-white/5 hover:text-white',
            )
          }
        >
          <node.icon size={18} />
          <span className="flex-1">{node.label}</span>
        </NavLink>
      )
    }

    return (
      <NavLink
        to={node.to}
        end={node.end}
        onClick={onNavigate}
        className={cn(
          'flex items-center gap-2 rounded-lg px-3 py-2 text-sm transition',
          active ? 'bg-white/10 font-medium text-white' : 'text-slate-400 hover:bg-white/5 hover:text-white',
        )}
      >
        <node.icon size={15} />
        <span className="leading-snug">{node.label}</span>
      </NavLink>
    )
  }

  const groupExpanded = Boolean(expandedSections[node.id]) || isGroupActive(node, pathname)
  return (
    <NavGroup
      group={node}
      depth={depth}
      expanded={groupExpanded}
      onToggle={() => toggleSection(node.id)}
      pathname={pathname}
      onNavigate={onNavigate}
      expandedSections={expandedSections}
      toggleSection={toggleSection}
    />
  )
}

export function AdminLayout() {
  const [open, setOpen] = useState(false)
  const [menuSearch, setMenuSearch] = useState('')
  const location = useLocation()
  const navigate = useNavigate()
  const groupIds = useMemo(() => collectGroupIds(nav), [])
  const flatNav = useMemo(() => flattenNavLinks(nav), [])
  const searchQuery = menuSearch.trim().toLowerCase()

  const searchResults = useMemo(() => {
    if (!searchQuery) return []
    return flatNav.filter((item) => {
      const haystack = `${item.breadcrumb} ${item.label} ${item.to}`.toLowerCase()
      return haystack.includes(searchQuery)
    })
  }, [flatNav, searchQuery])

  const [expandedSections, setExpandedSections] = useState<Record<string, boolean>>(() => {
    const initial: Record<string, boolean> = {}
    for (const id of groupIds) {
      const group = findGroup(nav, id)
      if (group && isGroupActive(group, location.pathname)) initial[id] = true
    }
    return initial
  })

  const toggleSection = (id: string) => {
    setExpandedSections((prev) => ({ ...prev, [id]: !prev[id] }))
  }

  const closeMobile = () => setOpen(false)

  const goToMenuItem = (to: string) => {
    navigate(to)
    setMenuSearch('')
    closeMobile()
  }

  return (
    <div className="flex h-dvh w-full max-w-[100vw] overflow-hidden">
      <aside
        className={cn(
          'fixed inset-y-0 left-0 z-40 flex w-72 shrink-0 flex-col bg-[linear-gradient(165deg,#0f172a_0%,#1e293b_55%,#0f172a_100%)] text-slate-100 transition md:static md:translate-x-0',
          open ? 'translate-x-0' : '-translate-x-full md:translate-x-0',
        )}
      >
        <div className="border-b border-white/10 px-5 py-6">
          <div className="text-xs tracking-[0.2em] text-rose-300 uppercase">MiniWebsite</div>
        </div>

        <div className="border-b border-white/10 px-3 py-3">
          <div className="relative">
            <Search size={16} className="pointer-events-none absolute top-1/2 left-3 -translate-y-1/2 text-slate-400" />
            <Input
              value={menuSearch}
              onChange={(e) => setMenuSearch(e.target.value)}
              placeholder="Search menu…"
              className="h-9 border-white/10 bg-white/5 pr-9 pl-9 text-sm text-white placeholder:text-slate-500 focus-visible:border-rose-400/40 focus-visible:ring-rose-400/20"
            />
            {menuSearch && (
              <button
                type="button"
                onClick={() => setMenuSearch('')}
                className="absolute top-1/2 right-2 -translate-y-1/2 rounded p-1 text-slate-400 hover:text-white"
                aria-label="Clear search"
              >
                <X size={14} />
              </button>
            )}
          </div>
        </div>

        <nav className="sidebar-nav-scroll flex-1 space-y-1 overflow-y-auto px-3 py-4">
          {searchQuery ? (
            searchResults.length > 0 ? (
              <div className="space-y-1">
                {searchResults.map((item) => (
                  <button
                    key={item.to}
                    type="button"
                    onClick={() => goToMenuItem(item.to)}
                    className={cn(
                      'flex w-full items-start gap-3 rounded-xl px-3 py-2.5 text-left text-sm transition',
                      isLinkActive(item.to, location.pathname, item.end)
                        ? 'bg-rose-500/20 text-white ring-1 ring-rose-400/30'
                        : 'text-slate-300 hover:bg-white/5 hover:text-white',
                    )}
                  >
                    <item.icon size={18} className="mt-0.5 shrink-0" />
                    <span className="min-w-0">
                      <span className="block font-medium">{item.label}</span>
                      {item.breadcrumb && (
                        <span className="mt-0.5 block truncate text-xs text-slate-400">{item.breadcrumb}</span>
                      )}
                    </span>
                  </button>
                ))}
              </div>
            ) : (
              <p className="px-3 py-6 text-center text-sm text-slate-400">No menu items found.</p>
            )
          ) : (
            nav.map((node) => (
              <NavNodeView
                key={node.kind === 'link' ? node.to : node.id}
                node={node}
                depth={0}
                expandedSections={expandedSections}
                toggleSection={toggleSection}
                pathname={location.pathname}
                onNavigate={closeMobile}
              />
            ))
          )}
        </nav>

        <div className="border-t border-white/10 p-4">
          <button
            type="button"
            className="flex w-full items-center gap-2 rounded-xl px-3 py-2 text-sm text-slate-300 hover:bg-white/5"
          >
            <LogOut size={16} />
            Sign out (soon)
          </button>
        </div>
      </aside>

      {open && (
        <button type="button" className="fixed inset-0 z-30 bg-black/40 md:hidden" onClick={() => setOpen(false)} />
      )}

      <div className="flex h-full min-w-0 flex-1 flex-col overflow-hidden">
        <header className="z-20 flex shrink-0 items-center justify-between gap-3 border-b border-slate-200/70 bg-white/75 px-4 py-3 backdrop-blur md:px-6">
          <button type="button" className="rounded-lg p-2 text-slate-600 hover:bg-slate-100 md:hidden" onClick={() => setOpen(true)}>
            <Menu size={20} />
          </button>
        </header>
        <main className="flex min-h-0 min-w-0 flex-1 flex-col overflow-hidden p-4 md:p-6">
          <div className="flex min-h-0 min-w-0 flex-1 flex-col">
            <Outlet />
          </div>
        </main>
      </div>
    </div>
  )
}
