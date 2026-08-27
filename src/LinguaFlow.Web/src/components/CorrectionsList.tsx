import type { Correction } from '../api/types'

export function CorrectionsList({ corrections }: { corrections: Correction[] }) {
  if (corrections.length === 0) return null

  return (
    <ul className="corrections">
      {corrections.map((c) => (
        <li key={c.id} className="correction">
          <span className="correction-category">{c.category}</span>
          <span className="correction-diff">
            <s>{c.original}</s> → <strong>{c.corrected}</strong>
          </span>
          <span className="correction-explanation">{c.explanation}</span>
        </li>
      ))}
    </ul>
  )
}
