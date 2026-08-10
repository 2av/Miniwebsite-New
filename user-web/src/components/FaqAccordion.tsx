import { useEffect, useState } from 'react'
import { apiGet } from '@/shared/api/client'

export type FaqItem = {
  id: number
  question: string
  answer: string
}

function normalizeFaqs(raw: unknown): FaqItem[] {
  const list = Array.isArray(raw) ? raw : []
  return list.map((item) => {
    const o = (item ?? {}) as Record<string, unknown>
    return {
      id: Number(o.id ?? o.Id ?? 0),
      question: String(o.question ?? o.Question ?? ''),
      answer: String(o.answer ?? o.Answer ?? ''),
    }
  })
}

export function FaqAccordion({ pageType, accordionId = 'faqAccordion' }: { pageType: string; accordionId?: string }) {
  const [faqs, setFaqs] = useState<FaqItem[]>([])
  const [openId, setOpenId] = useState<number | null>(null)
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    let cancelled = false
    ;(async () => {
      setLoading(true)
      try {
        const data = await apiGet<unknown>('/api/v1/public/faqs', { pageType })
        if (!cancelled) setFaqs(normalizeFaqs(data))
      } catch {
        if (!cancelled) setFaqs([])
      } finally {
        if (!cancelled) setLoading(false)
      }
    })()
    return () => {
      cancelled = true
    }
  }, [pageType])

  if (loading) return <p className="text-muted">Loading FAQs...</p>
  if (faqs.length === 0) return <p className="text-muted">No FAQs available right now.</p>

  return (
    <div className="faq-container accordion" id={accordionId}>
      {faqs.map((faq, idx) => {
        const isOpen = openId === faq.id
        return (
          <div className="faq-item card" key={faq.id}>
            <div className="card-header" id={`heading-${accordionId}-${faq.id}`}>
              <button
                type="button"
                className={`btn btn-link btn-block text-left${isOpen ? '' : ' collapsed'}`}
                onClick={() => setOpenId(isOpen ? null : faq.id)}
                aria-expanded={isOpen}
              >
                <span className="faq-q">{idx + 1}. {faq.question}</span>
                <i className={`fa fa-angle-${isOpen ? 'up' : 'down'}`} />
              </button>
            </div>
            {isOpen && (
              <div className="collapse show">
                <div className="card-body" dangerouslySetInnerHTML={{ __html: faq.answer }} />
              </div>
            )}
          </div>
        )
      })}
    </div>
  )
}
