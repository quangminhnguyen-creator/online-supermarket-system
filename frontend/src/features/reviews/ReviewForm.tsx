import { useState, useEffect, type FormEvent } from 'react'
import { reviewApi } from '../../api/reviewApi'
import { ApiError } from '../../api/httpClient'
import { useAuth } from '../auth/AuthContext'

export interface ReviewFormProps {
  productId: string
  orderItemId?: string | null
  reviewId?: string | null
  initialRating?: number
  initialComment?: string | null
  onSuccess: () => void
  onCancel?: () => void
}

export function ReviewForm({
  orderItemId,
  reviewId,
  initialRating = 5,
  initialComment = '',
  onSuccess,
  onCancel,
}: ReviewFormProps) {
  const { accessToken } = useAuth()
  const isEditing = Boolean(reviewId)

  const [rating, setRating] = useState<number>(initialRating || 5)
  const [hoverRating, setHoverRating] = useState<number | null>(null)
  const [comment, setComment] = useState<string>(initialComment || '')
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [errorMessage, setErrorMessage] = useState<string | null>(null)
  const [successMessage, setSuccessMessage] = useState<string | null>(null)

  useEffect(() => {
    setRating(initialRating || 5)
    setComment(initialComment || '')
  }, [initialRating, initialComment])

  const remainingChars = 2000 - comment.length

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault()
    if (!accessToken) {
      setErrorMessage('Vui lòng đăng nhập để thực hiện đánh giá.')
      return
    }

    if (rating < 1 || rating > 5) {
      setErrorMessage('Vui lòng chọn số sao từ 1 đến 5.')
      return
    }

    setIsSubmitting(true)
    setErrorMessage(null)
    setSuccessMessage(null)

    try {
      if (isEditing && reviewId) {
        await reviewApi.updateReview(reviewId, accessToken, {
          rating,
          comment: comment.trim() ? comment.trim() : null,
        })
        setSuccessMessage('Cập nhật đánh giá thành công!')
      } else if (orderItemId) {
        await reviewApi.createReview(accessToken, {
          orderItemId,
          rating,
          comment: comment.trim() ? comment.trim() : null,
        })
        setSuccessMessage('Gửi đánh giá thành công!')
      } else {
        setErrorMessage('Không tìm thấy thông tin sản phẩm đủ điều kiện đánh giá.')
        setIsSubmitting(false)
        return
      }

      setTimeout(() => {
        onSuccess()
      }, 500)
    } catch (err: unknown) {
      if (err instanceof ApiError) {
        if (err.status === 409) {
          setErrorMessage('Bạn đã gửi đánh giá cho sản phẩm này rồi hoặc đơn hàng chưa hoàn tất.')
        } else if (err.status === 403) {
          setErrorMessage('Bạn không có quyền đánh giá đơn hàng này.')
        } else if (err.status === 400) {
          setErrorMessage('Dữ liệu đánh giá không hợp lệ. Vui lòng kiểm tra lại.')
        } else {
          setErrorMessage(err.message || 'Có lỗi xảy ra khi gửi đánh giá.')
        }
      } else {
        setErrorMessage('Không thể kết nối đến máy chủ. Vui lòng thử lại.')
      }
    } finally {
      setIsSubmitting(false)
    }
  }

  const ratingDescriptions: Record<number, string> = {
    1: '1 sao - Rất tệ',
    2: '2 sao - Chưa tốt',
    3: '3 sao - Bình thường',
    4: '4 sao - Hài lòng',
    5: '5 sao - Rất hài lòng',
  }

  const displayRating = hoverRating ?? rating

  return (
    <form className="review-form" onSubmit={handleSubmit} noValidate>
      <h3 className="review-form__title">
        {isEditing ? 'Sửa đánh giá của bạn' : 'Viết đánh giá sản phẩm'}
      </h3>

      {errorMessage && (
        <div className="review-form__alert review-form__alert--error" role="alert">
          {errorMessage}
        </div>
      )}

      {successMessage && (
        <div className="review-form__alert review-form__alert--success" role="status">
          {successMessage}
        </div>
      )}

      <div className="review-form__group">
        <fieldset className="review-form__fieldset">
          <legend className="review-form__label">
            Chất lượng sản phẩm <span className="review-form__required">*</span>
          </legend>
          <div className="review-form__stars-row">
            {[1, 2, 3, 4, 5].map((star) => {
              const isFilled = star <= displayRating
              return (
                <label
                  key={star}
                  className={`review-form__star-btn ${isFilled ? 'is-active' : ''}`}
                  onMouseEnter={() => setHoverRating(star)}
                  onMouseLeave={() => setHoverRating(null)}
                >
                  <input
                    type="radio"
                    name="rating"
                    value={star}
                    checked={rating === star}
                    onChange={() => setRating(star)}
                    className="sr-only"
                    aria-label={`${star} sao`}
                  />
                  <span aria-hidden="true">★</span>
                </label>
              )
            })}
            <span className="review-form__rating-text" aria-live="polite">
              {ratingDescriptions[displayRating] || ''}
            </span>
          </div>
        </fieldset>
      </div>

      <div className="review-form__group">
        <label htmlFor="review-comment" className="review-form__label">
          Bình luận nhận xét (không bắt buộc)
        </label>
        <textarea
          id="review-comment"
          className="review-form__textarea"
          rows={4}
          maxLength={2000}
          value={comment}
          onChange={(e) => setComment(e.target.value)}
          placeholder="Hãy chia sẻ trải nghiệm về sản phẩm (đóng gói, chất lượng, hạn sử dụng...)"
        />
        <div className="review-form__char-count" aria-live="polite">
          Còn lại {remainingChars} ký tự
        </div>
      </div>

      <div className="review-form__actions">
        <button
          type="submit"
          className="review-form__submit-btn"
          disabled={isSubmitting}
        >
          {isSubmitting
            ? 'Đang gửi...'
            : isEditing
            ? 'Cập nhật đánh giá'
            : 'Gửi đánh giá'}
        </button>
        {isEditing && onCancel && (
          <button
            type="button"
            className="review-form__cancel-btn"
            onClick={onCancel}
            disabled={isSubmitting}
          >
            Hủy
          </button>
        )}
      </div>
    </form>
  )
}
