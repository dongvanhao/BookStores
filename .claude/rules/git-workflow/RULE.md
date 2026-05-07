# Git Workflow — BookStore

## Quy tắc bắt buộc

### KHÔNG tự ý commit hoặc push code
- **Không bao giờ** chạy `git commit` mà không có yêu cầu rõ ràng từ user
- **Không bao giờ** chạy `git push` mà không có yêu cầu rõ ràng từ user
- **Không bao giờ** tạo pull request mà không có yêu cầu rõ ràng từ user

Khi hoàn thành một task (viết code, sửa bug, refactor...), agent **dừng lại** tại đó.  
Chỉ commit/push khi user nói rõ: *"commit đi"*, *"push lên"*, *"tạo PR"*, hoặc tương đương.

### Khi được yêu cầu commit
Thực hiện đúng thứ tự:
1. `git status` — kiểm tra file thay đổi
2. `git diff` — xem nội dung thay đổi
3. Stage đúng file (không dùng `git add -A` bừa bãi)
4. Commit message theo format:
   ```
   <type>(<scope>): <mô tả ngắn>
   ```
   Các type hợp lệ: `feat`, `fix`, `refactor`, `test`, `docs`, `chore`

### Khi được yêu cầu push
- Xác nhận branch hiện tại trước khi push
- **Không dùng** `--force` / `--force-with-lease` trừ khi user yêu cầu tường minh
- **Không push trực tiếp lên `main` / `master`** — cảnh báo user nếu họ yêu cầu

### Thao tác nguy hiểm — luôn hỏi trước
| Lệnh | Mức độ | Hành động |
|------|--------|-----------|
| `git push --force` | Nguy hiểm cao | Từ chối, giải thích lý do |
| `git reset --hard` | Nguy hiểm cao | Hỏi xác nhận, mô tả hậu quả |
| `git clean -f` | Nguy hiểm cao | Hỏi xác nhận |
| `git rebase` (có conflict) | Trung bình | Thông báo trước khi tiếp tục |
| `git checkout --` | Trung bình | Hỏi xác nhận |

## Tóm tắt
> Agent chỉ đọc, viết, và chạy test. Mọi thao tác git ảnh hưởng đến lịch sử hoặc remote **phải có lệnh tường minh từ user**.
