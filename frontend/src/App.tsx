import { AppShell } from './app/AppShell'
import { AuthProvider } from './features/auth/AuthContext'

const foundationItems = [
  ['01', 'Catalog', 'Sản phẩm, thương hiệu và danh mục dùng chung.'],
  ['02', 'Branch stock', 'Giá và tồn kho độc lập tại từng chi nhánh.'],
  ['03', 'Checkout', 'Giỏ hàng được kiểm tra lại trước khi đặt đơn.'],
  ['04', 'Authentication', 'Đăng ký, đăng nhập JWT, phân quyền Customer & Admin.'],
]

export default function App() {
  return (
    <AuthProvider>
      <AppShell>
        <section className="hero" aria-labelledby="hero-title">
          <div className="hero__copy">
            <p className="eyebrow">Nền tảng mua sắm đa chi nhánh</p>
            <h1 id="hero-title">Đi chợ online,<br />biết rõ hàng ở đâu.</h1>
            <p className="hero__lead">
              Chọn đúng chi nhánh, xem đúng giá và lượng hàng trước khi thêm vào giỏ.
              Hệ thống xác thực và quản lý tài khoản người dùng đã sẵn sàng.
            </p>
            <a className="primary-action" href="#roadmap">Xem nền tảng đã dựng</a>
          </div>

          <aside className="stock-ticket" aria-label="Phiếu kiểm tra nền tảng">
            <div className="stock-ticket__head">
              <span>PHIẾU KIỂM HÀNG</span>
              <span>SPRINT 02</span>
            </div>
            <div className="stock-ticket__route">
              <span>Kho dữ liệu</span><b>MySQL</b>
              <span>Quầy dịch vụ</span><b>ASP.NET Minimal API</b>
              <span>Bảo mật</span><b>JWT + Refresh Token</b>
              <span>Mặt tiền</span><b>React 19</b>
            </div>
            <div className="stock-ticket__stamp">XÁC THỰC<br />ĐÃ SẴN SÀNG</div>
          </aside>
        </section>

        <section className="foundation" id="roadmap" aria-labelledby="foundation-title">
          <header>
            <p className="eyebrow">Cấu trúc nghiệp vụ</p>
            <h2 id="foundation-title">Một nguồn dữ liệu, nhiều quầy hàng.</h2>
          </header>
          <div className="foundation__list">
            {foundationItems.map(([number, title, detail]) => (
              <article key={number}>
                <span>{number}</span>
                <h3>{title}</h3>
                <p>{detail}</p>
              </article>
            ))}
          </div>
        </section>
      </AppShell>
    </AuthProvider>
  )
}
