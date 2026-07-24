// Small on/off switch (brand red when on).
export default function Toggle({
  checked,
  onChange,
  disabled,
  label,
}: {
  checked: boolean
  onChange: (v: boolean) => void
  disabled?: boolean
  label?: string
}) {
  return (
    <button
      type="button"
      role="switch"
      aria-checked={checked}
      aria-label={label}
      disabled={disabled}
      className={`toggle${checked ? ' toggle--on' : ''}`}
      onClick={() => onChange(!checked)}
    >
      <span className="toggle__knob" />
    </button>
  )
}
