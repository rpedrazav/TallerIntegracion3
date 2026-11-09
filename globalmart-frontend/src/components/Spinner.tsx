interface SpinnerProps {
  size?: number;
  color?: string;
}

export default function Spinner({ size = 20, color = '#ffffff' }: SpinnerProps) {
  return (
    <svg
      style={{
        display: 'inline-block',
        animation: 'spin 0.8s linear infinite',
        verticalAlign: 'middle',
      }}
      width={size}
      height={size}
      viewBox="0 0 24 24"
      fill="none"
      xmlns="http://www.w3.org/2000/svg"
      role="status"
      aria-label="Cargando"
    >
      <circle
        cx="12"
        cy="12"
        r="10"
        stroke={color}
        strokeWidth="3"
        strokeOpacity="0.25"
      />
      <path
        d="M12 2a10 10 0 0 1 10 10"
        stroke={color}
        strokeWidth="3"
        strokeLinecap="round"
      />
    </svg>
  );
}
