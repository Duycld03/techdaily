# Spec Delta: library

## ADDED Requirements

### Requirement: Document Ingestion Dialog Layout & Sticky Action Architecture
The document import and ingestion dialog on `/library` SHALL utilize the standardized 3-tier modal shell (`AppModal.vue`):
1. **Pinned Modal Header**: Title and close button SHALL remain pinned at the top (`shrink-0`).
2. **Scrollable Form Body**: Markdown form fields, PDF dropzone upload areas, and Web URL crawler options SHALL reside in an independently scrollable body container capped at `max-h-[60vh]`.
3. **Sticky Action Footer**: The "Cancel" (`library.cancel`) and primary action buttons ("Save Document", "Upload & Process PDF", "Crawl URL") SHALL remain permanently pinned at the bottom of the modal (`shrink-0`) and visible on all viewports without scrolling.

#### Scenario: User opens PDF import tab on 1080p desktop
- **WHEN** user opens the "Nhập Tài Liệu Mới" modal on `/library` and selects the "Tệp PDF" tab
- **THEN** the modal fits comfortably within `max-h-[85vh]`
- **AND** the Dropzone area and inputs scroll inside the body container if vertical space is constrained
- **AND** the "Hủy" and "Tải Lên & Xử Lý PDF" buttons are immediately visible in the sticky bottom footer.
