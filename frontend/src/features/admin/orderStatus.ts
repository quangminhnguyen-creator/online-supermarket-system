// Shared order-status vocabulary for the admin area.
// The transition map mirrors the backend state machine in OrderEndpoints.GetValidTransitions;
// the backend remains the source of truth and still validates every transition.

export const ORDER_STATUSES = [
  'Pending',
  'Confirmed',
  'Preparing',
  'Ready',
  'Shipped',
  'Delivered',
  'Completed',
  'Cancelled',
  'Failed',
] as const

export const STATUS_LABELS: Record<string, string> = {
  Pending: 'Chờ xác nhận',
  Confirmed: 'Đã xác nhận',
  Preparing: 'Đang chuẩn bị',
  Ready: 'Sẵn sàng nhận hàng',
  Shipped: 'Đang giao hàng',
  Delivered: 'Đã giao hàng',
  Completed: 'Hoàn tất',
  Cancelled: 'Đã hủy',
  Failed: 'Thất bại',
}

export function formatStatus(status: string) {
  return STATUS_LABELS[status] ?? status
}

const VALID_TRANSITIONS: Record<string, string[]> = {
  Pending: ['Confirmed', 'Cancelled'],
  Confirmed: ['Preparing', 'Cancelled'],
  Preparing: ['Ready', 'Shipped', 'Cancelled'],
  Ready: ['Shipped', 'Delivered'],
  Shipped: ['Delivered'],
  Delivered: ['Completed'],
  Completed: [],
  Cancelled: [],
  Failed: [],
}

export function validTransitions(status: string): string[] {
  return VALID_TRANSITIONS[status] ?? []
}
