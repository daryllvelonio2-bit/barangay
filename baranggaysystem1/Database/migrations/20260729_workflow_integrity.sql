-- Workflow integrity upgrade
-- Retains voided payments as append-only audit records instead of deleting ledger rows.

CREATE TABLE IF NOT EXISTS payment_void (
    void_id INT NOT NULL AUTO_INCREMENT,
    barangay_id INT NOT NULL DEFAULT 1,
    payment_source VARCHAR(20) NOT NULL,
    payment_id INT NOT NULL,
    void_reason TEXT NOT NULL,
    voided_by_user_id INT NULL,
    voided_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (void_id),
    UNIQUE KEY ux_payment_void_source (barangay_id, payment_source, payment_id),
    KEY idx_payment_void_date (voided_at)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
