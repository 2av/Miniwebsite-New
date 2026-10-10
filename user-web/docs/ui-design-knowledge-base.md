# ShareBizz UI design knowledge base

Source: `user-web/docs/03_SB_UI_Design Knowledge Base.pdf`.

Visual boards (open these when building a screen):

| Board | File |
| --- | --- |
| Input controls | `user-web/docs/design-boards/01-input-controls.jpg` |
| Navigation | `user-web/docs/design-boards/02-navigation.jpg` |
| Informational components | `user-web/docs/design-boards/03-informational-components.jpg` |
| Containers | `user-web/docs/design-boards/04-containers.jpg` |
| Landing page | `user-web/docs/design-boards/05-landing-page.jpg` |
| Dashboard | `user-web/docs/design-boards/06-dashboard.jpg` |

Use this file for every ShareBizz **marketing page** and **logged-in portal** (dashboard, website builder, account screens). Do **not** apply it to customer Digital Business Profile themes (`user-web/public/theme/**`, public card templates). Those themes stay independent of this brand system.

Stack: **Tailwind CSS** and **Poppins**.

## Brand color

| Hex | Name | Use |
| --- | --- | --- |
| `#6C2EFF` | Primary purple | Brand, primary actions, active nav, focus rings |
| `#FF38BD` | Hot pink | Secondary accent, gradient stop |
| `#FF8A00` | Orange | Gradient end, highlights |
| `#007BFF` | Blue | Information, links |
| `#00C853` | Green | Success, active status |
| `#F3F0FF` | Active wash | Selected rows, hover, soft purple surfaces |
| `#1F2937` | Dark text | Headings, body, arrow icons |
| `#F8FAFC` | Light background | Page and dashboard background |
| `#FFFFFF` | Surface | Cards, nav, inputs |
| `#6B7280` | Icon | Default icons |
| `#687280` | Nav icon | Sidebar icons when idle |
| `#E5E7EB` | Border | Cards, tables, dividers |
| `#D1D5DB` | Control border | Inputs and unchecked controls |
| `#64748B` | Secondary text | Captions, helper text |
| `#000080` | Overlay | Modal and drawer scrim (use at low opacity) |
| `#000014` | Shadow | Card shadow color |

Landing mock also uses `#1A1A1A` for hero text and `#F8F9FF` for the lightest background.

Primary button fill is a left-to-right gradient: `#6C2EFF` → `#FF38BD` → `#FF8A00`.

## Typography

Font: **Poppins**.

| Role | Marketing | Portal | Weight |
| --- | --- | --- | --- |
| H1 | 48–64 px | 28–32 px | Bold |
| H2 | 32–40 px | 20–24 px | Bold / semibold |
| H3 | 20–24 px | 16–18 px | Semibold |
| Body | 16–18 px | 14–16 px | Regular |
| Small | 14 px | 12 px | Regular |

## Shape and elevation

- Control height: **44 px**. Radius **9–12 px**.
- Cards: radius **12 px**, border `#E5E7EB`, padding **16–24 px**, shadow using `#000014` at low opacity.
- Focus: border and ring `#6C2EFF`.
- Keep contrast readable, spacing even, and layouts usable on phone and desktop.

## Inputs and actions

- **Primary button:** height 44 px, padding 16–24 px, white label, purple–pink–orange gradient. Example label: Add Item.
- **Secondary button:** height 44 px, white surface, purple border and label. Example: Save Changes.
- **Text button:** no fill. Example: Cancel.
- **Danger:** red fill, trash icon. Example: Delete.
- **Checkbox:** 20 px. Checked `#6C2EFF`. Unchecked white with border `#D1D5DB`.
- **Radio:** 20 px. Selected `#6C2EFF`. Unselected border `#D1D5DB`.
- **Toggle:** 44 × 24 px. On `#6C2EFF`. Off `#D1D5DB`.
- **Text field:** height 44 px, radius 12 px, border `#D1D5DB`, focus `#6C2EFF`. Password fields include a visibility icon.
- **Select:** same as text field. Open menu highlights the current option with `#F3F0FF` and a purple label.
- **Slider:** track 4–6 px, knob 20 px, fill `#6C2EFF`.
- **Stepper:** bordered group, minus and plus around the value.
- **Date:** 44 px field plus calendar. Selected day `#6C2EFF`.
- **File upload:** drop zone about 120 px tall, border `#D1D5DB`, icon accent `#6C2EFF`. Show file name, size, and remove.

## Navigation

- **Desktop navbar:** height 64 px, white, text `#1F2937`, active `#6C2EFF`.
- **Mobile tab bar:** height 56 px, white, active icon tile `#6C2EFF`.
- **Sidebar:** expanded **280 px**, collapsed **72 px**, white. Active item is the brand gradient with white text and icon. Idle text `#1F2937`, idle icon `#687280`, hover wash `#F3F0FF`.
- **Breadcrumbs:** height 32 px. Current crumb `#6C2EFF`. Separator `#D1D5DB`.
- **Pagination:** height 40 px. Current page `#6C2EFF` with white text. Border `#D1D5DB`. Hover `#F3F0FF`.
- **Search:** height 44 px, border `#D1D5DB`, icon `#687280`, focus `#6C2EFF`.

## Feedback

- **Badge:** height 24–28 px, padding 8–12 px. Success `#00C853`, warning `#FF8A00`, danger `#FF8A00` / pink `#FF38BD`, info `#007BFF`, neutral surface `#F1F5F9`, text `#1F2937`.
- **Toast:** padding 16 px. Success surface `#ECFDF5` / text `#00C853`. Info `#EFF6FF` / `#007BFF`. Warning `#FFF7ED` / `#FF8A00`. Error `#FEF2F2` / `#FF38BD`.
- **Progress:** height 6–8 px, fill `#6C2EFF`, track `#E5E7EB`.
- **Tooltip:** 14 px, background `#1F2937`, text white.
- **Skeleton:** background `#F8FAFC`, bone `#E5E7EB`.

## Containers

- **Card:** width flexible, about 280–320 px in grids. White, radius 12 px, border `#E5E7EB`.
- **Modal:** width 480–560 px, radius 12 px, padding 24–32 px, overlay `#000080` at low opacity, border `#E5E7EB`.
- **Accordion:** full width, white, border `#E5E7EB`. Open section wash `#F8FAFC`. Icon `#687280`.
- **Drawer:** width 280–320 px from the side. Same overlay as the modal.
- **Table:** white rows, header muted, status as green/red text, row actions as icon buttons, pagination under the table.

## Marketing landing

White page on a soft purple, pink, and orange wash.

1. Header: logo, text links, gradient **Get Started**.
2. Hero: eyebrow “Made in India. Made for Indian businesses.” Headline with purple and pink emphasis. Short supporting line. Gradient primary button and white secondary button. Proof row (counts, satisfaction, made in India).
3. Feature grid: “Everything you need to grow your business,” circular icon tiles in purple, pink, orange, and blue.
4. Phone layout stacks the same story: headline, buttons, feature tiles, made-in-India mark.

Keep real ShareBizz copy, plans, demos, and routes. Use the mock for layout and color, not as replacement product data.

## Logged-in dashboard

- Top bar: logo, menu button, search, alerts, help, profile (name and role).
- Left sidebar: portal or website-builder items. The current item uses the brand gradient.
- Content sits on `#F8FAFC` inside a white card.
- Page title, short helper text, gradient **Add** action on the right.
- Stat tiles in a row, each with its own soft tint (purple, pink, orange, blue, green).
- Data table with image, status toggle, and edit/delete actions.
- Phone: stacked stats, card list instead of the wide table, bottom tab bar.

## Implementation notes

- Put new UI in Tailwind utilities and the shared classes in `user-web/src/styles/index.css` (`.sb-btn-primary`, `.sb-btn-secondary`).
- Brand tokens live on `:root` in that file and on the portal tokens in `user-web/src/styles/portal-shell.css`.
- Do not restyle `user-web/public/theme/**` or the public card renderer to this system.
