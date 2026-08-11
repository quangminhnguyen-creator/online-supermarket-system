# Thiết kế tối ưu workflow cho task chỉ sửa tài liệu

## 1. Vấn đề

Workflow hiện tại đưa mọi task qua `action -> review -> docs`. Với task chỉ sửa tài liệu, cách này gây ba vấn đề:

- `action` bị chặn bởi policy `docs/**: deny` dù task đã duyệt cho sửa tài liệu;
- cùng một thay đổi tài liệu có thể đi qua cả `action` và `docs`;
- artifact tài liệu không được ghi bởi đúng agent, khiến workflow chuyển sang `BLOCKED` dù nội dung đã hoàn tất.

TASK-001 và TASK-002 là bằng chứng thực tế của cả ba vấn đề trên.

## 2. Mục tiêu

- Tách task chỉ sửa tài liệu khỏi luồng triển khai code.
- Giữ một quality gate độc lập nhưng giới hạn đúng phạm vi tài liệu.
- Không cấp `action` quyền sửa rộng trong `docs/**`.
- Để agent thực hiện thay đổi tự ghi artifact của chính nó.
- Giữ nguyên luồng review đầy đủ cho code, migration, API, cấu hình build và thay đổi kiến trúc chạy thực tế.

## 3. Phân loại task

`workflow` phân loại task là `DOCS_ONLY` khi toàn bộ file được phép sửa chỉ thuộc một trong các nhóm:

- `README.md` hoặc `README.*`;
- `CHANGELOG` hoặc `CHANGELOG.*`;
- `docs/**`.

Nếu task cho phép sửa bất kỳ file application, test, migration, API contract, build config, `.opencode/**`, `opencode.json` hoặc workflow config nào, task không phải `DOCS_ONLY` và tiếp tục dùng luồng code hiện tại.

Phân loại phải dựa trên allowlist trong task đã duyệt, không dựa trên lời mô tả tự do.

## 4. Luồng xử lý

### 4.1 Task chỉ sửa tài liệu

```text
workflow -> user approval -> docs -> docs-review
                              ^          |
                              | findings |
                              +----------+
                                   |
                                approved
                                   |
                                  DONE
```

Trình tự:

1. `workflow` tạo task, đánh dấu loại `DOCS_ONLY` và chờ người dùng duyệt.
2. `docs` sửa đúng các file trong allowlist và ghi `.ai/results/TASK-NNN-DOCS.md`.
3. `docs-review` chỉ review task, diff tài liệu và evidence tương ứng.
4. Nếu có lỗi blocking, trả finding có file, vị trí, mức độ, cách sửa và tiêu chí kiểm tra về `docs`.
5. Cho phép tối đa hai vòng docs-review. Hết vòng mà chưa đạt thì chuyển `BLOCKED`.
6. Khi đạt, `workflow` báo kết quả, chuyển `DONE` và reset `.ai/STATUS.md` về trạng thái trung lập.

### 4.2 Task có code hoặc cấu hình runtime

Giữ nguyên:

```text
workflow -> user approval -> action -> review -> docs when needed -> DONE
```

Review code vẫn dùng tối đa ba vòng và route `workflow-review`.

## 5. Trách nhiệm và permission

### `workflow`

- Phân loại task từ allowlist.
- Gọi `docs` rồi `docs-review` đối với `DOCS_ONLY`.
- Không trực tiếp ghi result artifact.

### `docs`

- Dùng route `workflow-docs`.
- Với `DOCS_ONLY`, được chạy ngay sau user approval, không yêu cầu review code có sẵn.
- Chỉ sửa `README*`, `CHANGELOG*`, `docs/**` và phải tuân thủ allowlist của task.
- Được ghi duy nhất `.ai/results/*-DOCS.md`.
- Không được sửa application, test, migration, API contract, task, review, workflow hoặc agent config.

### `docs-review`

- Là subagent read-only dùng route `workflow-docs` với nhiệt độ thấp.
- Chỉ được ghi `.ai/reviews/**`.
- Không build backend/frontend trừ khi task tài liệu yêu cầu rõ ràng.
- Trả đúng `APPROVED` hoặc `CHANGES_REQUIRED`.

### `action`

- Tiếp tục deny `docs/**`, `README*` và changelog.
- Loại bỏ các ngoại lệ tạm thời dành riêng cho TASK-002 sau khi luồng mới có hiệu lực.

## 6. Phạm vi review nhẹ

`docs-review` kiểm tra:

1. acceptance criteria và file scope;
2. thuật ngữ, số liệu và nội dung có nhất quán với nguồn đã duyệt;
3. Markdown/HTML không bị hỏng cấu trúc cơ bản;
4. liên kết hoặc anchor bị thay đổi không trở thành tham chiếu hỏng rõ ràng;
5. không có file ngoài allowlist bị sửa;
6. `git diff --check` thành công;
7. các kiểm tra tài liệu được ghi trong task đã chạy và có evidence thật.

Review nhẹ không phân tích security, database, concurrency, migration hoặc chạy toàn bộ build/test nếu task không liên quan các nội dung đó.

## 7. Artifact

`.ai/results/TASK-NNN-DOCS.md` phải chứa:

- task và mode;
- file tài liệu đã đổi;
- acceptance criteria đã đáp ứng;
- lệnh kiểm tra cùng kết quả chính xác;
- nguồn đã duyệt dùng để cập nhật nội dung;
- rủi ro hoặc blocker còn lại.

Artifact của lần chạy bị chặn trước đó được giữ lại làm lịch sử; lần chạy theo luồng mới ghi artifact `-DOCS.md` riêng thay vì giả vờ action cũ đã thành công.

## 8. Xử lý lỗi và resume TASK-002

- Permission hoặc dependency thiếu: chuyển `BLOCKED` và ghi nguyên nhân cụ thể.
- Finding tài liệu: chuyển `CHANGES_REQUIRED`, giao lại đúng finding cho `docs`.
- Evidence thiếu hoặc không khớp diff: không được approve.
- Khi workflow mới được nạp, TASK-002 giữ nguyên approval hiện tại và được phân loại lại từ allowlist dù status cũ đang trỏ tới `action` hoặc full code review. Action result, code-review report và finding cũ được giữ làm lịch sử nhưng không được giao lại cho `action` hay tính vào vòng docs-review; task resume từ `docs` theo acceptance criteria đã duyệt.

## 9. Xác minh triển khai

Triển khai chỉ đạt khi:

- OpenCode resolve được agent `docs-review` và quyền gọi từ `workflow`;
- `docs` resolve quyền ghi `.ai/results/*-DOCS.md`;
- `action` vẫn deny tài liệu và không còn ngoại lệ TASK-002;
- workflow mô tả rõ hai nhánh `DOCS_ONLY` và code;
- config/diff không có lỗi cú pháp hoặc whitespace;
- các thay đổi TASK-001/TASK-002 đang tồn tại không bị sửa hoặc xóa.

## 10. Ngoài phạm vi

- Không thay model/provider trong 9Router.
- Không thay quy trình review code.
- Không tự động hoàn tất nội dung TASK-002 trong thay đổi workflow này.
- Không mở quyền ghi tài liệu cho `action` trên toàn repository.
