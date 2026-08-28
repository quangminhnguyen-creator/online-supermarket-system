import { chromium } from 'playwright';
import fs from 'fs';
import path from 'path';

const ARTIFACT_DIR = 'C:/Users/manh/.gemini/antigravity-ide/brain/151fc48c-2a82-4ff7-b5bd-2762ac0ba2fc';
const BASE_URL = 'http://localhost:5173';
const API_URL = 'http://localhost:5072/api';
const TEST_USER = {
  email: 'checkout_user@test.com',
  password: 'Password@123',
};
const PRODUCT_ID = '1021209b-bfb4-4c69-9c32-76ae3db37fc5'; // AirPods Pro 2

function ensureDir(dir) {
  if (!fs.existsSync(dir)) {
    fs.mkdirSync(dir, { recursive: true });
  }
}

async function delay(ms) {
  return new Promise((resolve) => setTimeout(resolve, ms));
}

// API helper to get token
async function getAuthToken() {
  const res = await fetch(`${API_URL}/auth/login`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(TEST_USER),
  });
  if (!res.ok) throw new Error(`Login failed: ${res.status}`);
  const data = await res.json();
  return data.accessToken;
}

// API helper to add product to user's cart
async function apiAddCartItem() {
  const token = await getAuthToken();
  const res = await fetch(`${API_URL}/cart/items`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      Authorization: `Bearer ${token}`,
    },
    body: JSON.stringify({ productId: PRODUCT_ID, quantity: 1 }),
  });
  if (!res.ok) throw new Error(`Add cart item failed: ${res.status}`);
  return await res.json();
}

// API helper to clear user's cart
async function apiClearCart() {
  const token = await getAuthToken();
  await fetch(`${API_URL}/cart`, {
    method: 'DELETE',
    headers: { Authorization: `Bearer ${token}` },
  });
}

const testResults = [];

function logTest(id, name, status, details = {}) {
  const result = { id, name, status, details, timestamp: new Date().toISOString() };
  testResults.push(result);
  console.log(`\n========================================`);
  console.log(`[TEST ${id}] ${name} => ${status ? 'PASSED ✅' : 'FAILED ❌'}`);
  console.log(`Details:`, JSON.stringify(details, null, 2));
  console.log(`========================================\n`);
}

async function runAllTests() {
  ensureDir(ARTIFACT_DIR);
  const browser = await chromium.launch({ headless: true });
  const context = await browser.newContext({
    viewport: { width: 1280, height: 800 },
  });

  // Mock sandbox payment gateways
  await context.route('https://sandbox.vnpayment.vn/**', (route) => {
    route.fulfill({
      status: 200,
      contentType: 'text/html',
      body: '<html><body style="font-family:sans-serif;padding:2rem;text-align:center;"><h1>VNPay Sandbox Payment Gateway</h1><p>Môi trường thử nghiệm cổng thanh toán VNPay</p></body></html>',
    });
  });

  await context.route('https://momo.vn/**', (route) => {
    route.fulfill({
      status: 200,
      contentType: 'text/html',
      body: '<html><body style="font-family:sans-serif;padding:2rem;text-align:center;"><h1>MoMo Sandbox Payment Gateway</h1><p>Môi trường thử nghiệm ví điện tử MoMo</p></body></html>',
    });
  });

  const page = await context.newPage();

  // Helper to clear frontend auth
  async function clearAuth() {
    await page.goto(BASE_URL);
    await page.evaluate(() => {
      localStorage.clear();
      sessionStorage.clear();
    });
    await context.clearCookies();
  }

  // Helper to set frontend auth in localStorage
  async function setAuthInStorage() {
    const res = await fetch(`${API_URL}/auth/login`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(TEST_USER),
    });
    const authData = await res.json();

    await page.goto(BASE_URL);
    await page.evaluate((data) => {
      localStorage.setItem('os_access_token', data.accessToken);
      localStorage.setItem('os_refresh_token', data.refreshToken);
    }, authData);
    await delay(300);
  }

  try {
    // -------------------------------------------------------------
    // TEST 1: Guest mở checkout
    // -------------------------------------------------------------
    {
      console.log('--- RUNNING TEST 1: Guest mở checkout ---');
      await clearAuth();

      const interceptedRequests = [];
      const onRequest = (req) => {
        if (req.url().includes('/api/checkout') && req.method() === 'POST') {
          interceptedRequests.push({ url: req.url(), method: req.method() });
        }
      };
      page.on('request', onRequest);

      await page.goto(`${BASE_URL}/shopping/checkout`);
      await page.waitForLoadState('networkidle');
      await delay(500);

      const headingText = await page.textContent('h1');
      const isAuthRequired = headingText?.includes('Đăng nhập để thanh toán');
      const hasLoginBtn = await page.isVisible('button:has-text("Đăng nhập")');
      const hasNoCheckoutPost = interceptedRequests.length === 0;

      const screenshotPath = path.join(ARTIFACT_DIR, '01_guest_checkout.png');
      await page.screenshot({ path: screenshotPath, fullPage: true });

      page.off('request', onRequest);

      const pass = isAuthRequired && hasLoginBtn && hasNoCheckoutPost;
      logTest(1, 'Guest mở checkout', pass, {
        headingText,
        hasLoginBtn,
        checkoutPostRequests: interceptedRequests,
        screenshot: '01_guest_checkout.png',
      });
    }

    // -------------------------------------------------------------
    // TEST 2: Đăng nhập rồi checkout
    // -------------------------------------------------------------
    {
      console.log('--- RUNNING TEST 2: Đăng nhập rồi checkout ---');
      await apiAddCartItem();

      await page.goto(`${BASE_URL}/shopping/checkout`);
      await page.waitForLoadState('networkidle');

      const loginBtn = await page.waitForSelector('button:has-text("Đăng nhập")', { timeout: 5000 });
      await loginBtn.click();

      await page.waitForSelector('#login-email', { state: 'visible', timeout: 5000 });
      await page.fill('#login-email', TEST_USER.email);
      await page.fill('#login-password', TEST_USER.password);
      await page.click('button.auth-submit-btn');

      // Wait for modal to close and checkout page to load
      await page.waitForSelector('.auth-modal', { state: 'detached', timeout: 8000 });
      await page.waitForLoadState('networkidle');
      await delay(1000);

      const headingText = await page.textContent('.checkout-page__heading');
      const hasCheckoutHeading = headingText?.includes('Thanh toán đơn hàng');
      const hasOrderSummary = await page.isVisible('.checkout-summary');

      const screenshotPath = path.join(ARTIFACT_DIR, '02_login_checkout.png');
      await page.screenshot({ path: screenshotPath, fullPage: true });

      const pass = Boolean(hasCheckoutHeading) && Boolean(hasOrderSummary);
      logTest(2, 'Đăng nhập rồi checkout', pass, {
        hasCheckoutHeading,
        hasOrderSummary,
        screenshot: '02_login_checkout.png',
      });
    }

    // -------------------------------------------------------------
    // TEST 3: Cart rỗng
    // -------------------------------------------------------------
    {
      console.log('--- RUNNING TEST 3: Cart rỗng ---');
      await apiClearCart();

      await page.goto(`${BASE_URL}/shopping/checkout`);
      await page.waitForLoadState('networkidle');
      await delay(500);

      const emptyHeading = await page.$('h2:has-text("Giỏ hàng đang trống")');
      const backLink = await page.$('a[href="/shopping/cart"]:has-text("Quay lại giỏ hàng")');

      let navigatedToCart = false;
      if (backLink) {
        await backLink.click();
        await page.waitForLoadState('networkidle');
        navigatedToCart = page.url().includes('/shopping/cart');
      }

      const screenshotPath = path.join(ARTIFACT_DIR, '03_cart_empty_checkout.png');
      await page.screenshot({ path: screenshotPath, fullPage: true });

      const pass = Boolean(emptyHeading) && Boolean(backLink) && navigatedToCart;
      logTest(3, 'Cart rỗng', pass, {
        hasEmptyHeading: Boolean(emptyHeading),
        hasBackLink: Boolean(backLink),
        navigatedToCart,
        screenshot: '03_cart_empty_checkout.png',
      });
    }

    // -------------------------------------------------------------
    // TEST 4: Cart có hàng → Pickup + COD
    // -------------------------------------------------------------
    {
      console.log('--- RUNNING TEST 4: Cart có hàng -> Pickup + COD ---');
      await apiAddCartItem();

      await page.goto(`${BASE_URL}/shopping/cart`);
      await page.waitForLoadState('networkidle');
      await delay(500);

      // Click "Tiến hành thanh toán"
      const checkoutBtn = await page.waitForSelector('a[href="/shopping/checkout"], button:has-text("Tiến hành thanh toán")', { timeout: 5000 });
      await checkoutBtn.click();
      await page.waitForLoadState('networkidle');
      await delay(500);

      // Select Pickup
      const pickupRadio = await page.waitForSelector('input[name="fulfillment"][value="Pickup"]', { timeout: 5000 });
      await pickupRadio.check();

      // Select COD
      const codRadio = await page.waitForSelector('input[name="paymentMethod"][value="COD"]', { timeout: 5000 });
      await codRadio.check();

      const requestsRecorded = [];
      const onRequest = (req) => {
        if (req.url().includes('/api/checkout')) {
          requestsRecorded.push({
            url: req.url(),
            method: req.method(),
            headers: req.headers(),
            postData: req.postDataJSON ? req.postDataJSON() : req.postData(),
          });
        }
      };
      page.on('request', onRequest);

      const submitBtn = await page.waitForSelector('button.checkout-submit-btn', { timeout: 5000 });
      await submitBtn.click();

      await page.waitForURL((url) => url.pathname.includes('/shopping/checkout/success'), { timeout: 10000 });
      await delay(1000);

      const checkoutReq = requestsRecorded.find((r) => r.url.endsWith('/api/checkout') && r.method === 'POST');
      const paymentReq = requestsRecorded.find((r) => r.url.includes('/api/checkout/payment') && r.method === 'POST');

      const isPickupBody = checkoutReq && checkoutReq.postData?.fulfillmentType === 'Pickup';
      const hasAuthHeader = checkoutReq && Boolean(checkoutReq.headers?.authorization?.startsWith('Bearer '));
      const isCodBody = paymentReq && paymentReq.postData?.method === 'COD' && Boolean(paymentReq.postData?.orderId);
      const isSuccessUrl = page.url().includes('/shopping/checkout/success');

      const screenshotPath = path.join(ARTIFACT_DIR, '04_pickup_cod_success.png');
      await page.screenshot({ path: screenshotPath, fullPage: true });

      page.off('request', onRequest);

      const pass = Boolean(isPickupBody && hasAuthHeader && isCodBody && isSuccessUrl);
      logTest(4, 'Cart có hàng → Pickup + COD', pass, {
        checkoutReqBody: checkoutReq?.postData,
        hasAuthHeader,
        paymentReqBody: paymentReq?.postData,
        finalUrl: page.url(),
        screenshot: '04_pickup_cod_success.png',
      });
    }

    // -------------------------------------------------------------
    // TEST 5: Delivery thiếu thông tin
    // -------------------------------------------------------------
    {
      console.log('--- RUNNING TEST 5: Delivery thiếu thông tin ---');
      await apiAddCartItem();

      await page.goto(`${BASE_URL}/shopping/checkout`);
      await page.waitForLoadState('networkidle');
      await delay(500);

      const deliveryRadio = await page.waitForSelector('input[name="fulfillment"][value="Delivery"]', { timeout: 5000 });
      await deliveryRadio.check();
      await delay(300);

      // Clear fields
      await page.fill('#recipient-name', '');
      await page.fill('#recipient-phone', '');
      await page.fill('#delivery-address', '');

      const interceptedRequests = [];
      const onRequest = (req) => {
        if (req.url().includes('/api/checkout') && req.method() === 'POST') {
          interceptedRequests.push({ url: req.url(), method: req.method() });
        }
      };
      page.on('request', onRequest);

      const submitBtn = await page.waitForSelector('button.checkout-submit-btn', { timeout: 5000 });
      await submitBtn.click();
      await delay(500);

      const alertEl = await page.waitForSelector('.checkout-alert[role="alert"]', { timeout: 5000 });
      const alertText = await alertEl.textContent();

      const screenshotPath = path.join(ARTIFACT_DIR, '05_delivery_missing_info.png');
      await page.screenshot({ path: screenshotPath, fullPage: true });

      page.off('request', onRequest);

      const hasAlert = alertText.includes('Vui lòng nhập đầy đủ thông tin giao hàng');
      const noCheckoutPost = interceptedRequests.length === 0;

      const pass = hasAlert && noCheckoutPost;
      logTest(5, 'Delivery thiếu thông tin', pass, {
        alertText,
        interceptedRequestsCount: interceptedRequests.length,
        screenshot: '05_delivery_missing_info.png',
      });
    }

    // -------------------------------------------------------------
    // TEST 6: Delivery đủ thông tin
    // -------------------------------------------------------------
    {
      console.log('--- RUNNING TEST 6: Delivery đủ thông tin ---');
      await page.goto(`${BASE_URL}/shopping/checkout`);
      await page.waitForLoadState('networkidle');
      await delay(500);

      const deliveryRadio = await page.waitForSelector('input[name="fulfillment"][value="Delivery"]', { timeout: 5000 });
      await deliveryRadio.check();
      await delay(300);

      await page.fill('#recipient-name', 'Nguyễn Văn Checkout');
      await page.fill('#recipient-phone', '0901234567');
      await page.fill('#delivery-address', '123 Đường Lê Lợi, Phường Bến Nghé, Quận 1, TP.HCM');

      // Check shipping fee in summary
      const summaryContent = await page.textContent('.checkout-summary');
      const hasShippingFee = summaryContent.includes('15.000') || summaryContent.includes('15,000');

      const requestsRecorded = [];
      const onRequest = (req) => {
        if (req.url().includes('/api/checkout') && req.method() === 'POST') {
          requestsRecorded.push({
            url: req.url(),
            postData: req.postDataJSON ? req.postDataJSON() : req.postData(),
          });
        }
      };
      page.on('request', onRequest);

      const submitBtn = await page.waitForSelector('button.checkout-submit-btn', { timeout: 5000 });
      await submitBtn.click();

      await page.waitForURL((url) => url.pathname.includes('/shopping/checkout/success'), { timeout: 10000 });
      await delay(1000);

      const checkoutReq = requestsRecorded.find((r) => r.url.endsWith('/api/checkout'));
      const body = checkoutReq?.postData;

      const pass =
        hasShippingFee &&
        body?.fulfillmentType === 'Delivery' &&
        body?.recipientName === 'Nguyễn Văn Checkout' &&
        body?.recipientPhone === '0901234567' &&
        body?.deliveryAddress?.includes('123 Đường Lê Lợi');

      const screenshotPath = path.join(ARTIFACT_DIR, '06_delivery_valid_info.png');
      await page.screenshot({ path: screenshotPath, fullPage: true });

      page.off('request', onRequest);

      logTest(6, 'Delivery đủ thông tin', pass, {
        hasShippingFee,
        checkoutBody: body,
        finalUrl: page.url(),
        screenshot: '06_delivery_valid_info.png',
      });
    }

    // -------------------------------------------------------------
    // TEST 7: VNPay/MoMo redirect
    // -------------------------------------------------------------
    {
      console.log('--- RUNNING TEST 7: VNPay/MoMo redirect ---');
      await apiAddCartItem();

      await page.goto(`${BASE_URL}/shopping/checkout`);
      await page.waitForLoadState('networkidle');
      await delay(500);

      // Select VNPay
      const vnpayRadio = await page.waitForSelector('input[name="paymentMethod"][value="VNPay"]', { timeout: 5000 });
      await vnpayRadio.check();

      const requestsRecorded = [];
      let redirectedUrl = '';

      const onRequest = (req) => {
        if (req.url().includes('/api/checkout')) {
          requestsRecorded.push({
            url: req.url(),
            method: req.method(),
            postData: req.postDataJSON ? req.postDataJSON() : req.postData(),
          });
        }
      };
      page.on('request', onRequest);

      page.on('framenavigated', (frame) => {
        if (frame === page.mainFrame() && frame.url().includes('vnpayment.vn')) {
          redirectedUrl = frame.url();
        }
      });

      try {
        const submitBtn = await page.waitForSelector('button.checkout-submit-btn', { timeout: 5000 });
        await submitBtn.click();
        await page.waitForURL((url) => url.hostname.includes('vnpayment.vn'), { timeout: 10000 });
        redirectedUrl = page.url();
      } catch {
        redirectedUrl = page.url();
      }

      const checkoutReq = requestsRecorded.find((r) => r.url.endsWith('/api/checkout') && r.method === 'POST');
      const paymentReq = requestsRecorded.find((r) => r.url.includes('/api/checkout/payment') && r.method === 'POST');

      const isVnPayMethod = paymentReq?.postData?.method === 'VNPay';
      const isRedirectedToSandbox = redirectedUrl.includes('sandbox.vnpayment.vn');

      const screenshotPath = path.join(ARTIFACT_DIR, '07_vnpay_momo_redirect.png');
      try {
        await page.screenshot({ path: screenshotPath, fullPage: true });
      } catch {}

      page.off('request', onRequest);

      const pass = Boolean(checkoutReq) && isVnPayMethod && isRedirectedToSandbox;
      logTest(7, 'VNPay/MoMo redirect', pass, {
        calledCheckout: Boolean(checkoutReq),
        paymentPostData: paymentReq?.postData,
        redirectedUrl,
        screenshot: '07_vnpay_momo_redirect.png',
      });
    }

    // -------------------------------------------------------------
    // TEST 8: Payment fail sau khi order đã tạo
    // -------------------------------------------------------------
    {
      console.log('--- RUNNING TEST 8: Payment fail sau khi order đã tạo ---');
      await apiAddCartItem();

      await page.goto(`${BASE_URL}/shopping/checkout`);
      await page.waitForLoadState('networkidle');
      await delay(500);

      // Reset fulfillment to Pickup
      const pickupRadio = await page.waitForSelector('input[name="fulfillment"][value="Pickup"]', { timeout: 5000 });
      await pickupRadio.check();

      // Route mock: intercept payment endpoint and fail first time
      let paymentFailCount = 0;
      await page.route('**/api/checkout/payment', async (route) => {
        if (paymentFailCount === 0) {
          paymentFailCount++;
          await route.fulfill({
            status: 500,
            contentType: 'application/json',
            body: JSON.stringify({ message: 'Simulated payment gateway timeout' }),
          });
        } else {
          await route.continue();
        }
      });

      const requestsRecorded = [];
      const onRequest = (req) => {
        if (req.url().includes('/api/checkout')) {
          requestsRecorded.push({
            url: req.url(),
            method: req.method(),
            postData: req.postDataJSON ? req.postDataJSON() : req.postData(),
          });
        }
      };
      page.on('request', onRequest);

      // First click: Đặt hàng
      const submitBtn = await page.waitForSelector('button.checkout-submit-btn', { timeout: 5000 });
      await submitBtn.click();
      await delay(1000);

      // Assert error alert shown
      const alertEl = await page.waitForSelector('.checkout-alert[role="alert"]', { timeout: 5000 });
      const alertText = await alertEl.textContent();
      const hasOrderCreatedAlert = alertText.includes('đã được tạo nhưng chưa thể khởi tạo thanh toán');

      // Assert button text changed to "Thử lại thanh toán"
      const retryBtnText = await page.textContent('button.checkout-submit-btn');
      const isRetryBtn = retryBtnText?.includes('Thử lại thanh toán');

      const requestsBeforeRetry = [...requestsRecorded];

      // Second click: Thử lại thanh toán
      await submitBtn.click();
      await page.waitForURL((url) => url.pathname.includes('/shopping/checkout/success'), { timeout: 10000 });
      await delay(1000);

      const requestsAfterRetry = requestsRecorded.slice(requestsBeforeRetry.length);
      const retryCallsToCheckout = requestsAfterRetry.filter((r) => r.url.endsWith('/api/checkout') && r.method === 'POST');
      const retryCallsToPayment = requestsAfterRetry.filter((r) => r.url.includes('/api/checkout/payment') && r.method === 'POST');

      const screenshotPath = path.join(ARTIFACT_DIR, '08_payment_fail_retry.png');
      await page.screenshot({ path: screenshotPath, fullPage: true });

      await page.unroute('**/api/checkout/payment');
      page.off('request', onRequest);

      const pass =
        hasOrderCreatedAlert &&
        isRetryBtn &&
        retryCallsToCheckout.length === 0 &&
        retryCallsToPayment.length === 1;

      logTest(8, 'Payment fail sau khi order đã tạo', pass, {
        hasOrderCreatedAlert,
        alertText,
        isRetryBtn,
        retryCallsToCheckoutCount: retryCallsToCheckout.length,
        retryCallsToPaymentCount: retryCallsToPayment.length,
        finalUrl: page.url(),
        screenshot: '08_payment_fail_retry.png',
      });
    }

    // -------------------------------------------------------------
    // TEST 9: Responsive + keyboard
    // -------------------------------------------------------------
    {
      console.log('--- RUNNING TEST 9: Responsive + keyboard ---');
      await apiAddCartItem();

      // Set viewport to 320px
      await page.setViewportSize({ width: 320, height: 750 });
      await page.goto(`${BASE_URL}/shopping/checkout`);
      await page.waitForLoadState('networkidle');
      await delay(500);

      // Check horizontal overflow
      const overflow = await page.evaluate(() => {
        const docWidth = document.documentElement.offsetWidth;
        const scrollWidth = document.documentElement.scrollWidth;
        const clientWidth = document.documentElement.clientWidth;
        return {
          docWidth,
          scrollWidth,
          clientWidth,
          hasHorizontalScroll: scrollWidth > clientWidth,
        };
      });

      // Test Keyboard Navigation
      const pickupRadio = await page.waitForSelector('input[name="fulfillment"][value="Pickup"]');
      await pickupRadio.focus();

      const focusedVal1 = await page.evaluate(() => document.activeElement?.getAttribute('value'));
      const isPickupFocused = focusedVal1 === 'Pickup';

      // Arrow down to switch radio
      await page.keyboard.press('ArrowDown');
      await delay(300);
      const focusedVal2 = await page.evaluate(() => document.activeElement?.getAttribute('value'));
      const isDeliveryFocused = focusedVal2 === 'Delivery';

      // Tab into inputs
      await page.keyboard.press('Tab');
      const isSelect = await page.evaluate(() => document.activeElement?.tagName === 'SELECT');
      if (isSelect) {
        await page.keyboard.press('Tab');
      }

      await page.keyboard.type('Test Keyboard Recipient');
      const nameVal = await page.$eval('#recipient-name', (el) => el.value);

      const screenshotPath = path.join(ARTIFACT_DIR, '09_responsive_320px_keyboard.png');
      await page.screenshot({ path: screenshotPath, fullPage: true });

      const pass = !overflow.hasHorizontalScroll && isPickupFocused && isDeliveryFocused && nameVal.includes('Test Keyboard');
      logTest(9, 'Responsive + keyboard', pass, {
        viewportWidth: 320,
        overflow,
        isPickupFocused,
        isDeliveryFocused,
        keyboardTypedName: nameVal,
        screenshot: '09_responsive_320px_keyboard.png',
      });
    }
  } catch (error) {
    console.error('Test execution error:', error);
  } finally {
    await browser.close();

    // Summary
    console.log('\n========================================');
    console.log('FINAL TEST EXECUTION SUMMARY:');
    const passedCount = testResults.filter((r) => r.status).length;
    console.log(`Passed: ${passedCount}/${testResults.length}`);
    console.log('========================================\n');

    fs.writeFileSync(
      path.join(ARTIFACT_DIR, 'checkout_gui_test_report.json'),
      JSON.stringify(testResults, null, 2)
    );
  }
}

runAllTests();
