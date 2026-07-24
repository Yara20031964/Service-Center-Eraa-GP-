import type { StatusInfo } from '../lib/status'

export default function Badge({ label, tone }: StatusInfo) {
  return <span className={`badge badge--${tone}`}>{label}</span>
}
