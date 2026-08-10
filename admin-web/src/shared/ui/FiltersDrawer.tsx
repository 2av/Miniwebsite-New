import { SlidersHorizontal } from 'lucide-react'
import type { ReactNode } from 'react'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import {
  Drawer,
  DrawerClose,
  DrawerContent,
  DrawerDescription,
  DrawerFooter,
  DrawerHeader,
  DrawerTitle,
} from '@/components/ui/drawer'

export function FiltersButton({
  activeCount = 0,
  onClick,
}: {
  activeCount?: number
  onClick: () => void
}) {
  return (
    <Button type="button" variant="outline" onClick={onClick}>
      <SlidersHorizontal size={16} />
      Filters
      {activeCount > 0 ? (
        <Badge variant="secondary" className="ml-0.5 h-5 min-w-5 px-1.5">
          {activeCount}
        </Badge>
      ) : null}
    </Button>
  )
}

export function FiltersDrawer({
  open,
  onOpenChange,
  title = 'Filters',
  description = 'Narrow the list results',
  onClear,
  children,
}: {
  open: boolean
  onOpenChange: (open: boolean) => void
  title?: string
  description?: string
  onClear?: () => void
  children: ReactNode
}) {
  return (
    <Drawer open={open} onOpenChange={onOpenChange} direction="right">
      <DrawerContent className="data-[vaul-drawer-direction=right]:sm:max-w-md">
        <DrawerHeader>
          <DrawerTitle>{title}</DrawerTitle>
          <DrawerDescription>{description}</DrawerDescription>
        </DrawerHeader>
        <div className="flex flex-1 flex-col gap-4 overflow-y-auto px-4 pb-2">{children}</div>
        <DrawerFooter>
          {onClear ? (
            <Button type="button" variant="outline" onClick={onClear}>
              Clear filters
            </Button>
          ) : null}
          <DrawerClose asChild>
            <Button type="button">Done</Button>
          </DrawerClose>
        </DrawerFooter>
      </DrawerContent>
    </Drawer>
  )
}
