// Shared inline SVG icons (no external dependencies).

export function LogoMark({ size = 22 }: { size?: number }) {
  return (
    <svg viewBox="0 0 24 24" width={size} height={size} aria-hidden="true">
      <path d="M12 2 2 7l10 5 10-5-10-5Z" fill="currentColor" />
      <path
        d="M2 12l10 5 10-5"
        fill="none"
        stroke="currentColor"
        strokeWidth="2"
        strokeLinecap="round"
        strokeLinejoin="round"
        opacity="0.6"
      />
      <path
        d="M2 17l10 5 10-5"
        fill="none"
        stroke="currentColor"
        strokeWidth="2"
        strokeLinecap="round"
        strokeLinejoin="round"
        opacity="0.35"
      />
    </svg>
  )
}

export function MailIcon() {
  return (
    <svg viewBox="0 0 24 24" width="18" height="18" aria-hidden="true">
      <rect
        x="3"
        y="5"
        width="18"
        height="14"
        rx="2.5"
        fill="none"
        stroke="currentColor"
        strokeWidth="1.7"
      />
      <path
        d="m4 7 8 5.5L20 7"
        fill="none"
        stroke="currentColor"
        strokeWidth="1.7"
        strokeLinecap="round"
        strokeLinejoin="round"
      />
    </svg>
  )
}

export function LockIcon() {
  return (
    <svg viewBox="0 0 24 24" width="18" height="18" aria-hidden="true">
      <rect
        x="4.5"
        y="10.5"
        width="15"
        height="10"
        rx="2.5"
        fill="none"
        stroke="currentColor"
        strokeWidth="1.7"
      />
      <path
        d="M8 10.5V8a4 4 0 0 1 8 0v2.5"
        fill="none"
        stroke="currentColor"
        strokeWidth="1.7"
        strokeLinecap="round"
      />
    </svg>
  )
}

export function EyeIcon({ off }: { off: boolean }) {
  return (
    <svg viewBox="0 0 24 24" width="18" height="18" aria-hidden="true">
      <path
        d="M2 12s3.5-7 10-7 10 7 10 7-3.5 7-10 7-10-7-10-7Z"
        fill="none"
        stroke="currentColor"
        strokeWidth="1.7"
        strokeLinecap="round"
        strokeLinejoin="round"
      />
      <circle cx="12" cy="12" r="3" fill="none" stroke="currentColor" strokeWidth="1.7" />
      {off && (
        <line
          x1="4"
          y1="4"
          x2="20"
          y2="20"
          stroke="currentColor"
          strokeWidth="1.7"
          strokeLinecap="round"
        />
      )}
    </svg>
  )
}

export function CheckIcon() {
  return (
    <svg viewBox="0 0 24 24" width="13" height="13" aria-hidden="true">
      <path
        d="m5 13 4 4L19 7"
        fill="none"
        stroke="currentColor"
        strokeWidth="2.6"
        strokeLinecap="round"
        strokeLinejoin="round"
      />
    </svg>
  )
}

export function AlertIcon() {
  return (
    <svg viewBox="0 0 24 24" width="17" height="17" aria-hidden="true">
      <circle cx="12" cy="12" r="10" fill="currentColor" opacity="0.14" />
      <path
        d="M12 7v6m0 4h.01"
        fill="none"
        stroke="currentColor"
        strokeWidth="2"
        strokeLinecap="round"
      />
    </svg>
  )
}

export function Spinner() {
  return <span className="spinner" aria-hidden="true" />
}

/* ---------- Navigation / dashboard icons ---------- */
/* All share a 24x24 viewBox and inherit color via currentColor. */

type IconProps = { size?: number }

function base(size = 20) {
  return { viewBox: '0 0 24 24', width: size, height: size, 'aria-hidden': true as const }
}

const strokeProps = {
  fill: 'none',
  stroke: 'currentColor',
  strokeWidth: 1.7,
  strokeLinecap: 'round' as const,
  strokeLinejoin: 'round' as const,
}

export function GridIcon({ size }: IconProps) {
  return (
    <svg {...base(size)}>
      <rect x="3" y="3" width="7" height="7" rx="1.5" {...strokeProps} />
      <rect x="14" y="3" width="7" height="7" rx="1.5" {...strokeProps} />
      <rect x="14" y="14" width="7" height="7" rx="1.5" {...strokeProps} />
      <rect x="3" y="14" width="7" height="7" rx="1.5" {...strokeProps} />
    </svg>
  )
}

export function UsersIcon({ size }: IconProps) {
  return (
    <svg {...base(size)}>
      <circle cx="9" cy="8" r="3.2" {...strokeProps} />
      <path d="M3.5 19a5.5 5.5 0 0 1 11 0" {...strokeProps} />
      <path d="M16 5.2a3.2 3.2 0 0 1 0 5.6M16.5 19a5.5 5.5 0 0 0-1.8-4.1" {...strokeProps} />
    </svg>
  )
}

export function ProvidersIcon({ size }: IconProps) {
  return (
    <svg {...base(size)}>
      <circle cx="12" cy="8" r="3.4" {...strokeProps} />
      <path d="M5.5 20a6.5 6.5 0 0 1 13 0" {...strokeProps} />
      <path d="m12 2 1.3 2 2 .3-1.4 1.5.3 2.1-1.9-1-1.9 1 .3-2.1L9 4.3l2-.3z" {...strokeProps} />
    </svg>
  )
}

export function CalendarIcon({ size }: IconProps) {
  return (
    <svg {...base(size)}>
      <rect x="3.5" y="4.5" width="17" height="16" rx="2.5" {...strokeProps} />
      <path d="M3.5 9h17M8 3v3M16 3v3" {...strokeProps} />
    </svg>
  )
}

export function CardIcon({ size }: IconProps) {
  return (
    <svg {...base(size)}>
      <rect x="2.5" y="5.5" width="19" height="13" rx="2.5" {...strokeProps} />
      <path d="M2.5 10h19" {...strokeProps} />
    </svg>
  )
}

export function WrenchIcon({ size }: IconProps) {
  return (
    <svg {...base(size)}>
      <path
        d="M14.5 6.5a3.5 3.5 0 0 1 4.6 4.5L21 13l-2 2-2-1.9a3.5 3.5 0 0 1-4.5-4.6L4.5 16.5 7.5 19.5"
        {...strokeProps}
      />
    </svg>
  )
}

export function StarIcon({ size }: IconProps) {
  return (
    <svg {...base(size)}>
      <path
        d="m12 3.5 2.6 5.3 5.9.8-4.3 4.1 1 5.8L12 17l-5.2 2.8 1-5.8-4.3-4.1 5.9-.8z"
        {...strokeProps}
      />
    </svg>
  )
}

export function GearIcon({ size }: IconProps) {
  return (
    <svg {...base(size)}>
      <circle cx="12" cy="12" r="3" {...strokeProps} />
      <path
        d="M12 2.5v2M12 19.5v2M4.2 4.2l1.4 1.4M18.4 18.4l1.4 1.4M2.5 12h2M19.5 12h2M4.2 19.8l1.4-1.4M18.4 5.6l1.4-1.4"
        {...strokeProps}
      />
    </svg>
  )
}

export function BellIcon({ size }: IconProps) {
  return (
    <svg {...base(size)}>
      <path d="M6 9a6 6 0 0 1 12 0c0 5 2 6 2 6H4s2-1 2-6" {...strokeProps} />
      <path d="M10 19a2 2 0 0 0 4 0" {...strokeProps} />
    </svg>
  )
}

export function SearchIcon({ size }: IconProps) {
  return (
    <svg {...base(size)}>
      <circle cx="11" cy="11" r="6.5" {...strokeProps} />
      <path d="m16 16 4.5 4.5" {...strokeProps} />
    </svg>
  )
}

export function LogoutIcon({ size }: IconProps) {
  return (
    <svg {...base(size)}>
      <path d="M15 4h3.5A1.5 1.5 0 0 1 20 5.5v13a1.5 1.5 0 0 1-1.5 1.5H15" {...strokeProps} />
      <path d="M10 8 6 12l4 4M6 12h11" {...strokeProps} />
    </svg>
  )
}

export function PencilIcon({ size }: IconProps) {
  return (
    <svg {...base(size)}>
      <path d="M4 20h4.5L19 9.5 14.5 5 4 15.5V20Z" {...strokeProps} />
      <path d="m13 6.5 4.5 4.5" {...strokeProps} />
    </svg>
  )
}

export function PlusIcon({ size }: IconProps) {
  return (
    <svg {...base(size)}>
      <path d="M12 5v14M5 12h14" {...strokeProps} />
    </svg>
  )
}

export function TrashIcon({ size }: IconProps) {
  return (
    <svg {...base(size)}>
      <path d="M4 7h16M9 7V5a1 1 0 0 1 1-1h4a1 1 0 0 1 1 1v2" {...strokeProps} />
      <path d="M6 7l1 12a1 1 0 0 0 1 1h8a1 1 0 0 0 1-1l1-12M10 11v6M14 11v6" {...strokeProps} />
    </svg>
  )
}

export function RevenueIcon({ size }: IconProps) {
  return (
    <svg {...base(size)}>
      <rect x="2.5" y="6" width="19" height="12" rx="2.5" {...strokeProps} />
      <circle cx="12" cy="12" r="2.6" {...strokeProps} />
      <path d="M6 9.5v5M18 9.5v5" {...strokeProps} />
    </svg>
  )
}
