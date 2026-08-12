export function LogoMark() {
  return (
    <span className="logo-mark" aria-hidden="true">
      <svg viewBox="0 0 64 64" shapeRendering="geometricPrecision">
        <circle className="logo-ball" cx="32" cy="32" r="28" />
        <circle className="logo-hole" cx="20" cy="15" r="4" />
        <circle className="logo-hole" cx="43" cy="19" r="4" />
        <circle className="logo-hole" cx="31" cy="30" r="4" />
        <circle className="logo-hole" cx="15" cy="38" r="4" />
        <circle className="logo-hole" cx="48" cy="40" r="4" />
        <circle className="logo-hole" cx="29" cy="49" r="4" />
      </svg>
    </span>
  )
}

export function PickleBall({ className }: { className: string }) {
  return (
    <span className={className} aria-hidden="true">
      <i />
      <i />
      <i />
      <i />
      <i />
    </span>
  )
}
