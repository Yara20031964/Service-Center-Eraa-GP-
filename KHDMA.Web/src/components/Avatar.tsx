import { initials } from '../lib/format'

const TONES = ['#fde2e4', '#dcedff', '#e4f7e7', '#fef3d7', '#ede7fb']
const INKS = ['#a01427', '#12518f', '#1c7a3a', '#8a5b00', '#5b3aa0']

export default function Avatar({
  name,
  tone = 0,
  className = '',
}: {
  name: string
  tone?: number
  className?: string
}) {
  const i = ((tone % TONES.length) + TONES.length) % TONES.length
  return (
    <span
      className={`avatar ${className}`.trim()}
      style={{ background: TONES[i], color: INKS[i] }}
    >
      {initials(name)}
    </span>
  )
}
