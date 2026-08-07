import { useEffect, useState } from 'react'
import { getHealth } from '../../api/systemApi'

type ConnectionState = 'checking' | 'ready' | 'offline'

export function ApiStatus() {
  const [state, setState] = useState<ConnectionState>('checking')

  useEffect(() => {
    const controller = new AbortController()

    getHealth(controller.signal)
      .then((response) => setState(response.status === 'ok' ? 'ready' : 'offline'))
      .catch((error: unknown) => {
        if (error instanceof DOMException && error.name === 'AbortError') return
        setState('offline')
      })

    return () => controller.abort()
  }, [])

  const label = {
    checking: 'Đang kiểm tra hệ thống',
    ready: 'API đã sẵn sàng',
    offline: 'Không thể kết nối API',
  }[state]

  return (
    <div className={`api-status api-status--${state}`} role="status" aria-live="polite">
      <span className="api-status__dot" aria-hidden="true" />
      <span>{label}</span>
    </div>
  )
}
